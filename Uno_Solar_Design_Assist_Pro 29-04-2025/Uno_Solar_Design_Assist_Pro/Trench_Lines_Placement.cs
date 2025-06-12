using System;
using System.Linq;
using System.Windows.Input;
using System.Windows.Forms;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Uno_Solar_Design_Assist_Pro
{
    internal class Trench_Lines_Placement : ICommand
    {
        public void Execute(object parameter)
        {
            Trenches trench = new Trenches();
            trench.ShowDialog();

            if (trench.DialogResult != DialogResult.OK)
            {
                return;
            }

            string linetypeName = null;
            string linetypeFilePath = @"D:\Desktop\Uno_Solar_Design_Assist_Pro\Uno_Solar_Design_Assist_Pro\Support Documents\Trenches\Line Types\";
            string layerName = null;

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            if (trench.Selected_Trench == "2AC Cable")
            {
                linetypeName = "400mm_45Degree";
                linetypeFilePath = linetypeFilePath + linetypeName + ".lin";
                linetypeName = "Trench-Layer1";
            }
            else if (trench.Selected_Trench == "3AC Cable")
            {
                linetypeName = "600mm_45Degree";
                linetypeFilePath = linetypeFilePath + linetypeName + ".lin";
                linetypeName = "Trench-Layer1";
            }
            else if (trench.Selected_Trench == "4AC Cable")
            {
                linetypeName = "800mm_Honeycombe";
                linetypeFilePath = linetypeFilePath + linetypeName + ".lin";
                linetypeName = "Trench-Layer1";
            }
            else if (trench.Selected_Trench == "5AC Cable")
            {
                linetypeName = "127mm_HoneyCombe";
                linetypeFilePath = linetypeFilePath + linetypeName + ".lin";
                linetypeName = "Trench-Layer1";                
            }
            else if (trench.Selected_Trench == "DC Cable")
            {
                linetypeName = "450mm_45_Degrees";
                linetypeFilePath = linetypeFilePath + linetypeName + ".lin";
                linetypeName = "Trench-Layer1";
            }
            else
            {
                MessageBox.Show("Invalid Trench Type Selection", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Load the linetype if it does not exist
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LinetypeTable ltTable = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);

                    if (!ltTable.Has(linetypeName))
                    {
                        db.LoadLineTypeFile(linetypeName, linetypeFilePath);
                    }

                    LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                    ObjectId layerId;

                    if (!lt.Has(layerName))
                    {
                        lt.UpgradeOpen();
                        LayerTableRecord newLayer = new LayerTableRecord
                        {
                            Name = layerName,
                            LinetypeObjectId = ltTable.Has(linetypeName) ? ltTable[linetypeName] : db.ContinuousLinetype
                        };
                        layerId = lt.Add(newLayer);
                        tr.AddNewlyCreatedDBObject(newLayer, true);
                    }
                    else
                    {
                        layerId = lt[layerName];
                    }

                    tr.Commit();
                }
                PromptPointOptions ppo = new PromptPointOptions("\nSpecify the start point of the polyline: ");
                ppo.AllowNone = true;
                PromptPointResult pprStart = ed.GetPoint(ppo);

                if (pprStart.Status != PromptStatus.OK) return;

                List<Point3d> points = new List<Point3d> { pprStart.Value };

                ObjectId polylineId = ObjectId.Null;

                while (true)
                {
                    // Ask for next point
                    ppo = new PromptPointOptions("\nSpecify the next point of the polyline or press Enter to finish: ");
                    ppo.BasePoint = points.Last();
                    ppo.UseBasePoint = true;
                    ppo.AllowNone = true;

                    PromptPointResult pprNext = ed.GetPoint(ppo);
                    if (pprNext.Status == PromptStatus.Cancel || pprNext.Status == PromptStatus.None)
                        break;

                    points.Add(pprNext.Value);
                    ppo.BasePoint = pprNext.Value;

                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                        Polyline polyline;

                        if (polylineId.IsNull)
                        {
                            polyline = new Polyline();
                            polyline.SetDatabaseDefaults();
                            polyline.Linetype = linetypeName;
                            polyline.Layer = layerName;

                            for (int i = 0; i < points.Count; i++)
                            {
                                polyline.AddVertexAt(i, new Point2d(points[i].X, points[i].Y), 0, 0, 0);
                            }

                            polylineId = btr.AppendEntity(polyline);
                            tr.AddNewlyCreatedDBObject(polyline, true);
                        }
                        else
                        {
                            // Modify existing polyline
                            polyline = (Polyline)tr.GetObject(polylineId, OpenMode.ForWrite);
                            polyline.AddVertexAt(polyline.NumberOfVertices,
                                new Point2d(pprNext.Value.X, pprNext.Value.Y), 0, 0, 0);
                        }

                        tr.Commit();
                    }
                }
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LinetypeTable ltTable = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);

                    if (ltTable.Has(linetypeName))
                    {
                        ltTable.UpgradeOpen();
                        ObjectId linetypeId = ltTable[linetypeName];
                        LinetypeTableRecord ltRecord = (LinetypeTableRecord)tr.GetObject(linetypeId, OpenMode.ForWrite);

                        if (!ltRecord.IsErased)
                        {
                            ltRecord.Erase();
                        }
                    }
                    tr.Commit();
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage($"\nError: {ex.Message}");
            }
        }
        
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
