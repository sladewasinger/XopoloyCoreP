using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GameModels = Progopoly.Models;

namespace Xopoly.Models
{
    public class ServerGameLog : GameModels.IGameLog
    {
        private ConcurrentQueue<GameModels.GameLogEntry> _entries = new ConcurrentQueue<GameModels.GameLogEntry>();

        public void Log(string message)
        {
            _entries.Enqueue(new GameModels.GameLogEntry() { Time = DateTime.Now, Message = $"{message}" });
        }

        public IEnumerable<string> DumpLog()
        {
            return _entries.Select(x => $"{x.Time}: {x.Message}");
        }

        public void EmptyLog()
        {
            _entries.TakeWhile((log) => log != null);
        }
    }
}