using System.Reflection;
using System.Windows;
using LogViewer;
using Microsoft.Extensions.DependencyInjection;

namespace LogViewerExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string _title = string.Empty;
        private readonly SomeObject _loggableObject;
        private readonly ExampleVM _example;

        public MainWindow()
        {
            InitializeComponent();

            _title = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "BaseLogger Example";
            _title += $" - {BaseLogger.Version}";
            Title = _title;

            // Resolve ExampleVM via DI - it receives ILogger<ExampleVM> automatically
            _example = App.ServiceProvider.GetRequiredService<ExampleVM>();
            _exampleControls.DataContext = _example;

            // Create a SomeObject using the legacy BaseLogger pattern (for demonstration)
            _loggableObject = new SomeObject(nameof(MainWindow));
            _loggableObject.LogInformation("Logging initialized");
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                await _example.DisposeAsync();
            }
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception
            catch (Exception)
            {
                // swallow it, this is an example application and it's closing
            }
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception
        }

        public static T Resolve<T>() where T : notnull
            => App.ServiceProvider.GetRequiredService<T>();
    }
}
