using Dapper;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Infrastructure;

namespace Histo.Histology.Repositories;

/// <summary>
/// Dapper implementation of <see cref="IBlockTestRepository"/>.
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
        var rows = await conn.QueryAsync<BlockTest>(
            "GetTestsByBatchID",
            new { BatchID = batchId },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
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
