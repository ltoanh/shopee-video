namespace ShopeeVideo.Worker.Models;

public static class GoogleSheetConstants
{
    public static class Sheets
    {
        public const string KeyWord = "KeyWord";
        public const string Log = "Log";
    }

    public static class KeyWordSheet
    {
        public const int KeywordColumnIndex = 0;    // Column A
        public const int KeywordZhColumnIndex = 1;  // Column B
        public const string Range = "KeyWord!A2:B";
    }

    public static class LogSheet
    {
        public const int KeywordColumnIndex = 0;        // Column A
        public const int SourceColumnIndex = 1;         // Column B
        public const int OriginalShopeeLinkIndex = 2;   // Column C
        public const int AffiliateLinkIndex = 3;        // Column D
        public const int DriveLinkIndex = 4;            // Column E
        public const int StatusColumnIndex = 5;         // Column F
        public const string Range = "Log!A2:F";
    }

    public static class Status
    {
        public const string Pending = "Pending";
        public const string InProgress = "InProgress";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
    }
}

public class VideoTaskInfo
{
    public int RowIndex { get; set; }
    public string Keyword { get; set; }
    public string KeywordZh { get; set; }
}

public class ProcessingLog
{
    public string Keyword { get; set; }
    public string Source { get; set; }
    public string OriginalShopeeLink { get; set; }
    public string AffiliateLink { get; set; }
    public string DriveLink { get; set; }
    public string Status { get; set; }
}

