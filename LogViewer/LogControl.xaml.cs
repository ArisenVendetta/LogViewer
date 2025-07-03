using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
        /// <summary>
        /// The view model backing this control, responsible for log data and state.
        /// </summary>
        private readonly LogControlViewModel _viewModel;

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

            _viewModel.LogEvents.CollectionChanged += (s, e) =>
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
            };
        }

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
        /// Handles changes to the <see cref="LogControl.LogHandleFilterProperty"/> dependency property.
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
            nameof(PausingEnabledProperty),
            typeof(bool),
            typeof(LogControl),
            new PropertyMetadata(true, OnPausingEnabledChanged));
        #endregion

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _viewModel?.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
