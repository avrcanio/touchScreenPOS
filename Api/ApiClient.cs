using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TouchScreenPOS.Api;

public sealed class ApiClient
{
    private static readonly Uri ApiBaseUri = new("https://mozart.sibenik1983.hr/");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly CookieContainer _cookieContainer = new();
    private readonly HttpClient _httpClient;
    private readonly string _cookieFilePath;

    public ApiClient()
    {
        _cookieFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TouchScreenPOS",
            "session.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_cookieFilePath)!);

        var handler = new HttpClientHandler
        {
            CookieContainer = _cookieContainer,
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = ApiBaseUri,
            Timeout = TimeSpan.FromSeconds(15)
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
    }

    public async Task<(bool success, string message)> LoginAsync(string username, string password)
    {
        var csrfToken = await EnsureCsrfTokenAsync();
        if (string.IsNullOrWhiteSpace(csrfToken))
        {
            return (false, "Ne mogu dohvatiti CSRF token.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/login/");
        request.Headers.Add("X-CSRFTOKEN", csrfToken);
        request.Headers.Referrer = new Uri(ApiBaseUri, "api/login/");
        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("username", username),
            new KeyValuePair<string, string>("password", password)
        });

        var response = await _httpClient.SendAsync(request);
        var payload = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var token = TryExtractToken(payload);
            if (!string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            SaveCookies();
            return (true, "OK");
        }

        var message = "Neispravni podaci.";
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("detail", out var detail))
            {
                message = detail.GetString() ?? message;
            }
        }
        catch (JsonException)
        {
        }

        return (false, message);
    }

    public string? GetSessionId()
    {
        var cookies = _cookieContainer.GetCookies(ApiBaseUri);
        return cookies["sessionid"]?.Value;
    }

    public bool LoadCookies()
    {
        if (!File.Exists(_cookieFilePath))
        {
            return false;
        }

        try
        {
            var json = File.ReadAllText(_cookieFilePath, Encoding.UTF8);
            var snapshot = JsonSerializer.Deserialize<CookieSnapshot>(json, JsonOptions);
            if (snapshot == null)
            {
                return false;
            }

            foreach (var item in snapshot.Cookies)
            {
                var cookie = new Cookie(item.Name, item.Value, item.Path, item.Domain)
                {
                    Expires = item.Expires ?? DateTime.MinValue,
                    Secure = item.Secure,
                    HttpOnly = item.HttpOnly
                };
                _cookieContainer.Add(ApiBaseUri, cookie);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public void ClearCookies()
    {
        try
        {
            if (File.Exists(_cookieFilePath))
            {
                File.Delete(_cookieFilePath);
            }
        }
        catch
        {
        }
    }

    public async Task<List<Representation>> GetRepresentationsAsync()
    {
        var response = await _httpClient.GetAsync("api/representations/");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Representation>>(payload, JsonOptions) ?? new List<Representation>();
    }

    public async Task<Representation?> GetRepresentationByIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"api/representations/{id}/");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Representation>(payload, JsonOptions);
    }

    public async Task<ApiUser?> GetMeAsync()
    {
        var response = await _httpClient.GetAsync("api/me/");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiUser>(payload, JsonOptions);
    }

    public async Task<ApiUser?> GetUserByIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"api/users/{id}/");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiUser>(payload, JsonOptions);
    }

    public async Task<List<Warehouse>> GetWarehousesAsync()
    {
        var response = await _httpClient.GetAsync("api/warehouses/");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Warehouse>>(payload, JsonOptions) ?? new List<Warehouse>();
    }

    public async Task<List<RepresentationReason>> GetRepresentationReasonsAsync()
    {
        var response = await _httpClient.GetAsync("api/representation-reasons/");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<RepresentationReason>>(payload, JsonOptions) ?? new List<RepresentationReason>();
    }

    public async Task<List<DrinkCategory>> GetDrinkCategoriesAsync()
    {
        var response = await _httpClient.GetAsync("api/drink-categories/");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<DrinkCategory>>(payload, JsonOptions) ?? new List<DrinkCategory>();
    }

    public async Task<List<Artikl>> GetArtikliAsync()
    {
        var response = await _httpClient.GetAsync("api/artikli/");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Artikl>>(payload, JsonOptions) ?? new List<Artikl>();
    }

    public Task<byte[]> GetBytesAsync(string url)
    {
        return _httpClient.GetByteArrayAsync(url);
    }

    public async Task<(bool success, string message, int statusCode, string responseBody, string requestBody)> CreateRepresentationAsync(RepresentationCreateRequest request)
    {
        var csrfToken = await EnsureCsrfTokenAsync();
        if (string.IsNullOrWhiteSpace(csrfToken))
        {
            return (false, "Ne mogu dohvatiti CSRF token.", 0, string.Empty, string.Empty);
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/representations/");
        httpRequest.Headers.Add("X-CSRFTOKEN", csrfToken);
        httpRequest.Headers.Referrer = new Uri(ApiBaseUri, "api/representations/");

        var json = JsonSerializer.Serialize(request, JsonOptions);
        httpRequest.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(httpRequest);
        var payload = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            return (true, "OK", (int)response.StatusCode, payload, json);
        }

        var message = "Spremanje nije uspjelo.";
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("detail", out var detail))
            {
                message = detail.GetString() ?? message;
            }
        }
        catch (JsonException)
        {
        }

        return (false, message, (int)response.StatusCode, payload, json);
    }

    public async Task LogoutAsync()
    {
        var csrfToken = await EnsureCsrfTokenAsync();
        if (string.IsNullOrWhiteSpace(csrfToken))
        {
            return;
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/logout/");
        httpRequest.Headers.Add("X-CSRFTOKEN", csrfToken);
        httpRequest.Headers.Referrer = new Uri(ApiBaseUri, "api/logout/");
        httpRequest.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        await _httpClient.SendAsync(httpRequest);
    }

    private async Task<string?> EnsureCsrfTokenAsync()
    {
        var response = await _httpClient.GetAsync("api/csrf/");
        response.EnsureSuccessStatusCode();

        var cookies = _cookieContainer.GetCookies(ApiBaseUri);
        var csrfCookie = cookies["csrftoken"];
        return csrfCookie?.Value;
    }

    private static string? TryExtractToken(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            if (root.TryGetProperty("token", out var token))
            {
                return token.GetString();
            }
            if (root.TryGetProperty("access", out var access))
            {
                return access.GetString();
            }
            if (root.TryGetProperty("access_token", out var accessToken))
            {
                return accessToken.GetString();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private void SaveCookies()
    {
        try
        {
            var cookies = _cookieContainer.GetCookies(ApiBaseUri);
            var snapshot = new CookieSnapshot();
            foreach (Cookie cookie in cookies)
            {
                snapshot.Cookies.Add(new CookieItem
                {
                    Name = cookie.Name,
                    Value = cookie.Value,
                    Domain = cookie.Domain,
                    Path = cookie.Path,
                    Expires = cookie.Expires == DateTime.MinValue ? null : cookie.Expires,
                    Secure = cookie.Secure,
                    HttpOnly = cookie.HttpOnly
                });
            }

            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            File.WriteAllText(_cookieFilePath, json, Encoding.UTF8);
        }
        catch
        {
        }
    }

    private sealed class CookieSnapshot
    {
        public List<CookieItem> Cookies { get; set; } = new();
    }

    private sealed class CookieItem
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Path { get; set; } = "/";
        public DateTime? Expires { get; set; }
        public bool Secure { get; set; }
        public bool HttpOnly { get; set; }
    }
}
