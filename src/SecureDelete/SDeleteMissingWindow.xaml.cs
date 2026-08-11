using System.Diagnostics;
using System.Windows;

namespace SecureDelete;

public partial class SDeleteMissingWindow : Window
{
    const string DownloadUrl = "https://learn.microsoft.com/en-us/sysinternals/downloads/sdelete";

    public SDeleteMissingWindow() => InitializeComponent();

    void OnOpenDownloadPage(object sender, RoutedEventArgs e)
    {
        try
        {
            // UseShellExecute launches the user's default browser; no shell string is constructed.
            Process.Start(new ProcessStartInfo(DownloadUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Dialogs.Error("Secure Delete",
                "The download page could not be opened automatically. Please visit:\n\n" +
                DownloadUrl + "\n\n(" + ex.Message + ")");
        }
    }

    void OnClose(object sender, RoutedEventArgs e) => Close();
}
