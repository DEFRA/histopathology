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

        // Build lookup dictionaries from block details and animal data
        var blockRefMap       = blockDetails.ToDictionary(b => (int)b.ID,   b => (string?)b.BlockRef);
        var blockAnimalMap    = blockDetails.ToDictionary(b => (int)b.ID,   b => (int?)b.AnimalID);
        var blockStatusMap    = blockDetails.ToDictionary(b => (int)b.ID,   b => (int?)b.Status);
        var animalHistoRefMap = animals.ToDictionary(a => (int)a.ID, a => (string?)a.HistologyRef);

        var results = new List<BlockTest>();

        BlockTest Map(dynamic row, string testType)
        {
            int blockId       = (int)row.BlockID;
            int? animalId     = blockAnimalMap.GetValueOrDefault(blockId);
            int? blockStatus  = blockStatusMap.GetValueOrDefault(blockId);
            string? histoRef  = animalId.HasValue ? animalHistoRefMap.GetValueOrDefault(animalId.Value) : null;
            bool onHold       = blockStatus == 2;
            bool archived     = row.ArchiveLocation is not null && row.ArchivedDate is not null;

            return new BlockTest
            {
                ID              = (int)row.ID,
                BlockID         = blockId,
                BlockRef        = blockRefMap.GetValueOrDefault(blockId) ?? string.Empty,
                HistologyRef    = histoRef,
                TestType        = testType,
                Code            = (string?)row.Code ?? string.Empty,
                TestDetails     = null,  // not available from legacy block SPs; page falls back to Code
                Result          = (string?)row.Result,
                QCCode          = (string?)row.QCCode,
                QCNote          = row.QCNote is bool b ? b : row.QCNote is not null && (int)row.QCNote != 0,
                QCNoteRef       = (int?)row.QCNoteRef,
                StainRef        = (string?)row.StainRef,
                Dispatched      = row.Dispatched is bool d ? d : row.Dispatched is not null && (int)row.Dispatched != 0,
                DispatchedDate  = (DateTime?)row.DispatchedDate,
                DispatchedBy    = (string?)row.DispatchedBy,
                DispatchedTo    = (string?)row.DispatchedTo,
                Comment         = (string?)row.Comment,
                RemedialAction  = (string?)row.RemedialAction,
                ArchiveLocation = (string?)row.ArchiveLocation,
                ArchivedDate    = (DateTime?)row.ArchivedDate,
                ArchiveComment  = (string?)row.ArchiveComment,
                NumberOfSlides  = (int?)row.NumberOfSlides,
                OnHold          = onHold,
                Archived        = archived,
                RowStamp        = (byte[]?)row.RowStamp,
            };
        }

        foreach (var row in histology)  results.Add(Map(row, BlockTestType.Histology));
        foreach (var row in antibodies) results.Add(Map(row, BlockTestType.Antibodies));
        foreach (var row in stains)     results.Add(Map(row, BlockTestType.Stain));

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
        parameters.Add("Result",          test.Result);
        parameters.Add("QCCode",          test.QCCode);
        parameters.Add("QCNoteRef",       test.QCNoteRef);
        parameters.Add("QCNote",          test.QCNote);
        parameters.Add("StainRef",        test.StainRef);
        parameters.Add("Dispatched",      test.Dispatched);
        parameters.Add("DispatchedDate",  test.DispatchedDate);
        parameters.Add("DispatchedBy",    test.DispatchedBy);
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
}
