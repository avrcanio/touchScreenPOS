using System.Windows;
using TouchScreenPOS.Api;

namespace TouchScreenPOS;

public partial class RepresentationDetailsWindow : Window
{
    public RepresentationDetailsWindow(Representation representation)
    {
        InitializeComponent();
        DataContext = representation;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
