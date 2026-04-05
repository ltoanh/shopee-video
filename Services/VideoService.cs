using Xabe.FFmpeg;
using Microsoft.Extensions.Logging;

namespace ShopeeVideo.Worker.Services;

public interface IVideoService
{
    Task<List<string>> ExtractImagesAsync(string videoPath, int count = 3);
    Task<double> GetDurationAsync(string videoPath);
}

public class VideoService : IVideoService
{
    private readonly ILogger<VideoService> _logger;
    private readonly string _outputFolder;

    public VideoService(ILogger<VideoService> logger)
    {
        _logger = logger;
        _outputFolder = Path.Combine(Directory.GetCurrentDirectory(), "temp_images");
        
        if (!Directory.Exists(_outputFolder))
        {
            Directory.CreateDirectory(_outputFolder);
        }
    }

    public async Task<double> GetDurationAsync(string videoPath)
    {
        try
        {
            var info = await FFmpeg.GetMediaInfo(videoPath);
            return info.Duration.TotalSeconds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video duration for {Path}", videoPath);
            return 0;
        }
    }

    public async Task<List<string>> ExtractImagesAsync(string videoPath, int count = 3)
    {
        var imagePaths = new List<string>();
        try
        {
            var info = await FFmpeg.GetMediaInfo(videoPath);
            var duration = info.Duration.TotalSeconds;
            var interval = duration / (count + 1);

            for (int i = 1; i <= count; i++)
            {
                var timestamp = interval * i;
                var fileName = $"img_{Guid.NewGuid():N}.jpg";
                var outputPath = Path.Combine(_outputFolder, fileName);

                // ffmpeg -ss [time] -i [input] -frames:v 1 -q:v 2 [output]
                var conversion = await FFmpeg.Conversions.New()
                    .AddParameter($"-ss {timestamp:F2}")
                    .AddParameter($"-i \"{videoPath}\"")
                    .AddParameter("-frames:v 1")
                    .AddParameter("-q:v 2")
                    .SetOutput(outputPath)
                    .Start();

                if (File.Exists(outputPath))
                {
                    imagePaths.Add(outputPath);
                }
            }

            _logger.LogInformation("Extracted {Count} images from {Path}", imagePaths.Count, videoPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting images from video {Path}", videoPath);
        }

        return imagePaths;
    }
}
