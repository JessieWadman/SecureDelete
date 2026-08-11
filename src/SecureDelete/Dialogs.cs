using System.Windows;

namespace SecureDelete;

/// <summary>Thin wrappers over <see cref="MessageBox"/> for the few fatal/early-exit cases.</summary>
internal static class Dialogs
{
    public static void Error(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public static void Info(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
}
