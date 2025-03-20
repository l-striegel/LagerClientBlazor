// CellFormattingToolbarTests.cs
using Bunit;
using Client.Pages;
using LagerClient.Blazor.Client.Services;
using LagerClient.Blazor.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Tests
{
    public class CellFormattingToolbarTests : TestContext
    {
        [Fact]
        public void CellFormattingToolbar_RenderCorrectly()
        {
            // Arrange
            var mockArticleService = new Mock<IOfflineArticleService>();
            var mockToastService = new Mock<Blazored.Toast.Services.IToastService>();
            
            Services.AddSingleton(mockArticleService.Object);
            Services.AddSingleton(mockToastService.Object);
            
            var selectedCells = new List<(Article Article, string Property)>
            {
                (new Article { Id = 1, Name = "Test Article" }, "Name")
            };
            
            // Act
            var cut = RenderComponent<CellFormattingToolbar>(parameters => parameters
                .Add(p => p.SelectedCells, selectedCells)
                .Add(p => p.IsDebugMode, true)
                .Add(p => p.SingleSelectedArticle, selectedCells[0].Article)
                .Add(p => p.SingleSelectedProperty, selectedCells[0].Property)
            );
            
            // Assert
            Assert.Contains("B</strong>", cut.Markup);
            Assert.Contains("I</i>", cut.Markup);
            Assert.Contains("Farbe:", cut.Markup);
            Assert.Contains("Zelle ausgewählt", cut.Markup);
        }
        
        [Fact]
        public void CellFormattingToolbar_DisableButtons_WhenNoSelection()
        {
            // Arrange
            var mockArticleService = new Mock<IOfflineArticleService>();
            var mockToastService = new Mock<Blazored.Toast.Services.IToastService>();
            
            Services.AddSingleton(mockArticleService.Object);
            Services.AddSingleton(mockToastService.Object);
            
            var selectedCells = new List<(Article Article, string Property)>();
            
            // Act
            var cut = RenderComponent<CellFormattingToolbar>(parameters => parameters
                .Add(p => p.SelectedCells, selectedCells)
                .Add(p => p.IsDebugMode, false)
            );
            
            // Assert
            Assert.Contains("disabled", cut.Markup);
            Assert.DoesNotContain("Zelle ausgewählt", cut.Markup);
        }
    }
}