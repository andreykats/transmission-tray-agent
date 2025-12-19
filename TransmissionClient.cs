using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TransmissionTrayAgent.Models;

namespace TransmissionTrayAgent;

public class TransmissionClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly AppSettings _settings;
    private string? _sessionToken;
    private readonly string _baseUrl;

    public TransmissionClient(AppSettings settings)
    {
        _settings = settings;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Build base URL
        string protocol = _settings.UseHttps ? "https" : "http";
        _baseUrl = $"{protocol}://{_settings.TransmissionHost}:{_settings.TransmissionPort}/transmission/rpc";

        // Setup Basic Authentication if credentials provided
        if (!string.IsNullOrWhiteSpace(_settings.Username))
        {
            var authValue = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{_settings.Username}:{_settings.Password}")
            );
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", authValue);
        }
    }

    /// <summary>
    /// Tests connection to Transmission server
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var stats = await GetSessionStatsAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets current session statistics including active torrent count
    /// </summary>
    public async Task<SessionStatsArguments> GetSessionStatsAsync()
    {
        var request = new
        {
            method = "session-stats",
            arguments = new { }
        };

        var response = await SendRequestAsync<SessionStatsResponse>(request);

        if (response?.Arguments == null)
        {
            throw new Exception("Invalid response from Transmission server");
        }

        return response.Arguments;
    }

    /// <summary>
    /// Gets list of all torrents with their status
    /// </summary>
    public async Task<TorrentInfo[]> GetAllTorrentsAsync()
    {
        var request = new TorrentRequest
        {
            Method = "torrent-get",
            Arguments = new TorrentRequestArguments
            {
                Fields = new[] { "id", "status" }
            }
        };

        var response = await SendRequestAsync<TorrentGetResponse>(request);

        if (response?.Arguments?.Torrents == null)
        {
            throw new Exception("Invalid response from Transmission server");
        }

        return response.Arguments.Torrents;
    }

    /// <summary>
    /// Starts all torrents
    /// </summary>
    public async Task StartAllTorrentsAsync()
    {
        var request = new TorrentRequest
        {
            Method = "torrent-start",
            Arguments = new TorrentRequestArguments
            {
                Ids = null // Null means all torrents (omitted from JSON)
            }
        };

        await SendRequestAsync<object>(request);
    }

    /// <summary>
    /// Stops all torrents
    /// </summary>
    public async Task StopAllTorrentsAsync()
    {
        var request = new TorrentRequest
        {
            Method = "torrent-stop",
            Arguments = new TorrentRequestArguments
            {
                Ids = null // Null means all torrents (omitted from JSON)
            }
        };

        await SendRequestAsync<object>(request);
    }

    /// <summary>
    /// Sends a request to Transmission RPC API with automatic session token handling
    /// </summary>
    private async Task<T?> SendRequestAsync<T>(object request, int retryCount = 0)
    {
        const int maxRetries = 1;

        try
        {
            var serializeOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request, serializeOptions),
                Encoding.UTF8,
                "application/json"
            );

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _baseUrl)
            {
                Content = content
            };

            // Add session token if we have one
            if (!string.IsNullOrEmpty(_sessionToken))
            {
                httpRequest.Headers.Add("X-Transmission-Session-Id", _sessionToken);
            }

            var response = await _httpClient.SendAsync(httpRequest);

            // Handle 409 Conflict - need to get session token
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                if (retryCount < maxRetries)
                {
                    // Extract session token from response headers
                    if (response.Headers.TryGetValues("X-Transmission-Session-Id", out var values))
                    {
                        _sessionToken = values.FirstOrDefault();

                        if (!string.IsNullOrEmpty(_sessionToken))
                        {
                            // Retry request with new token
                            return await SendRequestAsync<T>(request, retryCount + 1);
                        }
                    }
                }

                throw new Exception("Failed to obtain session token from Transmission server");
            }

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();

            if (typeof(T) == typeof(object))
            {
                return default;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<T>(responseJson, options);
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Network error: {ex.Message}", ex);
        }
        catch (TaskCanceledException)
        {
            throw new Exception("Request timeout - Transmission server did not respond");
        }
        catch (JsonException ex)
        {
            throw new Exception($"Invalid response format: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
