using GBX.NET;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TM_GenericMapping.Common;
using TM_MappingTools;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddSingleton<TM_MappingTools.Services.ClipFileService>();
builder.Services.AddSingleton<TM_MappingTools.Services.OperationHistoryService>();

GbxExtensions.Setup();

await builder.Build().RunAsync();
