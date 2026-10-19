using System.Windows;
using System.Windows.Threading;

namespace SatWatch.App.Views;

public partial class FlashOverlay : Window
{
    public FlashOverlay(int seconds = 5)
    {
        InitializeComponent();
        var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
        t.Tick += (_, _) => { t.Stop(); Close(); };
        t.Start();
    }
}
