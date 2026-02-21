using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System.Diagnostics;

namespace ui.Dialogs
{
    public class ResultDialog : Window
    {
        public ResultDialog(bool success, string message)
        {
            Title = success ? "Success" : "Error";
            Width = 450;
            Height = success ? 180 : 150;
            CanResize = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var stack = new StackPanel
            {
                Margin = new Thickness(20),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            if (success)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "Operation completed successfully.",
                    FontWeight = FontWeight.Bold,
                    TextAlignment = TextAlignment.Center
                });

                stack.Children.Add(new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 8, 0, 0),
                    TextAlignment = TextAlignment.Center
                });

                var buttonRow = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Spacing = 8,
                    Margin = new Thickness(0, 12, 0, 0)
                };

                var openBtn = new Button { Content = "Open in Explorer" };
                var copyBtn = new Button { Content = "Copy Path" };
                var okBtn = new Button { Content = "OK", IsDefault = true, IsCancel = true };

                openBtn.Click += (s, e) =>
                {
                    try
                    {
                        var psi = new ProcessStartInfo("explorer.exe", $"/select,\"{message}\"")
                        { UseShellExecute = true };
                        Process.Start(psi);
                    }
                    catch
                    {
                    }
                };


                copyBtn.Click += async (s, e) =>
                {
                    try
                    {
                        var top = GetTopLevel(this);
                        var clipboard = top?.Clipboard;
                        if (clipboard is not null)
                        {
                            await clipboard.SetTextAsync(message);
                        }
                    }
                    catch
                    {
                    }
                };

                okBtn.Click += (s, e) => Close();

                buttonRow.Children.Add(openBtn);
                buttonRow.Children.Add(copyBtn);
                buttonRow.Children.Add(okBtn);

                stack.Children.Add(buttonRow);
            }
            else
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "An error occurred:",
                    FontWeight = FontWeight.Bold,
                    TextAlignment = TextAlignment.Center
                });

                stack.Children.Add(new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 8, 0, 0),
                    TextAlignment = TextAlignment.Center
                });

                var okBtn = new Button
                {
                    Content = "OK",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Margin = new Thickness(0, 12, 0, 0),
                    IsDefault = true,
                    IsCancel = true
                };
                okBtn.Click += (s, e) => Close();
                stack.Children.Add(okBtn);
            }

            Content = stack;

            KeyDown += (s, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Enter || e.Key == Avalonia.Input.Key.Return ||
                    e.Key == Avalonia.Input.Key.Escape)
                {
                    Close();
                }
            };
        }
    }
}
