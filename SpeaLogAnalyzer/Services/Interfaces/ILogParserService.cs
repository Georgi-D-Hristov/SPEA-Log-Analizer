using SpeaLogAnalyzer.Models;

namespace SpeaLogAnalyzer.Services.Interfaces;

public interface ILogParserService
{
    Task<TestSession> ParseFileAsync(string filePath, CancellationToken ct = default);
    Task<List<TestSession>> ParseFolderAsync(string folderPath, IProgress<int>? progress = null, CancellationToken ct = default);
}
