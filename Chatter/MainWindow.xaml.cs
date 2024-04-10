using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Chatter;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel mainWindowViewModel)
    {
        DataContext = mainWindowViewModel;
        InitializeComponent();
        ((INotifyCollectionChanged)listBoxChats.Items).CollectionChanged += ListBoxChats_CollectionChanged;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
    }

    private void ListBoxChats_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (VisualTreeHelper.GetChildrenCount(listBoxChats) > 0)
        {
            FrameworkElement border = (FrameworkElement)VisualTreeHelper.GetChild(listBoxChats, 0);
            ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
            scrollViewer.ScrollToBottom();
        }
    }

    private void Window_KeyUp(object sender, KeyEventArgs e)
    {
        if ((e.Key == Key.R) &&
            (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) &&
            (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
        {
            windowScaleTransform.ScaleX = 1;
            windowScaleTransform.ScaleY = 1;
            e.Handled = true;
        }
    }

    private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        {
            var delta = ((double)e.Delta / 1000d);
            if (windowScaleTransform.ScaleX + delta < .2)
                return;
            if (windowScaleTransform.ScaleX + delta > 5)
                return;
            windowScaleTransform.ScaleX += delta;
            windowScaleTransform.ScaleY += delta;

            //mark as handled so the scrollviewer on the listbox doesn't try and scroll too
            e.Handled = true;
        }
    }
}