using Chatter.ViewModels;
using ChatterChatHandler;
using ChatterChatHandlerBackgroundJob;
using ChatterChatHandlerBasic;
using ChatterChatHandlerSignalR;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

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


    [ObservableProperty]
    bool isColorPickerExpanded = false;

    protected Dispatcher Dispatcher { get; }
    protected IMessenger Messenger { get; }
    protected ChatHandler ChatHandler { get; set; }

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
        //Messenger.Register<MainWindowViewModel, WriteToChatMessage>(this, (r, m) => r.Receive(m));

        //ChatHandler = new SystemMessageChatHandler(Messenger);
        //ChatHandler = new BasicChatHandler(Messenger);
        //ChatHandler = new BackgroundChatHandler(Messenger);
        ChatHandler = new SignalRChatHandler(Messenger);
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
