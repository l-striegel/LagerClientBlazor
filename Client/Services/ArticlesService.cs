using System.Net.Http.Json;
using System.Text.Json;
using LagerClient.Blazor.Shared.Models;

namespace LagerClient.Blazor.Client.Services;

public interface IArticleService
{
    Task<List<Article>> GetArticlesAsync();
    Task<Article?> GetArticleAsync(int id);
    Task<Article> CreateArticleAsync(Article article);
    Task<Article> UpdateArticleAsync(Article article);
    Task DeleteArticleAsync(int id);
}

public class ArticleService : IArticleService
{
    private readonly HttpClient _httpClient;
    private readonly AppConfigService _configService;
    private readonly JsonSerializerOptions _jsonOptions;

    public ArticleService(HttpClient httpClient, AppConfigService configService)
    {
        _httpClient = httpClient;
        _configService = configService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private async Task<string> GetApiUrlAsync()
    {
        var config = await _configService.GetConfigAsync();
        return config.ApiUrl;
    }

    public async Task<List<Article>> GetArticlesAsync()
    {
        var apiUrl = await GetApiUrlAsync();
        var response = await _httpClient.GetAsync(apiUrl);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var articles = JsonSerializer.Deserialize<List<Article>>(content, _jsonOptions);
        return articles ?? new List<Article>();
    }

    public async Task<Article?> GetArticleAsync(int id)
    {
        var apiUrl = await GetApiUrlAsync();
        var response = await _httpClient.GetAsync($"{apiUrl}/{id}");
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<Article>(_jsonOptions);
    }

    public async Task<Article> CreateArticleAsync(Article article)
    {
        var apiUrl = await GetApiUrlAsync();
        
        // Stelle sicher, dass die Styles richtig serialisiert werden
        PrepareArticleForSaving(article);
        
        var response = await _httpClient.PostAsJsonAsync(apiUrl, article);
        response.EnsureSuccessStatusCode();
        
        return (await response.Content.ReadFromJsonAsync<Article>(_jsonOptions))!;
    }

    public async Task<Article> UpdateArticleAsync(Article article)
    {
        var apiUrl = await GetApiUrlAsync();

        // Artikel für das Speichern vorbereiten mit korrekter Serialisierung
        PrepareArticleForSaving(article);

        // Für Debugging-Zwecke
        Console.WriteLine($"Aktualisiere Artikel: {article.Id}");
        Console.WriteLine($"StylesJson: {article.StylesJson}");
        
        var response = await _httpClient.PutAsJsonAsync($"{apiUrl}/{article.Id}", article);
        
        // Für Debugging-Zwecke
        Console.WriteLine($"Antwort-Status: {response.StatusCode}");
        
        response.EnsureSuccessStatusCode();
        
        // Prüfen, ob der Server Daten zurückgibt
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            // Bei NoContent (204) gibt es keinen Body - wir geben das Original zurück
            Console.WriteLine("Server hat NoContent (204) zurückgegeben, Original-Artikel wird zurückgegeben");
            return article;
        }
        
        // Bei anderen Erfolgsstatuscodes versuchen wir, die Antwort zu deserialisieren
        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Antwort-Body: {responseBody}");
        
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            // Leerer Body - Original zurückgeben
            Console.WriteLine("Leerer Antwort-Body, Original-Artikel wird zurückgegeben");
            return article;
        }
        
        try
        {
            var updatedArticle = JsonSerializer.Deserialize<Article>(responseBody, _jsonOptions);
            return updatedArticle ?? article;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Deserialisieren der Antwort: {ex.Message}");
            // Bei Deserialisierungsfehler geben wir das Original zurück
            return article;
        }
    }

    public async Task DeleteArticleAsync(int id)
    {
        var apiUrl = await GetApiUrlAsync();
        var response = await _httpClient.DeleteAsync($"{apiUrl}/{id}");
        response.EnsureSuccessStatusCode();
    }
    
    // Hilfsmethode zur Vorbereitung eines Artikels fürs Speichern
    private void PrepareArticleForSaving(Article article)
    {
        // Sicherstellen, dass Styles initialisiert ist
        if (article.Styles == null)
        {
            article.Styles = new Dictionary<string, CellStyle>();
        }
        
        // Einheitliche Serialisierungsoptionen erstellen
        var jsonOptions = new JsonSerializerOptions
        { 
            PropertyNamingPolicy = null,
            WriteIndented = false
        };
        
        // Styles nur serialisieren, wenn welche vorhanden sind
        if (article.Styles.Count > 0)
        {
            try
            {
                article.StylesJson = JsonSerializer.Serialize(article.Styles, jsonOptions);
                
                // Überprüfen, ob wir gültiges JSON haben
                if (string.IsNullOrEmpty(article.StylesJson) || article.StylesJson == "{}")
                {
                    Console.WriteLine("Warnung: StylesJson ist leer oder {} nach der Serialisierung");
                }
                else
                {
                    // Testweise deserialisieren zur Überprüfung der Gültigkeit
                    var test = JsonSerializer.Deserialize<Dictionary<string, CellStyle>>(article.StylesJson, jsonOptions);
                    if (test == null || test.Count == 0)
                    {
                        Console.WriteLine("Warnung: Deserialisiertes Styles-Dictionary ist leer");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Fehler beim Serialisieren der Styles: {ex.Message}");
                // Standard-JSON bereitstellen, falls die Serialisierung fehlschlägt
                article.StylesJson = "{}";
            }
        }
        else
        {
            // Sicherstellen, dass wir gültiges JSON haben, auch ohne Styles
            article.StylesJson = "{}";
        }
        
        // Zeitstempel auf aktuelle UTC-Zeit setzen (wie im Java-Client)
        article.Timestamp = DateTime.UtcNow;
    }
}