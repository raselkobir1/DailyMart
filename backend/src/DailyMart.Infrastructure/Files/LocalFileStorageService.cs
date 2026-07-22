using DailyMart.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace DailyMart.Infrastructure.Files;

/// <summary>
/// Saves files under wwwroot/uploads/{subFolder}. Filenames are replaced with a fresh GUID (keeping only
/// the original extension) so a malicious/crafted filename (e.g. path traversal segments) never reaches
/// the filesystem.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _webRootPath;

    public LocalFileStorageService(IWebHostEnvironment environment)
    {
        _webRootPath = string.IsNullOrEmpty(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;
    }

    public async Task<string> SaveAsync(
        Stream content, string fileName, string subFolder, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        var safeFileName = $"{Guid.NewGuid()}{extension}";

        var folderPath = Path.Combine(_webRootPath, "uploads", subFolder);
        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, safeFileName);
        await using var fileStream = File.Create(filePath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return $"/uploads/{subFolder}/{safeFileName}";
    }
}
