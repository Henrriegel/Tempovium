using System.Security.Cryptography;
using Tempovium.Core.Interfaces;

namespace Tempovium.Infrastructure.Services;

public class FileHashService : IFileHashService
{
    public async Task<string> ComputeHashAsync(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();

        var hashBytes = await sha256.ComputeHashAsync(stream);

        return Convert.ToHexString(hashBytes);
    }
}