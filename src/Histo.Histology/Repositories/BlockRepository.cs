using Dapper;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Infrastructure;

namespace Histo.Histology.Repositories;

/// <summary>
/// Dapper implementation of <see cref="IBlockRepository"/>.
/// </summary>
public sealed class BlockRepository : IBlockRepository
{
    private readonly IDbConnectionFactory _db;

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
    public async Task<int> SaveAsync(Block block, int userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();

        if (block.ID == 0)
        {
            // Insert — real AddBlock signature (confirmed against the database directly) has no
            // @UserID and returns the new ID via @NewID output, not a RETURN value.
            var parameters = new DynamicParameters();
            parameters.Add("ID", 0);
            parameters.Add("BatchID",     block.BatchID);
            parameters.Add("AnimalID",    block.AnimalID);
            parameters.Add("BlockRef",    block.BlockRef);
            parameters.Add("CustomerRef", block.CustomerRef);
            parameters.Add("RepeatBlock", block.RepeatBlock);
            parameters.Add("Comment",     block.Comment);
            parameters.Add("Status",      block.Status);
            parameters.Add("Order",       block.Order);
            parameters.Add("OldID", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);
            parameters.Add("NewID", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

            await conn.ExecuteAsync("AddBlock", parameters,
                commandType: System.Data.CommandType.StoredProcedure);
            return parameters.Get<int>("NewID");
        }
        else
        {
            // Update — real EditBlock signature (confirmed against the database directly) has no
            // @RowStamp (full-row replace, no optimistic concurrency) and requires the 3 archive
            // columns on every call.
            await conn.ExecuteAsync(
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
                commandType: System.Data.CommandType.StoredProcedure);
            return block.ID;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(int blockId, int userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "DeleteBlock",
            new { ID = blockId, UserID = userId },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    /// <inheritdoc/>
    public async Task CreatePreBookedBlockAsync(int animalId, string blockRef, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
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
        parameters.Add("NewID", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

        await conn.ExecuteAsync("AddBlock", parameters, commandType: System.Data.CommandType.StoredProcedure);
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
        // already covered by the Stain/Antibodies calls above) — Special Stain rows alone are only
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

    /// <summary>Row shape of <c>GetAnimalBatches</c> — the batch/submission-type list a slide archive search fans out over.</summary>
    private sealed class AnimalBatch
    {
        public int BatchID { get; init; }
        public int SubmissionType { get; init; }
    }
}
