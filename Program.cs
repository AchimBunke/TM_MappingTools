using GBX.NET;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TM_GenericMapping.Common;
using TM_MappingTools;
using TM_MappingTools.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<SessionStorage>();

builder.Services.AddSingleton<FileService>();
builder.Services.AddSingleton<ClipFileService>();
builder.Services.AddSingleton<MapFileService>();
builder.Services.AddSingleton<ItemFileService>();
builder.Services.AddSingleton<FileTypeManager>();
builder.Services.AddSingleton<OperationHistoryService>();

GbxExtensions.Setup();

await builder.Build().RunAsync();
