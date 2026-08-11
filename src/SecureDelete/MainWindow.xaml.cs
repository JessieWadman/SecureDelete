using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace SecureDelete;

public partial class MainWindow : Window
{
    readonly DeleteRequest _initial;
    readonly ISDeleteRunner _runner;

    CancellationTokenSource? _cts;
    bool _running;
    bool _completed;

    public MainWindow(DeleteRequest initial, ISDeleteRunner runner)
    {
        _initial = initial;
        _runner = runner;
        InitializeComponent();

        bool isDir = initial.Kind == TargetKind.Directory;
        HeadingText.Text = isDir
            ? "Securely delete this folder and its contents?"
            : "Securely delete this file?";
        TargetPathText.Text = initial.Path;
        TargetPathText.ToolTip = initial.Path;

        if (initial.IsUnc)
        {
            // Network storage: SDelete's overwrite guarantees generally do not hold. Warn, don't block.
            var note = new TextBlock
            {
                Text = "This item is on a network location. Secure overwrite cannot be guaranteed there.",
                Foreground = (System.Windows.Media.Brush)FindResource("Danger"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 0),
            };
            ConfirmPanel.Children.Insert(ConfirmPanel.Children.IndexOf(TargetPathText.Parent as UIElement) + 1, note);
        }
    }

    int SelectedPasses()
    {
        // The selector is fixed, but validate anyway before trusting the value.
        string text = (PassesCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "1";
        return int.TryParse(text, out int p) ? p : 1;
    }

    async void OnSecureDelete(object sender, RoutedEventArgs e)
    {
        int passes;
        DeleteRequest request;
        try
        {
            passes = SelectedPasses();
            TargetInspector.ValidatePasses(passes);
            // Re-inspect: the target (or a link inside it) may have changed since confirmation opened.
            request = TargetInspector.Inspect(_initial.Path, passes);
        }
        catch (TargetValidationException ex)
        {
            ShowResult(success: false, heading: "Cannot delete", message: ex.Message, details: null, canceled: false);
            return;
        }

        ShowProgress(request);

        _cts = new CancellationTokenSource();
        var progress = new Progress<string>(line => ProgressStatus.Text = line);

        SDeleteResult result;
        try
        {
            result = await _runner.RunAsync(request, progress, _cts.Token);
        }
        catch (Exception ex)
        {
            ShowResult(success: false, heading: "Secure delete failed",
                message: "An unexpected error occurred while running SDelete.",
                details: ex.Message, canceled: false);
            return;
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            _running = false;
        }

        InterpretAndShow(request, result);
    }

    void InterpretAndShow(DeleteRequest request, SDeleteResult result)
    {
        if (result.Canceled)
        {
            ShowResult(success: false, heading: "Operation aborted",
                message: "The secure delete was aborted before it finished.\n\n" +
                         "The item may have been partially overwritten or partially deleted. " +
                         "This is not an undo — nothing has been recovered or restored.",
                details: Combine(result), canceled: true);
            return;
        }

        if (result.Success)
        {
            string what = request.Kind == TargetKind.Directory ? "folder" : "file";
            ShowResult(success: true, heading: "Completed successfully.",
                message: $"The {what} was securely overwritten and deleted.",
                details: string.IsNullOrWhiteSpace(result.StandardOutput) ? null : result.StandardOutput,
                canceled: false);
            return;
        }

        // Non-zero exit / failure.
        string reason = ExplainFailure(result);
        ShowResult(success: false, heading: "Secure delete failed",
            message: reason, details: Combine(result), canceled: false);
    }

    static string ExplainFailure(SDeleteResult result)
    {
        string stderr = result.StandardError;
        string hint =
            stderr.Contains("Access is denied", StringComparison.OrdinalIgnoreCase)
                ? "Access was denied. You may not have permission to delete this item."
            : stderr.Contains("being used by another process", StringComparison.OrdinalIgnoreCase)
                ? "The item is locked or in use by another program. Close it and try again."
            : stderr.Contains("No files", StringComparison.OrdinalIgnoreCase)
                ? "SDelete reported that the target could not be found."
                : "SDelete reported an error while deleting the item.";

        return hint + $"\n\nSDelete exit code: {result.ExitCode}.";
    }

    static string Combine(SDeleteResult result)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(result.StandardError))
            parts.Add(result.StandardError.Trim());
        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
            parts.Add(result.StandardOutput.Trim());
        return parts.Count == 0 ? $"SDelete exit code: {result.ExitCode}." : string.Join("\n\n", parts);
    }

    void ShowProgress(DeleteRequest request)
    {
        _running = true;
        ConfirmPanel.Visibility = Visibility.Collapsed;
        ResultPanel.Visibility = Visibility.Collapsed;
        ProgressPanel.Visibility = Visibility.Visible;
        ProgressHeading.Text = request.Kind == TargetKind.Directory
            ? "Securely deleting folder…"
            : "Securely deleting file…";
        ProgressStatus.Text = "Processing…";
    }

    void ShowResult(bool success, string heading, string message, string? details, bool canceled)
    {
        _running = false;
        _completed = true;

        ConfirmPanel.Visibility = Visibility.Collapsed;
        ProgressPanel.Visibility = Visibility.Collapsed;
        ResultPanel.Visibility = Visibility.Visible;

        ResultHeading.Text = heading;
        ResultHeading.Foreground = success
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x16, 0xA3, 0x4A))
            : (System.Windows.Media.Brush)FindResource(canceled ? "TextPrimary" : "Danger");
        ResultMessage.Text = message;

        if (string.IsNullOrWhiteSpace(details))
        {
            DetailsExpander.Visibility = Visibility.Collapsed;
        }
        else
        {
            DetailsExpander.Visibility = Visibility.Visible;
            DetailsText.Text = details;
        }

        CloseButton.Focus();
    }

    void OnAbort(object sender, RoutedEventArgs e)
    {
        AbortButton.IsEnabled = false;
        ProgressStatus.Text = "Aborting…";
        _cts?.Cancel();
    }

    void OnCancel(object sender, RoutedEventArgs e) => Close();

    void OnClose(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(CancelEventArgs e)
    {
        // Don't allow the window to close while SDelete is still running; require an explicit Abort.
        if (_running && !_completed)
        {
            e.Cancel = true;
            return;
        }
        base.OnClosing(e);
    }
}
