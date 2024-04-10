using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatterChatHandler;

public abstract class ChatHandler
{
    public abstract string Name { get; }
    public abstract string Description { get; }

    public IMessenger Messenger { get; set; }
    public ChatHandler(IMessenger messenger)
    {
        Messenger = messenger;
    }

    public abstract Task<bool> SendChatAsync(WriteToChatMessage message);
}
