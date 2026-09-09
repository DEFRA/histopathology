using Dapper;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Infrastructure;

namespace Histo.Histology.Repositories;

/// <summary>
/// Dapper implementation of <see cref="IBlockTestRepository"/>.
///
/// <see cref="GetByBatchAsync"/> uses the legacy <c>GetBatchBlocksByID</c> SP which
/// returns 10 result sets in one roundtrip (same SP the legacy app used to populate
/// the block-level DataSet in session). Result sets read:
///   0 = GetBatchBlockDetails  → BlockID, BlockRef, AnimalID, Status
///   1 = GetBatchBlockTissues  → (skipped)
///   2 = GetBatchBlockHistology → full histology test rows
///   3 = GetBatchBlockAntibodies → full antibodies test rows
///   4 = GetBatchBlockStain → full stain test rows
///   5 = GetBatchBlockAnimal → AnimalID, HistologyRef
///   6–9 = refs/TC codes → (skipped)
///
/// Update is dispatched to the insert/update/delete stored-procedure family that
/// matches <see cref="BlockTest.TestType"/>, mirroring the legacy
/// <c>clsCheckBoxData.UpdateBlockTablesDetails</c> table-ID switch.
/// </summary>
public sealed class BlockTestRepository : IBlockTestRepository
{
    private readonly IDbConnectionFactory _db;

    public BlockTestRepository(IDbConnectionFactory db) => _db = db;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BlockTest>> GetByBatchAsync(int batchId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        using var multi = await conn.QueryMultipleAsync(
            "GetBatchBlocksByID",
            new { ID = batchId },
            commandType: System.Data.CommandType.StoredProcedure);

        // Result set 0: block details — BlockID→BlockRef, AnimalID, Status
        var blockDetails = (await multi.ReadAsync<dynamic>()).ToList();
        // Result set 1: tissues — skip
        await multi.ReadAsync<dynamic>();
        // Result set 2: histology test rows
        var histology = (await multi.ReadAsync<dynamic>()).ToList();
        // Result set 3: antibody test rows
        var antibodies = (await multi.ReadAsync<dynamic>()).ToList();
        // Result set 4: stain test rows
        var stains = (await multi.ReadAsync<dynamic>()).ToList();
        // Result set 5: animals — AnimalID→HistologyRef
        var animals = (await multi.ReadAsync<dynamic>()).ToList();
        // Result set 6: histology refs — skip
        await multi.ReadAsync<dynamic>();
        // Result set 7: stain TC codes (BLOCK_SPECIALSTAIN_TCCODES)
        var stainTcRows = (await multi.ReadAsync<dynamic>()).ToList();
        // Result set 8: antibodies TC codes (BLOCK_ANTIBODIES_TCCODES)
        var antibodiesTcRows = (await multi.ReadAsync<dynamic>()).ToList();
        // Result set 9: histology TC codes (BLOCK_HISTOLOGY_TCCODES)
        var histologyTcRows = (await multi.ReadAsync<dynamic>()).ToList();

        // Build TC code lookups keyed by TestID for each test type
        static Dictionary<int, List<TcCode>> BuildTcLookup(List<dynamic> rows) =>
            rows.GroupBy(r => (int)r.TestID)
                .ToDictionary(g => g.Key, g => g.Select(r => new TcCode((int)r.ID, (string)r.Code)).ToList());

        var histologyTcByTestId  = BuildTcLookup(histologyTcRows);
        var antibodiesTcByTestId = BuildTcLookup(antibodiesTcRows);
        var stainTcByTestId      = BuildTcLookup(stainTcRows);

        // Build lookup dictionaries from block details and animal data
        var blockRefMap       = blockDetails.ToDictionary(b => (int)b.ID,   b => (string?)b.BlockRef);
        var blockAnimalMap    = blockDetails.ToDictionary(b => (int)b.ID,   b => (int?)b.AnimalID);
        var blockStatusMap    = blockDetails.ToDictionary(b => (int)b.ID,   b => (int?)b.Status);
        var animalHistoRefMap = animals.ToDictionary(a => (int)a.ID, a => (string?)a.HistologyRef);

        var results = new List<BlockTest>();

        // Dapper's dynamic rows return DBNull.Value (not C# null) for NULL columns — a raw
        // `dynamic` cast like `(int)row.QCNoteRef` or a DBNull `is not null` check (true for
        // DBNull, since it's a real object) throws, which BlockTestService's catch-all then
        // silently swallows, wiping every indicator column for the WHOLE batch. QCNote/Dispatched
        // are NULL on any not-yet-QC'd/not-yet-dispatched test — a very common state — so this hit
        // real submissions. Read every nullable column via IDictionary<string, object> instead,
        // matching the DBNull-safe convention used elsewhere in this codebase (BatchRepository etc).
        static string? Str(IDictionary<string, object> d, string key) =>
            d.TryGetValue(key, out var v) && v is not DBNull && v is not null ? Convert.ToString(v) : null;
        static int? IntOrNull(IDictionary<string, object> d, string key) =>
            d.TryGetValue(key, out var v) && v is not DBNull && v is not null ? Convert.ToInt32(v) : null;
        static DateTime? DateOrNull(IDictionary<string, object> d, string key) =>
            d.TryGetValue(key, out var v) && v is not DBNull && v is not null ? Convert.ToDateTime(v) : null;
        static bool Bool(IDictionary<string, object> d, string key)
        {
            if (!d.TryGetValue(key, out var v) || v is DBNull || v is null) return false;
            return v is bool b ? b : Convert.ToInt64(v) != 0;
        }
        static byte[]? Bytes(IDictionary<string, object> d, string key) =>
            d.TryGetValue(key, out var v) && v is not DBNull && v is not null ? (byte[])v : null;

        BlockTest Map(dynamic dynamicRow, string testType)
        {
            var row           = (IDictionary<string, object>)dynamicRow;
            int blockId       = Convert.ToInt32(row["BlockID"]);
            int? animalId     = blockAnimalMap.GetValueOrDefault(blockId);
            int? blockStatus  = blockStatusMap.GetValueOrDefault(blockId);
            string? histoRef  = animalId.HasValue ? animalHistoRefMap.GetValueOrDefault(animalId.Value) : null;
            bool onHold       = blockStatus == 2;
            var archiveLocation = Str(row, "ArchiveLocation");
            var archivedDate    = DateOrNull(row, "ArchivedDate");
            bool archived     = archiveLocation is not null && archivedDate is not null;
            int id            = Convert.ToInt32(row["ID"]);

            var tcLookup = testType switch
            {
                BlockTestType.Histology  => histologyTcByTestId,
                BlockTestType.Antibodies => antibodiesTcByTestId,
                BlockTestType.Stain      => stainTcByTestId,
                _ => new Dictionary<int, List<TcCode>>()
            };

            return new BlockTest
            {
                ID              = id,
                BlockID         = blockId,
                BlockRef        = blockRefMap.GetValueOrDefault(blockId) ?? string.Empty,
                HistologyRef    = histoRef,
                TestType        = testType,
                Code            = Str(row, "Code") ?? string.Empty,
                TestDetails     = null,  // not available from legacy block SPs; page falls back to Code
                Result          = Str(row, "Result"),
                QCCode          = Str(row, "QCCode"),
                QCNote          = Bool(row, "QCNote"),
                QCNoteRef       = IntOrNull(row, "QCNoteRef"),
                StainRef        = Str(row, "StainRef"),
                Dispatched      = Bool(row, "Dispatched"),
                DispatchedDate  = DateOrNull(row, "DispatchedDate"),
                DispatchedBy    = Str(row, "DispatchedBy"),
                EnteredBy       = IntOrNull(row, "EnteredBy"),
                PremiumCharge   = Str(row, "PremiumCharge"),
                DispatchedTo    = Str(row, "DispatchedTo"),
                Comment         = Str(row, "Comment"),
                RemedialAction  = Str(row, "RemedialAction"),
                ArchiveLocation = archiveLocation,
                ArchivedDate    = archivedDate,
                ArchiveComment  = Str(row, "ArchiveComment"),
                NumberOfSlides  = IntOrNull(row, "NumberOfSlides"),
                OnHold          = onHold,
                Archived        = archived,
                RowStamp        = Bytes(row, "RowStamp"),
                TCCodes         = tcLookup.TryGetValue(id, out var codes) ? codes : [],
            };
        }

        // BLOCK_HISTOLOGY codes 3 (Special Stain), 4 (IHC-PrP), 6 (IHC-Other) are gating/indicator
        // flags only — the real per-test worklist rows for those categories live in the separate
        // BLOCK_ANTIBODIES/BLOCK_STAIN child tables (added below), which already have their own
        // rows. Legacy's clsBatchSummary.vb::CreateTestSummaryData explicitly excludes these 3
        // codes (`Case Else: 'Do nothing`) — only 1 (EO), 2 (H&E), 5 (H&E-BSE), 7 (Archive) get a
        // worklist row from this table. Including 3/4/6 here double-counts a test already
        // represented by its own Antibodies/Stain row (confirmed live: batch 29399/block 317172
        // had a BLOCK_HISTOLOGY Code=3 row plus 3 real BLOCK_STAIN rows — legacy shows 3 rows,
        // not 4).
        var histologyWorklistCodes = new HashSet<string> { "1", "2", "5", "7" };
        var histologyFiltered = histology.Where(row => histologyWorklistCodes.Contains(Str((IDictionary<string, object>)row, "Code") ?? string.Empty));

        foreach (var row in histologyFiltered) results.Add(Map(row, BlockTestType.Histology));
        foreach (var row in antibodies)         results.Add(Map(row, BlockTestType.Antibodies));
        foreach (var row in stains)              results.Add(Map(row, BlockTestType.Stain));


        return results;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(BlockTest test, int userId, CancellationToken ct = default)
    {
        var editSp = test.TestType switch
        {
            BlockTestType.Histology  => "EditBlockHistology",
            BlockTestType.Antibodies => "EditBlockAntibodies",
            BlockTestType.Stain      => "EditBlockStain",
            _ => throw new ArgumentOutOfRangeException(nameof(test), test.TestType, "Unknown test type."),
        };

        using var conn = _db.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("RETURN_VALUE", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.ReturnValue);
        parameters.Add("ID",              test.ID);
        parameters.Add("BlockID",         test.BlockID);
        parameters.Add("Code",            test.Code);
        parameters.Add("Result",          test.Result);
        parameters.Add("QCCode",          test.QCCode);
        parameters.Add("QCNoteRef",       test.QCNoteRef);
        parameters.Add("QCNote",          test.QCNote);
        parameters.Add("StainRef",        test.StainRef);
        parameters.Add("Dispatched",      test.Dispatched);
        parameters.Add("DispatchedDate",  test.DispatchedDate);
        parameters.Add("DispatchedBy",    test.DispatchedBy);
        // Legacy always overwrites EnteredBy with the current session user on every save
        // (QualityData.aspx.vb: .Item("EnteredBy") = CInt(Session.Item(SV_HeaderUserID))).
        parameters.Add("EnteredBy",       userId.ToString());
        // Not editable via the QC screen — round-tripped unchanged.
        parameters.Add("PremiumCharge",   test.PremiumCharge);
        parameters.Add("DispatchedTo",    test.DispatchedTo);
        parameters.Add("Comment",         test.Comment);
        parameters.Add("RemedialAction",  test.RemedialAction);
        parameters.Add("ArchiveLocation", test.ArchiveLocation);
        parameters.Add("ArchivedDate",    test.ArchivedDate);
        parameters.Add("ArchiveComment",  test.ArchiveComment);
        parameters.Add("NumberOfSlides",  test.NumberOfSlides);
        parameters.Add("UserID",          userId);
        parameters.Add("RowStamp",        test.RowStamp, dbType: System.Data.DbType.Binary);

        await conn.ExecuteAsync(editSp, parameters, commandType: System.Data.CommandType.StoredProcedure);

        var returnValue = parameters.Get<int>("RETURN_VALUE");
        if (returnValue == 1)
            throw new BlockTestConcurrencyException();
    }

    /// <inheritdoc/>
    public async Task SaveTCCodesAsync(
        int batchId, int testId, string testType,
        IReadOnlyList<TcCode> existing, IReadOnlyList<string> selected,
        int userId, CancellationToken ct = default)
    {
        var (insertSp, deleteSp) = testType switch
        {
            BlockTestType.Histology  => ("AddHistologyTCCode",    "DeleteHistologyTCCode"),
            BlockTestType.Antibodies => ("AddAntibodiesTCCode",   "DeleteAntibodiesTCCode"),
            BlockTestType.Stain      => ("AddSpecialStainTCCode", "DeleteSpecialStainTCCode"),
            _ => throw new ArgumentOutOfRangeException(nameof(testType), testType, "Unknown test type.")
        };

        var toDelete = existing.Where(e => !selected.Contains(e.Code)).ToList();
        var toInsert = selected.Where(s => !existing.Any(e => e.Code == s)).ToList();

        using var conn = _db.CreateConnection();

        foreach (var tc in toDelete)
            await conn.ExecuteAsync(deleteSp, new { ID = tc.Id },
                commandType: System.Data.CommandType.StoredProcedure);

        foreach (var code in toInsert)
            await conn.ExecuteAsync(insertSp,
                new { TestID = testId, Code = code, UserID = userId, BatchID = batchId },
                commandType: System.Data.CommandType.StoredProcedure);
    }

    /// <inheritdoc/>
    public async Task SaveTestSelectionsAsync(int batchId, int blockId,
        IReadOnlyList<string> histologyCodes, IReadOnlyList<string> antibodyCodes, IReadOnlyList<string> stainCodes,
        int userId, CancellationToken ct = default)
    {
        var current = (await GetByBatchAsync(batchId, ct)).Where(t => t.BlockID == blockId).ToList();

        using var conn = _db.CreateConnection();
        await ApplyBlockTestDeltaAsync(conn, batchId, blockId, userId,
            current.Where(t => t.TestType == BlockTestType.Histology).ToList(), histologyCodes, "AddBlockHistology", "DeleteBlockHistology");
        await ApplyBlockTestDeltaAsync(conn, batchId, blockId, userId,
            current.Where(t => t.TestType == BlockTestType.Antibodies).ToList(), antibodyCodes, "AddBlockAntibodies", "DeleteBlockAntibodies");
        await ApplyBlockTestDeltaAsync(conn, batchId, blockId, userId,
            current.Where(t => t.TestType == BlockTestType.Stain).ToList(), stainCodes, "AddBlockStain", "DeleteBlockStain");
    }

    /// <summary>Mirrors BatchRepository.ApplyTestSelectionDeltaAsync, scoped to one block.</summary>
    private static async Task ApplyBlockTestDeltaAsync(
        System.Data.IDbConnection conn, int batchId, int blockId, int userId,
        IReadOnlyList<BlockTest> current, IReadOnlyList<string> newCodes, string addSp, string deleteSp)
    {
        var currentSet = current.ToDictionary(t => t.Code, t => t.ID, StringComparer.OrdinalIgnoreCase);
        var newSet = newCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in current)
        {
            if (!newSet.Contains(row.Code))
                await conn.ExecuteAsync(deleteSp, new { ID = row.ID },
                    commandType: System.Data.CommandType.StoredProcedure);
        }

        foreach (var code in newCodes.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!currentSet.ContainsKey(code))
                await conn.ExecuteAsync(addSp,
                    new { BlockID = blockId, Code = code, Comment = (string?)null, UserID = userId, BatchID = batchId },
                    commandType: System.Data.CommandType.StoredProcedure);
        }
    }
}
