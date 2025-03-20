using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Configuration;
using Blazored.LocalStorage;
using System;
using System.Threading.Tasks;

namespace LagerClient.Blazor.Client.Services
{
    public class AppStateService : IAppStateService
    {
        private readonly IConfiguration _configuration;
        private BehaviorSubject<AppConfig> _configSubject;

        public AppStateService(IConfiguration configuration)
        {
            _configuration = configuration;
            
            var initialConfig = new AppConfig
            {
                ApiUrl = _configuration["ApiUrl"] ?? "https://localhost:5001/api/article",
                IsDebugMode = bool.TryParse(_configuration["DebugMode"], out bool debugMode) && debugMode,
                UiSettings = new UiSettings
                {
                    TableRowHeight = int.TryParse(_configuration["UI:Table:RowHeight"], out int rowHeight) ? rowHeight : 25,
                    TableZebraColor = _configuration["UI:Table:ZebraColor"] ?? "#F0F0F0"
                }
            };

            _configSubject = new BehaviorSubject<AppConfig>(initialConfig);
        }

        public IObservable<AppConfig> ConfigChanges => _configSubject;

        public async Task UpdateConfigAsync(AppConfig config)
        {
            Console.WriteLine($"AppStateService - Konfiguration wird aktualisiert: Debug-Modus = {config.IsDebugMode}");
            
            // Eine neue Kopie erstellen, um sicherzustellen, dass Änderungen erkannt werden
            var updatedConfig = new AppConfig
            {
                ApiUrl = config.ApiUrl,
                IsDebugMode = config.IsDebugMode,
                UiSettings = new UiSettings
                {
                    TableRowHeight = config.UiSettings.TableRowHeight,
                    TableZebraColor = config.UiSettings.TableZebraColor
                }
            };
            
            _configSubject.OnNext(updatedConfig);
            Console.WriteLine($"AppStateService - Benachrichtigung gesendet: Debug-Modus = {updatedConfig.IsDebugMode}");
            
            await Task.CompletedTask; // Um die async-Signatur zu erfüllen
        }

        public AppConfig GetCurrentConfig() => _configSubject.Value;

        public void NotifySettingsChanged()
        {
            Console.WriteLine($"NotifySettingsChanged - Aktuelle Konfiguration: Debug-Modus = {_configSubject.Value.IsDebugMode}");
            _configSubject.OnNext(GetCurrentConfig());
        }
    }
}