# LogViewer (RealTimeLogStream)

**Version:** 0.1.4.0 | **Framework:** .NET 8.0 Windows | **License:** GPL v3

A reusable, real-time log viewer control and infrastructure for .NET 8 WPF applications.
LogViewer provides structured, color-coded, and filterable logging with MVVM-friendly APIs, and event-driven extensibility.

It is designed to be easily integrated into WPF applications, allowing developers to monitor application logs in real time with minimal setup.

---

## Features

- **WPF UserControl** (`LogControl`) for displaying logs in real time.
- **MVVM-ready ViewModel** for log filtering, pausing, and size management.
- **Thread-safe, observable log collection** (`LogCollection`) with O(1) duplicate detection.
- **Structured log events** with color, timestamp, thread ID, and log level.
- **Integration with `ILogger` and `LogLevel`** from Microsoft.Extensions.Logging.
- **Regular expression and wildcard pattern filtering** with case sensitivity options.
- **Log level filtering** with inclusive or exact match modes.
- **Pause/resume and clear log functionality** with automatic buffering during pause.
- **Customizable log message formatting** using template placeholders.
- **Event-driven extensibility** for custom log consumers.
- **Export logs** to JSON, CSV, or TXT formats for diagnostics or archival.
- **Auto-scroll** to the latest log entry with manual override.
- **Configurable memory limits** with automatic trimming (10% overflow buffer).
- **Built-in commands** for exporting, clearing, and pausing logs.
- **High-performance regex** using `NonBacktracking` mode for secure filtering.

---

## Getting Started

### Prerequisites

- .NET 8 SDK
- WPF application

### Installation

#### Option 1: NuGet Package (Recommended)

Install the `RealTimeLogStream` package from GitHub Packages:

```bash
dotnet add package RealTimeLogStream --source https://nuget.pkg.github.com/ArisenVendetta/index.json
```

Or add to your `.csproj`:

```xml
<PackageReference Include="RealTimeLogStream" Version="0.1.4.0" />
```

#### Option 2: Project Reference

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
                          LogDisplayFormat="{}{timestamp}|{threadid}|{handle}|{message}"
                          LogDisplayFormatDelimiter="|"/>
    ```

    The user control provides properties to define:
    - `MaxLogSize`: Maximum number of log entries to keep in memory.
    - `IgnoreCase`: Whether to ignore case in log handle filtering.
    - `HandleFilter`: A string to filter log handles using regex or wildcard patterns (e.g., `*Error*`).
    - `AutoScroll`: Whether to always show the latest log or stay where the user scrolls.
    - `HandleFilterVisible`: Whether to show the filter input in the UI.
    - `PausingEnabled`: Whether to enable the pause/resume functionality.
    - `LogDisplayFormat`: Custom format template (see [Custom Format Support](#custom-format-support)).
    - `LogDisplayFormatDelimiter`: Delimiter used to separate format sections.

    Make sure to add the appropriate XML namespace for `logViewer`.


3. **Create an instance of `BaseLoggerProvider` in your application startup and register it with your DI container:** or
   **Add logging to your class by inheriting from `BaseLogger`:**

    ```csharp
    public class MyViewModel : BaseLogger
    {
        public MyViewModel() : base("MyViewModel", Colors.Green) { }
        // Use LogInfo, LogError, etc.
    }
    ```


    ```csharp
    var baseLoggerProvider = new BaseLoggerProvider();
    loggerFactory.AddProvider(loggerProvider);

    ServiceCollection services = new();
    services.AddSingleton<BaseLoggerProvider>(baseLoggerProvider);

    ServiceProvider serviceProvider = services.BuildServiceProvider();
    ```


    Then, you can use `baseLoggerProvider.CreateLogger(string categoryName, Color? color = null, LogLevel? logLevel = null)` 
    to create loggers in your classes which are implementing ILogger, and are of type `Logger`.


    ```csharp
    public class MyService
    {
        private readonly ILogger<MyService> _logger;
        public MyService(ILogger<MyService> logger)
        {
            _logger = logger;
        }
        public void DoWork()
        {
            _logger.LogInformation("Doing work...");
            // Other work...
        }
    }
    ```

    ```csharp
    IServiceProvider serviceProvider = ...; // Your DI container

    var baseLoggerProvider = serviceProvider.GetRequiredService<BaseLoggerProvider>();
    var myService = new(baseLoggerProvider.CreateLogger("some-unique-name", Colors.Blue));
    ```
    or
    ```csharp
    IServiceProvider serviceProvider = ...; // Your DI container

    var baseLoggerProvider = serviceProvider.GetRequiredService<BaseLoggerProvider>();
    var myService = new(baseLoggerProvider.CreateLogger<MyService>(Colors.Blue));
    ```

    **Alternative: Use the static factory method (without DI):**

    ```csharp
    var logger = BaseLogger.CreateLogger("MyCategory", Colors.Blue);
    logger.LogInformation("This works without DI setup!");
    ```

4. **Log messages using the provided methods:**

```csharp
LogInformation("Application started.");
LogError("An error occurred.");
LogException(exception);
```

5. **Filter, pause, clear, or export logs in the UI using the LogControl's built-in features.**

---

## Core Components

- **LogControl**: WPF UserControl for displaying and interacting with logs.
- **LogControlViewModel**: Handles log filtering, pausing, exporting, and collection management.
- **LogCollection**: Thread-safe, observable collection of log events with HashSet-based deduplication.
- **BaseLogger**: Abstract base class for log-capable objects; implements `ILoggable` and `ILogger`.
- **Logger**: Concrete logger implementation created via `BaseLoggerProvider` or `BaseLogger.CreateLogger()`.
- **BaseLoggerProvider**: Factory for creating logger instances with optional color assignments.
- **ILoggable**: Interface for log-capable objects.
- **LogEventArgs**: Record class encapsulating log event data (level, handle, text, color, timestamp, thread ID).
- **Converters**: WPF value converters for log level coloring and brush conversion.
- **ExportLogsCommand**: Command for exporting logs to JSON, CSV, or TXT formats.
- **ClearLogsCommand**: Command for clearing all logs.
- **TogglePauseCommand**: Command for pausing or resuming log updates.

---

## Customization

### Log Colors

Assign a `Color` to each logger for visual distinction:

```csharp
// Via BaseLogger inheritance
public class MyService : BaseLogger
{
    public MyService() : base("MyService", Colors.Teal) { }
}

// Via BaseLoggerProvider
var logger = baseLoggerProvider.CreateLogger("MyCategory", Colors.Purple);

// Via static factory
var logger = BaseLogger.CreateLogger("MyCategory", Colors.Orange);
```

### Filtering

**Regex Filtering:**
```xml
<logViewer:LogControl HandleFilter="Error|Warning" IgnoreCase="True"/>
```

**Wildcard Filtering:**
The filter also supports wildcard patterns that are automatically converted to regex:
```xml
<logViewer:LogControl HandleFilter="*Service*" IgnoreCase="True"/>
```

**Log Level Filtering:**
Filter by log level with inclusive (show selected level and above) or exact match modes via the ViewModel.

### Memory Management

Control memory usage with configurable limits:

```csharp
// At initialization - set max queue size (default: 10,000)
BaseLogger.Initialize(loggerFactory, maxLogQueueSize: 5000);
```

```xml
<!-- In XAML - set max display size -->
<logViewer:LogControl MaxLogSize="5000"/>
```

When the limit is reached, older entries are automatically trimmed with a 10% buffer to prevent constant trimming.

### Custom Format Support

Customize the log display format using placeholders:

| Placeholder | Description |
|------------|-------------|
| `{handle}` | Logger name/category |
| `{message}` | Log message text |
| `{timestamp}` | Formatted date/time |
| `{loglevel}` | Log level (Info, Warning, Error, etc.) |
| `{threadid}` | Thread ID |

**Example formats:**

```xml
<!-- Timestamp | Thread | Handle | Message -->
<logViewer:LogControl LogDisplayFormat="{}{timestamp}|{threadid}|{handle}|{message}"
                      LogDisplayFormatDelimiter="|"/>

<!-- Simple: Handle - Message -->
<logViewer:LogControl LogDisplayFormat="{}{handle}|{message}"
                      LogDisplayFormatDelimiter="|"/>

<!-- Detailed with log level -->
<logViewer:LogControl LogDisplayFormat="{}{timestamp}|{loglevel}|{handle}|{message}"
                      LogDisplayFormatDelimiter="|"/>
```

### Export Formats

Export logs to various formats:
- **JSON**: Structured data format for programmatic analysis
- **CSV**: Spreadsheet-compatible format
- **TXT**: Plain text format

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

## Thread Safety & Performance

LogViewer is designed for high-performance, thread-safe operation in multi-threaded applications:

- **ConcurrentQueue**: Log messages are buffered using `ConcurrentQueue<T>` for lock-free producer operations.
- **HashSet Deduplication**: O(1) duplicate detection prevents redundant log entries.
- **Lock-based Collection**: `LogCollection` uses internal locking for thread-safe enumeration and modification.
- **NonBacktracking Regex**: Filter patterns use `RegexOptions.NonBacktracking` with a 100ms timeout to prevent ReDoS attacks.
- **Dispatcher-aware Operations**: UI updates are automatically marshaled to the UI thread.
- **Snapshot Iteration**: Collection enumeration uses snapshots to prevent "collection modified" exceptions.

---

## Integration with Existing Logging Frameworks

LogViewer integrates with Microsoft.Extensions.Logging and can work alongside other logging providers like NLog, Serilog, etc.

**Example with NLog:**

```csharp
// In App.xaml.cs or startup
var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddNLog(); // Add NLog provider
});

var baseLoggerProvider = new BaseLoggerProvider();
loggerFactory.AddProvider(baseLoggerProvider);

BaseLogger.Initialize(loggerFactory, maxLogQueueSize: 10000);

// Register for DI
services.AddSingleton(baseLoggerProvider);
```

This allows logs to be written to both NLog targets (file, console, etc.) and displayed in the LogViewer UI simultaneously.

---

## Contributing

Contributions are welcome! Here's how to get started:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Run the example project to test your changes
5. Commit your changes (`git commit -m 'Add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

### Development Setup

```bash
git clone https://github.com/Arisenvendetta/LogViewer.git
cd LogViewer
dotnet restore
dotnet build
```

### CI/CD

This project uses GitHub Actions for continuous integration. When a version tag (e.g., `v0.1.5.0`) is pushed, the workflow automatically:
1. Builds the project in Release configuration
2. Creates the NuGet package
3. Publishes to GitHub Packages

---

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

The full text of the Apache-2.0 License for these libraries can be found in the `THIRD_PARTY_LICENSES\Apache-2.0.md` file.

---

## License

This project is licensed under the terms of the [GNU General Public License v3.0](LICENSE.txt).