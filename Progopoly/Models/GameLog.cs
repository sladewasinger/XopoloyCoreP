using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace Progopoly.Models
{
    public class GameLog : IGameLog
    {
        private BlockingCollection<GameLogEntry> _entries = new BlockingCollection<GameLogEntry>();

        public void Log(string message)
        {
            _entries.Add(new GameLogEntry() { Time = DateTime.Now, Message = $"{message}", StackFrame = new StackFrame(1) });
        }

        public IEnumerable<string> DumpLog()
        {
            return _entries.Select(x => $"{$"{x.Time} [{x.StackFrame.GetMethod().Name}]".PadRight(50).Substring(0, 50)}: {x.Message}");
        }

        public void EmptyLog()
        {
            _entries.TakeWhile((log) => log != null);
        }
    }
}