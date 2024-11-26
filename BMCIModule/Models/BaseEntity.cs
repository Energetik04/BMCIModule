using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMCIModule.Models
{
    public class BaseEntity:INotifyDataErrorInfo
    {
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
        private void AddError(string propertyName, string error)
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
