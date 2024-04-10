using ChatterAPI;
using ChatterAPI.Data;
using ChatterAPI.Hubs;
using ChatterChatHandler;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddResponseCompression(options =>
{
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/octet-stream" });
});

builder.Services.AddDbContextFactory<MessageDbContext>();
builder.Services.AddSignalR();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseResponseCompression();
app.MapHub<ChatterChatHub>("/chatterChatHub");

app.MapPost("/sendMessage", async (IDbContextFactory<MessageDbContext> dbFactory, IHubContext<ChatterChatHub, IChatClient> hub, WriteToChatMessage chatMessage) =>
{
    await ChatterChatHub.SendMessage(dbFactory, hub.Clients.All, chatMessage);
});

app.MapPost("/sendMessage2", async (IDbContextFactory<MessageDbContext> dbFactory, IHubContext<ChatterChatHub> hub, WriteToChatMessage chatMessage) =>
{
    await hub.Clients.All.SendAsync("ReceiveMessage", chatMessage);
});

app.MapGet("/getMessages/{lastId}", async (IDbContextFactory<MessageDbContext> dbFactory, int lastId, [FromQuery(Name = "skip")] int skip = 0, [FromQuery(Name = "take")] int take = 0) =>
{
    if (take > 10)
        throw new ArgumentException("take may not be greater than 10");

    var db = dbFactory.CreateDbContext();
    try
    {
        var chatMessages = new List<WriteToChatMessage>();

        //if they are new then they won't know where to start so just give them the last message
        if (lastId <= 0)
        {
            var lastMessage = db.Messages.OrderBy(x => x.Id).LastOrDefault();
            if (lastMessage != null)
            {
                var chatMessage = JsonSerializer.Deserialize<WriteToChatMessage>(lastMessage.JsonMessage);
                chatMessage = chatMessage with { messageId = lastMessage.Id };
                chatMessages.Add(chatMessage);
                return chatMessages;
            }
        }

        if (take <= 0)
            take = 10;

        var messages = db.Messages.Where(x => x.Id > lastId).OrderBy(x => x.Id).Skip(skip).Take(take);
        foreach (var message in messages)
        {
            var chatMessage = JsonSerializer.Deserialize<WriteToChatMessage>(message.JsonMessage);
            chatMessage = chatMessage with { messageId = message.Id };
            chatMessages.Add(chatMessage);
        }

        return chatMessages;
    }
    finally
    {
        await db.DisposeAsync();
    }
});


app.Run();
