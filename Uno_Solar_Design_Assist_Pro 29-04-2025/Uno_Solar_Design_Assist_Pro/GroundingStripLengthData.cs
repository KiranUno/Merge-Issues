using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

namespace Uno_Solar_Design_Assist_Pro
{
    internal class GroundingStripLengthData : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public static Color Vertical_color = Color.FromRgb((byte)Grounding.red1, (byte)Grounding.green1, (byte)Grounding.blue1);
        public static Color Horizontal_color = Color.FromRgb((byte)Grounding.red, (byte)Grounding.green, (byte)Grounding.blue);

        public bool CanExecute(object parameter) => true;

        [CommandMethod("CalculateGroundStripLengthsInBoundary")]
        public void Execute(object parameter)
        {
            Vertical_color = Color.FromRgb((byte)Grounding.red1, (byte)Grounding.green1, (byte)Grounding.blue1);

            Horizontal_color = Color.FromRgb((byte)Grounding.red, (byte)Grounding.green, (byte)Grounding.blue);

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Select the boundary polyline
                Polyline boundary = SelectBoundaryPolyline(ed, tr);
                if (boundary == null) return;

                double totalMainGroundStripLength = 0;
                double totalModuleGroundStripLength = 0;



                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                foreach (ObjectId objId in btr)
                {
                    Entity ent = (Entity)tr.GetObject(objId, OpenMode.ForRead);
                    if (ent is Polyline poly)
                    {
                        if (ent.Layer == "UnoTEAM_GROUNDING" || ent.Layer == "UnoTEAM_GROUNDING") // Handle potential leading space

                        {
                            // Main Ground Strip: Green (ColorIndex 3)
                            if (poly.Color == Color.FromRgb((byte)Grounding.red1, (byte)Grounding.green1, (byte)Grounding.blue1))
                            {
                                totalMainGroundStripLength += poly.Length;
                            }
                            // Module Ground Strip: Blue (ColorIndex 6)
                            else if (poly.Color == Color.FromRgb((byte)Grounding.red, (byte)Grounding.green, (byte)Grounding.blue))
                            {
                                totalModuleGroundStripLength += poly.Length;
                            }
                        }
                    }
                }



                string mainLengthFormatted = $"{totalMainGroundStripLength:F3}";
                string moduleLengthFormatted = $"{totalModuleGroundStripLength:F3}";

                ed.WriteMessage($"\nTotal Main Ground Strip Length: {mainLengthFormatted} m");
                ed.WriteMessage($"\nTotal Module Ground Strip Length: {moduleLengthFormatted} m");

                string folderPath = GetFolderPath();
                if (string.IsNullOrEmpty(folderPath))
                {
                    ed.WriteMessage("\nOperation cancelled by user.");
                    return;
                }

                string excelFilePath = Path.Combine(folderPath, "GroundStripLengthsInBoundary.xlsx");
                ExportToExcel(mainLengthFormatted, moduleLengthFormatted, excelFilePath);

                tr.Commit();
            }
        }

        private static Polyline SelectBoundaryPolyline(Editor ed, Transaction tr)
        {
            PromptEntityOptions peo = new PromptEntityOptions("\nSelect the outer boundary polyline: ");
            peo.SetRejectMessage("\nOnly closed polylines allowed.");
            peo.AddAllowedClass(typeof(Polyline), true);

            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status == PromptStatus.OK)
            {
                Polyline boundary = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Polyline;
                if (boundary != null && boundary.Closed)
                    return boundary;
            }
            ed.WriteMessage("\nInvalid or non-closed polyline selected as boundary.");
            return null;
        }

        private static bool IsPointInside(Polyline poly, Point3d pt)
        {
            Point2d test = new Point2d(pt.X, pt.Y);
            int count = poly.NumberOfVertices;
            int crossings = 0;

            for (int i = 0; i < count; i++)
            {
                Point2d p1 = new Point2d(poly.GetPoint3dAt(i).X, poly.GetPoint3dAt(i).Y);
                Point2d p2 = new Point2d(poly.GetPoint3dAt((i + 1) % count).X, poly.GetPoint3dAt((i + 1) % count).Y);

                if (((p1.Y <= test.Y && test.Y < p2.Y) || (p2.Y <= test.Y && test.Y < p1.Y)) &&
                    (test.X < (p2.X - p1.X) * (test.Y - p1.Y) / (p2.Y - p1.Y + 0.00001) + p1.X))
                {
                    crossings++;
                }
            }

            return (crossings % 2 == 1);
        }

        private static string GetFolderPath()
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select the folder to save the Excel file.";
                folderDialog.ShowNewFolderButton = true;
                DialogResult result = folderDialog.ShowDialog();
                if (result == DialogResult.OK)
                    return folderDialog.SelectedPath;
            }
            return null;
        }

        private static void ExportToExcel(string mainGroundStripLength, string moduleGroundStripLength, string excelFilePath)
        {
            dynamic excelApp = null;
            dynamic workbook = null;
            dynamic sheet = null;

            try
            {
                excelApp = Activator.CreateInstance(Type.GetTypeFromProgID("Excel.Application"));
                workbook = excelApp.Workbooks.Add();
                sheet = workbook.ActiveSheet;
                excelApp.Visible = false;

                // Headers
                sheet.Cells[1, 1] = "Ground Strip Type";
                sheet.Cells[1, 2] = "Length (m)";

                // Data
                sheet.Cells[2, 1] = "Main Ground Strips";
                sheet.Cells[2, 2] = mainGroundStripLength;
                sheet.Cells[3, 1] = "Module Ground Strips";
                sheet.Cells[3, 2] = moduleGroundStripLength;

                // Format
                dynamic headerRange = sheet.Range["A1:B1"];
                headerRange.Font.Bold = true;
                headerRange.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.LightGray);
                headerRange.HorizontalAlignment = -4108; // xlCenter
                sheet.Columns.AutoFit();

                workbook.SaveAs(excelFilePath);
                excelApp.Visible = true;

                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\nData saved to {excelFilePath}");
            }
            catch (Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\nExcel Error: {ex.Message}");
            }
            finally
            {
                if (sheet != null) Marshal.ReleaseComObject(sheet);
                if (workbook != null) Marshal.ReleaseComObject(workbook);
                if (excelApp != null)
                {
                    excelApp.Quit();
                    Marshal.ReleaseComObject(excelApp);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}


  
  