namespace LagerClient.Blazor.Client.Services
{
    public class AppConfig
    {
        public string ApiUrl { get; set; } = string.Empty;
        public bool IsDebugMode { get; set; }
        public UiSettings UiSettings { get; set; } = new UiSettings();
    }

    public class UiSettings
    {
        public int TableRowHeight { get; set; } = 25;
        public string TableZebraColor { get; set; } = "#F0F0F0";
    }
}