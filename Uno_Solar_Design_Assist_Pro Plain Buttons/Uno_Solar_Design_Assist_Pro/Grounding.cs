using Autodesk.AutoCAD.ApplicationServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;


namespace Uno_Solar_Design_Assist_Pro
{
    public partial class Grounding : Form
    {
        private Panel mainColorPanel;
        private Panel moduleColorPanel;
        private TextBox mainWeightBox;
        private TextBox moduleWeightBox;
        public static Autodesk.AutoCAD.Colors.Color veticalcolor;
        public static Autodesk.AutoCAD.Colors.Color Horizontalcolor;
        public static int red;
        public static int green;
        public static int blue;

        public static int red1;
        public static int green1;
        public static int blue1;

        public double Vertical_Strip_Weight { get; private set; }
        public double Horizontal_Strip_Weight { get; private set; }
        public Autodesk.AutoCAD.Colors.Color Vertical_Strip_Color { get; private set; }
        public Autodesk.AutoCAD.Colors.Color Horizontal_Strip_Color { get; private set; }


        public Grounding()
        {
            this.Text = "Grounding";
            this.Size = new Size(300, 350);
            this.BackColor = Color.White;

            Label titleLabel = new Label
            {
                Text = "GROUNDING",
                Font = new Font("Arial", 12, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(90, 10)
            };

            // Main Ground strip
            Label mainLabel = new Label
            {
                Text = "Main Ground strip",
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.DeepSkyBlue,
                AutoSize = true,
                Location = new Point(70, 40)
            };

            Label mainWeightLabel = new Label
            {
                Text = "Line Weight :",
                Location = new Point(20, 70),
                AutoSize = true
            };

            mainWeightBox = new TextBox
            {
                Location = new Point(120, 70),
                Width = 100
            };

            Label mainColorLabel = new Label
            {
                Text = "Layer Color :",
                Location = new Point(20, 100),
                AutoSize = true
            };

            mainColorPanel = new Panel
            {
                Location = new Point(120, 100),
                Size = new Size(25, 25),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            mainColorPanel.Click += ColorPanel_Click;

            // Module Ground strip
            Label moduleLabel = new Label
            {
                Text = "Module Ground strip",
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.DeepSkyBlue,
                AutoSize = true,
                Location = new Point(55, 140)
            };

            Label moduleWeightLabel = new Label
            {
                Text = "Line Weight :",
                Location = new Point(20, 170),
                AutoSize = true
            };

            moduleWeightBox = new TextBox
            {
                Location = new Point(120, 170),
                Width = 100
            };

            Label moduleColorLabel = new Label
            {
                Text = "Layer Color :",
                Location = new Point(20, 200),
                AutoSize = true
            };

            moduleColorPanel = new Panel
            {
                Location = new Point(120, 200),
                Size = new Size(25, 25),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            moduleColorPanel.Click += ColorPanel_Click;

            // Proceed Button
            Button proceedButton = new Button
            {
                Text = "Proceed",
                Location = new Point(100, 250),
                Size = new Size(80, 30)
            };
            proceedButton.Click += ProceedButton_Click;

            this.Controls.AddRange(new Control[] {
                titleLabel, mainLabel, mainWeightLabel, mainWeightBox,
                mainColorLabel, mainColorPanel,
                moduleLabel, moduleWeightLabel, moduleWeightBox,
                moduleColorLabel, moduleColorPanel,
                proceedButton
            });
        }

        private void ColorPanel_Click(object sender, EventArgs e)
        {
            Panel colorPanel = sender as Panel;
            using (ColorDialog colorDialog = new ColorDialog())
            {
                colorDialog.AllowFullOpen = true;
                colorDialog.FullOpen = true;
                colorDialog.Color = colorPanel.BackColor;

                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    colorPanel.BackColor = colorDialog.Color;
                    Autodesk.AutoCAD.Colors.Color acadColor = Autodesk.AutoCAD.Colors.Color.FromRgb(colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B);

                    if (colorPanel == mainColorPanel)
                    {
                        veticalcolor = acadColor;
                        red1 = acadColor.Red;
                        green1 = acadColor.Green;
                        blue1 = acadColor.Blue;
                    }
                    else if (colorPanel == moduleColorPanel)
                    {
                        Horizontalcolor = acadColor;
                        red = acadColor.Red;
                        green = acadColor.Green;
                        blue = acadColor.Blue;
                    }
                }
            }
        }

        private void ProceedButton_Click(object sender, EventArgs e)
        {

            if (veticalcolor == null)
            {
                MessageBox.Show("Please select a color for the Main Ground Strip.", "Missing Color", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return;
            }

            if (Horizontalcolor == null)
            {
                MessageBox.Show("Please select a color for the Module Ground Strip.", "Missing Color", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return;
            }

            Vertical_Strip_Color = null;
            Horizontal_Strip_Color = null;

            // Try parsing weights with validation
            if (!double.TryParse(mainWeightBox.Text, out double verticalWeight) || verticalWeight <= 0)
            {
                MessageBox.Show("Main Ground strip Line Weight must be a valid positive number.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!double.TryParse(moduleWeightBox.Text, out double horizontalWeight) || horizontalWeight <= 0)
            {
                MessageBox.Show("Module Ground strip Line Weight must be a valid positive number.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Vertical_Strip_Weight = verticalWeight;
            Horizontal_Strip_Weight = horizontalWeight;

            // Execute the AutoCAD command
            Document doc = Application.DocumentManager.MdiActiveDocument;
            doc.SendStringToExecute("DrawFullWiringWithGroundingStrips ", true, false, false);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }


        private void Grounding_Load(object sender, EventArgs e)
        {

        }
    }
}
