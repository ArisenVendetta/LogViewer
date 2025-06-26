using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogViewer
{
    /// <summary>
    /// Represents a method that handles log events asynchronously.
    /// </summary>
    /// <param name="sender">The source of the log event.</param>
    /// <param name="eventArgs">The event data containing details about the log event.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public delegate Task LogEvent(object sender, LogEventArgs eventArgs);
}
