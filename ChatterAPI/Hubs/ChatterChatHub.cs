using ChatterAPI.Data;
using ChatterChatHandler;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ChatterAPI.Hubs;

public class ChatterChatHub : Hub<IChatClient>
{
    IDbContextFactory<MessageDbContext> dbFactory;
    public ChatterChatHub(IDbContextFactory<MessageDbContext> dbFactory)
    {
        this.dbFactory = dbFactory;
    }
    public async Task SendMessageWithSignalR(WriteToChatMessage chatMessage)
    {
        await ChatterChatHub.SendMessage(dbFactory, Clients.All, chatMessage);
    }

    public async static Task<WriteToChatMessage> SendMessage(IDbContextFactory<MessageDbContext> dbFactory, IChatClient clients, WriteToChatMessage chatMessage)
    {
        var db = await dbFactory.CreateDbContextAsync();
        try
        {
            var message = new Message
            {
                Username = chatMessage.alias,
                JsonMessage = JsonSerializer.Serialize(chatMessage),
            };
            db.Add(message);
            await db.SaveChangesAsync();
            chatMessage = chatMessage with { messageId = message.Id };

            //For SignalR send to the hub
            await clients.ReceiveMessage(chatMessage);

            return chatMessage;
        }
        finally
        {
            await db.DisposeAsync();
        }
    }
}

public interface IChatClient
{
    Task ReceiveMessage(WriteToChatMessage chatMessage);
}