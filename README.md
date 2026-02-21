# LogViewer (RealTimeLogStream)

**Version:** 0.2.0 | **Framework:** .NET 8.0 Windows | **License:** MIT

A real-time log viewer control for .NET 8 WPF applications. LogViewer integrates seamlessly with `Microsoft.Extensions.Logging` to display structured, color-coded, filterable logs in your application's UI.

---

## Features

- **WPF UserControl** for displaying logs in real time
- **Standard DI integration** via `ILogger<T>` and `ILoggingBuilder.AddLogViewer()`
- **Framework agnostic** - works alongside NLog, Serilog, or any logging provider
- **Thread-safe** log collection with automatic UI marshalling
- **Color-coded categories** for visual log differentiation
- **Regex and wildcard filtering** with NonBacktracking mode for security
- **Log level filtering** with inclusive or exact match modes
- **Pause/resume** with automatic buffering
- **Export logs** to JSON, CSV, or TXT
- **Auto-scroll** with manual override
- **Configurable memory limits** with automatic trimming

---

## Installation

### NuGet Package

```bash
dotnet add package RealTimeLogStream --source https://nuget.pkg.github.com/ArisenVendetta/index.json
```

Or add to your `.csproj`:

```xml
<PackageReference Include="RealTimeLogStream" Version="0.2.0" />
```

### Project Reference

Add the LogViewer project to your solution and reference it from your WPF application.

---

## Quick Start

### 1. Configure Services

```csharp
// App.xaml.cs
using LogViewer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);

            // Add LogViewer for real-time UI logging
            builder.AddLogViewer(options =>
            {
                options.MinimumLevel = LogLevel.Trace;
                options.MaxQueueSize = 10000;

                // Optional: Set colors for specific categories
                options.CategoryColors["MyService"] = Colors.DodgerBlue;
                options.CategoryColors["DataAccess"] = Colors.MediumPurple;
            });

            // Optional: Add other providers (NLog, Serilog, etc.)
            // builder.AddNLog();
        });

        // Register your services
        services.AddTransient<MyViewModel>();

        ServiceProvider = services.BuildServiceProvider();

        // Required: Attach logger factory to LogViewer for internal logging
        ServiceProvider.AttachLoggerFactoryToLogViewer();
    }
}
```

### 2. Add LogControl to Your XAML

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns:logViewer="clr-namespace:LogViewer;assembly=LogViewer">
    <Grid>
        <logViewer:LogControl MaxLogSize="5000"
                              AutoScroll="True"
                              HandleFilterVisible="True"
                              PausingEnabled="True" />
    </Grid>
</Window>
```

### 3. Use Standard ILogger

```csharp
public class MyViewModel
{
    private readonly ILogger<MyViewModel> _logger;

    public MyViewModel(ILogger<MyViewModel> logger)
    {
        _logger = logger;
    }

    public void DoWork()
    {
        _logger.LogInformation("Starting work...");

        try
        {
            // ... work
            _logger.LogDebug("Processing item {Id}", itemId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Work failed");
        }
    }
}
```

That's it! Logs will appear in the LogControl in real time.

---

## Configuration Options

### LogViewerOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MinimumLevel` | `LogLevel` | `Trace` | Minimum log level to process |
| `MaxQueueSize` | `int` | `10000` | Maximum log entries in memory |
| `CategoryColors` | `Dictionary<string, Color>` | Empty | Colors for specific categories |
| `DateTimeFormat` | `string` | `"yyyy-MM-dd HH:mm:ss.fff (zzz)"` | Timestamp format |
| `UseUtcTime` | `bool` | `false` | Use UTC instead of local time |
| `StripNamespaceFromCategory` | `bool` | `true` | Remove namespace from category names |

### LogControl Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MaxLogSize` | `int` | `10000` | Maximum displayed log entries |
| `AutoScroll` | `bool` | `true` | Auto-scroll to latest entry |
| `HandleFilter` | `string` | `""` | Regex filter for log handles |
| `IgnoreCase` | `bool` | `false` | Case-insensitive filtering |
| `HandleFilterVisible` | `bool` | `true` | Show filter input in UI |
| `PausingEnabled` | `bool` | `true` | Enable pause/resume feature |
| `LogDisplayFormat` | `string` | See below | Custom display format |
| `LogDisplayFormatDelimiter` | `string` | `" "` | Delimiter between format sections |

---

## Custom Display Format

Customize how log entries are displayed using format placeholders:

| Placeholder | Description |
|-------------|-------------|
| `{timestamp}` | Formatted date/time |
| `{loglevel}` | Log level (Trace, Debug, Info, etc.) |
| `{threadid}` | Thread ID |
| `{handle}` | Logger category name |
| `{message}` | Log message text |

**Examples:**

```xml
<!-- Timestamp | Handle | Message -->
<logViewer:LogControl LogDisplayFormat="{}{timestamp}|{handle}|{message}"
                      LogDisplayFormatDelimiter="|"/>

<!-- Detailed with log level -->
<logViewer:LogControl LogDisplayFormat="{}{timestamp}|{loglevel}|{threadid}|{handle}|{message}"
                      LogDisplayFormatDelimiter="|"/>
```

---

## Filtering

### Regex Filtering

```xml
<logViewer:LogControl HandleFilter="Error|Warning" IgnoreCase="True"/>
```

### Wildcard Filtering

Wildcards are automatically converted to regex:

```xml
<logViewer:LogControl HandleFilter="*Service*" IgnoreCase="True"/>
```

### Programmatic Filtering

```csharp
// Convert wildcard to regex
string regex = LogControlViewModel.WildcardToRegex("*Error*");
logControl.HandleFilter = regex;
```

---

## Working with Other Logging Providers

LogViewer works alongside other providers. Logs are sent to all configured providers:

```csharp
services.AddLogging(builder =>
{
    // Add NLog for file logging
    builder.AddNLog();

    // Add Serilog for structured logging
    builder.AddSerilog();

    // Add LogViewer for UI display
    builder.AddLogViewer();
});
```

---

## Legacy Pattern: BaseLogger Inheritance

For scenarios where DI isn't available, you can extend `BaseLogger`:

```csharp
// Initialize once at startup
BaseLogger.Initialize(loggerFactory, maxLogQueueSize: 10000);

// Create a logger by extending BaseLogger
public class MyService : BaseLogger
{
    public MyService() : base("MyService", Colors.Blue) { }

    public void DoWork()
    {
        LogInformation("Starting work...");
        LogWarning("This is a warning");
        LogError("This is an error");
    }
}

// Or use the static factory
var logger = BaseLogger.CreateLogger("MyCategory", Colors.Green);
logger.LogInformation("Hello!");
```

> **Note:** Don't mix both patterns for the same logger to avoid duplicate log entries.

---

## Export Formats

Export logs programmatically or via the UI:

- **JSON** - Structured data for programmatic analysis
- **CSV** - Spreadsheet-compatible format
- **TXT** - Plain text with configurable format

```csharp
var result = await logControl.LogControlViewModel.ExportLogsAsync();
if (result.Success)
{
    Console.WriteLine($"Exported to: {result.FilePath}");
}
```

---

## Thread Safety

LogViewer is designed for multi-threaded applications:

- **ConcurrentQueue** for lock-free log buffering
- **HashSet deduplication** with O(1) lookup
- **Automatic UI thread marshalling**
- **NonBacktracking regex** with 100ms timeout to prevent ReDoS

---

## API Reference

### Core Types

| Type | Description |
|------|-------------|
| `LogControl` | WPF UserControl for displaying logs |
| `LogControlViewModel` | ViewModel for log operations |
| `BaseLoggerProvider` | `ILoggerProvider` implementation |
| `BaseLogger` | `ILogger` implementation |
| `IBaseLoggerSink` | Interface for log event routing |
| `BaseLoggerSink` | Default singleton sink |
| `LogEventArgs` | Log event data container |
| `LogCollection` | Thread-safe observable collection |

### Extension Methods

```csharp
// Add LogViewer to logging
builder.AddLogViewer();
builder.AddLogViewer(options => { ... });
builder.AddLogViewer(innerFactory);
builder.AddLogViewer(innerFactory, options => { ... });

// Attach logger factory after building
serviceProvider.AttachLoggerFactoryToLogViewer();
```

---

## Example Project

See the `LogViewerExample` project for a complete working example demonstrating:

- DI setup with `AddLogViewer()`
- Category colors configuration
- Integration with NLog
- Continuous log generation for testing
- Exception logging

---

## Contributing

Contributions are welcome!

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Run tests (`dotnet test`)
5. Commit (`git commit -m 'Add amazing feature'`)
6. Push (`git push origin feature/amazing-feature`)
7. Open a Pull Request

### Development Setup

```bash
git clone https://github.com/Arisenvendetta/LogViewer.git
cd LogViewer
dotnet restore
dotnet build
dotnet test
```

---

## License

This project is licensed under the [MIT License](LICENSE.txt).

### Third-Party Licenses

**MIT License:**
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- [Fody](https://github.com/Fody/Fody)
- [Microsoft.Extensions.Logging](https://dot.net)
- [Newtonsoft.Json](https://www.newtonsoft.com/json)
- [PropertyChanged.Fody](https://github.com/Fody/PropertyChanged)

**Apache-2.0 License:**
- [CsvHelper](https://joshclose.github.io/CsvHelper/)

See `THIRD_PARTY_LICENSES/` for full license texts.
