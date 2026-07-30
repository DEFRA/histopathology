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
            "GetBlocksByBatchID",
            new { ID = batchId },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Block>> GetPreBookedByAnimalAsync(int animalId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<Block>(
            "GetPreBookedBlocksByAnimalID",
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
            // Insert
            var parameters = new DynamicParameters();
            parameters.Add("RETURN_VALUE", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.ReturnValue);
            parameters.Add("BatchID",     block.BatchID);
            parameters.Add("AnimalID",    block.AnimalID);
            parameters.Add("BlockRef",    block.BlockRef);
            parameters.Add("CustomerRef", block.CustomerRef);
            parameters.Add("Comment",     block.Comment);
            parameters.Add("RepeatBlock", block.RepeatBlock);
            parameters.Add("Status",      block.Status);
            parameters.Add("Order",       block.Order);
            parameters.Add("UserID",      userId);

            await conn.ExecuteAsync("AddBlock", parameters,
                commandType: System.Data.CommandType.StoredProcedure);
            return parameters.Get<int>("RETURN_VALUE");
        }
        else
        {
            // Update
            await conn.ExecuteAsync(
                "EditBlock",
                new
                {
                    block.ID,
                    block.BatchID,
                    block.AnimalID,
                    block.BlockRef,
                    block.CustomerRef,
                    block.Comment,
                    block.RepeatBlock,
                    block.Status,
                    block.Order,
                    block.RowStamp,
                    UserID = userId,
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
    public async Task BookRefAsync(int blockId, string blockRef, int userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "BookBlockRef",
            new { ID = blockId, BlockRef = blockRef, UserID = userId },
            commandType: System.Data.CommandType.StoredProcedure);
    }
}
