using ShopeeVideo.Worker.Models;
using ShopeeVideo.Worker.Services;

namespace ShopeeVideo.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IGoogleSheetService _sheetService;
    private readonly ICrawlService _crawlService;
    private readonly IVideoService _videoService;
    private readonly IConfiguration _configuration;

    public Worker(ILogger<Worker> logger, IGoogleSheetService sheetService, ICrawlService crawlService, IVideoService videoService, IConfiguration configuration)
    {
        _logger = logger;
        _sheetService = sheetService;
        _crawlService = crawlService;
        _videoService = videoService;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ShopeeVideo Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Step 1: Get Random Pending Task
                var task = await _sheetService.GetRandomPendingTaskAsync();
                if (task == null)
                {
                    _logger.LogInformation("No pending tasks found. Waiting for 5 minutes.");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    continue;
                }

                _logger.LogInformation("Processing keyword: {Keyword} (ZH: {KeywordZh}) at row {Row}", 
                    task.Keyword, task.KeywordZh, task.RowIndex);

                // Step 2: Update status to InProgress
                await _sheetService.UpdateTaskStatusAsync(task.RowIndex, GoogleSheetConstants.Status.InProgress);

                // Step 3-4: Crawl & Process Video
                var videoPath = await _crawlService.CrawlVideoAsync(task.KeywordZh);
                string source = "Douyin"; // For now, assumed
                
                // Step 5: Extract Images
                string originalShopeeLink = "";
                if (!string.IsNullOrEmpty(videoPath) && File.Exists(videoPath))
                {
                    var images = await _videoService.ExtractImagesAsync(videoPath);
                    _logger.LogInformation("Extracted {Count} images for processing.", images.Count);
                    // Sprint 3 will populate originalShopeeLink
                }

                // TODO: Sprint 3 - Google Lens & Affiliate
                string affiliateLink = "Simulated_Affiliate_Link";
                
                // TODO: Sprint 4 - Upload Drive
                string driveLink = "Simulated_Drive_Link";

                // Step 7: Ghi Log & Hoàn tất
                var processingLog = new ProcessingLog
                {
                    Keyword = task.Keyword,
                    Source = source,
                    OriginalShopeeLink = originalShopeeLink,
                    AffiliateLink = affiliateLink,
                    DriveLink = driveLink,
                    Status = "Thành công"
                };

                await _sheetService.WriteLogAsync(processingLog);
                await _sheetService.UpdateTaskStatusAsync(task.RowIndex, GoogleSheetConstants.Status.Completed);

                _logger.LogInformation("Job completed and logged for row {Row}.", task.RowIndex);


                // Step 8: Cooldown
                var minCooldown = _configuration.GetValue<int>("Automation:MinCooldownMinutes");
                var maxCooldown = _configuration.GetValue<int>("Automation:MaxCooldownMinutes");
                var cooldownMinutes = new Random().Next(minCooldown, maxCooldown + 1);

                _logger.LogInformation("Cooldown for {Minutes} minutes...", cooldownMinutes);
                await Task.Delay(TimeSpan.FromMinutes(cooldownMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in worker loop.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}

