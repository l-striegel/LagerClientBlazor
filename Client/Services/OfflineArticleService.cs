using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Blazored.LocalStorage;
using LagerClient.Blazor.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Blazored.Toast.Services;

namespace LagerClient.Blazor.Client.Services;

public interface IOfflineArticleService
{
    Task<List<Article>> GetArticlesAsync();
    Task<Article?> GetArticleAsync(int id);
    Task<Article> SaveArticleAsync(Article article);
    Task DeleteArticleAsync(int id);
    Task SyncWithBackendAsync();
    Task ResetToServerDataAsync();
    bool IsOnline { get; }
    Task<bool> VerifyDataIntegrityAsync();
}

public class OfflineArticleService : IOfflineArticleService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly AppConfigService _configService;
    private readonly IArticleService _articleService;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IToastService? _toastService; 
    
    private const string ARTICLES_KEY = "offline_articles";
    private const string HASH_KEY = "offline_articles_hash";
    
    private bool _isOnline = true;

    public bool IsOnline 
    { 
        get => _isOnline; 
        internal set => _isOnline = value; 
    }

    public OfflineArticleService(
        HttpClient httpClient,
        ILocalStorageService localStorage,
        AppConfigService configService,
        IArticleService articleService,
        IToastService? toastService = null)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _configService = configService;
        _articleService = articleService;
        _toastService = toastService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = null,
            WriteIndented = false
        };
    }

    public async Task<List<Article>> GetArticlesAsync()
    {
        // Wenn wir bereits offline sind, hole Artikel aus dem Offline-Speicher
        if (!_isOnline)
        {
            return await GetOfflineArticlesAsync();
        }

        // Versuche, Online-Artikel zu laden
        try
        {
            var config = await _configService.GetConfigAsync();
            var response = await _httpClient.GetAsync(config.ApiUrl);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var articles = JsonSerializer.Deserialize<List<Article>>(content, _jsonOptions) 
                ?? new List<Article>();
            
            // Speichere die Artikel offline und erstelle einen Hash
            await SaveArticlesOfflineAsync(articles);
            
            return articles;
        }
        catch (Exception ex)
        {
            // Bei Netzwerkfehlern in den Offline-Modus wechseln
            Console.WriteLine($"Fehler beim Online-Abruf: {ex.Message}");
            _isOnline = false;
            
            // Debug-Logging wenn Debug-Modus aktiviert ist
            var config = await _configService.GetConfigAsync();
            if (config.IsDebugMode)
            {
                Console.WriteLine($"Detaillierte Fehlerinformationen:");
                Console.WriteLine($"Exception-Typ: {ex.GetType().Name}");
                Console.WriteLine($"Stacktrace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }
            
            // Benutzer mit Toast benachrichtigen
            if (_toastService != null)
            {
                _toastService.ShowWarning("Offline-Modus aktiviert: Keine Verbindung zum Backend");
            }
            
            // Aus dem lokalen Speicher laden
            return await GetOfflineArticlesAsync();
        }
    }

    public async Task<Article?> GetArticleAsync(int id)
    {
        var articles = await GetArticlesAsync();
        return articles.FirstOrDefault(a => a.Id == id);
    }

    public async Task<Article> SaveArticleAsync(Article article)
    {
        try
        {
            // Stelle sicher, dass Styles initialisiert ist
            if (article.Styles == null)
            {
                article.Styles = new Dictionary<string, CellStyle>();
            }
            
            // Erstelle einheitliche Serialisierungsoptionen
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // Serialisiere Styles nur, wenn welche vorhanden sind
            if (article.Styles.Count > 0)
            {
                try
                {
                    var stylesDict = new Dictionary<string, object>();
                    foreach (var style in article.Styles)
                    {
                        stylesDict[style.Key] = new 
                        {
                            Bold = style.Value.Bold,
                            Italic = style.Value.Italic,
                            Color = style.Value.Color ?? "#000000"
                        };
                    }

                    article.StylesJson = JsonSerializer.Serialize(stylesDict, jsonOptions);
                    
                    // Überprüfe, ob gültiges JSON erzeugt wurde
                    if (string.IsNullOrEmpty(article.StylesJson) || article.StylesJson == "{}")
                    {
                        Console.WriteLine("Warnung: StylesJson ist leer oder {} nach der Serialisierung");
                        article.StylesJson = "{}"; // Stelle gültiges JSON sicher
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Fehler beim Serialisieren der Styles: {ex.Message}");
                    article.StylesJson = "{}"; // Fallback zu leerem, gültigem JSON
                }
            }
            else
            {
                // Stelle sicher, dass auch ohne Styles gültiges JSON vorhanden ist
                article.StylesJson = "{}";
            }

            // Debug-Ausgabe
            Console.WriteLine($"Speichere Artikel ID: {article.Id}, Name: {article.Name}");
            Console.WriteLine($"StylesJson: {article.StylesJson}");

            // Zeitstempel setzen
            article.Timestamp = DateTime.UtcNow;
            
            // Wenn wir offline sind, direkt im lokalen Speicher speichern
            if (!_isOnline)
            {
                // Für neue Artikel (ID = 0) eine negative ID zuweisen 
                // (für temporäre Offline-IDs)
                if (article.Id == 0)
                {
                    var offlineArticles = await GetOfflineArticlesAsync();
                    article.Id = GetNextNegativeId(offlineArticles);
                    offlineArticles.Add(article);
                    await SaveArticlesOfflineAsync(offlineArticles);
                    return article;
                }
                else
                {
                    // Vorhandenen Artikel im Offline-Speicher aktualisieren
                    var offlineArticles = await GetOfflineArticlesAsync();
                    var index = offlineArticles.FindIndex(a => a.Id == article.Id);
                    if (index >= 0)
                    {
                        offlineArticles[index] = article;
                        await SaveArticlesOfflineAsync(offlineArticles);
                        return article;
                    }
                    else
                    {
                        throw new Exception($"Artikel mit ID {article.Id} nicht gefunden");
                    }
                }
            }
            else
            {
                // Online-Modus: verwende den injizierten ArticleService
                if (article.Id == 0)
                {
                    return await _articleService.CreateArticleAsync(article);
                }
                else
                {
                    return await _articleService.UpdateArticleAsync(article);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fehler beim Speichern: {ex.Message}");
            
            // Bei Netzwerkfehlern automatisch in den Offline-Modus wechseln
            if (ex is System.Net.Http.HttpRequestException || 
                ex.Message.Contains("Network") || 
                ex.Message.Contains("connect"))
            {
                Console.WriteLine("Netzwerkfehler erkannt - wechsle in den Offline-Modus");
                _isOnline = false;
                
                // Nach dem Umschalten in den Offline-Modus noch einmal versuchen
                return await SaveArticleAsync(article);
            }
            
            throw;
        }
    }

    // Holt die Artikel vom Server ohne den lokalen Cache zu aktualisieren
    public async Task<List<Article>> GetServerArticlesAsync()
    {
        try
        {
            var config = await _configService.GetConfigAsync();
            var response = await _httpClient.GetAsync(config.ApiUrl);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var articles = JsonSerializer.Deserialize<List<Article>>(content, _jsonOptions) 
                ?? new List<Article>();
            
            return articles;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Abrufen der Server-Artikel: {ex.Message}");
            throw;
        }
    }

    // Führt eine detaillierte Synchronisierung mit Konfliktlösung durch
    public async Task<bool> SyncWithResolutionAsync(List<ArticleDifference> resolutions)
    {
        try
        {
            var config = await _configService.GetConfigAsync();
            var apiUrl = config.ApiUrl;
            
            // Liste der aufgetretenen Fehler zur späteren Anzeige
            var errors = new List<string>();
            var successfulSyncs = new List<string>();
            
            // Sammle IDs der erfolgreich synchronisierten Artikel
            var syncedArticleIds = new HashSet<int>();
            
            // 1. Verarbeite alle Entscheidungen
            foreach (var resolution in resolutions)
            {
                try
                {
                    switch (resolution.DifferenceType)
                    {
                        case DifferenceType.NewLocal:
                            if (resolution.Decision == ResolutionDecision.KeepLocal && resolution.LocalArticle != null)
                            {
                                // Neuen lokalen Artikel hochladen
                                var newArticle = new Article
                                {
                                    // ID nicht kopieren, da der Server eine neue ID generiert
                                    Name = resolution.LocalArticle.Name,
                                    Type = resolution.LocalArticle.Type,
                                    Stock = resolution.LocalArticle.Stock,
                                    Unit = resolution.LocalArticle.Unit,
                                    Price = resolution.LocalArticle.Price,
                                    Location = resolution.LocalArticle.Location,
                                    Status = resolution.LocalArticle.Status ?? "Auf Lager",
                                    Link = resolution.LocalArticle.Link,
                                    StylesJson = resolution.LocalArticle.StylesJson,
                                    Styles = resolution.LocalArticle.Styles,
                                    Timestamp = DateTime.UtcNow  // Aktueller Zeitstempel
                                };
                                
                                var createResponse = await _httpClient.PostAsJsonAsync(apiUrl, newArticle);
                                
                                if (createResponse.IsSuccessStatusCode)
                                {
                                    var createdArticle = await createResponse.Content.ReadFromJsonAsync<Article>(_jsonOptions);
                                    if (createdArticle != null)
                                    {
                                        syncedArticleIds.Add(createdArticle.Id);
                                        successfulSyncs.Add($"Artikel '{newArticle.Name}' erfolgreich erstellt (ID: {createdArticle.Id})");
                                    }
                                }
                                else
                                {
                                    var errorMsg = $"Fehler beim Erstellen von '{newArticle.Name}': {createResponse.StatusCode}";
                                    Console.Error.WriteLine(errorMsg);
                                    errors.Add(errorMsg);
                                }
                            }
                            break;
                            
                        case DifferenceType.NewOnServer:
                            if (resolution.Decision == ResolutionDecision.UseServer && resolution.ServerArticle != null)
                            {
                                // Wir müssen nichts hochladen, aber markieren den Artikel als synchronisiert
                                syncedArticleIds.Add(resolution.ServerArticle.Id);
                                successfulSyncs.Add($"Neuer Server-Artikel '{resolution.ServerArticle.Name}' übernommen");
                            }
                            break;
                            
                        case DifferenceType.DeletedOnServer:
                            if (resolution.Decision == ResolutionDecision.KeepLocal && resolution.LocalArticle != null)
                            {
                                // Gelöschten Artikel wiederherstellen (neu erstellen)
                                var restoreArticle = new Article
                                {
                                    // ID nicht kopieren, da der Server eine neue ID generiert
                                    Name = resolution.LocalArticle.Name,
                                    Type = resolution.LocalArticle.Type,
                                    Stock = resolution.LocalArticle.Stock,
                                    Unit = resolution.LocalArticle.Unit,
                                    Price = resolution.LocalArticle.Price,
                                    Location = resolution.LocalArticle.Location,
                                    Status = resolution.LocalArticle.Status ?? "Auf Lager",
                                    Link = resolution.LocalArticle.Link,
                                    StylesJson = resolution.LocalArticle.StylesJson,
                                    Styles = resolution.LocalArticle.Styles,
                                    Timestamp = DateTime.UtcNow  // Aktueller Zeitstempel
                                };
                                
                                var restoreResponse = await _httpClient.PostAsJsonAsync(apiUrl, restoreArticle);
                                
                                if (restoreResponse.IsSuccessStatusCode)
                                {
                                    var restoredArticle = await restoreResponse.Content.ReadFromJsonAsync<Article>(_jsonOptions);
                                    if (restoredArticle != null)
                                    {
                                        syncedArticleIds.Add(restoredArticle.Id);
                                        successfulSyncs.Add($"Artikel '{restoreArticle.Name}' wiederhergestellt (neue ID: {restoredArticle.Id})");
                                    }
                                }
                                else
                                {
                                    var errorMsg = $"Fehler beim Wiederherstellen von '{restoreArticle.Name}': {restoreResponse.StatusCode}";
                                    Console.Error.WriteLine(errorMsg);
                                    errors.Add(errorMsg);
                                }
                            }
                            else if (resolution.Decision == ResolutionDecision.UseServer && resolution.LocalArticle != null)
                            {
                                // Lokalen Artikel löschen (im Cache)
                                if (resolution.LocalArticle.Id > 0)
                                {
                                    syncedArticleIds.Add(resolution.LocalArticle.Id);
                                    successfulSyncs.Add($"Lokal gelöschter Artikel '{resolution.LocalArticle.Name}' (ID: {resolution.LocalArticle.Id})");
                                }
                            }
                            break;
                            
                        case DifferenceType.Modified:
                            if (resolution.Decision == ResolutionDecision.KeepLocal && resolution.LocalArticle != null)
                            {
                                try
                                {
                                    // Erstelle eine Kopie des lokalen Artikels für die Aktualisierung
                                    var updateArticle = new Article
                                    {
                                        Id = resolution.LocalArticle.Id,
                                        Name = resolution.LocalArticle.Name,
                                        Type = resolution.LocalArticle.Type,
                                        Stock = resolution.LocalArticle.Stock,
                                        Unit = resolution.LocalArticle.Unit,
                                        Price = resolution.LocalArticle.Price,
                                        Location = resolution.LocalArticle.Location,
                                        Status = resolution.LocalArticle.Status ?? "Auf Lager",
                                        Link = resolution.LocalArticle.Link,
                                        StylesJson = resolution.LocalArticle.StylesJson,
                                        Styles = resolution.LocalArticle.Styles,
                                        Timestamp = DateTime.UtcNow  // Zeitstempel aktualisieren
                                    };
                                    
                                    // Stelle sicher, dass StylesJson korrekt ist
                                    if (string.IsNullOrEmpty(updateArticle.StylesJson) && updateArticle.Styles != null && updateArticle.Styles.Count > 0)
                                    {
                                        updateArticle.StylesJson = JsonSerializer.Serialize(updateArticle.Styles, _jsonOptions);
                                    }
                                    
                                    Console.WriteLine($"Aktualisiere Artikel: {updateArticle.Id}, Name: {updateArticle.Name}, Timestamp: {updateArticle.Timestamp}");
                                    
                                    // Direktes Update über den ArticleService, der Konflikterkennung bereits implementiert
                                    var updatedArticle = await _articleService.UpdateArticleAsync(updateArticle);
                                    
                                    if (updatedArticle != null)
                                    {
                                        syncedArticleIds.Add(updatedArticle.Id);
                                        successfulSyncs.Add($"Artikel '{updatedArticle.Name}' (ID: {updatedArticle.Id}) erfolgreich aktualisiert");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Versuche ein Force-Update ohne Konfliktprüfung
                                    try
                                    {
                                        Console.WriteLine($"Versuche Force-Update für Artikel {resolution.LocalArticle.Id}");
                                        
                                        // Setze Timestamp in Zukunft
                                        resolution.LocalArticle.Timestamp = DateTime.UtcNow.AddSeconds(1);
                                        
                                        // Stelle sicher, dass StylesJson korrekt ist
                                        if (string.IsNullOrEmpty(resolution.LocalArticle.StylesJson) && 
                                            resolution.LocalArticle.Styles != null && 
                                            resolution.LocalArticle.Styles.Count > 0)
                                        {
                                            resolution.LocalArticle.StylesJson = JsonSerializer.Serialize(
                                                resolution.LocalArticle.Styles, _jsonOptions);
                                        }
                                        
                                        var forceUpdateResponse = await _httpClient.PutAsJsonAsync(
                                            $"{apiUrl}/{resolution.LocalArticle.Id}", resolution.LocalArticle);
                                        
                                        if (forceUpdateResponse.IsSuccessStatusCode)
                                        {
                                            syncedArticleIds.Add(resolution.LocalArticle.Id);
                                            successfulSyncs.Add($"Artikel '{resolution.LocalArticle.Name}' (ID: {resolution.LocalArticle.Id}) mit Force-Update aktualisiert");
                                        }
                                        else
                                        {
                                            var errorContent = await forceUpdateResponse.Content.ReadAsStringAsync();
                                            var errorMsg = $"Fehler beim Force-Update von '{resolution.LocalArticle.Name}': {forceUpdateResponse.StatusCode} - {errorContent}";
                                            Console.Error.WriteLine(errorMsg);
                                            errors.Add(errorMsg);
                                        }
                                    }
                                    catch (Exception forceEx)
                                    {
                                        var errorMsg = $"Fehler beim Force-Update von '{resolution.LocalArticle.Name}': {forceEx.Message}";
                                        Console.Error.WriteLine(errorMsg);
                                        errors.Add(errorMsg);
                                    }
                                }
                            }
                            else if (resolution.Decision == ResolutionDecision.UseServer && resolution.ServerArticle != null)
                            {
                                // Server-Version übernehmen
                                syncedArticleIds.Add(resolution.ServerArticle.Id);
                                successfulSyncs.Add($"Server-Version von Artikel '{resolution.ServerArticle.Name}' (ID: {resolution.ServerArticle.Id}) übernommen");
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Fehler bei der Verarbeitung eines Artikels: {ex.Message}";
                    Console.Error.WriteLine(errorMsg);
                    errors.Add(errorMsg);
                }
            }
            
            // 2. Aktualisiere nur die tatsächlich synchronisierten Artikel im lokalen Cache
            try 
            {
                if (syncedArticleIds.Count > 0) 
                {
                    // Lade nur die synchronisierten Artikel vom Server
                    var currentOfflineArticles = await GetOfflineArticlesAsync();
                    var updatedArticles = new List<Article>();
                    
                    foreach (var id in syncedArticleIds.Where(id => id > 0)) // Ignoriere negative IDs (lokale Entwürfe)
                    {
                        try 
                        {
                            // Artikel vom Server abrufen
                            var response = await _httpClient.GetAsync($"{apiUrl}/{id}");
                            
                            if (response.IsSuccessStatusCode)
                            {
                                var updatedArticle = await response.Content.ReadFromJsonAsync<Article>(_jsonOptions);
                                if (updatedArticle != null)
                                {
                                    updatedArticles.Add(updatedArticle);
                                }
                            }
                            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                            {
                                // Artikel wurde auf dem Server gelöscht
                                currentOfflineArticles.RemoveAll(a => a.Id == id);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Fehler beim Abrufen von Artikel {id}: {ex.Message}");
                        }
                    }
                    
                    // Aktualisiere oder füge die synchronisierten Artikel hinzu
                    foreach (var updatedArticle in updatedArticles)
                    {
                        var existingIndex = currentOfflineArticles.FindIndex(a => a.Id == updatedArticle.Id);
                        if (existingIndex >= 0)
                        {
                            // Ersetze vorhandenen Artikel
                            currentOfflineArticles[existingIndex] = updatedArticle;
                        }
                        else
                        {
                            // Füge neuen Artikel hinzu
                            currentOfflineArticles.Add(updatedArticle);
                        }
                    }
                    
                    // Entferne lokale Artikel mit negativer ID, die synchronisiert wurden 
                    // (diese haben jetzt Server-IDs und wurden bereits hinzugefügt)
                    currentOfflineArticles.RemoveAll(a => a.Id < 0 && successfulSyncs.Any(s => s.Contains(a.Name) && s.Contains("erfolgreich erstellt")));
                    
                    // Speichere den aktualisierten Cache
                    await SaveArticlesOfflineAsync(currentOfflineArticles);
                    
                    Console.WriteLine($"Lokaler Cache mit {updatedArticles.Count} synchronisierten Artikeln aktualisiert");
                }
                else
                {
                    Console.WriteLine("Keine Artikel wurden synchronisiert.");
                }
                
                // 3. Online-Status setzen
                _isOnline = true;
                
                // 4. Zusammenfassung ausgeben
                if (errors.Count > 0)
                {
                    Console.Error.WriteLine($"Es gab {errors.Count} Probleme bei der Synchronisierung.");
                    foreach (var error in errors)
                    {
                        Console.Error.WriteLine($"- {error}");
                    }
                }
                
                if (successfulSyncs.Count > 0)
                {
                    Console.WriteLine($"{successfulSyncs.Count} Artikel erfolgreich synchronisiert:");
                    foreach (var success in successfulSyncs)
                    {
                        Console.WriteLine($"+ {success}");
                    }
                }
                
                return errors.Count == 0 || successfulSyncs.Count > 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Fehler bei der Aktualisierung des lokalen Caches: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fehler bei der Synchronisierung mit Konfliktlösung: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            _isOnline = false;
            return false;
        }
    }

    // Importiere Artikel und setze den Offline-Modus
    public async Task ImportArticlesAsync(List<Article> articles)
    {
        // Setze den Offline-Modus
        _isOnline = false;
        
        try
        {
            // Stelle sicher, dass alle Styles korrekt serialisiert sind
            foreach (var article in articles)
            {
                if (string.IsNullOrEmpty(article.StylesJson) && article.Styles != null && article.Styles.Count > 0)
                {
                    article.StylesJson = JsonSerializer.Serialize(article.Styles, _jsonOptions);
                }
            }
            
            // Speichere die Artikel direkt im Offline-Speicher
            await SaveArticlesOfflineAsync(articles);
            
            Console.WriteLine($"{articles.Count} Artikel im Offline-Modus importiert");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fehler beim Importieren der Artikel: {ex.Message}");
            Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }
    public async Task DeleteArticleAsync(int id)
    {
        if (_isOnline)
        {
            try
            {
                var config = await _configService.GetConfigAsync();
                var response = await _httpClient.DeleteAsync($"{config.ApiUrl}/{id}");
                response.EnsureSuccessStatusCode();
                
                // Aktualisiere den Offline-Cache
                var articles = await GetOfflineArticlesAsync();
                articles.RemoveAll(a => a.Id == id);
                await SaveArticlesOfflineAsync(articles);
                
                return;
            }
            catch (Exception ex)
            {
                _isOnline = false;
                Console.WriteLine($"Fehler beim Löschen: {ex.Message}");
            }
        }

        // Offline-Löschung
        var offlineArticles = await GetOfflineArticlesAsync();
        offlineArticles.RemoveAll(a => a.Id == id);
        await SaveArticlesOfflineAsync(offlineArticles);
    }

    public async Task SyncWithBackendAsync()
    {
        try
        {
            var config = await _configService.GetConfigAsync();
            
            // Versuche, eine Verbindung zum Backend herzustellen
            var response = await _httpClient.GetAsync(config.ApiUrl);
            response.EnsureSuccessStatusCode();
            
            var onlineArticles = await response.Content.ReadFromJsonAsync<List<Article>>(_jsonOptions);
            var offlineArticles = await GetOfflineArticlesAsync();
            
            // Temporäre IDs (negative) identifizieren neue Artikel im Offline-Modus
            var newArticles = offlineArticles
                .Where(a => a.Id < 0)
                .ToList();
            
            // Synchronisiere neue Artikel
            foreach (var newArticle in newArticles)
            {
                var articleToCreate = new Article
                {
                    Name = newArticle.Name,
                    Type = newArticle.Type,
                    Stock = newArticle.Stock,
                    Unit = newArticle.Unit,
                    Price = newArticle.Price,
                    Location = newArticle.Location,
                    Status = newArticle.Status,
                    Link = newArticle.Link,
                    StylesJson = newArticle.StylesJson,
                    Styles = newArticle.Styles
                };
                
                var createResponse = await _httpClient.PostAsJsonAsync(config.ApiUrl, articleToCreate);
                createResponse.EnsureSuccessStatusCode();
            }
            
            // Hole die aktuellen Artikel vom Server
            response = await _httpClient.GetAsync(config.ApiUrl);
            response.EnsureSuccessStatusCode();
            
            onlineArticles = await response.Content.ReadFromJsonAsync<List<Article>>(_jsonOptions);
            
            // Cache aktualisieren
            await SaveArticlesOfflineAsync(onlineArticles);
            
            _isOnline = true;
        }
        catch (Exception ex)
        {
            _isOnline = false;
            Console.WriteLine($"Fehler bei der Synchronisierung: {ex.Message}");
        }
    }

    public async Task<bool> VerifyDataIntegrityAsync()
    {
        var articlesJson = await _localStorage.GetItemAsStringAsync(ARTICLES_KEY);
        if (string.IsNullOrEmpty(articlesJson))
            return true; // Keine Daten vorhanden ist valid
            
        var savedHash = await _localStorage.GetItemAsync<string>(HASH_KEY);
        if (string.IsNullOrEmpty(savedHash))
            return false; // Hash sollte vorhanden sein, wenn Daten existieren
            
        var computedHash = ComputeHash(articlesJson);
        return savedHash == computedHash;
    }

    private async Task<List<Article>> GetOfflineArticlesAsync()
    {
        try
        {
            if (await _localStorage.ContainKeyAsync(ARTICLES_KEY))
            {
                // JSON-String direkt abrufen
                var json = await _localStorage.GetItemAsStringAsync(ARTICLES_KEY);
                Console.WriteLine($"Aus localStorage abgerufen: {json.Length} Zeichen");
                
                if (string.IsNullOrEmpty(json))
                {
                    Console.WriteLine("Warnung: Leeres JSON aus localStorage abgerufen");
                    return new List<Article>();
                }
                
                // JSON-String direkt parsen
                var articles = JsonSerializer.Deserialize<List<Article>>(json, _jsonOptions);
                if (articles != null)
                {
                    Console.WriteLine($"{articles.Count} Artikel erfolgreich aus localStorage deserialisiert");
                    return articles;
                }
                else
                {
                    Console.WriteLine("Warnung: Deserialisierung ergab eine leere Artikelliste");
                    return new List<Article>();
                }
            }
            else
            {
                Console.WriteLine("Keine Artikel im localStorage gefunden");
                return new List<Article>();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fehler beim Abrufen aus localStorage: {ex.Message}");
            Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            return new List<Article>();
        }
    }

    private async Task SaveArticlesOfflineAsync(List<Article> articles)
    {
        try
        {
            // Stelle sicher, dass Styles für jeden Artikel korrekt serialisiert sind
            foreach (var article in articles)
            {
                if (string.IsNullOrEmpty(article.StylesJson) && article.Styles != null && article.Styles.Count > 0)
                {
                    article.StylesJson = JsonSerializer.Serialize(article.Styles, _jsonOptions);
                }
            }
            
            // Serialisiere die Artikelliste
            var json = JsonSerializer.Serialize(articles, _jsonOptions);
            Console.WriteLine($"Speichere in localStorage: {json.Length} Zeichen");
            
            // Speichere den JSON-String
            await _localStorage.SetItemAsStringAsync(ARTICLES_KEY, json);
            
            // Überprüfe, dass die Daten korrekt gespeichert wurden
            var storedJson = await _localStorage.GetItemAsStringAsync(ARTICLES_KEY);
            Console.WriteLine($"Speicherung verifiziert: {storedJson.Length} Zeichen");
            
            // Berechne und speichere den Hash
            var hash = ComputeHash(json);
            await _localStorage.SetItemAsStringAsync(HASH_KEY, hash);
            
            Console.WriteLine($"{articles.Count} Artikel mit Hash im localStorage gespeichert");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fehler beim Speichern im localStorage: {ex.Message}");
            Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            throw; // Wirf den Fehler weiter, um ihn sichtbar zu machen
        }
    }

    private string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    // Fix me kein aurfuf
    private int GetNextNegativeId(List<Article> articles)
    {
        var minId = articles
            .Where(a => a.Id < 0)
            .DefaultIfEmpty(new Article { Id = 0 })
            .Min(a => a.Id);
            
        return minId < 0 ? minId - 1 : -1;
    }

    public async Task ResetToServerDataAsync()
    {
        try
        {
            Console.WriteLine("Lade Daten direkt vom Server...");
            var config = await _configService.GetConfigAsync();
            var response = await _httpClient.GetAsync(config.ApiUrl);
            response.EnsureSuccessStatusCode();
            
            var serverArticles = await response.Content.ReadFromJsonAsync<List<Article>>(_jsonOptions) ?? new List<Article>();
            Console.WriteLine($"{serverArticles.Count} Artikel vom Server geladen");
            
            // Speichere die Artikel direkt im Offline-Speicher
            await SaveArticlesOfflineAsync(serverArticles);
            Console.WriteLine("Artikel im Offline-Speicher aktualisiert");
            
            // Verbindungsstatus auf "online" setzen
            _isOnline = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Laden der Serverdaten: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }
    
}