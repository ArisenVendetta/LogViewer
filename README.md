# LogViewer

A reusable, real-time log viewer control and infrastructure for .NET 8 WPF applications.  
LogViewer provides structured, color-coded, and filterable logging with MVVM-friendly APIs, event-driven extensibility, and seamless integration with `Microsoft.Extensions.Logging`.

---

## Features

- **WPF UserControl** for displaying logs in real time
- **MVVM-ready ViewModel** for log filtering, pausing, and size management
- **Thread-safe, observable log collection** (`LogCollection`)
- **Structured log events** with color, timestamp, and log level
- **Integration with `ILogger` and `LogLevel`**
- **Wildcard log handle filtering**
- **Pause/resume and clear log functionality**
- **Customizable log message formatting and coloring**
- **Event-driven extensibility for custom log consumers**

---

## Getting Started

### Prerequisites

- .NET 8 SDK
- WPF application

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
    <logViewer:LogControl x:Name="LogViewerControl" />
    ```

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

5. **Filter, pause, or clear logs in the UI using the LogControl’s built-in features.**

---

## Core Components

- **LogControl**: WPF UserControl for displaying and interacting with logs.
- **LogControlViewModel**: Handles log filtering, pausing, and collection management.
- **LogCollection**: Thread-safe, observable collection of log events.
- **BaseLogger**: Abstract base class for log-capable objects.
- **ILoggable**: Interface for log-capable objects.
- **LogEventArgs**: Encapsulates log event data.
- **Converters**: WPF value converters for log level coloring and brush conversion.

---

## Customization

- **Log Colors**: Assign a `Color` to each logger for visual distinction.
- **Filtering**: Use wildcards (`*`, `?`) or regex in the log handle filter.
- **Max Log Size**: Set `MaxLogSize` in the view model to control memory usage.

---

## Example
```csharp
public class ExampleVM : BaseLogger 
{ 
    public ExampleVM() : base("ExampleVM", Colors.Blue) 
    {
    }
    
    public void DoSomething()
    {
        LogInfo("This is an info message.");
        LogWarning("This is a warning.");
        LogError("This is an error.");
    }
}
```

A more in-depth example can be found in the LogViewerExample project (included with the source code), which demonstrates how to use the LogViewer in a WPF application.

---

## License

This project is intended to be open source, but a specific license has not yet been chosen.
If you have questions or suggestions about licensing, please open an issue or discussion.

