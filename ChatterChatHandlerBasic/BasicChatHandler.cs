using ChatterChatHandler;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatterChatHandlerBasic;

public class BasicChatHandler : ChatHandler
{
    public override string Name => "Basic Chat Handler";

    public override string Description => "Sends a chat to ourself.";

    public BasicChatHandler(IMessenger messenger) : base(messenger) { }

    public override void Activate()
    {
        //NOOP
    }

    public override void Deactivate()
    {
        //NOOP
    }

    public override Task<bool> SendChatAsync(WriteToChatMessage message)
    {
        Messenger.Send(message);
        return Task.FromResult(true);
    }
}
