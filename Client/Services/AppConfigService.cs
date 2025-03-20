using Microsoft.Extensions.Configuration;
using Blazored.LocalStorage;
using System.Text.Json;
using System.Threading.Tasks;
using System;

namespace LagerClient.Blazor.Client.Services
{
    public class AppConfigService : IAppConfigService
    {
        private readonly IConfiguration _configuration;
        private readonly ILocalStorageService _localStorage;
        private readonly IAppStateService _appStateService;
        private readonly string _configKey = "app_config";
        private AppConfig? _cachedConfig;

        public AppConfigService(IConfiguration configuration, ILocalStorageService localStorage, IAppStateService appStateService)
        {
            _configuration = configuration;
            _localStorage = localStorage;
            _appStateService = appStateService;
        }

        public async Task<AppConfig> GetConfigAsync()
        {
            if (_cachedConfig != null)
                return _cachedConfig;

            // Versuche zuerst aus dem lokalen Speicher zu laden
            if (await _localStorage.ContainKeyAsync(_configKey))
            {
                var storedConfig = await _localStorage.GetItemAsync<AppConfig>(_configKey);
                if (storedConfig != null)
                {
                    _cachedConfig = storedConfig;
                    
                    // Stelle sicher, dass der AppStateService die aktuelle Konfiguration hat
                    await _appStateService.UpdateConfigAsync(storedConfig);
                    
                    return storedConfig;
                }
            }

            // Falls nicht im lokalen Speicher vorhanden, lade die Standardkonfiguration
            var config = new AppConfig
            {
                ApiUrl = _configuration["ApiUrl"] ?? "https://localhost:5001/api/article",
                IsDebugMode = bool.TryParse(_configuration["DebugMode"], out bool debugMode) && debugMode,
                UiSettings = new UiSettings
                {
                    TableRowHeight = int.TryParse(_configuration["UI:Table:RowHeight"], out int rowHeight) ? rowHeight : 25,
                    TableZebraColor = _configuration["UI:Table:ZebraColor"] ?? "#F0F0F0"
                }
            };

            // Speichere die Standardkonfiguration im lokalen Speicher
            await _localStorage.SetItemAsync(_configKey, config);
            _cachedConfig = config;
            
            // Stelle sicher, dass der AppStateService die aktuelle Konfiguration hat
            await _appStateService.UpdateConfigAsync(config);
            
            return config;
        }

        public async Task UpdateConfigAsync(AppConfig config)
        {
            Console.WriteLine($"AppConfigService - Konfiguration wird aktualisiert: Debug-Modus = {config.IsDebugMode}");
            
            await _localStorage.SetItemAsync(_configKey, config);
            _cachedConfig = config;
            
            // AppStateService über die Änderung informieren
            await _appStateService.UpdateConfigAsync(config);
        }
    }
}