using System.Windows;

namespace SecureDelete;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // We open and close windows explicitly, so do not let WPF shut down on its own timing.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        CommandLineOptions options = CommandLineOptions.Parse(e.Args);

        switch (options.Mode)
        {
            case CommandMode.Help:
                ShowUsage();
                Shutdown();
                return;

            case CommandMode.Invalid:
                Dialogs.Error("Secure Delete", options.Error ?? "The arguments could not be understood.");
                Shutdown();
                return;
        }

        // Validate the target up front (existence, kind, reparse points). Pass count is finalised
        // in the confirmation dialog; use the default of 1 purely to satisfy validation here.
        DeleteRequest request;
        try
        {
            request = TargetInspector.Inspect(options.Target, 1);
        }
        catch (TargetValidationException ex)
        {
            Dialogs.Error("Secure Delete", ex.Message);
            Shutdown();
            return;
        }
        catch (Exception ex)
        {
            Dialogs.Error("Secure Delete", "The target could not be inspected:\n\n" + ex.Message);
            Shutdown();
            return;
        }

        string? sdelete = SDeleteLocator.Default().Locate();
        if (sdelete is null)
        {
            new SDeleteMissingWindow().ShowDialog();
            Shutdown();
            return;
        }

        var window = new MainWindow(request, new SDeleteRunner(sdelete));
        MainWindow = window;
        window.Closed += (_, _) => Shutdown();
        window.Show();
    }

    static void ShowUsage() => Dialogs.Info(
        "SecureDelete",
        "SecureDelete is normally launched from the Windows Explorer \"Secure Delete\" context-menu command.\n\n" +
        "Usage:\n    SecureDelete.exe --delete \"<file or folder>\"\n\n" +
        "It shows a confirmation dialog and then uses Microsoft Sysinternals SDelete to securely overwrite " +
        "and delete the selected item.");
}
