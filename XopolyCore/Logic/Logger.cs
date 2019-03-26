using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XopolyCore.Logic
{
    public class Logger
    {
        BlobManager _blobManager;

        public Logger(BlobManager blobManager)
        {
            _blobManager = blobManager;
        }

        public void Log(Exception ex)
        {
            string logMessage = $"[{DateTime.Now}] {ex}";
            _blobManager.UploadText(logMessage);
        }
    }
}
