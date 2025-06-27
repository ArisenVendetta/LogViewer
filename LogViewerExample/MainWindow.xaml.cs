using System;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using LogViewer;

namespace LogViewerExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string _title = string.Empty;
        private readonly SomeObject _loggableObject = new SomeObject(nameof(MainWindow));
        private readonly ExampleVM _example;

        public MainWindow()
        {
            InitializeComponent();

            _title = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "BaseLogger Example";
            _title += $" - {BaseLogger.Version}";
            Title = _title;

            _example = new ExampleVM();
            _exampleControls.DataContext = _example;

            _loggableObject.LogInfo("Logging initialized");
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                await _example.DisposeAsync();
            }
            catch (Exception)
            {
                // swallow it, this is an example application and it's closing
            }
        }
    }
}