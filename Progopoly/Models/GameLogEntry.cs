using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace Progopoly.Models
{
    public class GameLogEntry
    {
        public DateTime Time { get; set; }
        public string Message { get; set; }
        public StackFrame StackFrame { get; set; }
    }
}