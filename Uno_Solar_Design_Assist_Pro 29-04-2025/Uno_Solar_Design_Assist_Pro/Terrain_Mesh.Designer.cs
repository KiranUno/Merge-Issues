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
            comboBox1 = new System.Windows.Forms.ComboBox();
            button2 = new System.Windows.Forms.Button();
            textBox2 = new System.Windows.Forms.TextBox();
            textBox1 = new System.Windows.Forms.TextBox();
            label7 = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            dataGridView1 = new System.Windows.Forms.DataGridView();
            button4 = new System.Windows.Forms.Button();
            button3 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)trackBar1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new System.Drawing.Point(75, 119);
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
            trackBar1.Location = new System.Drawing.Point(12, 53);
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
            label1.Location = new System.Drawing.Point(17, 78);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(40, 14);
            label1.TabIndex = 5;
            label1.Text = "Small";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new System.Drawing.Font("Verdana", 9F);
            label2.Location = new System.Drawing.Point(84, 78);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(55, 14);
            label2.TabIndex = 6;
            label2.Text = "Medium";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new System.Drawing.Font("Verdana", 9F);
            label3.Location = new System.Drawing.Point(161, 78);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(43, 14);
            label3.TabIndex = 7;
            label3.Text = "Large";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            label4.Location = new System.Drawing.Point(20, 20);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(125, 15);
            label4.TabIndex = 8;
            label4.Text = "Mesh Dencity Control:";
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Items.AddRange(new object[] { "4", "5", "6", "7", "8", "9", "10" });
            comboBox1.Location = new System.Drawing.Point(112, 296);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new System.Drawing.Size(100, 23);
            comboBox1.TabIndex = 31;
            // 
            // button2
            // 
            button2.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            button2.Location = new System.Drawing.Point(30, 335);
            button2.Name = "button2";
            button2.Size = new System.Drawing.Size(75, 25);
            button2.TabIndex = 30;
            button2.Text = "Get Table";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // textBox2
            // 
            textBox2.Location = new System.Drawing.Point(112, 253);
            textBox2.Name = "textBox2";
            textBox2.Size = new System.Drawing.Size(100, 23);
            textBox2.TabIndex = 29;
            // 
            // textBox1
            // 
            textBox1.Location = new System.Drawing.Point(112, 213);
            textBox1.Name = "textBox1";
            textBox1.Size = new System.Drawing.Size(100, 23);
            textBox1.TabIndex = 28;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            label7.Location = new System.Drawing.Point(30, 299);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(64, 15);
            label7.TabIndex = 27;
            label7.Text = "Row count";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            label6.Location = new System.Drawing.Point(30, 256);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(66, 15);
            label6.TabIndex = 26;
            label6.Text = "End value,*";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            label5.Location = new System.Drawing.Point(30, 216);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(70, 15);
            label5.TabIndex = 25;
            label5.Text = "Start value,*";
            // 
            // dataGridView1
            // 
            dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new System.Drawing.Point(240, 22);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.ScrollBars = System.Windows.Forms.ScrollBars.None;
            dataGridView1.Size = new System.Drawing.Size(423, 297);
            dataGridView1.TabIndex = 32;
            // 
            // button4
            // 
            button4.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            button4.Location = new System.Drawing.Point(551, 335);
            button4.Name = "button4";
            button4.Size = new System.Drawing.Size(103, 25);
            button4.TabIndex = 34;
            button4.Text = "Export to CAD";
            button4.UseVisualStyleBackColor = true;
            button4.Click += button4_Click;
            // 
            // button3
            // 
            button3.CausesValidation = false;
            button3.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            button3.Location = new System.Drawing.Point(437, 335);
            button3.Name = "button3";
            button3.Size = new System.Drawing.Size(108, 25);
            button3.TabIndex = 33;
            button3.Text = "Generate Mesh";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // Terrain_Mesh
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(666, 374);
            Controls.Add(button4);
            Controls.Add(button3);
            Controls.Add(dataGridView1);
            Controls.Add(comboBox1);
            Controls.Add(button2);
            Controls.Add(textBox2);
            Controls.Add(textBox1);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(label5);
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
            Load += Terrain_Mesh_Load;
            ((System.ComponentModel.ISupportInitialize)trackBar1).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
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
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button3;
    }
}