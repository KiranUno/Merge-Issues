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
    public partial class Zoom : Form
    {
        private Panel zoomPanel;
        private ZoomablePictureBox pictureBox;

        private TextBox leftTextBox;
        private TextBox rightTextBox;

        private Point lastMousePos;
        private Font baseFont; // Used for font scaling if desired

        private Button proceedButton;

        // Member variables to hold the parsed values, accessible for drawing
        private float _currentLeftValue = 0.0f;
        private float _currentRightValue = 0.0f;

        public float InitialLeftValue { get; set; } = 0.1f; // Default value, can be set before Form1 is shown
        public float InitialRightValue { get; set; } = 8.0f; // Default value, can be set before Form1 is shown

        public Zoom()
        {
            this.Text = "Dimension";
            this.MinimumSize = new Size(700, 800); // optional: prevent resizing too small
            this.MaximumSize = new Size(700, 800); // optional: prevent resizing too big
            InitializeComponent();

        }

        private void Zoom_Load(object sender, EventArgs e)
        {
            zoomPanel = new Panel
            {
                Location = new Point(0, 120), // Position below the input controls
                Size = new Size(this.ClientSize.Width, this.ClientSize.Height - 120),
                AutoScroll = false,

                BackColor = Color.White
            };
            this.Controls.Add(zoomPanel);
            zoomPanel.BringToFront(); // Ensure it's on top of other controls within its area

            // PictureBox setup

            pictureBox = new ZoomablePictureBox
            {
                Size = new Size(700, 600),
                /* Size = new Size(800, 800) ,*///size, will be updated when image loads
                SizeMode = PictureBoxSizeMode.Normal,
                Cursor = Cursors.SizeAll, // Indicate it's draggable
                Location = new Point(0, 0), // Position within the zoomPanel
                BackColor = Color.White
            };
            zoomPanel.Controls.Add(pictureBox);

            // Attach the Paint event handler for custom drawing
            pictureBox.Paint += PictureBox_Paint;

            // --- Control Layout (Matching the screenshot) ---
            int labelLeft = 30;
            int baseLeft = 30; // Starting Y position for the top row of controls

            // LeftTextBox Label (LeftTextBox)
            Label leftLabel = new Label
            {
                Text = "Left",
                Location = new Point(labelLeft + 130, baseLeft), // Adjusted to be above the textbox
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            this.Controls.Add(leftLabel);

            // RightTextBox Label (RightTextBox)
            Label rightLabel = new Label
            {
                Text = "Right",
                Location = new Point(labelLeft + 200, baseLeft), // Adjusted to be above the textbox
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            this.Controls.Add(rightLabel);

            // Dimensions Label
            Label dimLabel = new Label
            {
                Text = "Dimensions :",
                Location = new Point(labelLeft, baseLeft + 25), // Aligned with textboxes
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            this.Controls.Add(dimLabel);

            // Left TextBox
            leftTextBox = new TextBox
            {
                Location = new Point(labelLeft + 130, baseLeft + 25),
                Width = 60
            };
            this.Controls.Add(leftTextBox);
            leftTextBox.TextChanged += DimensionTextChanged;
            leftTextBox.Validating += LeftTextBox_Validating;

            // Right TextBox
            rightTextBox = new TextBox
            {
                Location = new Point(labelLeft + 200, baseLeft + 25),
                Width = 60
            };
            this.Controls.Add(rightTextBox);
            rightTextBox.TextChanged += DimensionTextChanged;
            rightTextBox.Validating += RightTextBox_Validating;


            leftTextBox.Text = InitialLeftValue.ToString("F2");
            rightTextBox.Text = InitialRightValue.ToString("F2");

            // Proceed Button
            proceedButton = new Button
            {
                Text = "Proceed",
                Location = new Point(labelLeft + 160, baseLeft + 65), // Centered below textboxes
                Width = 100,
                Height = 30
            };
            proceedButton.Click += ProceedBtn_Click;
            this.Controls.Add(proceedButton);


            baseFont = leftTextBox.Font; // Capture base font for scaling

            baseFont = rightTextBox.Font; // Capture base font for scaling
            DimensionTextChanged(null, null);
            LoadImageIntoPictureBox();
            this.Resize += (s, args) =>
            {
                zoomPanel.Size = new Size(this.ClientSize.Width, this.ClientSize.Height - 120);

            };

        }
        private void LoadImageIntoPictureBox()
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.bmp;*.gif|All Files|*.*"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Image img = Image.FromFile(ofd.FileName);
                    pictureBox.Image = img;
                    pictureBox.ZoomFactor = 0.8f; // Reset zoom

                    pictureBox.CenterImage(); // ✅ Center the image inside the PictureBox

                    zoomPanel.AutoScrollPosition = new Point(0, 0);
                    pictureBox.Invalidate(); // Repaint
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Image Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }



        private void DimensionTextChanged(object sender, EventArgs e)
        {
            bool leftParsed = float.TryParse(leftTextBox.Text, out _currentLeftValue);
            bool rightParsed = float.TryParse(rightTextBox.Text, out _currentRightValue);


        }

        // --- Custom Drawing on PictureBox ---
        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            // Only draw if there's an image to draw on
            if (pictureBox.Image == null) return;

            Graphics g = e.Graphics;


            // Set high quality rendering for text to avoid pixelation when zoomed
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            // Define text properties
            using (Font drawFont = new Font("Arial", 20, FontStyle.Bold)) // Larger font for better visibility on image
            using (SolidBrush drawBrush = new SolidBrush(Color.Blue)) // Use a contrasting color
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center })
            {
                // Calculate text positions relative to the *original image coordinates*.
                // These will be automatically scaled and positioned by the PictureBox's transformations.

                float leftTextX = pictureBox.Image.Width * 0.2f; // 10% from left edge of image
                float leftTextY = pictureBox.Image.Height * 0.3f; // 10% from top edge of image
                g.DrawString($" {_currentLeftValue:F2}", drawFont, drawBrush, leftTextX, leftTextY);

                float rightTextX = pictureBox.Image.Width * 0.4f; // 90% from left edge of image
                float rightTextY = pictureBox.Image.Height * 0.3f; // 10% from top edge of image

                g.DrawString($" {_currentRightValue:F2}", drawFont, drawBrush, rightTextX, rightTextY);


            }
        }

        private void ProceedBtn_Click(object sender, EventArgs e)
        {
            if (float.TryParse(leftTextBox.Text, out float leftValue) &&
                 float.TryParse(rightTextBox.Text, out float rightValue))
            {
                // _currentLeftValue and _currentRightValue are already updated by DimensionTextChanged
                MessageBox.Show($"Proceeding with Left: {_currentLeftValue:F2}, Right: {_currentRightValue:F2}",
                                "Proceed Clicked", MessageBoxButtons.OK, MessageBoxIcon.Information);

                pictureBox.Invalidate(); // Force a repaint to ensure values are current on image
            }
            else
            {
                MessageBox.Show("Invalid input for Left or Right dimension. Please enter valid numbers.",
                                "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- Validation Handlers ---
        private void LeftTextBox_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!float.TryParse(leftTextBox.Text, out _))
            {

                leftTextBox.BackColor = SystemColors.Window; // Default background
            }
        }

        private void RightTextBox_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!float.TryParse(rightTextBox.Text, out _))
            {

                rightTextBox.BackColor = SystemColors.Window; // Default background
            }
        }
    }
}








