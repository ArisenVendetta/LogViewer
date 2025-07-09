# LogViewer

A reusable, real-time log viewer control and infrastructure for .NET 8 WPF applications.  
LogViewer provides structured, color-coded, and filterable logging with MVVM-friendly APIs, event-driven extensibility, and seamless integration with `Microsoft.Extensions.Logging`.

---

## Features

- **WPF UserControl** for displaying logs in real time.
- **MVVM-ready ViewModel** for log filtering, pausing, and size management.
- **Thread-safe, observable log collection** (`LogCollection`).
- **Structured log events** with color, timestamp, and log level.
- **Integration with `ILogger` and `LogLevel`**.
- **Regular expression log handle filtering** with case sensitivity options.
- **Pause/resume and clear log functionality**.
- **Customizable log message formatting and coloring**.
- **Event-driven extensibility for custom log consumers**.
- **Export logs** to various formats (e.g., JSON, CSV) for diagnostics or archival purposes.
- **Auto-scroll** to the latest log entry or manual scrolling.
- **Support for large log sizes** with configurable memory limits.
- **Built-in commands** for exporting, clearing, and pausing logs.
- **Custom Format Support**: Choose how you want your log display to look using pre-defined placeholders

---

## Getting Started

### Prerequisites

- .NET 8 SDK
- WPF application

## Acknowledgments

This project uses the following third-party libraries, licensed under the MIT License:

- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- [Fody](https://github.com/Fody/Fody)
- [Microsoft.Extensions.Logging](https://dot.net)
- [Newtonsoft.Json](https://www.newtonsoft.com/json)
- [PropertyChanged.Fody](https://github.com/Fody/PropertyChanged)

The full text of the MIT License for these libraries can be found in the `THIRD_PARTY_LICENSES\MIT.md` file.


This project uses the following third-party libraries, licensed under the Apache-2.0 License:

- [CsvHelper](https://joshclose.github.io/CsvHelper/)

the full text of the Apache-2.0 License for these libraries can be found in the `THIRD_PARTY_LICENSES\Apache-2.0.md` file.

### Installation

1. **Add the LogViewer project** to your solution.
2. **Reference LogViewer** from your WPF application project.

### Usage

1. **Initialize the logger factory** at application startup:

    ```csharp
    BaseLogger.Initialize(loggerFactory, maxLogQueueSize: 1000, logDateTimeFormat: "yyyy-MM-dd HH:mm:ss.fff");
    ```

2. **Add the LogControl to your XAML:**

    ```xml
    <logViewer:LogControl x:Name="LogViewerControl" 
                          MaxLogSize="5000" 
                          IgnoreCase="True" 
                          HandleFilter="Reg(?:ular)?Ex(?:pression)" 
                          AutoScroll="True"
                          LogDisplayFormat="{}{timestamp}|{threadid}|{handle}|{message}"/>
    ```

    The user control provides properties to define:
    - `MaxLogSize`: Maximum number of log entries to keep in memory.
    - `IgnoreCase`: Whether to ignore case in log handle filtering.
    - `HandleFilter`: A string to filter log handles using regex.
    - `AutoScroll`: Whether to always show the latest log or stay where the user scrolls.

    Make sure to add the appropriate XML namespace for `logViewer`.

3. **Inherit from `BaseLogger` in your view models or services:**

    ```csharp
    public class MyViewModel : BaseLogger
    {
        public MyViewModel() : base("MyViewModel", Colors.Green) { }
        // Use LogInfo, LogError, etc.
    }
    ```

4. **Log messages using the provided methods:**

    ```csharp
    LogInfo("Application started.");
    LogError("An error occurred.");
    LogException(exception);
    ```

5. **Filter, pause, clear, or export logs in the UI using the LogControl’s built-in features.**

---

## Core Components

- **LogControl**: WPF UserControl for displaying and interacting with logs.
- **LogControlViewModel**: Handles log filtering, pausing, exporting, and collection management.
- **LogCollection**: Thread-safe, observable collection of log events.
- **BaseLogger**: Abstract base class for log-capable objects.
- **ILoggable**: Interface for log-capable objects.
- **LogEventArgs**: Encapsulates log event data.
- **Converters**: WPF value converters for log level coloring and brush conversion.
- **ExportLogsCommand**: Command for exporting logs to supported file formats.
- **ClearLogsCommand**: Command for clearing all logs.
- **TogglePauseCommand**: Command for pausing or resuming log updates.

---

## Customization

- **Log Colors**: Assign a `Color` to each logger for visual distinction.
- **Filtering**: Use regular expressions in the log handle filter with optional case sensitivity.
- **Max Log Size**: Set `MaxLogSize` in the view model to control memory usage.
- **Export Formats**: Extend or customize supported file types for log export.

---

## Example
```csharp
public class ExampleClass : BaseLogger 
{ 
    public ExampleClass() : base("Example", Colors.Blue) 
    {
    }

    public void DoSomething()
    {
        try
        {
            LogInfo("This is an info message.");
            LogWarning("This is a warning.");
            LogError("This is an error.");
        }
        catch (Exception ex)
        {
            LogException(ex, "An exception happened");
        }
    }
}
```


A more in-depth example can be found in the LogViewerExample project (included with the source code), which demonstrates how to use the LogViewer in a WPF application.

---

## License
This project is licensed under the terms of the [GNU General Public License v3.0](LICENSE.txt).