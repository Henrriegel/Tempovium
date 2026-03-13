namespace Tempovium.Core.Interfaces;

public interface IFileHashService
{
    Task<string> ComputeHashAsync(string filePath);
}