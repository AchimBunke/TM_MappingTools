using GBX.NET;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TM_GenericMapping.Common;
using TM_MappingTools;
using TM_MappingTools.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

ConfigureServices(builder.Services, builder.HostEnvironment.BaseAddress);




// global instances of file services for shared access across tools




GbxExtensions.Setup();

var host = builder.Build();

// last line of defense: exceptions nobody handled (async void handlers, background tasks) end up in the output
var messages = host.Services.GetRequiredService<ToolMessageService>();
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    if (e.ExceptionObject is Exception ex) messages.ReportUnhandled(ex);
};
TaskScheduler.UnobservedTaskException += (_, e) =>
{
    messages.ReportUnhandled(e.Exception.GetBaseException());
    e.SetObserved();
};

await host.RunAsync();

static void ConfigureServices(IServiceCollection services, string baseAddress)
{
    services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseAddress) });
    services.AddSingleton<ClipFileService>();
    services.AddSingleton<MapFileService>();
    services.AddSingleton<ItemFileService>();

    services.AddSingleton<ToolMessageService>();
    services.AddSingleton<ILoggerProvider, ToolMessageLoggerProvider>();
    services.AddSingleton<ToolRegistryService>();
    services.AddSingleton<GlobalClipHistoryService>();
    services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseAddress) });

    services.AddScoped<SessionStorage>();
}