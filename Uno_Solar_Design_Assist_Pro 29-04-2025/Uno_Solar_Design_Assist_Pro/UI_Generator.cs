using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using RibbonPanelSource = Autodesk.Windows.RibbonPanelSource;
using Autodesk.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace Uno_Solar_Design_Assist_Pro
{
    public class UI_Generator : IExtensionApplication
    {
        public void Initialize()
        {
            try
            {
                // Get the RibbonControl
                Autodesk.Windows.RibbonControl rbncntrl = ComponentManager.Ribbon;
                RibbonTab rbntab = null;

                // Check if tab already exists
                foreach (RibbonTab tab in rbncntrl.Tabs)
                {
                    if (tab.Title == "UNO Tool")
                    {
                        rbntab = tab;
                        break;
                    }
                }
                // If tab doesn't exist, create and add it
                if (rbntab == null)
                {
                    rbntab = new RibbonTab();                        // Create a new RibbonTab                
                    rbntab.Title = "UNO Tool";
                    rbntab.Id = "UNOTool_Tab";
                    
                    rbncntrl.Tabs.Add(rbntab);                      // Add the RibbonTab to the RibbonControl
                }

                // Create a new RibbonPanel
                RibbonPanelSource rbnpnlsrc = new RibbonPanelSource();
                RibbonPanel rbnpnl = new RibbonPanel();
                rbnpnl.Source = rbnpnlsrc;                          // Add the RibbonPanel to the RibbonTab
                rbntab.Panels.Add(rbnpnl);

                // Create a separator as a thicker border
                RibbonSeparator separator = new RibbonSeparator();
                separator.Height = 80;                              // Set the height to create a thicker border
                separator.IsEnabled = false;                        // Make the separator non-interactive                

                // Create a new RibbonButton
                Autodesk.Windows.RibbonButton button = new Autodesk.Windows.RibbonButton();
                button.Text = "Create Mesh";
                button.ShowText = true;
                button.CommandHandler = new Mesh_Creation();        // Add a click event handler for the button
                rbnpnlsrc.Items.Add(button);                        // Add the RibbonButton to the RibbonPanel
                rbnpnlsrc.Items.Add(separator);

                // Create a new RibbonButton
                Autodesk.Windows.RibbonButton button1 = new Autodesk.Windows.RibbonButton();
                button1.Text = "Place Roads";
                button1.ShowText = true;
                button1.CommandHandler = new Extents();    // Add a click event handler for the button
                rbnpnlsrc.Items.Add(button1);                       // Add the RibbonButton to the RibbonPanel
                rbnpnlsrc.Items.Add(separator);

                Autodesk.Windows.RibbonButton button2 = new Autodesk.Windows.RibbonButton();
                button2.Text = "Place Frames";
                button2.ShowText = true;
                button2.CommandHandler = new Frames_Placement();
                rbnpnlsrc.Items.Add(button2);
                rbnpnlsrc.Items.Add(separator);


                Autodesk.Windows.RibbonButton button2b = new Autodesk.Windows.RibbonButton();
                button2b.Text = "Re-Arrange Frames";
                button2b.ShowText = true;
                button2b.CommandHandler = new Re_Arrange_Frames();
                rbnpnlsrc.Items.Add(button2b);
                rbnpnlsrc.Items.Add(separator);

                Autodesk.Windows.RibbonButton button3 = new Autodesk.Windows.RibbonButton();
                button3.Text = "Place Trenches";
                button3.ShowText = true;
                button3.CommandHandler = new Trench_Lines_Placement();
                rbnpnlsrc.Items.Add(button3);
                rbnpnlsrc.Items.Add(separator);

                Autodesk.Windows.RibbonButton button4 = new Autodesk.Windows.RibbonButton();
                button4.Text = "Place\nLightArrester";
                button4.ShowText = true;
                button4.CommandHandler = new Light_Arresters_Placement();
                rbnpnlsrc.Items.Add(button4);
                rbnpnlsrc.Items.Add(separator);

                Autodesk.Windows.RibbonButton button5 = new Autodesk.Windows.RibbonButton();
                button5.Text = "Place Modules";
                button5.ShowText = true;
                button5.CommandHandler = new Cabling_Creation_DC();
                rbnpnlsrc.Items.Add(button5);
                rbnpnlsrc.Items.Add(separator);

                RibbonButton button6 = new RibbonButton();
                button6.Text = "Place Piles";
                button6.ShowText = true;
                button6.CommandHandler = new Pile_Placement();
                rbnpnlsrc.Items.Add(button6);
                rbnpnlsrc.Items.Add(separator);

                RibbonButton button7 = new RibbonButton();
                button7.Text = "Piling Info";
                button7.ShowText = true;
                button7.CommandHandler = new Piles_Naming();
                rbnpnlsrc.Items.Add(button7);
                rbnpnlsrc.Items.Add(separator);

                RibbonButton button8 = new RibbonButton();
                button8.Text = "Table Info";
                button8.ShowText = true;
                button8.CommandHandler = new Table_Naming();
                rbnpnlsrc.Items.Add(button8);
                rbnpnlsrc.Items.Add(separator);

                RibbonButton button9 = new RibbonButton();
                button9.Text = "Stringing";
                button9.ShowText = true;
                button9.CommandHandler = new Stringing_Creation();
                rbnpnlsrc.Items.Add(button9);
                rbnpnlsrc.Items.Add(separator);

                RibbonButton button10 = new RibbonButton();
                button10.Text = "DC Cabling";
                button10.ShowText = true;
                button10.CommandHandler = new Cabling_Creation_DC();
                rbnpnlsrc.Items.Add(button10);
                rbnpnlsrc.Items.Add(separator);

                RibbonButton button11 = new RibbonButton();
                button11.Text = "AC Cabling";
                button11.ShowText = true;
                button11.CommandHandler = new Cabling_Creation_AC();
                rbnpnlsrc.Items.Add(button11);
                rbnpnlsrc.Items.Add(separator);

                RibbonButton button12 = new RibbonButton();
                button12.Text = "Pile Export";
                button12.ShowText = true;
                button12.CommandHandler = new Pile_Info_Export();
                rbnpnlsrc.Items.Add(button12);
                rbnpnlsrc.Items.Add(separator);

                RibbonButton button13 = new RibbonButton();
                button13.Text = "Cable Export";
                button13.ShowText = true;
                button13.CommandHandler = new Cable_Info_Export();
                rbnpnlsrc.Items.Add(button13);
                rbnpnlsrc.Items.Add(separator);

                RibbonButton button14 = new RibbonButton();
                button14.Text = "Grounding";
                button14.ShowText = true;
                button14.CommandHandler = new Grounding_Creation();
                rbnpnlsrc.Items.Add(button14);
                rbnpnlsrc.Items.Add(separator);

                RibbonButton button15 = new RibbonButton();
                button15.Text = "GroundingStrips Length";
                button15.ShowText = true;
                button15.CommandHandler = new GroundingStripLengthData();
                rbnpnlsrc.Items.Add(button15);
                rbnpnlsrc.Items.Add(separator);

                RibbonButton button16 = new RibbonButton();
                button16.Text = "ZoomFunction";
                button16.ShowText = true;
                button16.CommandHandler = new ZoomFunction();
                rbnpnlsrc.Items.Add(button16);
                rbnpnlsrc.Items.Add(separator);

                RibbonButton button17 = new RibbonButton();
                button17.Text = "Shadow Analysis";
                button17.ShowText = true;
                button17.CommandHandler = new Shadow_Analysis();
                rbnpnlsrc.Items.Add(button17);
                rbnpnlsrc.Items.Add(separator);


            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void Terminate()
        {
            //throw new NotImplementedException();
        }

        public class CommandHandler : System.Windows.Input.ICommand
        {
            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter)
            {
                try
                {
                    return true; //throw new NotImplementedException();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    return true;
                }
            }

            public void Execute(object parameter)
            {
                try
                {
                    //Layer_Manager lm = new Layer_Manager();
                    //lm.ShowDialog();
                    //throw new NotImplementedException();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }
    }

    
}
