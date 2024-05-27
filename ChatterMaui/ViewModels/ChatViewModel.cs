using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Controls.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ChatterMaui.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    [ObservableProperty]
    private Brush foreground;
    [ObservableProperty]
    private Brush background;
    [ObservableProperty]
    private string text;
    [ObservableProperty]
    //private FontStyle fontStyle;
    private string fontStyle;

    public ChatViewModel(string text, Brush? background = null, Brush? foreground = null, string? fontStyle = null)
    {
        Text = text;
        Background = background ?? Colors.White;
        Foreground = foreground ?? Colors.Black;
        FontStyle = fontStyle ?? "Normal";
    }
}
