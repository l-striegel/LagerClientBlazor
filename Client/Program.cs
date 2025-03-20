using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.LocalStorage;
using Blazored.Modal;
using Blazored.Toast;
using LagerClient.Blazor.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<LagerClient.Blazor.Client.App>("#app");

builder.RootComponents.Add<HeadOutlet>("head::after");

// HTTP Client
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Blazored Services
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredModal();
builder.Services.AddBlazoredToast();

// Eigene Services
builder.Services.AddScoped<ModalServiceWrapper>();
builder.Services.AddSingleton<AppStateService>();
builder.Services.AddSingleton<IAppStateService>(sp => sp.GetRequiredService<AppStateService>());
builder.Services.AddScoped<AppConfigService>();
builder.Services.AddScoped<IAppConfigService, AppConfigService>();
builder.Services.AddScoped<IOfflineArticleService, OfflineArticleService>();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<IOfflineArticleService, OfflineArticleService>();
builder.Services.AddScoped<IDataExportImportService, DataExportImportService>();



await builder.Build().RunAsync();