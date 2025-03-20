using System.Threading.Tasks;

namespace LagerClient.Blazor.Client.Services
{
    public interface IAppConfigService
    {
        Task<AppConfig> GetConfigAsync();
        Task UpdateConfigAsync(AppConfig config);
    }
}