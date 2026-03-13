namespace Tempovium.Core.Services;

public class MediaFileValidator
{
    public bool FileExists(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }
        
        return File.Exists(filePath);
    }
}