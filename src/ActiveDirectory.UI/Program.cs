using System.IO;
using ActiveDirectory.Core.Interfaces;
using ActiveDirectory.Infrastructure.Services;
using ActiveDirectory.UI.ViewModels;
using ActiveDirectory.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ActiveDirectory.UI;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Path to external uncompiled CSV
        var csvFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "codici_comuni_esteri.csv");

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Core & Infrastructure Services
                services.AddSingleton<IActiveDirectoryService, ActiveDirectoryService>();

                // Register Catasto dictionary loading (e.g., from local resources or files)
                services.AddSingleton<ICadastralCodeLoader, CadastralCodeLoader>();
                services.AddSingleton<IReadOnlyDictionary<string, string>>(sp =>
                    sp.GetRequiredService<ICadastralCodeLoader>().LoadCadastralCodes());
                services.AddSingleton<IFiscalCodeDecoder, FiscalCodeDecoder>();

                // ViewModels & Views
                services.AddTransient<MainViewModel>();
                services.AddTransient<MainWindow>();

                // WPF App Instance
                services.AddSingleton<App>();
            })
            .Build();

        var app = host.Services.GetRequiredService<App>();
        var mainWindow = host.Services.GetRequiredService<MainWindow>();

        app.Run(mainWindow);
    }
}