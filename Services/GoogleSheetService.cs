using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShopeeVideo.Worker.Models;

namespace ShopeeVideo.Worker.Services;

public interface IGoogleSheetService
{
    Task<VideoTaskInfo?> GetRandomPendingTaskAsync();
    Task UpdateTaskStatusAsync(int rowIndex, string status);
    Task WriteLogAsync(ProcessingLog log);
}

public class GoogleSheetService : IGoogleSheetService
{
    private readonly ILogger<GoogleSheetService> _logger;
    private readonly string _sheetId;
    private readonly string _credentialsPath;
    private readonly SheetsService _service;

    public GoogleSheetService(ILogger<GoogleSheetService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _sheetId = configuration["GoogleSheets:SheetId"] ?? throw new ArgumentNullException("SheetId is missing");
        _credentialsPath = configuration["GoogleSheets:CredentialsPath"] ?? "credentials.json";

        GoogleCredential credential;
        using (var stream = new FileStream(_credentialsPath, FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.Spreadsheets);
        }

        _service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "ShopeeVideoWorker",
        });
    }

    public async Task<VideoTaskInfo?> GetRandomPendingTaskAsync()
    {
        try
        {
            var request = _service.Spreadsheets.Values.Get(_sheetId, GoogleSheetConstants.KeyWordSheet.Range);
            var response = await request.ExecuteAsync();
            var values = response.Values;

            if (values == null || values.Count == 0)
            {
                _logger.LogWarning("No data found in KeyWord sheet.");
                return null;
            }

            var pendingTasks = new List<VideoTaskInfo>();
            for (int i = 0; i < values.Count; i++)
            {
                var row = values[i];
                
                pendingTasks.Add(new VideoTaskInfo
                {
                    RowIndex = i + 2, // A2 starts at index 0, so row is index + 2
                    Keyword = row.Count > GoogleSheetConstants.KeyWordSheet.KeywordColumnIndex ? row[GoogleSheetConstants.KeyWordSheet.KeywordColumnIndex]?.ToString() ?? "" : "",
                    KeywordZh = row.Count > GoogleSheetConstants.KeyWordSheet.KeywordZhColumnIndex ? row[GoogleSheetConstants.KeyWordSheet.KeywordZhColumnIndex]?.ToString() ?? "" : ""
                });
            }

            if (pendingTasks.Count == 0) return null;

            var random = new Random();
            return pendingTasks[random.Next(pendingTasks.Count)];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending tasks from KeyWord sheet.");
            return null;
        }
    }

    public async Task UpdateTaskStatusAsync(int rowIndex, string status)
    {
        try
        {
            var values = new List<object> { status };
            var updateRange = $"{GoogleSheetConstants.Sheets.KeyWord}!C{rowIndex}";

            var valueRange = new ValueRange { Values = new List<IList<object>> { values } };
            var updateRequest = _service.Spreadsheets.Values.Update(valueRange, _sheetId, updateRange);
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await updateRequest.ExecuteAsync();

            _logger.LogInformation("Updated KeyWord row {RowIndex} to {Status}", rowIndex, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for KeyWord row {RowIndex}", rowIndex);
        }
    }

    public async Task WriteLogAsync(ProcessingLog log)
    {
        try
        {
            var values = new List<object> 
            { 
                log.Keyword, 
                log.Source, 
                log.OriginalShopeeLink, 
                log.AffiliateLink, 
                log.DriveLink, 
                log.Status 
            };

            var valueRange = new ValueRange { Values = new List<IList<object>> { values } };
            var appendRequest = _service.Spreadsheets.Values.Append(valueRange, _sheetId, $"{GoogleSheetConstants.Sheets.Log}!A2");
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
            await appendRequest.ExecuteAsync();

            _logger.LogInformation("Wrote log for keyword: {Keyword}", log.Keyword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing log to Log sheet.");
        }
    }
}

