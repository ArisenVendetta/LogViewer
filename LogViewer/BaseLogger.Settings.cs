using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LogViewer
{
    public abstract partial class BaseLogger
    {
        public static ILoggerFactory? LoggerFactory { get; set; }

        public static string LogDateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff (zzz)";
        public static bool LogUTCTime { get; set; }
        public static bool IncludeTimestampInOutput { get; set; } = true;
    }
}
