
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace BMCIModuleTest
{
    public abstract class ViewModel: INotifyPropertyChanged, INotifyDataErrorInfo
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _isChanged;

        public bool IsChanged
        {
            get => _isChanged;
            set
            {
                if (value == _isChanged) return;
                _isChanged = value;
                OnPropertyChanged();
            }
        }
        protected virtual void OnPropertyChanged([CallerMemberName] string PropertyName = null)
        {
            if (PropertyName != nameof(IsChanged))
            {
                IsChanged = true;
            }
            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }

        protected virtual bool Set<T>(ref T field, T value, [CallerMemberName] string ProportyName = null)
        {
            if (Equals(field, value)) return false;
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
            if (!Disposing || _Disposed) return;
            _Disposed = true;
            // Освобождение управляемых ресурсов
        }


        private readonly Dictionary<string, List<string>> dictionaryErrors = new Dictionary<string, List<string>>();
        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return dictionaryErrors.Values;
            }
            return dictionaryErrors.ContainsKey(propertyName) ? dictionaryErrors[propertyName] : null;
        }

        public bool HasErrors => dictionaryErrors.Any();
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        private void OnErrorsChanged(string proportyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(proportyName));
        }
        private void AddErrors(string propertyName, IList<string> errors)
        {
            if (errors == null || !errors.Any())
            {
                return;
            }

            var changed = false;
            if (!dictionaryErrors.ContainsKey(propertyName))
            {
                dictionaryErrors.Add(propertyName, new List<string>());
                changed = true;
            }
            //тут чехарда какая-то. надо разобраться
            foreach (var error in dictionaryErrors)
            {
                if (dictionaryErrors[propertyName].Contains(error.Key)) continue;
                dictionaryErrors[propertyName].Add(error.Key);
                changed = true;
            }

            if (changed)
            {
                OnErrorsChanged(propertyName);
            }
        }

        public void AddError(string propertyName, string error)
        {
            AddErrors(propertyName, new List<string> { error });
        }
        protected void ClearErrors(string propertyName = "")
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                dictionaryErrors.Clear();
            }
            else
            {
                dictionaryErrors.Remove(propertyName);
            }
            OnErrorsChanged(propertyName);
        }
    }
}
