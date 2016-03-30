namespace nSwagger.GUI
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Input;
    using Microsoft.Win32;

    public class MainViewModel : INotifyPropertyChanged
    {
        private bool _allowOverride;
        private string _customNamespace;
        private string _language;
        private bool _running;
        private bool _saveSetting;
        private string _target;
        private string _timeout;
        private string _url;

        public MainViewModel()
        {
            Version = Configuration.nSwaggerVersion;
            Run = new Command(RunExecute);
            BrowseForFile = new Command(BrowseForFileExecute);
            BrowseForTarget = new Command(BrowseForTargetExecute);
            SaveSettings = true;
            LoadSettings = new Command(LoadSettingsExecute);
            Language = Languages[0];
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool AllowOverride
        {
            get
            {
                return _allowOverride;
            }
            set
            {
                UpdateProperty(ref _allowOverride, value);
            }
        }

        public ICommand BrowseForFile { get; }

        public ICommand BrowseForTarget { get; }

        public string CustomNamespace
        {
            get
            {
                return _customNamespace;
            }
            set
            {
                UpdateProperty(ref _customNamespace, value);
            }
        }

        public string Language
        {
            get
            {
                return _language;
            }
            set
            {
                UpdateProperty(ref _language, value);
            }
        }

        public string[] Languages { get; } = new[] { "C# (client code for API)", "TypeScript (TypeScript definations for API)" };

        public ICommand LoadSettings { get; }

        public ICommand Run { get; }

        public bool Running
        {
            get
            {
                return _running;
            }
            set
            {
                UpdateProperty(ref _running, value);
            }
        }

        public bool SaveSettings
        {
            get
            {
                return _saveSetting;
            }
            set
            {
                UpdateProperty(ref _saveSetting, value);
            }
        }

        public string Target
        {
            get
            {
                return _target;
            }
            set
            {
                UpdateProperty(ref _target, value);
            }
        }

        public string Timeout
        {
            get
            {
                return _timeout;
            }
            set
            {
                UpdateProperty(ref _timeout, value);
            }
        }

        public string Url
        {
            get
            {
                return _url;
            }
            set
            {
                UpdateProperty(ref _url, value);
            }
        }

        public string Version { get; }

        protected bool UpdateProperty<T>(ref T currentValue, T newValue, [CallerMemberName] string propertyName = "")
        {
            if (currentValue == null || !currentValue.Equals(newValue))
            {
                currentValue = newValue;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }

            return false;
        }

        private void BrowseForFileExecute()
        {
            var openFileDialog = new OpenFileDialog
            {
                AddExtension = false,
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = ".json",
                RestoreDirectory = true,
                ShowReadOnly = true,
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"
            };

            var result = openFileDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                Url = openFileDialog.FileName;
            }
        }

        private void BrowseForTargetExecute()
        {
            var defaultExt = "";
            var filter = "";
            if (Language != null)
            {
                if (Language.StartsWith("c#", StringComparison.OrdinalIgnoreCase))
                {
                    defaultExt = "*.cs";
                    filter = "C# Files (*.cs)|*.cs";
                }

                if (Language.StartsWith("TypeScript", StringComparison.OrdinalIgnoreCase))
                {
                    defaultExt = "*.ts";
                    filter = "TypeScript Files (*.ts)|*.ts";
                }
            }

            filter += "|All Files (*.*)|*.*";

            var saveFileDialog = new SaveFileDialog
            {
                AddExtension = false,
                CheckPathExists = true,
                DefaultExt = defaultExt,
                RestoreDirectory = true,
                Filter = filter
            };

            var result = saveFileDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                Target = saveFileDialog.FileName;
            }
        }

        private void LoadSettingsExecute()
        {
            var openFileDialog = new OpenFileDialog
            {
                AddExtension = false,
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = ".nSwagger",
                RestoreDirectory = true,
                ShowReadOnly = true,
                Filter = "nSwagger Files (*.nSwagger)|*.nSwagger|All Files (*.*)|*.*"
            };

            var result = openFileDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                LoadSettingsInternal(openFileDialog.FileName);
            }
        }

        private void LoadSettingsInternal(string fileName)
        {
            var swaggerConfig = Configuration.LoadFromFile(fileName);
            if (swaggerConfig == null)
            {
                MessageBox.Show("Configuration file not found or invalid.");
                return;
            }

            AllowOverride = swaggerConfig.AllowOverride;
            CustomNamespace = swaggerConfig.Namespace;
            if (swaggerConfig.Language.HasFlag(TargetLanguage.csharp))
            {
                Language = Languages[0];
            }

            if (swaggerConfig.Language.HasFlag(TargetLanguage.typescript))
            {
                Language = Languages[1];
            }

            SaveSettings = swaggerConfig.SaveSettings;
            Target = swaggerConfig.Target;
            Url = swaggerConfig.Sources[0];
        }

        private async void RunExecute()
        {
            if (Language == null)
            {
                MessageBox.Show("You must select a target language for the project.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Url))
            {
                MessageBox.Show("You must select a valid source file or URL.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Target))
            {
                MessageBox.Show("You must select a valid target file.");
                return;
            }

            var config = new Configuration
            {
                AllowOverride = AllowOverride,
                Sources = new[] { Url },
                Target = Target,
                SaveSettings = SaveSettings
            };

            if (Language.StartsWith("c#", StringComparison.OrdinalIgnoreCase))
            {
                config.Language = TargetLanguage.csharp;
            }

            if (Language.StartsWith("typescript", StringComparison.OrdinalIgnoreCase))
            {
                config.Language = TargetLanguage.typescript;
            }

            Running = true;
            try
            {
                var timeout = 0;
                if (!string.IsNullOrWhiteSpace(Timeout) && int.TryParse(Timeout, out timeout))
                {
                    config.HTTPTimeout = TimeSpan.FromSeconds(timeout);
                }

                if (!string.IsNullOrWhiteSpace(CustomNamespace))
                {
                    config.Namespace = CustomNamespace;
                }

                await Engine.Run(config);
            }
            catch (nSwaggerException ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            finally
            {
                Running = false;
            }

            if (config.Language.HasFlag(TargetLanguage.csharp))
            {
                MessageBox.Show("Your C# client wrapper has been generated.");
            }

            if (config.Language.HasFlag(TargetLanguage.typescript))
            {
                MessageBox.Show("Your TypeScript definations has been generated.");
            }
        }
    }

    internal class Command : ICommand
    {
        private readonly Action action;

        public Command(Action action)
        {
            this.action = action;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter) => action();
    }
}