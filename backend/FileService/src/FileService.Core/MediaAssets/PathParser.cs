namespace FileService.Core.MediaAssets;

public static class PathParser
{
    public static (string location, string? prefix, string key) ParsePath(string path)
    {
        string[] parts = Uri.UnescapeDataString(path).Split('/', StringSplitOptions.RemoveEmptyEntries);

        string location = parts[0];

        string key = parts[^1];

        string? prefix = null;

        if (parts.Length > 2)
        {
            prefix = string.Join("/", parts.Skip(1).Take(parts.Length - 2));
        }

        return (location, prefix, key);
    }

    public static bool BeValidPathStructure(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        string[] parts = Uri.UnescapeDataString(path).Split('/', StringSplitOptions.RemoveEmptyEntries);

        return parts.Length >= 2 && parts.All(part => !string.IsNullOrWhiteSpace(part));
    }
}