using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatterChatHandler;

public record class WriteToChatMessage(string alias, string message, string background, string foreground, string fontStyle, int? messageId = null);