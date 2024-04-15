using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BMCIModule.ViewModel.Base
{
    internal abstract class ViewModel:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnProperyChanged([CallerMemberName] string PropertyName=null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        protected virtual bool Set<T>(ref T field, T value, [CallerMemberName] string ProportyName = null)
        {
            if(Equals(field,value)) return false;
            field = value;
            OnProperyChanged();
            return true;
        }
    }
}
