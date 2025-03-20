using LagerClient.Blazor.Shared.Models;
using System.Text.Json;

namespace LagerClient.Blazor.Client.Services
{
    public interface IDataExportImportService
    {
        Task ExportDataToJson();
        Task<List<Article>?> ImportDataFromJson();
    }
}