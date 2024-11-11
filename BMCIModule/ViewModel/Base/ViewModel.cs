
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace BMCIModule.ViewModel.Base
{
    public abstract class ViewModel:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string PropertyName=null)
        {
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
    }
}
