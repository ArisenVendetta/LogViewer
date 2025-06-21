using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogViewer
{
    public delegate Task LogEventHandler(object sender, LogEventArgs eventArgs);
}
