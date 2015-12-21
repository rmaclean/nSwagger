namespace nSwagger.GUI
{
    using System.Windows;

    public partial class MainWindow : Window
    {
        private MainViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();
            viewModel = new MainViewModel();
            DataContext = viewModel;
        }

        public void SetUI(UIOptions options)
        {
            viewModel.Target = options.Target;
            viewModel.AllowOverride = options.Overwrite;
        }
    }

    public class UIOptions
    {
        public bool Overwrite { get; set; }

        public string Target { get; set; }
    }
}