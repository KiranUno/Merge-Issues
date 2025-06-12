using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Uno_Solar_Design_Assist_Pro
{
    public partial class Frames : Form
    {
        public double TableHeight { get; private set; }
        public double TableWidth { get; private set; }
        public double HorizontalOffset { get; private set; }
        public double VerticalOffset { get; private set; }
        public double BorderOffset { get; private set; }

        public double TablePitch { get; private set; }

        public double RoadWidth { get; private set; }

        public Frames()
        {
            InitializeComponent();
        }

        private void Frames_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!double.TryParse(Height.Text, out double height))
                height = 0;

            if (!double.TryParse(Width.Text, out double width))
                width = 0;

            if (!double.TryParse(HOffset.Text, out double hOffset))
                hOffset = 0;

            if (!double.TryParse(VOffset.Text, out double vOffset))
                vOffset = 0;
            //VOffset
            if (!double.TryParse(BOffset.Text, out double bOffset))
                bOffset = 0;

            if (!double.TryParse(RWidth.Text, out double roadWidth))
                roadWidth = 0;


            if (!double.TryParse(Pitch.Text, out double tablePitch))
                tablePitch = 0;

            TableHeight = height;
            TableWidth = width;
            HorizontalOffset = hOffset;
            VerticalOffset = vOffset;
            BorderOffset = bOffset;
            RoadWidth = roadWidth;
            TablePitch = tablePitch;

            this.DialogResult = DialogResult.OK;
            //Properties.Settings.Default.TableHeight = Height.Text;
            //Properties.Settings.Default.TableWidth = Width.Text;
            //Properties.Settings.Default.HOffset = HOffset.Text;
            //Properties.Settings.Default.VOffset = VOffset.Text;
            //Properties.Settings.Default.BOffset = BOffset.Text;
            //Properties.Settings.Default.RoadWidth = RWidth.Text;
            //Properties.Settings.Default.TablePitch = Pitch.Text;
            //Properties.Settings.Default.Save();

            Close();
        }
    }
}
