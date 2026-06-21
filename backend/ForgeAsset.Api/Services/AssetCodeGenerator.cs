using ForgeAsset.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ForgeAsset.Api.Services;

public sealed class AssetCodeGenerator
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<string> GenerateAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var prefix = $"FA-{DateTime.Now:yyyyMMdd}-";
            var latestCode = await db.Assets.IgnoreQueryFilters()
                .Where(x => x.AssetCode.StartsWith(prefix))
                .OrderByDescending(x => x.AssetCode)
                .Select(x => x.AssetCode)
                .FirstOrDefaultAsync(cancellationToken);

            var sequence = 1;
            if (latestCode is not null && int.TryParse(latestCode[prefix.Length..], out var current))
            {
                sequence = current + 1;
            }
            return $"{prefix}{sequence:0000}";
        }
        finally
        {
            _gate.Release();
        }
    }
}
