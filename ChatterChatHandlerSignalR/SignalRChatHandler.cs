using ChatterChatHandler;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AspNetCore.SignalR.Client;

namespace ChatterChatHandlerSignalR;

public class SignalRChatHandler : ChatHandler
{
    public override string Name => "Signal R Chat Handler";

    public override string Description => "Talks to the Chatter API and uses SignalR to send and receive messages to the API.";

    HubConnection connection;

    public SignalRChatHandler(IMessenger messenger) : this(messenger, null) { }
    public SignalRChatHandler(IMessenger messenger, string? endPoint) : base(messenger)
    {
        var url = endPoint ?? "https://localhost:7076/chatterChatHub";

        connection = new HubConnectionBuilder()
#if DEBUG
                        .WithUrl(new Uri(url), options =>
                        {
                            //when using this in android it doesn't like self-issued certs
                            //Done per: https://learn.microsoft.com/en-us/previous-versions/xamarin/cross-platform/deploy-test/connect-to-local-web-services#bypass-the-certificate-security-check
                            HttpClientHandler handler = new HttpClientHandler();
                            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                            {
                                if (cert.Issuer.Equals("CN=localhost"))
                                    return true;
                                return errors == System.Net.Security.SslPolicyErrors.None;
                            };
                            options.HttpMessageHandlerFactory = _ => handler;
                        })
#else
                        .WithUrl(url)
#endif
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
    }

    public override void Activate()
    {
        connection.StartAsync();
    }

    public override void Deactivate()
    {
        connection.StopAsync();
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
