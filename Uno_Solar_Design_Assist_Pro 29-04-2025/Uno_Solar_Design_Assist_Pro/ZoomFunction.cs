using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Windows.Input;

namespace Uno_Solar_Design_Assist_Pro
{
    internal class ZoomFunction : ICommand
    {
        public event EventHandler CanExecuteChanged;

  
        public bool CanExecute(object parameter) => true;


        [CommandMethod("ShowZoomForm")]
        public void Execute(object parameter)
        {
            Zoom zoomForm = new Zoom();
            Application.ShowModalDialog(zoomForm); // For modal dialog in AutoCAD
        }
    }
}