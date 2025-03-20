using System.Reactive.Linq;
using System.Threading.Tasks;

namespace LagerClient.Blazor.Client.Services
{
    public interface IAppStateService
    {
        void NotifySettingsChanged();
        IObservable<AppConfig> ConfigChanges { get; }
        Task UpdateConfigAsync(AppConfig config);
        AppConfig GetCurrentConfig();
    }
}