using System.Text.Json.Serialization;

namespace TransmissionTrayAgent.Models;

public class TorrentRequest
{
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public TorrentRequestArguments Arguments { get; set; } = new();
}

public class TorrentRequestArguments
{
    [JsonPropertyName("ids")]
    public int[]? Ids { get; set; }

    [JsonPropertyName("fields")]
    public string[]? Fields { get; set; }
}

public class TorrentGetResponse
{
    [JsonPropertyName("arguments")]
    public TorrentGetArguments? Arguments { get; set; }

    [JsonPropertyName("result")]
    public string? Result { get; set; }
}

public class TorrentGetArguments
{
    [JsonPropertyName("torrents")]
    public TorrentInfo[]? Torrents { get; set; }
}

public class TorrentInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }
}
