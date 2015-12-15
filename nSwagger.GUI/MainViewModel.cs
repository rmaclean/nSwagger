namespace nSwagger.GUI
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows.Input;

    public class MainViewModel : INotifyPropertyChanged
    {
        private bool _allowOverride;
        private string _customNamespace;
        private string _timeout;
        private string _url;

        public MainViewModel()
        {
            Add = new Command(AddExecute);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand Add { get; }

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

        private void AddExecute()
        {
            throw new NotImplementedException();
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