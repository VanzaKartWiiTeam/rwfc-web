using Microsoft.Extensions.Caching.Memory;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Collections.Concurrent;

namespace RetroRewindWebsite.Services.Domain;

/// <summary>
/// Renders and caches Mii avatar images, converting the Wii Mii block in process and asking a
/// Studio render endpoint for the picture.
/// </summary>
public class MiiService : IMiiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheOptions;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly ILogger<MiiService> _logger;

    /*
        Render endpoints, tried in order. The second one is the same fallback the VanzaKart
        launcher uses, so a Nintendo outage no longer means no avatars at all.

        The conversion step that used to run through miicontestp.wii.rc24.xyz is gone: it is now
        done locally by MiiStudioConverter. That service answers "Invalid request." to every
        request, including its own root, which is what left the leaderboard full of blanks.
    */
    private static readonly string[] RenderEndpoints =
    [
        "https://studio.mii.nintendo.com/miis/image.png",
        "https://mii-unsecure.ariankordi.net/miis/image.png"
    ];

    // One network call per render now instead of two, so more can be in flight without the
    // queue behind them timing out.
    private static readonly SemaphoreSlim _renderSemaphore = new(8, 8);

    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

    public MiiService(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache, ILogger<MiiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = memoryCache;
        _logger = logger;

        // Extended cache duration since Mii data rarely changes
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromDays(1))
            .SetAbsoluteExpiration(TimeSpan.FromDays(7))
            .SetSize(1);
    }

    public async Task<string?> GetMiiImageAsync(string friendCode, string miiData, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(friendCode) || string.IsNullOrEmpty(miiData))
            return null;

        // Check memory cache first
        if (_cache.TryGetValue(friendCode, out string? cachedMiiImage))
        {
            return cachedMiiImage;
        }

        var semaphore = _locks.GetOrAdd(friendCode, _ => new SemaphoreSlim(1, 1));

        try
        {
            await semaphore.WaitAsync(cancellationToken);

            // Double-check cache after acquiring lock
            if (_cache.TryGetValue(friendCode, out cachedMiiImage))
            {
                return cachedMiiImage;
            }

            // Converting first means a malformed block costs nothing: no request goes out.
            if (!MiiStudioConverter.TryConvert(miiData, out var studioData))
            {
                _logger.LogWarning("Mii data for {FriendCode} is not a usable Wii Mii block", friendCode);
                return null;
            }

            await _renderSemaphore.WaitAsync(cancellationToken);

            try
            {
                var imageBytes = await RenderAsync(studioData, friendCode, cancellationToken);
                if (imageBytes == null)
                    return null;

                using var image = Image.Load(imageBytes);
                image.Mutate(x => x.Resize(64, 64));
                using var ms = new MemoryStream();
                await image.SaveAsPngAsync(ms, cancellationToken);
                var base64Image = Convert.ToBase64String(ms.ToArray());

                // Cache in memory
                _cache.Set(friendCode, base64Image, _cacheOptions);

                _logger.LogDebug("Successfully rendered and cached Mii for {FriendCode}", friendCode);

                return base64Image;
            }
            finally
            {
                _renderSemaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error rendering Mii for {FriendCode}", friendCode);
            return null;
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Asks each render endpoint in turn for the picture, returning the first one that answers
    /// with an image.
    /// </summary>
    private async Task<byte[]?> RenderAsync(string studioData, string friendCode, CancellationToken cancellationToken)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = RequestTimeout;

        foreach (var endpoint in RenderEndpoints)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var url = BuildImageUrl(endpoint, studioData);
                var response = await httpClient.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Renderer {Endpoint} returned {StatusCode} for {FriendCode}",
                        endpoint, response.StatusCode, friendCode);
                    continue;
                }

                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

                // A body too small to be a face usually means an error page with a 200 on it.
                if (bytes.Length < 512)
                {
                    _logger.LogWarning("Renderer {Endpoint} returned {Size} bytes for {FriendCode}, too small to be an image",
                        endpoint, bytes.Length, friendCode);
                    continue;
                }

                return bytes;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // The client timed out on this endpoint; the next one still gets a turn.
                _logger.LogWarning("Renderer {Endpoint} timed out for {FriendCode}", endpoint, friendCode);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Renderer {Endpoint} unreachable for {FriendCode}", endpoint, friendCode);
            }
        }

        _logger.LogWarning("No renderer produced an image for {FriendCode}", friendCode);
        return null;
    }

    private static string BuildImageUrl(string endpoint, string studioData)
    {
        var query = new Dictionary<string, string>
        {
            ["data"] = studioData,
            ["type"] = "face",
            ["expression"] = "normal",
            ["width"] = "270",
            ["bgColor"] = "FFFFFF00"
        };

        return $"{endpoint}?{string.Join("&", query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"))}";
    }
}
