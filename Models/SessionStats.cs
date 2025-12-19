using System.Text.Json.Serialization;

namespace TransmissionTrayAgent.Models;

public class SessionStatsResponse
{
    [JsonPropertyName("arguments")]
    public SessionStatsArguments? Arguments { get; set; }

    [JsonPropertyName("result")]
    public string? Result { get; set; }
}

public class SessionStatsArguments
{
    [JsonPropertyName("activeTorrentCount")]
    public int ActiveTorrentCount { get; set; }

    [JsonPropertyName("downloadSpeed")]
    public long DownloadSpeed { get; set; }

    [JsonPropertyName("uploadSpeed")]
    public long UploadSpeed { get; set; }
}
