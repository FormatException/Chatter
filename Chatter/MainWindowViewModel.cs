using ChatPluginLoader;
using Chatter.ViewModels;
using ChatterChatHandler;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace Chatter;

public partial class MainWindowViewModel : ObservableObject, IRecipient<WriteToChatMessage>, IDisposable
{
    [ObservableProperty]
    private ObservableCollection<ChatViewModel> chats = new();

    [ObservableProperty]
    private string chatText = "";

    [ObservableProperty]
    private string alias = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ForegroundBrush))]
    Color foreground = Colors.Black;

    public Brush ForegroundBrush
    {
        get => new SolidColorBrush(Foreground);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackgroundBrush))]
    Color background = Colors.White;
    public Brush BackgroundBrush
    {
        get => new SolidColorBrush(Background);
    }

    [ObservableProperty]
    FontStyle fontStyle = FontStyles.Normal;


    private ObservableCollection<FontStyle> availableFontStyles = new ObservableCollection<FontStyle>() { FontStyles.Normal, FontStyles.Italic, FontStyles.Oblique };
    public ObservableCollection<FontStyle> AvailableFontStyles
    {
        get => availableFontStyles;
    }

    private ObservableCollection<ChatHandler> chatPlugins = new ObservableCollection<ChatHandler>();
    public ObservableCollection<ChatHandler> ChatPlugins
    {
        get => chatPlugins;
    }

    [ObservableProperty]
    bool isColorPickerExpanded = false;

    [ObservableProperty]
    ChatHandler chatHandler = null;
    partial void OnChatHandlerChanging(ChatHandler? oldValue, ChatHandler newValue)
    {
        if (oldValue != null)
            oldValue.Deactivate();
        if (newValue != null) 
            newValue.Activate();
    }

    protected Dispatcher Dispatcher { get; }
    protected IMessenger Messenger { get; }

    /// <summary>
    /// ctor for MainWindowViewModel
    /// </summary>
    /// <param name="dispatcher">We need the dispatcher we were created under so we can manage the receive as it may come in on different threads</param>
    /// <param name="messenger"></param>
    public MainWindowViewModel(Dispatcher dispatcher, IMessenger messenger)
    {
        BindingOperations.EnableCollectionSynchronization(Chats, new());

        Dispatcher = dispatcher;
        Alias = "Unknown " + Guid.NewGuid().ToString();

        Messenger = messenger;
        Messenger.RegisterAll(this);

        LoadPluginsAsync();
    }

    private async Task LoadPluginsAsync()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var pluginPath = Path.Combine(baseDir, "Plugins");
        var plugins = PluginManager.LoadPlugins<ChatHandler>(pluginPath, Messenger);

        await Dispatcher.InvokeAsync(() =>
        {
            foreach(var plugin in plugins)
            {
                ChatPlugins.Add(plugin);
            }
            if (ChatPlugins.Count > 0)
                ChatHandler = ChatPlugins[0];
        });
    }

    public async void Receive(WriteToChatMessage message)
    {
        //we don't know where we're coming from so we need to switch over to the dispatcher and have it update everything
        await Dispatcher.InvokeAsync(() =>
        {
            var time = DateTime.Now.ToString("MM/dd/yy HH:mm:ss.fff zzz");
            var alias = string.IsNullOrEmpty(message.alias) ? "Unknown" : message.alias;
            var timeStampMessage = $"{alias} [{time}] {message.message}";

            Brush? background = null;
            if (!string.IsNullOrEmpty(message.background))
                background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(message.background));

            Brush? foreground = null;
            if (!string.IsNullOrEmpty(message.foreground))
                foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(message.foreground));

            FontStyle? fontStyle = null;
            //can't use a switch here because I can't define a constant value for the type of font style
            if (!string.IsNullOrEmpty(message.fontStyle))
            {
                if (message.fontStyle == FontStyles.Normal.ToString())
                    fontStyle = FontStyles.Normal;
                else if (message.fontStyle == FontStyles.Italic.ToString())
                    fontStyle = FontStyles.Italic;
                else if (message.fontStyle == FontStyles.Oblique.ToString())
                    fontStyle = FontStyles.Oblique;
            }

            Chats.Add(new ChatViewModel(timeStampMessage, background, foreground, fontStyle));
        });
    }

    [RelayCommand]
    public async Task SendChat()
    {
        if (string.IsNullOrEmpty(ChatText)) return;
        var successful = await ChatHandler.SendChatAsync(new WriteToChatMessage(Alias, ChatText, Background.ToString(), Foreground.ToString(), FontStyle.ToString()));
        if (successful)
        {
            ChatText = "";
            return;
        }

        //if the message failed to send just write straight to the chat
        Receive(new WriteToChatMessage(Alias, $"FAILED TO SEND MESSAGE: {ChatText}", Colors.Red.ToString(), Colors.White.ToString(), FontStyles.Italic.ToString()));
    }

    public void Dispose()
    {
        Messenger.UnregisterAll(this);
    }
}
