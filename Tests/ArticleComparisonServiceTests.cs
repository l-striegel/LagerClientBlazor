using LagerClient.Blazor.Client.Services;
using LagerClient.Blazor.Shared.Models;
using Xunit;

namespace LagerClient.Blazor.Tests
{
    public class ArticleComparisonServiceTests
    {
        [Fact]
        public void CompareArticles_DetectsNewLocalArticles()
        {
            // Arrange
            var service = new ArticleComparisonService();
            var localArticles = new List<Article>
            {
                new Article { Id = -1, Name = "New Local Article" },
                new Article { Id = 1, Name = "Existing Article" }
            };
            
            var serverArticles = new List<Article>
            {
                new Article { Id = 1, Name = "Existing Article" }
            };
            
            // Act
            var differences = service.CompareArticles(localArticles, serverArticles);
            
            // Assert
            Assert.Single(differences.Where(d => d.DifferenceType == DifferenceType.NewLocal));
            var newLocalArticle = differences.First(d => d.DifferenceType == DifferenceType.NewLocal);
            Assert.Equal(-1, newLocalArticle.LocalArticle?.Id);
            Assert.Equal("New Local Article", newLocalArticle.LocalArticle?.Name);
        }
        
        [Fact]
        public void CompareArticles_DetectsNewServerArticles()
        {
            // Arrange
            var service = new ArticleComparisonService();
            var localArticles = new List<Article>
            {
                new Article { Id = 1, Name = "Existing Article" }
            };
            
            var serverArticles = new List<Article>
            {
                new Article { Id = 1, Name = "Existing Article" },
                new Article { Id = 2, Name = "New Server Article" }
            };
            
            // Act
            var differences = service.CompareArticles(localArticles, serverArticles);
            
            // Assert
            Assert.Single(differences.Where(d => d.DifferenceType == DifferenceType.NewOnServer));
            var newServerArticle = differences.First(d => d.DifferenceType == DifferenceType.NewOnServer);
            Assert.Equal(2, newServerArticle.ServerArticle?.Id);
            Assert.Equal("New Server Article", newServerArticle.ServerArticle?.Name);
        }
        
        [Fact]
        public void CompareArticles_DetectsModifiedArticles()
        {
            // Arrange
            var service = new ArticleComparisonService();
            var localArticles = new List<Article>
            {
                new Article { Id = 1, Name = "Modified Name", Stock = 20, Type = "Electronics" }
            };
            
            var serverArticles = new List<Article>
            {
                new Article { Id = 1, Name = "Original Name", Stock = 10, Type = "Electronics" }
            };
            
            // Act
            var differences = service.CompareArticles(localArticles, serverArticles);
            
            // Assert
            Assert.Single(differences.Where(d => d.DifferenceType == DifferenceType.Modified));
            var modifiedArticle = differences.First(d => d.DifferenceType == DifferenceType.Modified);
            Assert.Equal(1, modifiedArticle.LocalArticle?.Id);
            Assert.Contains("Name", modifiedArticle.ChangedProperties);
            Assert.Contains("Bestand", modifiedArticle.ChangedProperties);
            Assert.DoesNotContain("Typ", modifiedArticle.ChangedProperties);
        }
        
        [Fact]
        public void CompareArticles_DetectsDeletedOnServerArticles()
        {
            // Arrange
            var service = new ArticleComparisonService();
            var localArticles = new List<Article>
            {
                new Article { Id = 1, Name = "Existing Article" },
                new Article { Id = 2, Name = "Deleted Article" }
            };
            
            var serverArticles = new List<Article>
            {
                new Article { Id = 1, Name = "Existing Article" }
            };
            
            // Act
            var differences = service.CompareArticles(localArticles, serverArticles);
            
            // Assert
            Assert.Single(differences.Where(d => d.DifferenceType == DifferenceType.DeletedOnServer));
            var deletedArticle = differences.First(d => d.DifferenceType == DifferenceType.DeletedOnServer);
            Assert.Equal(2, deletedArticle.LocalArticle?.Id);
            Assert.Equal("Deleted Article", deletedArticle.LocalArticle?.Name);
        }
        
        [Fact]
        public void ComputeArticleContentHash_GeneratesConsistentHashes()
        {
            // Arrange
            var service = new ArticleComparisonService();
            var article1 = new Article 
            { 
                Id = 1, 
                Name = "Test Article", 
                Type = "Electronics",
                Stock = 10,
                Unit = "pcs",
                Price = 19.99m,
                Location = "Warehouse A",
                Status = "In Stock",
                Link = "https://example.com"
            };
            
            var article2 = new Article 
            { 
                Id = 1, 
                Name = "Test Article", 
                Type = "Electronics",
                Stock = 10,
                Unit = "pcs",
                Price = 19.99m,
                Location = "Warehouse A",
                Status = "In Stock",
                Link = "https://example.com"
            };
            
            // Act
            var hash1 = service.ComputeArticleContentHash(article1);
            var hash2 = service.ComputeArticleContentHash(article2);
            
            // Assert
            Assert.Equal(hash1, hash2);
            
            // Modify one property and verify hash changes
            article2.Stock = 15;
            var hash3 = service.ComputeArticleContentHash(article2);
            Assert.NotEqual(hash1, hash3);
        }
    }
}