using CommunityToolkit.Mvvm.ComponentModel;
using static System.Net.Mime.MediaTypeNames;

namespace ChatterBlazor.Components.Models;

public class ChatItem
{
    public string Foreground { get; set; } = "Black";
    public string Background { get; set; } = "White";
    public string Text { get; set; } = "";
    public string FontStyle { get; set; } = "Normal";

    public ChatItem(string text, string? background = null, string? foreground = null, string? fontStyle = null)
    {
        Text = text;
        Background = background ?? "White";
        Foreground = foreground ?? "Black";
        FontStyle = fontStyle ?? "Normal";
    }
}
