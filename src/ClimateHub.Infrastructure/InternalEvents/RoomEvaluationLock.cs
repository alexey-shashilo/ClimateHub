using ClimateHub.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Infrastructure.InternalEvents;

public class RoomEvaluationLock
{
    private readonly InternalEventsDbContext _ctx;

    public RoomEvaluationLock(InternalEventsDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<bool> TryAcquireAsync(RoomId roomId, CancellationToken ct = default)
    {
        var key = GetStableHash(roomId);
        using var cmd = _ctx.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = $"SELECT pg_try_advisory_xact_lock({key})";
        cmd.CommandType = System.Data.CommandType.Text;

        if (cmd.Connection!.State != System.Data.ConnectionState.Open)
            await cmd.Connection.OpenAsync(ct);

        var result = await cmd.ExecuteScalarAsync(ct);
        return result is bool b && b;
    }

    private static long GetStableHash(RoomId roomId)
    {
        var guid = roomId.Value;
        var bytes = guid.ToByteArray();
        return BitConverter.ToInt64(bytes, 0) ^ BitConverter.ToInt64(bytes, 8);
    }
}
