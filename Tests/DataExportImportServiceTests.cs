using LagerClient.Blazor.Client.Services;
using LagerClient.Blazor.Shared.Models;
using Microsoft.JSInterop;
using Moq;
using System.Text.Json;
using Xunit;
using Blazored.Toast.Services;
using Microsoft.Extensions.Configuration;
using Blazored.Modal.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tests
{
    public class DataExportImportServiceTests
    {
        [Fact]
        public async Task ExportDataToJson_ShouldCallJsInterop()
        {
            // Arrange
            var mockArticleService = new Mock<IOfflineArticleService>();
            var mockJsRuntime = new Mock<IJSRuntime>();
            var mockToastService = new Mock<IToastService>();
            var mockConfigService = new Mock<IAppConfigService>();
            var mockModalService = new Mock<ModalServiceWrapper>(Mock.Of<IModalService>());
            var mockAppStateService = new Mock<IAppStateService>();
            
            mockConfigService.Setup(m => m.GetConfigAsync())
                .ReturnsAsync(new AppConfig { IsDebugMode = false });
            
            mockArticleService.Setup(m => m.GetArticlesAsync())
                .ReturnsAsync(new List<Article> { new Article { Id = 1, Name = "Test Article" } });
            
            var service = new DataExportImportService(
                mockArticleService.Object,
                mockJsRuntime.Object,
                mockToastService.Object,
                mockConfigService.Object as AppConfigService,
                mockModalService.Object,
                mockAppStateService.Object);
            
            // Act
            await service.ExportDataToJson();
            
            // Assert
            mockJsRuntime.Verify(m => m.InvokeAsync<object>("downloadFile", It.IsAny<object[]>()), Times.Once);
            mockToastService.Verify(m => m.ShowSuccess(It.IsAny<string>(), null), Times.Once);
        }
        
        [Fact]
        public async Task ImportDataFromJson_ShouldReturnNull_WhenEmptyContent()
        {
            // Arrange
            var mockArticleService = new Mock<IOfflineArticleService>();
            var mockJsRuntime = new Mock<IJSRuntime>();
            var mockToastService = new Mock<IToastService>();
            var mockConfigService = new Mock<IAppConfigService>();
            var mockModalService = new Mock<ModalServiceWrapper>(Mock.Of<IModalService>());
            var mockAppStateService = new Mock<IAppStateService>();
            
            mockConfigService.Setup(m => m.GetConfigAsync())
                .ReturnsAsync(new AppConfig { IsDebugMode = false });
            
            mockJsRuntime.Setup(m => m.InvokeAsync<string>("importFile", It.IsAny<object[]>()))
                .ReturnsAsync(null as string);
            
            var service = new DataExportImportService(
                mockArticleService.Object,
                mockJsRuntime.Object,
                mockToastService.Object,
                mockConfigService.Object as AppConfigService,
                mockModalService.Object,
                mockAppStateService.Object);
            
            // Act
            var result = await service.ImportDataFromJson();
            
            // Assert
            Assert.Null(result);
            mockToastService.Verify(m => m.ShowWarning(It.IsAny<string>(), null), Times.Once);
        }
    }
}