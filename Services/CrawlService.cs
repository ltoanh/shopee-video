using Microsoft.Playwright;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ShopeeVideo.Worker.Services;

public interface ICrawlService
{
    Task<string?> CrawlVideoAsync(string keywordCn);
}

public class CrawlService : ICrawlService
{
    private readonly ILogger<CrawlService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _downloadPath;
    private readonly string _userDataDir;

    public CrawlService(ILogger<CrawlService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _downloadPath = Path.Combine(Directory.GetCurrentDirectory(), "temp_videos");
        _userDataDir = Path.Combine(Directory.GetCurrentDirectory(), "BrowserProfile");
        
        if (!Directory.Exists(_downloadPath)) Directory.CreateDirectory(_downloadPath);
        if (!Directory.Exists(_userDataDir)) Directory.CreateDirectory(_userDataDir);
    }

    public async Task<string?> CrawlVideoAsync(string keywordCn)
    {
        using var playwright = await Playwright.CreateAsync();
        
        var launchOptions = new BrowserTypeLaunchPersistentContextOptions
        {
            Headless = false, // Set to false to see the process or true for background
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
            Args = new[] { "--disable-blink-features=AutomationControlled" },
            Channel = "chrome", // Use local Chrome instead of downloading Chromium
            // ExecutablePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe" // Optional: Use Chrome if needed
        };

        await using var browserContext = await playwright.Chromium.LaunchPersistentContextAsync(_userDataDir, launchOptions);

        try
        {
            var platforms = _configuration.GetSection("Automation:Platforms").Get<string[]>() ?? new[] { "Douyin" };
            var platform = platforms[new Random().Next(platforms.Length)];
            
            _logger.LogInformation("Scraping from platform: {Platform} for keyword: {Keyword}", platform, keywordCn);

            var page = browserContext.Pages.Count > 0 ? browserContext.Pages[0] : await browserContext.NewPageAsync();
            
            // Apply stealth script (simplified version of what ThreadAff does)
            await page.AddInitScriptAsync("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");

            if (platform == "Douyin")
            {
                return await ScrapeDouyinAsync(page, keywordCn);
            }
            else if (platform == "Xiaohongshu")
            {
                return await ScrapeXiaohongshuAsync(page, keywordCn);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scraping.");
            return null;
        }
    
    }

    private async Task<string?> ScrapeDouyinAsync(IPage page, string keywordCn)
    {
        string? videoUrl = null;
        try 
        {
            _logger.LogInformation("Navigating to Douyin search for: {Keyword}", keywordCn);
            
            // Intercept media responses
            page.Response += async (sender, response) =>
            {
                if (response.Request.ResourceType == "media" || response.Url.Contains(".mp4") || response.Url.Contains("video_id"))
                {
                    videoUrl = response.Url;
                }
            };

            await page.GotoAsync($"https://www.douyin.com/search/{Uri.EscapeDataString(keywordCn)}", 
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            
            await page.WaitForTimeoutAsync(3000);
            
            var firstVideo = page.Locator("ul[data-e2e='scroll-list'] li a").First;
            if (await firstVideo.IsVisibleAsync())
            {
                _logger.LogInformation("Found video result, clicking...");
                await firstVideo.ClickAsync();
                
                // Wait for video to start playing and capture the URL
                for (int i = 0; i < 10; i++)
                {
                    if (!string.IsNullOrEmpty(videoUrl)) break;
                    await page.WaitForTimeoutAsync(1000);
                }

                if (!string.IsNullOrEmpty(videoUrl))
                {
                    _logger.LogInformation("Found video URL: {Url}", videoUrl);
                    return await DownloadFileAsync(videoUrl, "douyin_video.mp4");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Douyin scraping failed.");
        }
        
        return null;
    }

    private async Task<string?> DownloadFileAsync(string url, string fileName)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Referer", "https://www.douyin.com/");

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var filePath = Path.Combine(_downloadPath, $"{Guid.NewGuid():N}_{fileName}");
            await using var fs = new FileStream(filePath, FileMode.Create);
            await response.Content.CopyToAsync(fs);

            _logger.LogInformation("Downloaded video to: {Path}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from {Url}", url);
            return null;
        }
    }


    private async Task<string?> ScrapeXiaohongshuAsync(IPage page, string keywordCn)
    {
        _logger.LogInformation("Xiaohongshu scraping logic triggered.");
        await page.GotoAsync($"https://www.xiaohongshu.com/search_result?keyword={Uri.EscapeDataString(keywordCn)}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Task.Delay(2000);
        return null;
    }
}

