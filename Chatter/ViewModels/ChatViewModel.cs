using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Chatter.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    [ObservableProperty]
    private Brush foreground;
    [ObservableProperty]
    private Brush background;
    [ObservableProperty]
    private string text;
    [ObservableProperty]
    private FontStyle fontStyle;

    public ChatViewModel(string text, Brush? background = null, Brush? foreground = null, FontStyle? fontStyle = null)
    {
        Text = text;
        Background = background ?? Brushes.White;
        Foreground = foreground ?? Brushes.Black;
        FontStyle = fontStyle ?? FontStyles.Normal;
    }
}
