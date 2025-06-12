using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
using Pen = System.Drawing.Pen;

namespace Uno_Solar_Design_Assist_Pro
{
    public partial class Terrain_Mesh : Form
    {

        public static List<double> angleMinList = new List<double>();
        public static List<double> angleMaxList = new List<double>();
        public static List<Color> colorList = new List<Color>();

        public static char Mesh_Dencity;
        public static int rowCount;
        public static double endValue;
        public static double startValue;
        public static bool createmesh = false;
        public Terrain_Mesh()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (angleMaxList.Count > 0)
            {
                angleMaxList.Clear();
                angleMinList.Clear();
                colorList.Clear();
                button4.Visible = false;
            }

            if (trackBar1.Value == 0)
                Mesh_Dencity = 'S';

            else if (trackBar1.Value == 1)
                Mesh_Dencity = 'M';

            else if (trackBar1.Value == 2)
                Mesh_Dencity = 'L';
            label5.Visible = true;
            label6.Visible = true;
            label7.Visible = true;
            textBox1.Visible = true;
            textBox2.Visible = true;
            button2.Visible = true;
            comboBox1.Visible = true;
            MessageBox.Show("Data Saved for Compactness");
        }

        private void Terrain_Mesh_Load(object sender, EventArgs e)
        {
            if (angleMaxList.Count > 0)
            {
                button4.Visible = true;
                button3.Visible = false;
                label5.Visible = true;
                label6.Visible = true;
                label7.Visible = true;
                textBox1.Visible = true;
                textBox2.Visible = true;
                button2.Visible = true;
                comboBox1.Visible = true;
                dataGridView1.Visible = true;
                textBox1.Text = startValue.ToString();
                textBox2.Text = endValue.ToString();
                comboBox1.Text = rowCount.ToString();
                button2.PerformClick();
            }
            else
            {
                button4.Visible = false;
                button3.Visible = false;
                label5.Visible = false;
                label6.Visible = false;
                label7.Visible = false;
                textBox1.Visible = false;
                textBox2.Visible = false;
                comboBox1.Visible = false;
                dataGridView1.Visible = false;
            }

            this.Paint += new PaintEventHandler(Form1_Paint);
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(System.Drawing.Color.Black, 2))
            {

                var submitButton = this.Controls["button1"]; // Replace with actual Name of Submit button
                var meshLabel = this.Controls["trackBar1"];       // Replace with actual Name of Mesh Control label

                // Horizontal line below Submit button
                int horizontalY = submitButton.Bottom + 20; // 5px gap below
                int verticalX = meshLabel.Right + 10;      // Vertical line just right of mesh panel

                // Draw horizontal line from left to vertical line
                e.Graphics.DrawLine(pen, 0, horizontalY, verticalX, horizontalY);

                // Draw vertical line from top to bottom of form
                e.Graphics.DrawLine(pen, verticalX, 0, verticalX, this.ClientSize.Height);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (angleMaxList.Count > 0)
            {
                dataGridView1.Columns.Clear();
                dataGridView1.Rows.Clear();
                dataGridView1.RowHeadersVisible = false;
                dataGridView1.AllowUserToAddRows = false;

                dataGridView1.Columns.Add("AngleMin", "Angle min., °");
                dataGridView1.Columns.Add("AngleMax", "Angle max., °");
                dataGridView1.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Color", HeaderText = "Color" });
                dataGridView1.Columns.Add("Distribution", "Distribution, %");

                for (int i = 0; i < angleMinList.Count; i++)
                {
                    int rowIndex = dataGridView1.Rows.Add(angleMinList[i].ToString("0.00"), angleMaxList[i].ToString("0.00"), "", "0.00%");

                    // Set color cell
                    DataGridViewCell colorCell = dataGridView1.Rows[rowIndex].Cells[2];
                    colorCell.Style.BackColor = colorList[i];
                    colorCell.Style.SelectionBackColor = colorList[i];
                    colorCell.Value = "";

                    // Set Angle Min read-only
                    var minCell = dataGridView1.Rows[rowIndex].Cells[0];
                    minCell.ReadOnly = true;
                    minCell.Style.BackColor = Color.LightGray;

                    // Set Angle Max read-only for first and last row
                    var maxCell = dataGridView1.Rows[rowIndex].Cells[1];
                    maxCell.ReadOnly = (i == 0 || i == angleMinList.Count - 1);
                    if (maxCell.ReadOnly)
                    {
                        maxCell.Style.BackColor = Color.LightGray;
                    }

                    // Set Distribution column read-only
                    dataGridView1.Rows[rowIndex].Cells[3].ReadOnly = true;
                }
                int rowHeight = dataGridView1.RowTemplate.Height;
                int headerHeight = dataGridView1.ColumnHeadersHeight;
                dataGridView1.Height = headerHeight + (rowHeight * rowCount) + 2;
            }
            else
            {


                startValue = Math.Round(Convert.ToDouble(textBox1.Text), 2);
                endValue = Math.Round(Convert.ToDouble(textBox2.Text), 2);

                if (comboBox1.SelectedItem != null && int.TryParse(comboBox1.SelectedItem.ToString(), out rowCount))
                {
                    // rowCount is now an int
                }

                button3.Visible = true;

                dataGridView1.Visible = true;

                List<Color> gradientColors = GenerateGradientColors(rowCount);
                dataGridView1.AllowUserToAddRows = false;
                dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                dataGridView1.RowHeadersVisible = false;
                dataGridView1.ReadOnly = false;

                dataGridView1.Columns.Clear();
                dataGridView1.Rows.Clear();

                dataGridView1.Columns.Add("AngleMin", "Angle min., °");
                dataGridView1.Columns.Add("AngleMax", "Angle max., °");
                dataGridView1.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Color", HeaderText = "Color" });
                dataGridView1.Columns.Add("Distribution", "Distribution, %");



                double finalMaxAngle = 55.00;

                int middleRowCount = rowCount - 2;

                double step = (endValue - startValue) / middleRowCount;

                for (int i = 0; i < rowCount; i++)
                {
                    double angleMin, angleMax;

                    if (i == 0)
                    {
                        angleMin = 0.00;
                        angleMax = 0.00;
                    }
                    else if (i == rowCount - 1)
                    {
                        angleMin = endValue;
                        angleMax = finalMaxAngle;
                    }
                    else
                    {
                        angleMin = startValue + (i - 1) * step;
                        angleMax = angleMin + step;
                    }

                    int rowIndex = dataGridView1.Rows.Add(angleMin.ToString("0.00"), angleMax.ToString("0.00"), "", "0.00%");

                    // Set color cell
                    DataGridViewCell colorCell = dataGridView1.Rows[rowIndex].Cells[2];
                    colorCell.Style.BackColor = gradientColors[i];
                    colorCell.Style.SelectionBackColor = gradientColors[i];
                    colorCell.Value = "";



                    var minCell = dataGridView1.Rows[rowIndex].Cells[0];
                    minCell.ReadOnly = true;
                    if (i == 0 || i == rowCount - 1)
                    {
                        minCell.ReadOnly = true;
                        minCell.Style.BackColor = Color.LightGray;
                    }
                    //minCell.Style.BackColor = Color.LightGray;

                    // Set angle max read-only and gray for first and last row
                    var maxCell = dataGridView1.Rows[rowIndex].Cells[1];
                    if (i == 0 || i == rowCount - 1)
                    {
                        maxCell.ReadOnly = true;
                        maxCell.Style.BackColor = Color.LightGray;
                    }
                    else
                    {
                        maxCell.ReadOnly = false;
                    }
                    dataGridView1.Rows[rowIndex].Cells[3].ReadOnly = true;
                    dataGridView1.Rows[rowIndex].Cells[0].ReadOnly = true;
                    dataGridView1.Rows[rowIndex].Cells[1].ReadOnly = (i == 0 || i == rowCount - 1);
                    dataGridView1.Rows[rowIndex].Cells[2].ReadOnly = true;
                    dataGridView1.Rows[rowIndex].Cells[3].ReadOnly = true;

                }

                int lastRowIndex = dataGridView1.Rows.Count - 1;
                dataGridView1.Rows[lastRowIndex].Cells[0].ReadOnly = true;
                dataGridView1.Rows[lastRowIndex].Cells[1].ReadOnly = true;

                // Adjust height to fit exactly 5 rows
                int rowHeight = dataGridView1.RowTemplate.Height;
                int headerHeight = dataGridView1.ColumnHeadersHeight;
                dataGridView1.Height = headerHeight + (rowHeight * rowCount) + 2;
                dataGridView1.CellClick += dataGridView1_CellClick;
                dataGridView1.CellValueChanged += dataGridView1_CellValueChanged;
                dataGridView1.CurrentCellDirtyStateChanged += dataGridView1_CurrentCellDirtyStateChanged;
            }
        }

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView1.IsCurrentCellDirty)
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 1 && e.RowIndex >= 0)
            {
                if (e.RowIndex < dataGridView1.Rows.Count - 1)
                {
                    var newValue = dataGridView1.Rows[e.RowIndex].Cells[1].Value?.ToString();

                    if (double.TryParse(newValue, out double parsedValue))
                    {
                        // Get corresponding Angle Min
                        var minValueObj = dataGridView1.Rows[e.RowIndex].Cells[0].Value;
                        if (minValueObj != null && double.TryParse(minValueObj.ToString(), out double minValue))
                        {
                            if (parsedValue < minValue)
                            {
                                MessageBox.Show("Angle Max should not be less than Angle Min.");
                                return;
                            }
                        }

                        // Check against start and end bounds
                        if (parsedValue < startValue || parsedValue > endValue)
                        {
                            MessageBox.Show("Please enter a value between Start & End Values.");

                            if (dataGridView1.IsCurrentCellInEditMode)
                                dataGridView1.CancelEdit();

                            dataGridView1.Rows[e.RowIndex].Cells[1].Value = "";
                            return;
                        }
                        else
                        {
                            int nextRowIndex = e.RowIndex + 1;

                            // Update next row's Angle Min if not the last row
                            if (nextRowIndex < dataGridView1.Rows.Count - 1)
                            {
                                dataGridView1.Rows[nextRowIndex].Cells[0].Value = parsedValue.ToString("0.00");
                            }
                        }
                    }
                }
            }
            if (e.ColumnIndex == 1 && e.RowIndex > 0 && e.RowIndex < dataGridView1.Rows.Count - 1)
            {
                var currentMaxText = dataGridView1.Rows[e.RowIndex].Cells[1].Value?.ToString();

                if (double.TryParse(currentMaxText, out double currentMaxValue))
                {
                    // Validate range
                    if (currentMaxValue < startValue || currentMaxValue > endValue)
                    {
                        MessageBox.Show("Please enter a value between start and end values.");
                        dataGridView1.Rows[e.RowIndex].Cells[1].Value = ""; // Clear
                        return;
                    }

                    // Ensure Max ≥ Min
                    var minCellText = dataGridView1.Rows[e.RowIndex].Cells[0].Value?.ToString();
                    if (double.TryParse(minCellText, out double currentMin))
                    {
                        if (currentMaxValue < currentMin)
                        {
                            MessageBox.Show("Angle Max should not be less than Angle Min.");
                            dataGridView1.Rows[e.RowIndex].Cells[1].Value = "";
                            return;
                        }
                    }

                    // Propagate updated value to both Angle Min and Max
                    double angleMin = currentMaxValue;

                    for (int row = e.RowIndex + 1; row < dataGridView1.Rows.Count - 1; row++)
                    {
                        // Update Angle Min
                        dataGridView1.Rows[row].Cells[0].Value = angleMin.ToString("0.00");

                        // Optionally recalculate Angle Max (e.g., based on previous step size or leave existing)
                        string nextMaxText = dataGridView1.Rows[row].Cells[1].Value?.ToString();
                        if (double.TryParse(nextMaxText, out double nextMax))
                        {
                            if (nextMax >= currentMaxValue)
                                break; // stop propagation if boundary hit
                            dataGridView1.Rows[row].Cells[1].Value = (angleMin).ToString("0.00");
                        }


                    }
                }
            }
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            //if (e.RowIndex >= 0 && e.ColumnIndex == 2) // Color column
            //{
            //    DataGridViewCell cell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
            //    Color currentColor = cell.Style.BackColor;

            //    using (ColorDialog colorDialog = new ColorDialog())
            //    {
            //        colorDialog.Color = currentColor.IsEmpty ? Color.White : currentColor;

            //        if (colorDialog.ShowDialog() == DialogResult.OK)
            //        {
            //            cell.Style.BackColor = colorDialog.Color;
            //            cell.Style.SelectionBackColor = colorDialog.Color;
            //            dataGridView1.InvalidateCell(cell); // refresh cell
            //        }
            //    }
            //}
        }

        private List<Color> GenerateGradientColors(int rowCount)
        {
            List<Color> colors = new List<Color>();

            for (int i = 0; i < rowCount; i++)
            {
                float ratio = (float)i / (rowCount - 1);
                Color color;

                if (ratio < 0.33f)
                {
                    color = InterpolateColor(Color.Green, Color.Yellow, ratio / 0.33f);
                }
                else if (ratio < 0.66f)
                {
                    color = InterpolateColor(Color.Yellow, Color.Orange, (ratio - 0.33f) / 0.33f);
                }
                else
                {
                    color = InterpolateColor(Color.Orange, Color.Red, (ratio - 0.66f) / 0.34f);
                }

                colors.Add(color);
            }

            return colors;
        }

        private Color InterpolateColor(Color green, Color yellow, float v)
        {
            int r = (int)(green.R + (yellow.R - green.R) * v);
            int g = (int)(green.G + (yellow.G - green.G) * v);
            int b = (int)(green.B + (yellow.B - green.B) * v);
            return Color.FromArgb(r, g, b);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            createmesh = true;
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                // Skip the last row if it's a new row placeholder
                if (!row.IsNewRow)
                {
                    if (double.TryParse(row.Cells[0].Value?.ToString(), out double angleMin) &&
                        double.TryParse(row.Cells[1].Value?.ToString(), out double angleMax))
                    {
                        angleMinList.Add(angleMin);
                        angleMaxList.Add(angleMax);

                        Color cellColor = row.Cells[2].Style.BackColor;
                        string rgb = $"{cellColor.R},{cellColor.G},{cellColor.B}";
                        colorList.Add(cellColor);
                    }
                }
            }
            MessageBox.Show("Please select contours");
            button3.Visible = false;
            button4.Visible = true;
            Mesh_Creation b = new Mesh_Creation();
            b.selectContours();
            b.colap();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.Close();
            if (angleMaxList.Count <= 0)
            {
                MessageBox.Show("No Data Found");
            }
            else
            {
                Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                Editor ed = doc.Editor;
                Database db = doc.Database;
                using (doc.LockDocument())
                {
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                        PromptPointResult ppr = ed.GetPoint("\nSelect a point to place the table: ");

                        if (ppr.Status == PromptStatus.OK)
                        {
                            Point3d pickedPoint = ppr.Value;

                            Table table = new Table();
                            table.TableStyle = db.Tablestyle;
                            table.NumRows = angleMaxList.Count;
                            table.NumColumns = 4;
                            int Cnt = angleMaxList.Count;
                            table.SetSize(Cnt + 2, 4);
                            table.SetRowHeight(5);
                            table.SetColumnWidth(20);
                            table.Position = pickedPoint;

                            table.Cells[1, 0].TextString = "Angle Min";
                            table.Cells[1, 1].TextString = "Angle Max";
                            table.Cells[1, 2].TextString = "Distribution %";
                            table.Cells[1, 3].TextString = "Color";
                            for (int col = 0; col < 4; col++)
                            {
                                table.Cells[1, col].TextHeight = 2;
                                table.Cells[1, col].ContentColor = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByAci, 2); // Red or change as needed
                            }

                            for (int i = 0; i < rowCount; i++)
                            {
                                int row = i + 2; // data starts at row index 1

                                table.Cells[row, 0].TextString = angleMinList[i].ToString("F2");
                                table.Cells[row, 1].TextString = angleMaxList[i].ToString("F2");

                                table.Cells[row, 2].TextString = Mesh_Creation.percnt[i].ToString("F2");

                                for (int col = 0; col < 4; col++)
                                {
                                    table.Cells[row, col].TextHeight = 2; // Or larger depending on your units
                                    table.Cells[row, col].ContentColor = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByAci, 1);
                                    table.Cells[row, col].Alignment = CellAlignment.MiddleCenter;
                                }

                                System.Drawing.Color sysColor = colorList[i];
                                Autodesk.AutoCAD.Colors.Color acColor = Autodesk.AutoCAD.Colors.Color.FromRgb(sysColor.R, sysColor.G, sysColor.B);
                                table.Cells[row, 3].BackgroundColor = acColor;
                            }

                            btr.AppendEntity(table);
                            tr.AddNewlyCreatedDBObject(table, true);
                            tr.Commit();
                        }
                    }
                }
            }
        }
    }
}
