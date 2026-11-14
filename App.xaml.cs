using System.Windows;
using TicketeraApp.Services;

namespace TicketeraApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Cualquier error no controlado queda registrado en el log para poder diagnosticarlo.
        DispatcherUnhandledException += (_, args) =>
            LogService.Error("Excepción no controlada (UI)", args.Exception);
        System.AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            LogService.Error("Excepción no controlada (dominio)", args.ExceptionObject as System.Exception);
    }
}
