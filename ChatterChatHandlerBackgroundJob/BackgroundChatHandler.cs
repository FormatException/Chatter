using ChatterChatHandler;
using CommunityToolkit.Mvvm.Messaging;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ChatterChatHandlerBackgroundJob;

public class BackgroundChatHandler : ChatHandler, IDisposable
{
    //cheat here and just use the url from the ChatterAPI project
    string baseUrl = "https://localhost:7076";
    Timer timer;

    //we want to control the flow of sending and receiving but we can't use a classic
    //lock as we should be posting/getting with async.  Thus I've got a SemaphoreSlim here.
    static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
    int lastMessageId = 0;
    HttpClient client;

    public override string Name => "Background Job Chat Handler";

    public override string Description => "Utilizes a background timer to poll for pending messages.";

    public BackgroundChatHandler(IMessenger messenger) : this(messenger, null) { }
    public BackgroundChatHandler(IMessenger messenger, string? endPoint) : base(messenger)
    {
        baseUrl = endPoint ?? "https://localhost:7076";
#if DEBUG
        client = new HttpClient(GetInsecureHandler());
#else
        client = new HttpClient();
#endif

    }

    public override void Activate()
    {
        timer = new Timer(GetRecentMessages, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    public override void Deactivate()
    {
        timer.Dispose();
    }

    /// <summary>
    /// Done per: https://learn.microsoft.com/en-us/previous-versions/xamarin/cross-platform/deploy-test/connect-to-local-web-services#bypass-the-certificate-security-check
    /// </summary>
    /// <returns></returns>
    public static HttpClientHandler GetInsecureHandler()
    {
        //when using this in android it doesn't like self-issued certs
        HttpClientHandler handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
        {
            if (cert.Issuer.Equals("CN=localhost"))
                return true;
            return errors == System.Net.Security.SslPolicyErrors.None;
        };
        return handler;
    }

    public override async Task<bool> SendChatAsync(WriteToChatMessage message)
    {
        var url = $"{baseUrl}/sendMessage";
        var jsonContent = JsonSerializer.Serialize(message);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        //wait on any currently running post/get
        await semaphoreSlim.WaitAsync();
        try
        {
            var response = await client.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
                return false;

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var messageFromServer = JsonSerializer.Deserialize<WriteToChatMessage>(jsonResponse);
            if (messageFromServer != null)
            {
                Messenger.Send(messageFromServer);
                lastMessageId = messageFromServer.messageId.HasValue ? messageFromServer.messageId.Value : 0;
            }
            return true;
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }

    private async void GetRecentMessages(object? state)
    {
        List<WriteToChatMessage> messages = new List<WriteToChatMessage>();
        //wait on any currently running post/get
        await semaphoreSlim.WaitAsync();
        try
        {
            //we're going to do a skip take so we don't want the last id we're using to change
            var lastId = lastMessageId;
            int skip = 0; int take = 10;
            for (; ; skip += take)
            {
                var url = $"{baseUrl}/getMessages/{lastId}?skip={skip}&take={take}";
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Messenger.Send(new WriteToChatMessage("system", "unable to get messages from server", "Red", "White", "Normal"));
                    return;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var messagesFromServer = JsonSerializer.Deserialize<List<WriteToChatMessage>>(jsonResponse);
                foreach (var message in messagesFromServer.OrderBy(x => x.messageId))
                {
                    Messenger.Send(message);
                    if (message.messageId.HasValue)
                        lastMessageId = message.messageId.Value;
                }

                if (messagesFromServer.Count < take)
                    break;
            }
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }

    public void Dispose()
    {
        timer?.Dispose();
        client?.Dispose();
    }
}
