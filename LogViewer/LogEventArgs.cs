using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace LogViewer
{
    public class LogEventArgs(string message, Color color) : EventArgs
    {
        public Color LogColor { get; } = color;
        public string LogText { get; } = message ?? throw new ArgumentNullException(nameof(message));
        public DateTime LogDateTime { get; init; }
    }
}
