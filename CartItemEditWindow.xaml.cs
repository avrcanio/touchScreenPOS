using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TouchScreenPOS.ViewModels;

namespace TouchScreenPOS;

public partial class CartItemEditWindow : Window
{
    public bool Removed { get; private set; }
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TouchScreenPOS",
        "logs",
        "touch-debug.log");

    public CartItemEditWindow(RepresentationCartItem item)
    {
        InitializeComponent();
        DataContext = item;
    }

    private void Increment_Click(object sender, RoutedEventArgs e)
    {
        LogEvent("Increment_Click", sender);
        if (DataContext is not RepresentationCartItem item)
        {
            return;
        }

        item.Quantity += 1;
    }

    private void Decrement_Click(object sender, RoutedEventArgs e)
    {
        LogEvent("Decrement_Click", sender);
        if (DataContext is not RepresentationCartItem item)
        {
            return;
        }

        if (item.Quantity > 1)
        {
            item.Quantity -= 1;
        }
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        Removed = true;
        Close();
    }

    private void Done_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Button_TouchDown(object sender, TouchEventArgs e)
    {
        LogEvent("TouchDown", sender);
    }

    private void Button_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        LogEvent("PreviewMouseDown", sender);
    }

    private static void LogEvent(string name, object sender)
    {
        try
        {
            var logDir = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrWhiteSpace(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            var button = sender as FrameworkElement;
            var label = button?.GetValue(ContentControl.ContentProperty)?.ToString()
                        ?? button?.Name
                        ?? "button";
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {name} | {label}\n";
            File.AppendAllText(LogPath, line);
        }
        catch
        {
        }
    }
}
