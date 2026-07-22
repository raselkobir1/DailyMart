namespace DailyMart.Application.Common.Interfaces;

/// <summary>
/// Saves an uploaded file to disk and returns a relative URL for it - callers persist only that URL,
/// never the file bytes, in the database (see CLAUDE.md-adjacent design note in Module 2's progress log).
/// </summary>
public interface IFileStorageService
{
    Task<string> SaveAsync(
        Stream content, string fileName, string subFolder, CancellationToken cancellationToken = default);
}
