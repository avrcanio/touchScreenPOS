using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TouchScreenPOS.Utils;

public sealed class ImageCache : IDisposable
{
    private readonly string _cacheDir;
    private readonly ConcurrentDictionary<string, Task<string?>> _inflight = new();
    private readonly SemaphoreSlim _gate = new(4, 4);
    private readonly Func<string, Task<byte[]>> _downloader;

    public ImageCache(Func<string, Task<byte[]>>? downloader = null)
    {
        _downloader = downloader ?? DefaultDownloadAsync;
        _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TouchScreenPOS",
            "cache",
            "images");
        Directory.CreateDirectory(_cacheDir);
    }

    public Task<string?> GetOrDownloadAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return Task.FromResult<string?>(null);
        }

        return _inflight.GetOrAdd(url, DownloadAsync);
    }

    private async Task<string?> DownloadAsync(string url)
    {
        try
        {
            var fileName = Hash(url) + Path.GetExtension(new Uri(url).AbsolutePath);
            var path = Path.Combine(_cacheDir, fileName);
            if (File.Exists(path))
            {
                return path;
            }

            await _gate.WaitAsync();
            try
            {
                if (File.Exists(path))
                {
                    return path;
                }

                var bytes = await _downloader(url);
                await File.WriteAllBytesAsync(path, bytes);
                return path;
            }
            finally
            {
                _gate.Release();
            }
        }
        catch
        {
            return null;
        }
        finally
        {
            _inflight.TryRemove(url, out _);
        }
    }

    private static string Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    private static async Task<byte[]> DefaultDownloadAsync(string url)
    {
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20)
        };
        return await httpClient.GetByteArrayAsync(url);
    }

    public void Dispose()
    {
        _gate.Dispose();
    }
}
