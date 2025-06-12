using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Uno_Solar_Design_Assist_Pro
{
    internal class Module_Placement : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {

        }
        public bool CanExecute(object parameter)
        {
            return true;
        }
    }
}
