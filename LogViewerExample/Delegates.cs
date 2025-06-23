using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogViewerExample
{
    /// <summary>
    /// Delegate for executing an action synchronously from the UI
    /// </summary>
    /// <returns></returns>
    public delegate void Command();

    /// <summary>
    /// Delegate for executing an action asynchronously from the UI
    /// </summary>
    /// <returns><see cref="Task"/></returns>
    public delegate Task CommandAsync();

    /// <summary>
    /// Delegate for executing an action with an argument synchronously from the UI
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    public delegate void CommandWithArg(object argument);

    /// <summary>
    /// Delegate for executing an action with an argument asynchronously from the UI
    /// </summary>
    /// <param name="argument"></param>
    /// <returns><see cref="Task"/></returns>
    public delegate Task CommandWithArgAsync(object argument);
}
