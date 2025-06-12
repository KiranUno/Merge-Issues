namespace Uno_Solar_Design_Assist_Pro
{
    partial class Terrain_Mesh
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            button1 = new System.Windows.Forms.Button();
            trackBar1 = new System.Windows.Forms.TrackBar();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)trackBar1).BeginInit();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new System.Drawing.Point(85, 134);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(75, 23);
            button1.TabIndex = 3;
            button1.Text = "Submit";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // trackBar1
            // 
            trackBar1.LargeChange = 1;
            trackBar1.Location = new System.Drawing.Point(20, 60);
            trackBar1.Maximum = 2;
            trackBar1.Name = "trackBar1";
            trackBar1.Size = new System.Drawing.Size(200, 45);
            trackBar1.TabIndex = 4;
            trackBar1.Value = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Verdana", 9F);
            label1.Location = new System.Drawing.Point(25, 85);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(40, 14);
            label1.TabIndex = 5;
            label1.Text = "Small";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new System.Drawing.Font("Verdana", 9F);
            label2.Location = new System.Drawing.Point(92, 85);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(55, 14);
            label2.TabIndex = 6;
            label2.Text = "Medium";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new System.Drawing.Font("Verdana", 9F);
            label3.Location = new System.Drawing.Point(169, 85);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(43, 14);
            label3.TabIndex = 7;
            label3.Text = "Large";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, 0);
            label4.Location = new System.Drawing.Point(20, 20);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(140, 14);
            label4.TabIndex = 8;
            label4.Text = "Mesh Dencity Control:";
            // 
            // Terrain_Mesh
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(234, 171);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(trackBar1);
            Controls.Add(button1);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Terrain_Mesh";
            Text = "Terrain_Mesh";
            ((System.ComponentModel.ISupportInitialize)trackBar1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TrackBar trackBar1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
    }
}