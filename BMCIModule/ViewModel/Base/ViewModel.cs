
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace BMCIModule.ViewModel.Base
{
    public abstract class ViewModel:INotifyPropertyChanged,IDataErrorInfo
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _isChanged;

        public bool IsChanged
        {
            get=> _isChanged;
            set
            {
                if(value==_isChanged) return;
                _isChanged = value;
                OnPropertyChanged();
            }
        }
        protected virtual void OnPropertyChanged([CallerMemberName] string PropertyName=null)
        {
            if (PropertyName != nameof(IsChanged))
            {
                IsChanged = true;
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        protected virtual bool Set<T>(ref T field, T value, [CallerMemberName] string ProportyName = null)
        {
            if(Equals(field,value)) return false;
            field = value;
            OnPropertyChanged(ProportyName);
            return true;
        }

       public void Dispose()
        {
            Dispose(true);
        }
        private bool _Disposed;
        protected virtual void Dispose(bool Disposing)
        {
            if(!Disposing || _Disposed) return;
            _Disposed = true;
            // Освобождение управляемых ресурсов
        }

        public string this[string columnName] => string.Empty; //большой вопрос 

        public string Error { get; }
    }
}
