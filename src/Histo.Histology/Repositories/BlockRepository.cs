using Dapper;
using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Infrastructure;
using System.Data;

namespace Histo.Histology.Repositories;

/// <summary>
/// Dapper implementation of <see cref="IBlockRepository"/>.
/// </summary>
public sealed class BlockRepository : IBlockRepository
{
    private readonly IDbConnectionFactory _db;

    private const string NewIdParam = "NewID";

    public BlockRepository(IDbConnectionFactory db) => _db = db;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Block>> GetByBatchAsync(int batchId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<Block>(
            "GetBatchBlockDetails",
            new { ID = batchId },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetBlockRefsOnOtherBatchesAsync(int animalId, int excludeBatchId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<string>(
            "GetAnimalBlockRefsOnOtherBatches",
            new { AnimalID = animalId, BatchID = excludeBatchId },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Block>> GetPreBookedByAnimalAsync(int animalId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<Block>(
            "GetAnimalPreBookedBlocks",
            new { AnimalID = animalId },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<int> SaveAsync(Block block, int userId, bool keepRefUsedOnOtherSubmissions = false, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();

        // Auto-allocated refs for new ordinary blocks are kept clear of other submissions' refs under the
        // sender-ref lock. Edits and deliberately re-used refs are not: legacy only checks the current submission.
        var guarded = block.ID == 0 && !keepRefUsedOnOtherSubmissions
            && block.Status == BlockStatus.Used && block.BatchID > 0 && !block.RepeatBlock;
        if (guarded)
        {
            await conn.OpenAsync(ct);
            using var tx = await conn.BeginTransactionAsync(ct);
            try
            {
                var senderRef = await FindSenderRefAsync(conn, tx, block.AnimalID, block.BatchID);
                if (!string.IsNullOrWhiteSpace(senderRef))
                    await AcquireSenderRefLockAsync(conn, tx, senderRef);
                var blockRef = await ResolveFreeRefAsync(conn, tx, block, senderRef);
                var id = await InsertBlockAsync(conn, tx, block, blockRef);
                await tx.CommitAsync(ct);
                return id;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        if (block.ID == 0)
            return await InsertBlockAsync(conn, null, block, block.BlockRef);

        await UpdateBlockRowAsync(conn, null, block, userId);
        return block.ID;
    }

    /// <summary>
    /// Sender ref of an animal on a batch, from the legacy batch SPs: <c>GetBatchAnimal</c> (animals with a submission row),
    /// then <c>GetBatchBlocksByID</c> result set 5 (BATCH_BLOCK_ANIMAL) for legacy animals that only have blocks.
    /// </summary>
    private static async Task<string?> FindSenderRefAsync(IDbConnection conn, IDbTransaction tx, int animalId, int batchId)
    {
        var animals = await conn.QueryAsync<AnimalSenderRow>(
            "GetBatchAnimal", new { ID = batchId }, tx, commandType: System.Data.CommandType.StoredProcedure);
        var found = animals.Where(a => a.ID == animalId).Select(a => a.SenderRef).FirstOrDefault();
        if (found is not null) return found.Trim();

        using var multi = await conn.QueryMultipleAsync(
            "GetBatchBlocksByID", new { ID = batchId }, tx, commandType: System.Data.CommandType.StoredProcedure);
        const int blockAnimalResultSetIndex = 5;
        for (var i = 0; i < blockAnimalResultSetIndex; i++)
            await multi.ReadAsync<dynamic>();
        var blockAnimals = await multi.ReadAsync<AnimalSenderRow>();
        return blockAnimals.Where(a => a.ID == animalId).Select(a => a.SenderRef?.Trim()).FirstOrDefault();
    }

    private sealed class AnimalSenderRow
    {
        public int ID { get; init; }
        public string? SenderRef { get; init; }
    }

    /// <summary>Refs held by the animal on other submissions, including unclaimed pre-booked placeholders.</summary>
    private static async Task<List<string>> OtherBatchRefsAsync(IDbConnection conn, IDbTransaction tx, int animalId, int batchId) =>
        (await conn.QueryAsync<string>(
            "GetAnimalBlockRefsOnOtherBatches",
            new { AnimalID = animalId, BatchID = batchId }, tx,
            commandType: System.Data.CommandType.StoredProcedure)).ToList();

    /// <summary>Returns the requested ref, or the next free one in sequence if another submission took it first.</summary>
    private static async Task<string> ResolveFreeRefAsync(IDbConnection conn, IDbTransaction tx, Block block, string? senderRef)
    {
        var other = await OtherBatchRefsAsync(conn, tx, block.AnimalID, block.BatchID);
        if (!other.Contains(block.BlockRef.Trim(), StringComparer.OrdinalIgnoreCase))
            return block.BlockRef;

        // Legacy GetBlocksForSenderRef returns refs as integers; refs are stored zero-padded to two digits.
        var held = string.IsNullOrWhiteSpace(senderRef)
            ? []
            : await conn.QueryAsync<int>(
                "GetBlocksForSenderRef", new { SenderRef = senderRef }, tx, commandType: System.Data.CommandType.StoredProcedure);
        var all = held.Select(n => n.ToString("00")).ToList();
        return BlockHelpers.ComputeNextBlockRef(all, null, block.BlockRef);
    }

    // Real AddBlock signature (confirmed against the database directly) has no @UserID and
    // returns the new ID via @NewID output, not a RETURN value.
    private static async Task<int> InsertBlockAsync(IDbConnection conn, IDbTransaction? tx, Block block, string blockRef)
    {
        var parameters = new DynamicParameters();
        parameters.Add("ID", 0);
        parameters.Add("BatchID",     block.BatchID);
        parameters.Add("AnimalID",    block.AnimalID);
        parameters.Add("BlockRef",    blockRef);
        parameters.Add("CustomerRef", block.CustomerRef);
        parameters.Add("RepeatBlock", block.RepeatBlock);
        parameters.Add("Comment",     block.Comment);
        parameters.Add("Status",      block.Status);
        parameters.Add("Order",       block.Order);
        parameters.Add("OldID", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);
        parameters.Add(NewIdParam, dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

        await conn.ExecuteAsync("AddBlock", parameters, tx, commandType: System.Data.CommandType.StoredProcedure);
        return parameters.Get<int>(NewIdParam);
    }

    // Real EditBlock signature (confirmed against the database directly) has no @RowStamp
    // (full-row replace, no optimistic concurrency) and requires the 3 archive columns on every call.
    private static Task<int> UpdateBlockRowAsync(IDbConnection conn, IDbTransaction? tx, Block block, int userId) =>
        conn.ExecuteAsync(
            "EditBlock",
            new
            {
                block.ID,
                block.BatchID,
                block.AnimalID,
                block.BlockRef,
                block.CustomerRef,
                block.RepeatBlock,
                ArchiveLocation = (object?)block.ArchiveLocation ?? DBNull.Value,
                ArchivedDate = (object?)block.ArchivedDate ?? DBNull.Value,
                ArchiveComment = (object?)block.ArchiveComment ?? DBNull.Value,
                block.Comment,
                block.Status,
                UserID = userId,
                block.Order,
            },
            tx,
            commandType: System.Data.CommandType.StoredProcedure);

    /// <inheritdoc/>
    public async Task DeleteAsync(int blockId, int userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "DeleteBlock",
            new { ID = blockId }, // DeleteBlock has no @UserID parameter â€” confirmed via sys.parameters.
            commandType: System.Data.CommandType.StoredProcedure);
    }

    /// <inheritdoc/>
    public async Task CreatePreBookedBlockAsync(int animalId, string blockRef, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("AddBlock", PreBookedBlockParameters(animalId, blockRef), commandType: System.Data.CommandType.StoredProcedure);
    }

    private static DynamicParameters PreBookedBlockParameters(int animalId, string blockRef)
    {
        var parameters = new DynamicParameters();
        parameters.Add("ID", 0);
        parameters.Add("BatchID", dbType: System.Data.DbType.Int32, value: DBNull.Value);
        parameters.Add("AnimalID", animalId);
        parameters.Add("BlockRef", blockRef);
        parameters.Add("CustomerRef", " ");
        parameters.Add("RepeatBlock", false);
        parameters.Add("Comment", " ");
        parameters.Add("Status", BlockStatus.PreBooked);
        parameters.Add("Order", dbType: System.Data.DbType.Int32, value: DBNull.Value);
        parameters.Add("OldID", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);
        parameters.Add(NewIdParam, dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);
        return parameters;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PreBookedBlockResult>> BookPreBookedBlocksAsync(
        string senderRef, IReadOnlyList<int> blockRefs, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            await AcquireSenderRefLockAsync(conn, tx, senderRef);

            var takenRefs = (await conn.QueryAsync<UsedBlockRef>(
                    "GetBlocksForSenderRef",
                    new { SenderRef = senderRef },
                    tx,
                    commandType: System.Data.CommandType.StoredProcedure))
                .Select(b => b.BlockRef)
                .ToHashSet();

            var results = new List<PreBookedBlockResult>(blockRefs.Count);
            var animalId = 0;

            foreach (var blockRefNum in blockRefs)
            {
                if (takenRefs.Contains(blockRefNum))
                {
                    results.Add(new PreBookedBlockResult(blockRefNum, PreBookedBlockOutcome.AlreadyExists));
                    continue;
                }

                // Deferred so a fully-duplicate range creates no Animal row.
                if (animalId == 0) animalId = await ResolveAnimalIdAsync(conn, tx, senderRef);
                if (animalId == 0)
                {
                    results.Add(new PreBookedBlockResult(blockRefNum, PreBookedBlockOutcome.NoSample));
                    continue;
                }

                await InsertPreBookedBlockAsync(conn, tx, animalId, SenderRefHelpers.FormatBlockRef(blockRefNum));
                takenRefs.Add(blockRefNum);
                results.Add(new PreBookedBlockResult(blockRefNum, PreBookedBlockOutcome.Booked));
            }

            await tx.CommitAsync(ct);
            return results;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    /// <summary>
    /// Transaction-scoped exclusive lock on the sender ref. There is no unique index on
    /// (AnimalID, BlockRef), so this is what stops two concurrent bookings both seeing a ref as free.
    /// </summary>
    private static async Task AcquireSenderRefLockAsync(IDbConnection conn, IDbTransaction tx, string senderRef)
    {
        var parameters = new DynamicParameters();
        parameters.Add("RETURN_VALUE", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.ReturnValue);
        parameters.Add("Resource", $"BookBlockRef:{senderRef}");
        parameters.Add("LockMode", "Exclusive");
        parameters.Add("LockOwner", "Transaction");
        parameters.Add("LockTimeout", 15000);

        await conn.ExecuteAsync("sp_getapplock", parameters, tx, commandType: System.Data.CommandType.StoredProcedure);

        // 0/1 granted; negative values are timeout (-1), deadlock (-2), cancelled (-3) or error (-999).
        var result = parameters.Get<int?>("RETURN_VALUE") ?? -999;
        if (result < 0)
            throw new InvalidOperationException($"Could not acquire the booking lock for sender ref '{senderRef}' (sp_getapplock returned {result}).");
    }

    /// <summary>Returns the first existing Animal row for the sender ref, creating one when none exists.</summary>
    private static async Task<int> ResolveAnimalIdAsync(IDbConnection conn, IDbTransaction tx, string senderRef)
    {
        var rows = await conn.QueryAsync<dynamic>(
            "GetAnimalBySender", new { SenderRef = senderRef }, tx, commandType: System.Data.CommandType.StoredProcedure);

        foreach (var row in rows)
        {
            var columns = new Dictionary<string, object?>(
                ((IDictionary<string, object>)row).ToDictionary(p => p.Key, p => (object?)p.Value),
                StringComparer.OrdinalIgnoreCase);
            if (columns.TryGetValue("ID", out var id) && id is not null)
                return Convert.ToInt32(id);
        }

        // AddAnimal's insert signature â€” matches SubmissionRepository.AddAnimalAsync.
        var parameters = new DynamicParameters();
        parameters.Add("SenderRef", senderRef);
        parameters.Add("HistologyRef", null, dbType: System.Data.DbType.String);
        parameters.Add("NextBlockRef", "01");
        parameters.Add("PMDate", DBNull.Value, dbType: System.Data.DbType.String);
        parameters.Add("OnHold", false);
        parameters.Add(NewIdParam, dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

        await conn.ExecuteAsync("AddAnimal", parameters, tx, commandType: System.Data.CommandType.StoredProcedure);
        return parameters.Get<int?>(NewIdParam) ?? 0;
    }

    /// <summary>Inserts one pre-booked placeholder inside the booking transaction.</summary>
    private static async Task InsertPreBookedBlockAsync(IDbConnection conn, IDbTransaction tx, int animalId, string blockRef)
    {
        await conn.ExecuteAsync("AddBlock", PreBookedBlockParameters(animalId, blockRef), tx, commandType: System.Data.CommandType.StoredProcedure);
    }

    // -----------------------------------------------------------------------
    // Search (read-only)
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IReadOnlyList<UsedBlockRef>> GetUsedBlockRefsByHistologyRefAsync(string histologyRef, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<UsedBlockRef>(
            "GetBlocksForHistoRef",
            new { HistologyRef = histologyRef },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<UsedBlockRef>> GetUsedBlockRefsBySenderRefAsync(string senderRef, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<UsedBlockRef>(
            "GetBlocksForSenderRef",
            new { SenderRef = senderRef },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BlockArchiveInfo>> GetBlockArchiveAsync(
        string? senderRef, string? histologyRef, string? blockRef, string? archiveLocation, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<BlockArchiveInfo>(
            "GetAnimalBlockArchiveInformation",
            new { SenderRef = senderRef, HistologyRef = histologyRef, BlockRef = blockRef, ArchiveLocation = archiveLocation },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SlideArchiveInfo>> GetSlideArchiveAsync(
        string? senderRef, string? histologyRef, string? archiveLocation, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var results = new List<SlideArchiveInfo>();

        var stainRows = await conn.QueryAsync<SlideArchiveInfo>(
            "GetAnimalStainArchiveInformation",
            new { SenderRef = senderRef, HistologyRef = histologyRef, ArchiveLocation = archiveLocation },
            commandType: System.Data.CommandType.StoredProcedure);
        results.AddRange(stainRows);

        // Legacy (clsAnimal.GetAnimalSlideArchiveInformation) also merges in Antibodies archive
        // data per batch/submission-type, and Histology archive data (excluding the descriptions
        // already covered by the Stain/Antibodies calls above) â€” Special Stain rows alone are only
        // a subset of everything archived against a submission.
        var batches = await conn.QueryAsync<AnimalBatch>(
            "GetAnimalBatches",
            new { SenderRef = senderRef, HistologyRef = histologyRef },
            commandType: System.Data.CommandType.StoredProcedure);

        foreach (var batch in batches)
        {
            var antibodyRows = await conn.QueryAsync<SlideArchiveInfo>(
                "GetAnimalAntibodiesArchiveInformation",
                new
                {
                    SenderRef = senderRef,
                    HistologyRef = histologyRef,
                    ArchiveLocation = archiveLocation,
                    batch.BatchID,
                    batch.SubmissionType,
                },
                commandType: System.Data.CommandType.StoredProcedure);
            results.AddRange(antibodyRows);
        }

        var histologyRows = await conn.QueryAsync<SlideArchiveInfo>(
            "GetAnimalHistologyArchiveInformation",
            new { SenderRef = senderRef, HistologyRef = histologyRef, ArchiveLocation = archiveLocation },
            commandType: System.Data.CommandType.StoredProcedure);
        results.AddRange(histologyRows.Where(r =>
            r.Description != "Special Stain" && r.Description != "IHC - PrP" && r.Description != "IHC - Other"));

        return results.OrderBy(r => r.BlockRef, StringComparer.Ordinal).ToList();
    }

    /// <summary>Row shape of <c>GetAnimalBatches</c> â€” the batch/submission-type list a slide archive search fans out over.</summary>
    private sealed record AnimalBatch(int BatchID, int SubmissionType);
}
