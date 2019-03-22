using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Progopoly.Models
{
    public interface IGameLog
    {
        void EmptyLog();
        void Log(string message);
        IEnumerable<string> DumpLog();
    }
}