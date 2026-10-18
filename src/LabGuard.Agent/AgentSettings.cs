using System.Text.Json;
using System.Text.Json.Serialization;

namespace LabGuard.Agent;

/// <summary>Per-installation settings. Contains a secret reference, never a token.</summary>
public sealed class AgentSettings
{
    public string? DeviceId { get; set; }
    public string? CentralHost { get; set; }
    public int CentralPort { get; set; } = 5000;
    public int CollectionIntervalSeconds { get; set; } = 5;
    public int HeartbeatIntervalSeconds { get; set; } = 5;
    public int ConnectTimeoutSeconds { get; set; } = 5;
    public int ProbeDurationSeconds { get; set; } = 2;
    public string AuthenticationTokenEnvironmentVariable { get; set; } = "LABGUARD_AGENT_TOKEN";
    public bool LogToConsole { get; set; } = true;
    public string? LogFilePath { get; set; }
    public int MaxLogFileBytes { get; set; } = 1_048_576;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true
    };

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(DeviceId) || DeviceId.Length > 64 ||
            !DeviceId.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.'))
            throw new ArgumentException("DeviceId phải có 1–64 ký tự chữ/số ASCII, dấu chấm, gạch ngang hoặc gạch dưới.");
        if (string.IsNullOrWhiteSpace(CentralHost) || CentralHost != CentralHost.Trim() ||
            Uri.CheckHostName(CentralHost) == UriHostNameType.Unknown)
            throw new ArgumentException("CentralHost phải là địa chỉ IP hoặc tên host, không gồm URL/cổng.");
        if (CentralPort is < 1 or > 65535) throw new ArgumentException("CentralPort phải từ 1 đến 65535.");
        if (CollectionIntervalSeconds is < 1 or > 3600 || HeartbeatIntervalSeconds is < 1 or > 3600)
            throw new ArgumentException("CollectionIntervalSeconds và HeartbeatIntervalSeconds phải từ 1 đến 3600.");
        if (ConnectTimeoutSeconds is < 1 or > 120) throw new ArgumentException("ConnectTimeoutSeconds phải từ 1 đến 120.");
        if (ProbeDurationSeconds is < 1 or > 30) throw new ArgumentException("ProbeDurationSeconds phải từ 1 đến 30.");
        if (string.IsNullOrWhiteSpace(AuthenticationTokenEnvironmentVariable) ||
            !(char.IsAsciiLetter(AuthenticationTokenEnvironmentVariable[0]) || AuthenticationTokenEnvironmentVariable[0] == '_') ||
            !AuthenticationTokenEnvironmentVariable.All(c => char.IsAsciiLetterOrDigit(c) || c == '_'))
            throw new ArgumentException("AuthenticationTokenEnvironmentVariable phải là tên biến môi trường, không phải token.");
        if (MaxLogFileBytes is < 1024 or > 10_485_760) throw new ArgumentException("MaxLogFileBytes phải từ 1024 đến 10485760.");
        if (LogFilePath is not null && string.IsNullOrWhiteSpace(LogFilePath)) throw new ArgumentException("LogFilePath rỗng không hợp lệ; dùng null nếu chỉ log console.");
        if (!LogToConsole && LogFilePath is null) throw new ArgumentException("Phải bật ít nhất một đầu ra log.");
    }

    public static AgentSettings Load(string path)
    {
        using var stream = File.OpenRead(path);
        if (stream.Length > 65_536) throw new ArgumentException("Tệp cấu hình vượt giới hạn 64 KiB.");
        using var document = JsonDocument.Parse(stream);
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new ArgumentException("Cấu hình phải là một JSON object.");
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in document.RootElement.EnumerateObject())
            if (!keys.Add(property.Name)) throw new ArgumentException("Cấu hình có trường trùng tên.");
        var settings = document.RootElement.Deserialize<AgentSettings>(JsonOptions) ?? throw new ArgumentException("Cấu hình không hợp lệ.");
        settings.Validate();
        return settings;
    }

    public static AgentSettings Create(string path, string host, int port = 5000)
    {
        var settings = new AgentSettings { DeviceId = "LG-" + Guid.NewGuid().ToString("N"), CentralHost = host, CentralPort = port };
        settings.Validate();
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        // CreateNew deliberately refuses to overwrite an existing machine identity.
        using var file = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        JsonSerializer.Serialize(file, settings, JsonOptions);
        return settings;
    }
}
