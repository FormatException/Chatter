using ChatterChatHandler;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AspNetCore.SignalR.Client;

namespace ChatterChatHandlerSignalR;

public class SignalRChatHandler : ChatHandler
{
    public override string Name => "Signal R Chat Handler";

    public override string Description => "Talks to the Chatter API and users Signal R to send and receive messages.";


    HubConnection connection;
    public SignalRChatHandler(IMessenger messenger) : base(messenger)
    {
        connection = new HubConnectionBuilder()
                        .WithUrl("https://localhost:7076/chatterChatHub")
                        .WithAutomaticReconnect()
                        .Build();

        connection.Reconnecting += (sender) =>
        {
            Messenger.Send(new WriteToChatMessage("system", "Trying to reconnect to server", "Red", "White", "Normal"));
            return Task.CompletedTask;
        };
        connection.Reconnected += (sender) =>
        {
            Messenger.Send(new WriteToChatMessage("system", "Reconnected to server", "Red", "White", "Normal"));
            return Task.CompletedTask;
        };
        connection.Closed += (sender) =>
        {
            Messenger.Send(new WriteToChatMessage("system", "Connection to server closed", "Red", "White", "Normal"));
            return Task.CompletedTask;
        };

        connection.On<WriteToChatMessage>("ReceiveMessage", (message) =>
        {
            Messenger.Send(message);
        });

        connection.StartAsync();
    }

    public override async Task<bool> SendChatAsync(WriteToChatMessage message)
    {
        try
        {
            await connection.InvokeAsync("SendMessageWithSignalR", message);
            return true;
        }
        catch(Exception ex)
        {
            Messenger.Send(new WriteToChatMessage("system", ex.Message, "Red", "White", "Normal"));
            return false;
        }
    }
}
