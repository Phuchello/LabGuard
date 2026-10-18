using System.Text;
using System.Text.Json;

namespace LabGuard.Agent;

/// <summary>Small JSON-lines operational log; no payloads or credentials.</summary>
internal sealed class AgentLog : IDisposable
{
    private readonly TextWriter _console;
    private readonly bool _toConsole;
    private readonly int _limit;
    private readonly FileStream? _file;
    private bool _limitReported;

    public AgentLog(AgentSettings settings, string configPath, TextWriter console)
    {
        _console = console;
        _toConsole = settings.LogToConsole;
        _limit = settings.MaxLogFileBytes;
        if (settings.LogFilePath is not null)
        {
            var path = Path.GetFullPath(settings.LogFilePath, Path.GetDirectoryName(Path.GetFullPath(configPath))!);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            _file = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
        }
    }

    public void Write(string eventName, string message, object? data = null, string level = "Information")
    {
        var line = JsonSerializer.Serialize(new { timestampUtc = DateTimeOffset.UtcNow, level, eventName, message, data });
        if (_toConsole) _console.WriteLine(line);
        if (_file is null) return;
        var bytes = Encoding.UTF8.GetBytes(line + Environment.NewLine);
        if (_file.Length + bytes.Length <= _limit)
        {
            _file.Write(bytes);
            _file.Flush();
        }
        else if (!_limitReported)
        {
            _limitReported = true;
            _console.WriteLine(JsonSerializer.Serialize(new { level = "Warning", eventName = "LogFileLimitReached", message = "File log đầy; dừng ghi file, không xóa nội dung cũ. Lưu trữ log trước khi chạy tiếp." }));
        }
    }

    public void Dispose() => _file?.Dispose();
}
