using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;

namespace LogViewer
{
    /// <summary>
    /// Interaction logic for LogControl.xaml.
    /// Provides a WPF UserControl for displaying and managing real-time log events.
    /// </summary>
    public partial class LogControl : UserControl, IDisposable
    {
        const string ReplacementPrefixKey = nameof(ReplacementPrefixKey);
        const string ReplacementSuffixKey = nameof(ReplacementSuffixKey);
        const string ReplacementContentKey = nameof(ReplacementContentKey);

        /// <summary>
        /// Represents a collection of default replacement elements used for templating purposes.
        /// </summary>
        /// <remarks>This dictionary maps predefined keys to their corresponding <see
        /// cref="FrameworkElementFactory"/> instances. A value of <see langword="null"/> indicates that no replacement
        /// element is defined for the associated key.</remarks>
        private static readonly IReadOnlyDictionary<string, FrameworkElementFactory?> DefaultReplacement = new Dictionary<string, FrameworkElementFactory?>()
        {
            { ReplacementPrefixKey, null },
            { ReplacementSuffixKey, null },
            { ReplacementContentKey, null }
        }.AsReadOnly();

        /// <summary>
        /// The view model backing this control, responsible for log data and state.
        /// </summary>
        private readonly LogControlViewModel _viewModel;
        private static readonly string[] _formatSplitComponents = ["{handle}", "{message}"];
        private static readonly string[] _supportedPlacementHolders = ["{handle}", "{message}", "{timestamp}", "{loglevel}", "{threadid}"];
        private bool _disposedValue;

        private ScrollViewer? _scrollViewer;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogControl"/> class.
        /// Sets up data context, event handlers, and auto-scroll behavior.
        /// </summary>
        public LogControl()
        {
            InitializeComponent();
            _viewModel = new LogControlViewModel(Dispatcher);
            this.DataContext = _viewModel;

            __logList.ItemTemplate = GenerateDataTemplate(LogDisplayFormat, LogDisplayFormatDelimiter);

            _viewModel.LogEvents.CollectionChanged += HandleCollectionChanged;
        }

        /// <summary>
        /// Gets the view model for controlling log operations.
        /// </summary>
        public LogControlViewModel LogControlViewModel => _viewModel;

        /// <summary>
        /// Gets or sets the maximum size, in bytes, of the log queue.
        /// </summary>
        /// <remarks>If the value assigned exceeds the maximum log queue size, it will be automatically
        /// capped at <see cref="BaseLogger.MaxLogQueueSize"/>.</remarks>
        public int MaxLogSize
        {
            get => (int)GetValue(MaxLogSizeProperty);
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "MaxLogSize cannot be negative.");
                int newValue = value > BaseLogger.MaxLogQueueSize ? BaseLogger.MaxLogQueueSize : value;

                SetValue(MaxLogSizeProperty, newValue);
                _viewModel.MaxLogSize = newValue;
            }
        }

        /// <summary>
        /// Gets or sets the filter string used to match specific handles.
        /// </summary>
        /// <remarks>This property is used to specify a filter that determines which handles are included
        /// in the operation. Setting this property updates the associated view model's handle filter.</remarks>
        public string HandleFilter
        {
            get => (string)GetValue(HandleFilterProperty) ?? string.Empty;
            set
            {
                SetValue(HandleFilterProperty, value);
                _viewModel.LogHandleFilter = value ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether string comparisons should ignore case.
        /// </summary>
        public bool IgnoreCase
        {
            get => (bool)GetValue(IgnoreCaseProperty);
            set
            {
                SetValue(IgnoreCaseProperty, value);
                _viewModel.LogHandleIgnoreCase = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the content should automatically scroll to display the most recent
        /// updates.
        /// </summary>
        public bool AutoScroll
        {
            get => (bool)GetValue(AutoScrollProperty);
            set
            {
                SetValue(AutoScrollProperty, value);
                _viewModel.AutoScroll = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the handle filter is visible.
        /// </summary>
        public bool HandleFilterVisible
        {
            get => (bool)GetValue(HandleFilterVisibleProperty);
            set
            {
                SetValue(HandleFilterVisibleProperty, value);
                _viewModel.HandleFilterVisible = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether pausing is enabled.
        /// </summary>
        public bool PausingEnabled
        {
            get => (bool)GetValue(PausingEnabledProperty);
            set
            {
                SetValue(PausingEnabledProperty, value);
                _viewModel.PausingEnabled = value;
            }
        }

        /// <summary>
        /// Gets or sets the format string used to display log entries.
        /// </summary>
        public string LogDisplayFormat
        {
            get => (string)GetValue(LogDisplayFormatProperty);
            set
            {
                string newFormat = string.IsNullOrWhiteSpace(value) ? BaseLogger.DefaultLogDisplayFormat : value;
                SetValue(LogDisplayFormatProperty, newFormat);
                __logList.ItemTemplate = GenerateDataTemplate(newFormat, _viewModel.LogDisplayFormatDelimiter);
                _viewModel.LogDisplayFormat = newFormat;
            }
        }

        /// <summary>
        /// Gets or sets the delimiter used to separate fields in the log display format.
        /// </summary>
        /// <remarks>Changing this property updates the log display format and applies the new delimiter
        /// to the log entries.</remarks>
        public string LogDisplayFormatDelimiter
        {
            get => (string)GetValue(LogDisplayFormatDelimiterProperty);
            set
            {
                string newDelimiter = string.IsNullOrEmpty(value) ? " " : value;
                SetValue(LogDisplayFormatDelimiterProperty, value);
                __logList.ItemTemplate = GenerateDataTemplate(_viewModel.LogDisplayFormat, newDelimiter);
                _viewModel.LogDisplayFormatDelimiter = newDelimiter;
            }
        }

        /// <summary>
        /// Handles changes to the <see cref="MaxLogSize"/> dependency property.
        /// </summary>
        /// <remarks>This method updates the <c>MaxLogSize</c> property of the associated view model, if
        /// available, to reflect the new value of the dependency property.</remarks>
        /// <param name="d">The <see cref="DependencyObject"/> on which the property value has changed.</param>
        /// <param name="e">The event data containing information about the property change, including the old and new values.</param>
        private static void OnMaxLogSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LogControl control && control._viewModel != null)
            {
                control._viewModel.MaxLogSize = (int)e.NewValue;
            }
        }

        /// <summary>
        /// Handles changes to the <see cref="HandleFilterProperty"/> dependency property.
        /// </summary>
        /// <remarks>This method updates the <c>LogHandleFilter</c> property of the associated view model,
        /// if available, to reflect the new value of the dependency property.</remarks>
        /// <param name="d">The <see cref="DependencyObject"/> on which the property value has changed.</param>
        /// <param name="e">The event data containing information about the property change, including the old and new values.</param>
        private static void OnHandleFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LogControl control && control._viewModel != null)
            {
#pragma warning disable CS8601 // Possible null reference assignment | null value is handled inside LogHandleFilter
                control._viewModel.LogHandleFilter = e.NewValue as string;
#pragma warning restore CS8601 // Possible null reference assignment.
            }
        }

        /// <summary>
        /// Handles changes to the <see cref="IgnoreCase"/> dependency property.
        /// </summary>
        /// <remarks>This method updates the associated view model's <c>LogHandleIgnoreCase</c> property
        /// to reflect the new value of the <see cref="IgnoreCase"/> property.</remarks>
        /// <param name="d">The <see cref="DependencyObject"/> on which the property value has changed.</param>
        /// <param name="e">The event data containing information about the property change.</param>
        private static void OnIgnoreCaseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LogControl control && control._viewModel != null)
            {
                control._viewModel.LogHandleIgnoreCase = (bool)e.NewValue;
            }
        }

        /// <summary>
        /// Handles changes to the <see cref="AutoScroll"/> dependency property.
        /// </summary>
        /// <remarks>This method updates the <see cref="LogControl"/>'s view model to reflect the new
        /// value of the <see cref="AutoScroll"/> property. If auto-scroll is enabled and a scroll viewer is available,
        /// the log list is automatically scrolled to the end.</remarks>
        /// <param name="d">The <see cref="DependencyObject"/> on which the property value has changed.</param>
        /// <param name="e">The event data containing information about the property change, including the old and new values.</param>
        private static void OnAutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LogControl control)
            {
                control._viewModel.AutoScroll = (bool)e.NewValue;
                // If auto-scroll is enabled, scroll to the end of the log list.
                if (control._viewModel.AutoScroll && control._scrollViewer != null)
                {
                    control._scrollViewer.ScrollToEnd();
                }
            }
        }

        /// <summary>
        /// Handles changes to the <see cref="HandleFilterVisible"/> dependency property.
        /// </summary>
        /// <remarks>This method updates the <c>HandleFilterVisible</c> property of the associated view
        /// model  when the dependency property value changes.</remarks>
        /// <param name="d">The object on which the property value has changed. Must be of type <see cref="LogControl"/>.</param>
        /// <param name="e">The event data containing information about the property change, including the old and new values.</param>
        private static void OnHandleFilterVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LogControl control && control._viewModel != null)
            {
                control._viewModel.HandleFilterVisible = (bool)e.NewValue;
            }
        }

        /// <summary>
        /// Handles changes to the <see cref="PausingEnabled"/> dependency property.
        /// </summary>
        /// <remarks>This method updates the associated view model's <see cref="PausingEnabled"/> property
        /// when the dependency property value changes.</remarks>
        /// <param name="d">The <see cref="DependencyObject"/> on which the property value has changed.</param>
        /// <param name="e">The event data containing information about the property change, including the old and new values.</param>
        private static void OnPausingEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LogControl control && control._viewModel != null)
            {
                control._viewModel.PausingEnabled = (bool)e.NewValue;
            }
        }

        /// <summary>
        /// Handles changes to the <see cref="LogControl"/> log display format when the associated dependency property
        /// changes.
        /// </summary>
        /// <remarks>This method updates the log display format of the <see cref="LogControl"/> by
        /// generating a new data template          based on the updated format string. If the new format is null or
        /// empty, a default format is applied.</remarks>
        /// <param name="d">The <see cref="DependencyObject"/> on which the property value has changed. Expected to be a <see
        /// cref="LogControl"/>.</param>
        /// <param name="e">The event data containing information about the property change, including the old and new values.</param>
        private static void OnHandleLogDisplayFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LogControl control && control._viewModel != null)
            {
#pragma warning disable CS8601 // Possible null reference assignment | null value is handled inside LogDisplayFormat
                string newFormat = e.NewValue as string ?? BaseLogger.DefaultLogDisplayFormat;
#pragma warning restore CS8601 // Possible null reference assignment.
                if (string.IsNullOrEmpty(newFormat)) newFormat = BaseLogger.DefaultLogDisplayFormat;
                control.__logList.ItemTemplate = control.GenerateDataTemplate(newFormat, control._viewModel.LogDisplayFormatDelimiter);
                control._viewModel.LogDisplayFormat = newFormat;
            }
        }

        /// <summary>
        /// Handles changes to the <see cref="LogControl.LogDisplayFormatDelimiter"/> dependency property.
        /// </summary>
        /// <remarks>This method updates the <see cref="LogControl"/> instance's log display format by
        /// regenerating the data template and updating the delimiter used in the log display. If the new value is null
        /// or empty, a default delimiter of a single space (" ") is used.</remarks>
        /// <param name="d">The <see cref="DependencyObject"/> on which the property value has changed. This is expected to be an
        /// instance of <see cref="LogControl"/>.</param>
        /// <param name="e">The event data containing information about the property change, including the old and new values.</param>
        private static void OnHandleLogDisplayFormatDelimiterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LogControl control && control._viewModel != null)
            {
#pragma warning disable CS8601 // Possible null reference assignment | null value is handled inside LogDisplayFormatDelimiter
                string newDelimiter = e.NewValue as string ?? " ";
#pragma warning restore CS8601 // Possible null reference assignment.
                if (string.IsNullOrEmpty(newDelimiter)) newDelimiter = " ";
                control.__logList.ItemTemplate = control.GenerateDataTemplate(control._viewModel.LogDisplayFormat, newDelimiter);
                control._viewModel.LogDisplayFormatDelimiter = newDelimiter;
            }
        }

        /// <summary>
        /// Handles the Unloaded event for the control, ensuring resources are disposed.
        /// </summary>
        private void LogViewer_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel?.Dispose();
        }

        /// <summary>
        /// Recursively searches for a <see cref="ScrollViewer"/> in the visual tree of the given dependency object.
        /// </summary>
        /// <param name="depObj">The root element to search from.</param>
        /// <returns>The first <see cref="ScrollViewer"/> found, or null if none exists.</returns>
        private static ScrollViewer? GetScrollViewer(DependencyObject depObj)
        {
            if (depObj is null) return null;
            if (depObj is ScrollViewer scrollViewer)
                return scrollViewer;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                var result = GetScrollViewer(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        /// <summary>
        /// Handles changes to the log collection and automatically scrolls to the end when new log entries are added.
        /// </summary>
        /// <remarks>This method checks if the view model is paused and, if not, it will auto-scroll to
        /// the end of the log list when new items are added, provided that auto-scrolling is enabled. The scrolling
        /// operation is performed on the UI thread with background priority.</remarks>
        /// <param name="sender">The source of the event, typically the collection that has changed.</param>
        /// <param name="e">The event data containing information about the change.</param>
        private void HandleCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (_viewModel.IsPaused) return;

            // Auto-scroll to the end when new log events are added, unless paused.
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                // Lazily find the ScrollViewer for the log list if not already found.
                _scrollViewer ??= GetScrollViewer(__logList);

                if (_scrollViewer is not null && _viewModel.AutoScroll)
                {
                    // Scroll to the end on the UI thread at background priority.
                    Dispatcher.InvokeAsync(() => _scrollViewer.ScrollToEnd(), DispatcherPriority.Background);
                }
            }
        }

        /// <summary>
        /// Generates a <see cref="DataTemplate"/> for displaying log entries based on the specified format and
        /// delimiter.
        /// </summary>
        /// <remarks>The generated <see cref="DataTemplate"/> is designed to display log entries using a
        /// <see cref="TextBlock"/>  with child <see cref="Run"/> elements. Each section of the format is processed to
        /// create either static text  or bindings to log entry properties. The method ensures that all <see
        /// cref="Run"/> elements are tagged with  the log entry's unique identifier for potential reference.  If an
        /// error occurs during template generation, a fallback template is returned, and the error is logged  at the
        /// critical level.</remarks>
        /// <param name="format">A string representing the format of the log entry. This string may include placeholders or specific
        /// components that define how the log entry should be structured.</param>
        /// <param name="formatDelimiter">A string used to delimit sections within the format. This delimiter is applied to separate components or
        /// placeholders in the generated template.</param>
        /// <returns>A <see cref="DataTemplate"/> configured to display log entries according to the specified format and
        /// delimiter. The template includes bindings to log entry properties and supports dynamic updates.</returns>
        private DataTemplate GenerateDataTemplate(string format, string formatDelimiter)
        {
            try
            {
                var sections = SplitFormatIntoSections(format.ToLowerInvariant(), formatDelimiter);

                var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                textBlockFactory.SetValue(TextBlock.MarginProperty, new Thickness(0));
                textBlockFactory.SetValue(TextBlock.PaddingProperty, new Thickness(0));
                textBlockFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.NoWrap);

                for (int i = 0; i < sections.Length; i++)
                {
                    string[] section = sections[i] ?? [];
                    if (section.Length == 0)
                    {
                        continue;
                    }
                    else if (section.Length == 1 && _formatSplitComponents.Any(x => section[0].Contains(x))) // we have a specific check for these because they get their own unique foreground color formatting
                    {
                        foreach (var element in GenerateHandleOrTextElements(section[0]))
                            textBlockFactory.AppendChild(element);
                    }
                    else
                    {
                        for (int j = 0; j < section.Length; j++)
                        {
                            var subsection = section[j].Trim();
                            bool isPlaceHolder = false;
                            foreach (var placeHolder in _supportedPlacementHolders)
                            {
                                if (subsection.Contains(placeHolder))
                                {
                                    isPlaceHolder = true;
                                    var replacement = SplitPlacementHolderFromPrefixAndSuffix(subsection, placeHolder);
                                    if (replacement.TryGetValue(ReplacementPrefixKey, out var prefix))
                                    {
                                        if (prefix != null)
                                            textBlockFactory.AppendChild(prefix);
                                    }
                                    if (replacement.TryGetValue(ReplacementContentKey, out var content))
                                    {
                                        if (content != null)
                                            textBlockFactory.AppendChild(content);
                                    }
                                    if (replacement.TryGetValue(ReplacementSuffixKey, out var suffix))
                                    {
                                        if (suffix != null)
                                            textBlockFactory.AppendChild(suffix);
                                    }
                                    break;
                                }
                            }

                            if (!isPlaceHolder)
                            {
                                textBlockFactory.AppendChild(TextToFrameworkElement(subsection));
                            }

                            if (j < section.Length - 1)
                                textBlockFactory.AppendChild(GenerateDelimiter(formatDelimiter));
                        }
                    }

                    if (i < sections.Length - 1)
                        textBlockFactory.AppendChild(GenerateDelimiter(formatDelimiter));
                }

                return new DataTemplate()
                {
                    VisualTree = textBlockFactory,
                    DataType = typeof(LogEventArgs)
                };
            }
            catch (Exception ex)
            {
                // Handle any exceptions that may occur during the generation of the DataTemplate.
                // This could include logging the error or providing a fallback template.
                const string message = "Error generating log display template";
                if (_viewModel.Logger is null)
                {
                    Debug.WriteLine(message);
                    Debug.WriteLine(ex.ToString());
                }
                else
                {
                    BaseLogger.LogExceptionActions[LogLevel.Critical](_viewModel.Logger, "Error generating log display template", ex);
                }

                return __logList.ItemTemplate; // Return the existing template as a fallback.
            }
        }

        private static FrameworkElementFactory GenerateDelimiter(string delimiter)
        {
            var delimiterRun = new FrameworkElementFactory(typeof(Run));
            delimiterRun.SetValue(Run.TextProperty, delimiter);
            return delimiterRun;
        }

        /// <summary>
        /// Splits the provided text into prefix, suffix, and placeholder content based on the specified placeholder
        /// string.
        /// </summary>
        /// <remarks>This method processes the <paramref name="originalText"/> by splitting it into up to
        /// three parts: prefix, suffix, and  placeholder content. If no <paramref name="placementHolder"/> is provided,
        /// the entire text is treated as content.  If the placeholder appears more than once, an exception is
        /// thrown.</remarks>
        /// <param name="originalText">The original text to be split. Cannot be null or whitespace.</param>
        /// <param name="placementHolder">The placeholder string used to split the text into parts. If null or whitespace, the method treats the
        /// entire  <paramref name="originalText"/> as a single content block.</param>
        /// <returns>A read-only dictionary containing the split parts of the text. The dictionary may include the following
        /// keys: <list type="bullet"> <item> <description><c>ReplacementPrefixKey</c>: The prefix content before the
        /// placeholder, if any.</description> </item> <item> <description><c>ReplacementSuffixKey</c>: The suffix
        /// content after the placeholder, if any.</description> </item> <item>
        /// <description><c>ReplacementContentKey</c>: The placeholder content or the entire text if no placeholder is
        /// provided.</description> </item> </list></returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="originalText"/> contains more than one occurrence of the <paramref
        /// name="placementHolder"/>.</exception>
        private static IReadOnlyDictionary<string, FrameworkElementFactory?> SplitPlacementHolderFromPrefixAndSuffix(string originalText, string? placementHolder = null)
        {
            if (string.IsNullOrWhiteSpace(originalText)) return  DefaultReplacement;

            var replacement = new Dictionary<string, FrameworkElementFactory?>(DefaultReplacement);
            string[] parts = [originalText];
            if (!string.IsNullOrWhiteSpace(placementHolder)) parts = originalText.Split(placementHolder, StringSplitOptions.None);

            if (parts.Length == 0) return replacement;
            if (parts.Length == 1)
            {
                FrameworkElementFactory? content = TextToFrameworkElement(parts[0].Trim());
                replacement[ReplacementContentKey] = content;
            }
            else
            {
                if (parts.Length > 2)
                {
                    throw new ArgumentException($"The provided text '{originalText}' contains more than one '{placementHolder}' placeholder. Only one is allowed.");
                }

                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i].Trim();
                    if (string.IsNullOrEmpty(part)) continue;
                    FrameworkElementFactory? content = TextToFrameworkElement(part);
                    if (i == 0)
                    {
                        replacement[ReplacementPrefixKey] = content;
                    }
                    else if (i == parts.Length - 1)
                    {
                        replacement[ReplacementSuffixKey] = content;
                    }
                }

                replacement[ReplacementContentKey] = TextToFrameworkElement(placementHolder!); // placementHolder should never be null here, because if it is null we would have only the originalText in the parts array, result in
                                                                                               // the logic entering the first if statement above, thus the use of the null-forgiving operator (!) ensuring it is not null.
            }
            return replacement.AsReadOnly();
        }

        /// <summary>
        /// Converts the specified text into a <see cref="FrameworkElementFactory"/> representing a <see cref="Run"/>
        /// element.
        /// </summary>
        /// <remarks>If the text contains a placeholder that can be bound, the method creates a binding to
        /// the placeholder's component. Otherwise, the text is trimmed and set as a static value.</remarks>
        /// <param name="text">The text to be converted. This can include placeholders for binding.</param>
        /// <returns>A <see cref="FrameworkElementFactory"/> configured to represent a <see cref="Run"/> element with the
        /// specified text. Returns <see langword="null"/> if <paramref name="text"/> is <see langword="null"/>.</returns>
        private static FrameworkElementFactory? TextToFrameworkElement(string text)
        {
            if (text == null) return null;
            FrameworkElementFactory output = new(typeof(Run));
            var (Bindable, Component) = GetLogEventArgsNameIfPlaceholder(text);
            if (Bindable)
            {
                output.SetBinding(Run.TextProperty, new Binding(Component)
                {
                    Mode = BindingMode.OneWay
                });
            }
            else
            {
                output.SetValue(Run.TextProperty, Component.Trim());
            }

            return output;
        }

        /// <summary>
        /// Generates an array of <see cref="FrameworkElementFactory"/> objects based on the specified log content.
        /// </summary>
        /// <remarks>This method inspects the input string to determine whether it contains the
        /// placeholder "{handle}" (case-insensitive).  If the placeholder is found, it delegates to a method that
        /// generates handle elements. Otherwise, it delegates to a  method that generates text elements.</remarks>
        /// <param name="logHandleOrLogText">A string that determines the type of elements to generate. If the string contains the placeholder "{handle}"
        /// (case-insensitive), handle elements are generated; otherwise, text elements are generated.</param>
        /// <returns>An array of <see cref="FrameworkElementFactory"/> objects representing either handle elements or text
        /// elements,  depending on the content of the input string.</returns>
        private FrameworkElementFactory[] GenerateHandleOrTextElements(string logHandleOrLogText)
        {
            if (logHandleOrLogText.Contains("{handle}", StringComparison.OrdinalIgnoreCase)) // case sensitivity doesn't matter because the format is already lowercased
                return GenerateLogHandleElement(logHandleOrLogText);
            else
                return GenerateLogTextElement(logHandleOrLogText);
        }

        /// <summary>
        /// Generates an array of <see cref="FrameworkElementFactory"/> objects representing the visual elements for a
        /// log handle, with bindings to log-related properties.
        /// </summary>
        /// <remarks>The generated elements include: <list type="bullet"> <item> A prefix element if the
        /// log handle contains text before the "{handle}" placeholder. </item> <item> A dynamically bound element for
        /// the log handle itself, with bindings to properties such as <see cref="LogEventArgs.LogHandle"/>, <see
        /// cref="LogEventArgs.LogLevel"/>, and <see cref="LogEventArgs.ID"/>. </item> <item> A suffix element if the
        /// log handle contains text after the "{handle}" placeholder. </item> </list> The log handle element's
        /// foreground color is dynamically determined using a converter bound to the log level.</remarks>
        /// <param name="logHandle">A string representing the log handle, which may include the placeholder "{handle}" to indicate where the
        /// dynamic log handle value should be inserted. If null, empty, or whitespace, the default placeholder
        /// "{handle}" is used.</param>
        /// <returns>An array of <see cref="FrameworkElementFactory"/> objects that represent the visual components of the log
        /// handle, including optional prefix and suffix text and a dynamically bound log handle.</returns>
        private FrameworkElementFactory[] GenerateLogHandleElement(string logHandle)
        {
            if (string.IsNullOrWhiteSpace(logHandle)) logHandle = "{handle}";

            var logHandleElements = SplitPlacementHolderFromPrefixAndSuffix(logHandle, "{handle}");
            logHandleElements[ReplacementContentKey]?.SetBinding(Run.ForegroundProperty, new Binding(nameof(LogEventArgs.LogLevel))
            {
                Converter = (IValueConverter)FindResource(BaseLogger.LogLevelToBrushConverterKey)
            });
            List<FrameworkElementFactory> handleElements = [];

            if (logHandleElements.TryGetValue(ReplacementPrefixKey, out var prefix))
            {
                if (prefix != null)
                    handleElements.Add(prefix);
            }
            if (logHandleElements.TryGetValue(ReplacementContentKey, out var content))
            {
                if (content != null)
                    handleElements.Add(content);
            }
            if (logHandleElements.TryGetValue(ReplacementSuffixKey, out var suffix))
            {
                if (suffix != null)
                    handleElements.Add(suffix);
            }

            return [.. handleElements];
        }

        /// <summary>
        /// Generates an array of <see cref="FrameworkElementFactory"/> objects representing formatted text elements
        /// based on the provided log text template.
        /// </summary>
        /// <remarks>The method splits the <paramref name="logText"/> template into sections using the
        /// <c>{handle}</c> placeholder. It creates a <see cref="Run"/> element for each section: <list type="bullet">
        /// <item> <description>A prefix <see cref="Run"/> for text before the placeholder, if any.</description>
        /// </item> <item> <description>A dynamic <see cref="Run"/> bound to log-specific properties, such as
        /// <c>LogText</c> and <c>LogColor</c>.</description> </item> <item> <description>A suffix <see cref="Run"/> for
        /// text after the placeholder, if any.</description> </item> </list> The dynamic <see cref="Run"/> binds to
        /// properties of <see cref="LogEventArgs"/> to display log-specific content.</remarks>
        /// <param name="logText">A string template containing the log text. The template may include the placeholder <c>{handle}</c> to
        /// indicate where dynamic log content should be inserted. If the value is <see langword="null"/>, empty, or
        /// whitespace, a default template of <c>"{message}"</c> is used.</param>
        /// <returns>An array of <see cref="FrameworkElementFactory"/> objects, where each element represents a portion of the
        /// formatted log text. The array includes static text elements for the prefix and suffix (if present) and a
        /// dynamic text element bound to log-specific properties.</returns>
        private FrameworkElementFactory[] GenerateLogTextElement(string logText)
        {
            if (string.IsNullOrWhiteSpace(logText)) logText = "{message}";

            var textElements = SplitPlacementHolderFromPrefixAndSuffix(logText, "{message}");
            textElements[ReplacementContentKey]?.SetBinding(Run.ForegroundProperty, new Binding(nameof(LogEventArgs.LogColor))
            {
                Converter = (IValueConverter)FindResource(BaseLogger.ColorToBrushConverterKey)
            });
            List<FrameworkElementFactory> handleElements = [];

            if (textElements.TryGetValue(ReplacementPrefixKey, out var prefix))
            {
                if (prefix != null)
                    handleElements.Add(prefix);
            }
            if (textElements.TryGetValue(ReplacementContentKey, out var content))
            {
                if (content != null)
                    handleElements.Add(content);
            }
            if (textElements.TryGetValue(ReplacementSuffixKey, out var suffix))
            {
                if (suffix != null)
                    handleElements.Add(suffix);
            }

            return [.. handleElements];
        }

        /// <summary>
        /// Determines whether the specified component is a recognized placeholder and, if so, maps it to the
        /// corresponding property name of <see cref="LogEventArgs"/>.
        /// </summary>
        /// <param name="component">The component string to evaluate, which may represent a placeholder or a literal value.</param>
        /// <returns>A tuple where the first value indicates whether the component is a recognized placeholder (<see
        /// langword="true"/> if it is; otherwise, <see langword="false"/>), and the second value is either the
        /// corresponding property name of <see cref="LogEventArgs"/> (if the component is a placeholder) or the
        /// original component string (if it is not a placeholder).</returns>
        private static (bool Bindable, string Component) GetLogEventArgsNameIfPlaceholder(string component)
        {
            return component switch
            {
                "{handle}" => (true, nameof(LogEventArgs.LogHandle)),
                "{message}" => (true, nameof(LogEventArgs.LogText)),
                "{timestamp}" => (true, nameof(LogEventArgs.LogDateTimeFormatted)),
                "{loglevel}" => (true, nameof(LogEventArgs.LogLevel)),
                "{threadid}" => (true, nameof(LogEventArgs.ThreadId)),
                _ => (false, component) // Return the component as is if it doesn't match any known placeholders.
            };
        }

        /// <summary>
        /// Splits a format string into sections based on a specified delimiter and predefined split components.
        /// </summary>
        /// <remarks>This method processes the format string by splitting it into components using the
        /// specified delimiter,  trimming whitespace, and grouping components into sections. Predefined split
        /// components act as section boundaries, and each section is returned as an array of strings.</remarks>
        /// <param name="format">The format string to be split. Cannot be null or empty.</param>
        /// <param name="delimiter">The delimiter used to split the format string into components. Cannot be null or empty.</param>
        /// <returns>A jagged array of strings, where each inner array represents a section of the format string.  Sections are
        /// determined by the delimiter and predefined split components.  Returns an empty array if the <paramref
        /// name="format"/> is null or empty.</returns>
        private static string[][] SplitFormatIntoSections(string format, string delimiter)
        {
            if (string.IsNullOrEmpty(format))
                return [];
            // Split the format string into components based on the specified delimiter.
            var components = format.Split([delimiter], StringSplitOptions.RemoveEmptyEntries)
                         .Select(part => part.Trim())
                         .ToArray();

            var sections = new List<string[]>();
            var subsection = new List<string>();
            foreach (var component in components)
            {
                if (_supportedPlacementHolders.Any(x => component.Contains(x)))
                {
                    if (subsection.Count > 0) sections.Add([.. subsection]);
                    subsection.Clear();
                    sections.Add([component]);
                }
                else
                {
                    subsection.Add(component);
                }
            }
            return [.. sections];
        }

        #region Dependency Properties
        /// <summary>
        /// Identifies the <see cref="MaxLogSize"/> dependency property.
        /// </summary>
        /// <remarks>This property represents the maximum size of the log queue. It is used to control the
        /// number of log entries that can be stored in the log control. The default value is determined by <see
        /// cref="BaseLogger.MaxLogQueueSize"/>.</remarks>
        public static readonly DependencyProperty MaxLogSizeProperty = DependencyProperty.Register(
            nameof(MaxLogSize),
            typeof(int),
            typeof(LogControl),
            new PropertyMetadata(BaseLogger.MaxLogQueueSize, OnMaxLogSizeChanged));

        /// <summary>
        /// Identifies the <see cref="HandleFilter"/> dependency property.
        /// </summary>
        /// <remarks>This property is used to register the <see cref="HandleFilter"/> dependency property
        /// for the <see cref="LogControl"/> class.</remarks>
        public static readonly DependencyProperty HandleFilterProperty = DependencyProperty.Register(
            nameof(HandleFilter),
            typeof(string),
            typeof(LogControl),
            new PropertyMetadata(null, OnHandleFilterChanged));

        /// <summary>
        /// Identifies the <see cref="IgnoreCase"/> dependency property, which determines whether case should be ignored
        /// in log filtering.
        /// </summary>
        /// <remarks>This property is used to configure case sensitivity when filtering log entries in the
        /// <see cref="LogControl"/>. The default value is <see langword="false"/>, meaning case is considered by
        /// default.</remarks>
        public static readonly DependencyProperty IgnoreCaseProperty = DependencyProperty.Register(
            nameof(IgnoreCase),
            typeof(bool),
            typeof(LogControl),
            new PropertyMetadata(false, OnIgnoreCaseChanged));

        /// <summary>
        /// Identifies the <see cref="AutoScroll"/> dependency property, which determines whether the log control
        /// automatically scrolls to the latest entry.
        /// </summary>
        /// <remarks>This property is registered as a dependency property to enable data binding and
        /// styling support. The default value is <see langword="true"/>.</remarks>
        public static readonly DependencyProperty AutoScrollProperty = DependencyProperty.Register(
            nameof(AutoScroll),
            typeof(bool),
            typeof(LogControl),
            new PropertyMetadata(true, OnAutoScrollChanged));

        /// <summary>
        /// Identifies the <see cref="HandleFilterVisible"/> dependency property, which determines whether the handle
        /// filter is visible.
        /// </summary>
        /// <remarks>This property is a dependency property and can be used in data binding or style
        /// setters.</remarks>
        public static readonly DependencyProperty HandleFilterVisibleProperty = DependencyProperty.Register(
            nameof(HandleFilterVisible),
            typeof(bool),
            typeof(LogControl),
            new PropertyMetadata(true, OnHandleFilterVisibleChanged));

        /// <summary>
        /// Identifies the <see cref="PausingEnabled"/> dependency property, which determines whether pausing is enabled
        /// for the log control.
        /// </summary>
        /// <remarks>This property is a dependency property and can be used in XAML bindings. The default
        /// value is <see langword="true"/>.</remarks>
        public static readonly DependencyProperty PausingEnabledProperty = DependencyProperty.Register(
            nameof(PausingEnabled),
            typeof(bool),
            typeof(LogControl),
            new PropertyMetadata(true, OnPausingEnabledChanged));

        /// <summary>
        /// Identifies the <see cref="LogDisplayFormat"/> dependency property, which determines the format used to
        /// display log entries.
        /// </summary>
        /// <remarks>This property is used to register and manage the <see cref="LogDisplayFormat"/>
        /// dependency property for the <see cref="LogControl"/> class. The format string specified by this property can
        /// influence how log entries are rendered in the control.</remarks>
        public static readonly DependencyProperty LogDisplayFormatProperty = DependencyProperty.Register(
            nameof(LogDisplayFormat),
            typeof(string),
            typeof(LogControl),
            new PropertyMetadata(BaseLogger.DefaultLogDisplayFormat, OnHandleLogDisplayFormatChanged));

        /// <summary>
        /// Identifies the <see cref="LogDisplayFormatDelimiter"/> dependency property.
        /// </summary>
        /// <remarks>This property determines the delimiter used in the log display format. The default
        /// value is a single space (" ").</remarks>
        public static readonly DependencyProperty LogDisplayFormatDelimiterProperty = DependencyProperty.Register(
            nameof(LogDisplayFormatDelimiter),
            typeof(string),
            typeof(LogControl),
            new PropertyMetadata(" ", OnHandleLogDisplayFormatDelimiterChanged));
        #endregion

        #region IDisposable Support
        /// <summary>
        /// Releases the resources used by the current instance of the class.
        /// </summary>
        /// <remarks>This method should be called when the instance is no longer needed to ensure proper
        /// cleanup of resources.</remarks>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _viewModel.LogEvents.CollectionChanged -= HandleCollectionChanged;
                    _viewModel?.Dispose();
                }

                _disposedValue = true;
            }
        }

        /// <summary>
        /// Releases the resources used by the current instance of the class.
        /// </summary>
        /// <remarks>This method should be called when the instance is no longer needed to free up
        /// resources.  It suppresses finalization to optimize garbage collection.</remarks>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
