using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codex.Logging
{
    public class ConsoleLogger(MessageKind minVerbosity = MessageKind.Informational) : TextLogger(Console.Out)
    {
        public static ConsoleLogger Instance { get; } = new();

        public MessageKind MinVerbosity { get; set; } = minVerbosity;

        public override void LogError(string error)
        {
            Console.Error.WriteLine(error);
        }

        public override void LogMessage(string message, MessageKind kind)
        {
            if (kind >= MinVerbosity)
            {
                base.LogMessage(message, kind);
            }
        }
    }
}
