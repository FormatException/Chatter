using ChatterChatHandler;
using ChatterChatHandlerBackgroundJob;
using ChatterChatHandlerBasic;
using ChatterChatHandlerSignalR;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Graphics.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatterMaui.ViewModels;

public partial class MainViewModel : ObservableObject, IRecipient<WriteToChatMessage>, IDisposable
{
    /// <summary>
    /// ctor for MainWindowViewModel
    /// </summary>
    /// <param name="dispatcher">We need the dispatcher we were created under so we can manage the receive as it may come in on different threads</param>
    /// <param name="messenger"></param>
    public MainViewModel(IDispatcher dispatcher, IMessenger messenger)
    {
        Dispatcher = dispatcher;
        Alias = "Maui " + Guid.NewGuid().ToString();

        Messenger = messenger;
        Messenger.RegisterAll(this);

        //this is done per: https://learn.microsoft.com/en-us/dotnet/maui/data-cloud/local-web-services?view=net-maui-8.0
        if (DeviceInfo.Current.Platform == DevicePlatform.Android)
        {
            ChatPlugins.Add(new BackgroundChatHandler(messenger, "https://10.0.2.2:7076"));
            ChatPlugins.Add(new SignalRChatHandler(messenger, "https://10.0.2.2:7076/chatterChatHub"));
        }
        else
        {
            ChatPlugins.Add(new BackgroundChatHandler(messenger));
            ChatPlugins.Add(new SignalRChatHandler(messenger));
        }
        
        ChatPlugins.Add(new BasicChatHandler(messenger));
        ChatHandler = ChatPlugins[0];
        //ChatHandler.Activate();
    }

    [ObservableProperty]
    private ObservableCollection<ChatViewModel> chats = new();

    [ObservableProperty]
    private string chatText = "";

    [ObservableProperty]
    private string alias = "";

    private ObservableCollection<ChatHandler> chatPlugins = new ObservableCollection<ChatHandler>();
    public ObservableCollection<ChatHandler> ChatPlugins
    {
        get => chatPlugins;
    }

    [ObservableProperty]
    ChatHandler? chatHandler = null;
    partial void OnChatHandlerChanging(ChatHandler? oldValue, ChatHandler? newValue)
    {
        if (oldValue != null)
            oldValue.Deactivate();
        if (newValue != null)
            newValue.Activate();
    }

    protected IDispatcher Dispatcher { get; }
    protected IMessenger Messenger { get; }

    [RelayCommand]
    async Task SendChat()
    {
        if (string.IsNullOrEmpty(ChatText)) return;
        var successful = await ChatHandler.SendChatAsync(new WriteToChatMessage(Alias, ChatText, Colors.White.ToArgbHex(true), Colors.Black.ToArgbHex(true), "Normal"));
        if (successful)
        {
            ChatText = "";
            return;
        }

        //if the message failed to send just write straight to the chat
        Receive(new WriteToChatMessage(Alias, $"FAILED TO SEND MESSAGE: {ChatText}", Colors.Red.ToArgbHex(true), Colors.White.ToArgbHex(true), "Normal"));
    }

    public async void Receive(WriteToChatMessage message)
    {
        await Dispatcher.DispatchAsync(() =>
        {
            var time = DateTime.Now.ToString("MM/dd/yy HH:mm:ss.fff zzz");
            var alias = string.IsNullOrEmpty(message.alias) ? "Unknown" : message.alias;
            var timeStampMessage = $"{alias} [{time}] {message.message}";

            Brush? background = null, foreground = null;
            try
            {
                //if some client sends a bad color we can't parse just ignore the color
                //Maui will convert the colors to brushes for us
                ColorTypeConverter converter = new ColorTypeConverter();
                background = converter.ConvertFromInvariantString(message.background) as Color;
                foreground = converter.ConvertFromInvariantString(message.foreground) as Color;
            }
            catch (InvalidOperationException) { }


            //FontStyle? fontStyle = null;
            ////can't use a switch here because I can't define a constant value for the type of font style
            //if (!string.IsNullOrEmpty(message.fontStyle))
            //{
            //    if (message.fontStyle == FontStyles.Normal.ToString())
            //        fontStyle = FontStyles.Normal;
            //    else if (message.fontStyle == FontStyles.Italic.ToString())
            //        fontStyle = FontStyles.Italic;
            //    else if (message.fontStyle == FontStyles.Oblique.ToString())
            //        fontStyle = FontStyles.Oblique;
            //}

            Chats.Add(new ChatViewModel(timeStampMessage, background, foreground, message.fontStyle));

        });
    }

    public void Dispose()
    {
        Messenger.UnregisterAll(this);
    }
}
