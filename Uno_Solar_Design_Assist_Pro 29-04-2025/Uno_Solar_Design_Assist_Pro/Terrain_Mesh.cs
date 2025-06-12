using System;
using System.Windows.Forms;

namespace Uno_Solar_Design_Assist_Pro
{
    public partial class Terrain_Mesh : Form
    {
        public char Mesh_Dencity;
        public Terrain_Mesh()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(trackBar1.Value == 0)            
                Mesh_Dencity = 'S';            
            
            else if (trackBar1.Value == 1)            
                Mesh_Dencity = 'M';
            
            else if (trackBar1.Value == 2)            
                Mesh_Dencity = 'L';
            
            this.DialogResult = DialogResult.OK;
        }
    }
}
