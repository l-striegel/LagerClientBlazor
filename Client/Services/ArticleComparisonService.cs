using System.Text;
using System.Text.Json;
using LagerClient.Blazor.Shared.Models;

namespace LagerClient.Blazor.Client.Services;

public class ArticleComparisonService
{
    private readonly JsonSerializerOptions _jsonOptions;

    public ArticleComparisonService()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    // Vergleicht zwei Listen von Artikeln und gibt die Unterschiede zurück
    public List<ArticleDifference> CompareArticles(List<Article> localArticles, List<Article> serverArticles)
    {
        var differences = new List<ArticleDifference>();
        
        // Erstelle Lookup-Tabellen für schnellen Zugriff
        var localById = localArticles.ToDictionary(a => a.Id);
        var serverById = serverArticles.ToDictionary(a => a.Id);
        
        // 1. Prüfe alle lokalen Artikel
        foreach (var localArticle in localArticles)
        {
            // Artikel mit negativer ID sind neue lokale Artikel
            if (localArticle.Id < 0)
            {
                differences.Add(new ArticleDifference
                {
                    LocalArticle = localArticle,
                    ServerArticle = null,
                    DifferenceType = DifferenceType.NewLocal,
                    ChangedProperties = new List<string> { "Neuer Artikel" }
                });
                continue;
            }
            
            // Prüfe, ob der Artikel auf dem Server existiert
            if (!serverById.TryGetValue(localArticle.Id, out var serverArticle))
            {
                differences.Add(new ArticleDifference
                {
                    LocalArticle = localArticle,
                    ServerArticle = null,
                    DifferenceType = DifferenceType.DeletedOnServer,
                    ChangedProperties = new List<string> { "Auf Server gelöscht" }
                });
                continue;
            }
            
            // Vergleiche die Artikel-Eigenschaften
            var changedProps = GetChangedProperties(localArticle, serverArticle);
            
            if (changedProps.Count > 0)
            {
                differences.Add(new ArticleDifference
                {
                    LocalArticle = localArticle,
                    ServerArticle = serverArticle,
                    DifferenceType = DifferenceType.Modified,
                    ChangedProperties = changedProps
                });
            }
        }
        
        // 2. Prüfe, ob es Artikel auf dem Server gibt, die lokal nicht existieren
        foreach (var serverArticle in serverArticles)
        {
            if (!localById.ContainsKey(serverArticle.Id))
            {
                differences.Add(new ArticleDifference
                {
                    LocalArticle = null,
                    ServerArticle = serverArticle,
                    DifferenceType = DifferenceType.NewOnServer,
                    ChangedProperties = new List<string> { "Neu auf Server" }
                });
            }
        }
        
        return differences;
    }
    
    // Vergleicht zwei Artikel und gibt die geänderten Eigenschaften zurück
    private List<string> GetChangedProperties(Article localArticle, Article serverArticle)
    {
        var changes = new List<string>();
        
        // Vergleiche Content-relevante Eigenschaften
        if (localArticle.Name != serverArticle.Name)
            changes.Add("Name");
            
        if (localArticle.Type != serverArticle.Type)
            changes.Add("Typ");
            
        if (localArticle.Stock != serverArticle.Stock)
            changes.Add("Bestand");
            
        if (localArticle.Unit != serverArticle.Unit)
            changes.Add("Einheit");
            
        if (localArticle.Price != serverArticle.Price)
            changes.Add("Preis");
            
        if (localArticle.Location != serverArticle.Location)
            changes.Add("Lagerplatz");
            
        if (localArticle.Status != serverArticle.Status)
            changes.Add("Status");
            
        if (localArticle.Link != serverArticle.Link)
            changes.Add("Link");
            
        // Vergleiche Formatierungen (nur ob es Änderungen gibt, nicht Details)
        bool hasStylingChanges = HasStylingChanges(localArticle, serverArticle);
        if (hasStylingChanges)
            changes.Add("Formatierung");
            
        return changes;
    }
    
    // Prüft, ob sich die Formatierungen geändert haben
    private bool HasStylingChanges(Article localArticle, Article serverArticle)
    {
        // Erstelle normalisierte JSON-Strings für die Styles
        var localStylesJson = NormalizeStylesJson(localArticle.Styles);
        var serverStylesJson = NormalizeStylesJson(serverArticle.Styles);
        
        // Vergleiche die normalisierten JSON-Strings
        return localStylesJson != serverStylesJson;
    }
    
    // Normalisiert ein Styles-Dictionary zu einem sortierten JSON-String für konsistenten Vergleich
    private string NormalizeStylesJson(Dictionary<string, CellStyle> styles)
    {
        if (styles == null || styles.Count == 0)
            return "{}";
            
        try
        {
            var sortedStyles = new SortedDictionary<string, CellStyle>(styles);
            return JsonSerializer.Serialize(sortedStyles, _jsonOptions);
        }
        catch
        {
            return "{}";
        }
    }
    
    // Berechnet einen Hash für einen Artikel (ohne Berücksichtigung der Formatierung)
    public string ComputeArticleContentHash(Article article)
    {
        var contentBuilder = new StringBuilder();
        contentBuilder.Append(article.Id)
            .Append('|').Append(article.Name)
            .Append('|').Append(article.Type)
            .Append('|').Append(article.Stock)
            .Append('|').Append(article.Unit)
            .Append('|').Append(article.Price)
            .Append('|').Append(article.Location)
            .Append('|').Append(article.Status)
            .Append('|').Append(article.Link);
            
        var content = contentBuilder.ToString();
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hashBytes);
    }
}

// Modell für Artikel-Unterschiede
public class ArticleDifference
{
    public Article? LocalArticle { get; set; }
    public Article? ServerArticle { get; set; }
    public DifferenceType DifferenceType { get; set; }
    public List<string> ChangedProperties { get; set; } = new List<string>();
    
    // Entscheidung, welcher Artikel verwendet werden soll
    public ResolutionDecision Decision { get; set; } = ResolutionDecision.Undecided;
}

// Typen von Unterschieden
public enum DifferenceType
{
    NewLocal,        // Neuer Artikel (lokal)
    NewOnServer,     // Neuer Artikel (Server)
    Modified,        // Artikel geändert
    DeletedOnServer  // Lokal existiert, aber auf dem Server gelöscht
}

// Entscheidungen zur Konfliktlösung
public enum ResolutionDecision
{
    Undecided,      // Noch keine Entscheidung getroffen
    KeepLocal,      // Lokale Version behalten
    UseServer,      // Server-Version verwenden
    Merge           // Zusammenführen (für zukünftige Erweiterungen)
}