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
            if (Trenches.radio1 == true)
            {

                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                Editor ed = doc.Editor;

                string linetypeName = "400mm_45Degree";
                string linetypeFilePath = @"C:\ProgramData\Autodesk\ApplicationPlugins\UnoTeams\Line Types\400mm_45Degree.lin";
                string layerName = "UnoTEAM_2AC CABEL TRENCH";
                Messagebox.show("Started");
                try
                {
                    doc.LockDocument();
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {

                        LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                        ObjectId layerId;

                        if (!lt.Has(layerName))
                        {
                            lt.UpgradeOpen();
                            LayerTableRecord newLayer = new LayerTableRecord
                            {
                                Name = layerName,
                                LinetypeObjectId = lt.Has(linetypeName) ? lt[linetypeName] : db.ContinuousLinetype
                            };
                            layerId = lt.Add(newLayer);
                            tr.AddNewlyCreatedDBObject(newLayer, true);
                        }
                        else
                        {
                            layerId = lt[layerName];
                        }

                        LinetypeTable ltTable = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);

                        // Load linetype if it doesn't already exist
                        if (!ltTable.Has(linetypeName))
                        {
                            ltTable.UpgradeOpen(); // Required before modifying the table
                            db.LoadLineTypeFile(linetypeName, linetypeFilePath);
                            ed.WriteMessage($"\nLinetype '{linetypeName}' loaded successfully.");
                        }
                        else
                        {
                            ed.WriteMessage($"\nLinetype '{linetypeName}' already exists.");
                        }

                        // Set the linetype as current
                        db.Celtype = ltTable[linetypeName];
                        ed.WriteMessage($"\nLinetype '{linetypeName}' is now set as current.");

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
                        if (pprNext.Status != PromptStatus.OK) break;

                        points.Add(pprNext.Value);
                        ppo.BasePoint = pprNext.Value;

                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                            Polyline polyline;

                            if (polylineId.IsNull)
                            {
                                // First time: create and store the polyline
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
                        db.Celtype = db.ByLayerLinetype;
                        if (ltTable.Has(linetypeName))
                        {
                            ObjectId ltId = ltTable[linetypeName];
                            LinetypeTableRecord ltRecord = (LinetypeTableRecord)tr.GetObject(ltId, OpenMode.ForWrite);


                            ltRecord.Erase();
                            ed.WriteMessage($"\nLinetype '{linetypeName}' deleted from table.");

                        }
                        else
                        {
                            ed.WriteMessage($"\nLinetype '{linetypeName}' does not exist in the table.");
                        }

                        tr.Commit();
                    }

                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\nError loading or setting linetype: {ex.Message}");
                }
                Trenches.radio1 = false;
            }
            else if (Trenches.radio2 == true)
            {

                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                Editor ed = doc.Editor;

                string linetypeName = "600mm_45Degree";
                string linetypeFilePath = @"C:\ProgramData\Autodesk\ApplicationPlugins\UnoTeams\Line Types\600mm_45Degree.lin";
                string layerName = "UnoTEAM_3AC CABEL TRENCH";

                try
                {
                    doc.LockDocument();
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {

                        LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                        ObjectId layerId;

                        if (!lt.Has(layerName))
                        {
                            lt.UpgradeOpen();
                            LayerTableRecord newLayer = new LayerTableRecord
                            {
                                Name = layerName,
                                LinetypeObjectId = lt.Has(linetypeName) ? lt[linetypeName] : db.ContinuousLinetype
                            };
                            layerId = lt.Add(newLayer);
                            tr.AddNewlyCreatedDBObject(newLayer, true);
                        }
                        else
                        {
                            layerId = lt[layerName];
                        }

                        LinetypeTable ltTable = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);

                        // Load linetype if it doesn't already exist
                        if (!ltTable.Has(linetypeName))
                        {
                            ltTable.UpgradeOpen(); // Required before modifying the table
                            db.LoadLineTypeFile(linetypeName, linetypeFilePath);
                            ed.WriteMessage($"\nLinetype '{linetypeName}' loaded successfully.");
                        }
                        else
                        {
                            ed.WriteMessage($"\nLinetype '{linetypeName}' already exists.");
                        }

                        // Set the linetype as current
                        db.Celtype = ltTable[linetypeName];
                        ed.WriteMessage($"\nLinetype '{linetypeName}' is now set as current.");

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
                        if (pprNext.Status != PromptStatus.OK) break;

                        points.Add(pprNext.Value);
                        ppo.BasePoint = pprNext.Value;

                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                            Polyline polyline;

                            if (polylineId.IsNull)
                            {
                                // First time: create and store the polyline
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
                        db.Celtype = db.ByLayerLinetype;
                        if (ltTable.Has(linetypeName))
                        {
                            ObjectId ltId = ltTable[linetypeName];
                            LinetypeTableRecord ltRecord = (LinetypeTableRecord)tr.GetObject(ltId, OpenMode.ForWrite);


                            ltRecord.Erase();
                            ed.WriteMessage($"\nLinetype '{linetypeName}' deleted from table.");

                        }
                        else
                        {
                            ed.WriteMessage($"\nLinetype '{linetypeName}' does not exist in the table.");
                        }

                        tr.Commit();
                    }

                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\nError loading or setting linetype: {ex.Message}");
                }
                Trenches.radio2 = false;
            }
            else if (Trenches.radio3 == true)
            {

                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                Editor ed = doc.Editor;

                string linetypeName = "800mm_Honeycombe";
                string linetypeFilePath = @"C:\ProgramData\Autodesk\ApplicationPlugins\UnoTeams\Line Types\800mm_Honeycombe.lin";
                string layerName = "UnoTEAM_4AC CABEL TRENCH";

                try
                {
                    doc.LockDocument();
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {

                        LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                        ObjectId layerId;

                        if (!lt.Has(layerName))
                        {
                            lt.UpgradeOpen();
                            LayerTableRecord newLayer = new LayerTableRecord
                            {
                                Name = layerName,
                                LinetypeObjectId = lt.Has(linetypeName) ? lt[linetypeName] : db.ContinuousLinetype
                            };
                            layerId = lt.Add(newLayer);
                            tr.AddNewlyCreatedDBObject(newLayer, true);
                        }
                        else
                        {
                            layerId = lt[layerName];
                        }

                        LinetypeTable ltTable = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);

                        // Load linetype if it doesn't already exist
                        if (!ltTable.Has(linetypeName))
                        {
                            ltTable.UpgradeOpen(); // Required before modifying the table
                            db.LoadLineTypeFile(linetypeName, linetypeFilePath);
                            ed.WriteMessage($"\nLinetype '{linetypeName}' loaded successfully.");
                        }
                        else
                        {
                            ed.WriteMessage($"\nLinetype '{linetypeName}' already exists.");
                        }

                        // Set the linetype as current
                        db.Celtype = ltTable[linetypeName];
                        ed.WriteMessage($"\nLinetype '{linetypeName}' is now set as current.");

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
                        if (pprNext.Status != PromptStatus.OK) break;

                        points.Add(pprNext.Value);
                        ppo.BasePoint = pprNext.Value;

                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                            Polyline polyline;

                            if (polylineId.IsNull)
                            {
                                // First time: create and store the polyline
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
                        db.Celtype = db.ByLayerLinetype;
                        if (ltTable.Has(linetypeName))
                        {
                            ObjectId ltId = ltTable[linetypeName];
                            LinetypeTableRecord ltRecord = (LinetypeTableRecord)tr.GetObject(ltId, OpenMode.ForWrite);


                            ltRecord.Erase();
                            ed.WriteMessage($"\nLinetype '{linetypeName}' deleted from table.");

                        }
                        else
                        {
                            ed.WriteMessage($"\nLinetype '{linetypeName}' does not exist in the table.");
                        }

                        tr.Commit();
                    }

                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\nError loading or setting linetype: {ex.Message}");
                }
                Trenches.radio3 = false;
            }
            else if (Trenches.radio4 == true)
            {

                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                Editor ed = doc.Editor;

                string linetypeName = "450mm_45_Degrees";
                string linetypeFilePath = @"C:\ProgramData\Autodesk\ApplicationPlugins\UnoTeams\Line Types\450mm_45_Degrees.lin";
                string layerName = "UnoTEAM_DC TRENCH";

                try
                {
                    doc.LockDocument();
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {

                        LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                        ObjectId layerId;

                        if (!lt.Has(layerName))
                        {
                            lt.UpgradeOpen();
                            LayerTableRecord newLayer = new LayerTableRecord
                            {
                                Name = layerName,
                                LinetypeObjectId = lt.Has(linetypeName) ? lt[linetypeName] : db.ContinuousLinetype
                            };
                            layerId = lt.Add(newLayer);
                            tr.AddNewlyCreatedDBObject(newLayer, true);
                        }
                        else
                        {
                            layerId = lt[layerName];
                        }

                        LinetypeTable ltTable = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);

                        // Load linetype if it doesn't already exist
                        if (!ltTable.Has(linetypeName))
                        {
                            ltTable.UpgradeOpen(); // Required before modifying the table
                            db.LoadLineTypeFile(linetypeName, linetypeFilePath);
                            ed.WriteMessage($"\nLinetype '{linetypeName}' loaded successfully.");
                        }
                        else
                        {
                            ed.WriteMessage($"\nLinetype '{linetypeName}' already exists.");
                        }

                        // Set the linetype as current
                        db.Celtype = ltTable[linetypeName];
                        ed.WriteMessage($"\nLinetype '{linetypeName}' is now set as current.");

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
                        if (pprNext.Status != PromptStatus.OK) break;

                        points.Add(pprNext.Value);
                        ppo.BasePoint = pprNext.Value;

                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                            Polyline polyline;

                            if (polylineId.IsNull)
                            {
                                // First time: create and store the polyline
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
                        db.Celtype = db.ByLayerLinetype;
                        if (ltTable.Has(linetypeName))
                        {
                            ObjectId ltId = ltTable[linetypeName];
                            LinetypeTableRecord ltRecord = (LinetypeTableRecord)tr.GetObject(ltId, OpenMode.ForWrite);


                            ltRecord.Erase();
                            ed.WriteMessage($"\nLinetype '{linetypeName}' deleted from table.");

                        }
                        else
                        {
                            ed.WriteMessage($"\nLinetype '{linetypeName}' does not exist in the table.");
                        }

                        tr.Commit();
                    }

                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\nError loading or setting linetype: {ex.Message}");
                }
                Trenches.radio4 = false;
            }
            else if (Trenches.radio5 == true)
            {

                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                Editor ed = doc.Editor;

                string linetypeName = "127mm_HoneyCombe";
                string linetypeFilePath = @"C:\ProgramData\Autodesk\ApplicationPlugins\UnoTeams\Line Types\127mm_HoneyCombe.lin";
                string layerName = "UnoTEAM_5AC CABEL TRENCH";

                try
                {
                    doc.LockDocument();
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {

                        LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                        ObjectId layerId;

                        if (!lt.Has(layerName))
                        {
                            lt.UpgradeOpen();
                            LayerTableRecord newLayer = new LayerTableRecord
                            {
                                Name = layerName,
                                LinetypeObjectId = lt.Has(linetypeName) ? lt[linetypeName] : db.ContinuousLinetype
                            };
                            layerId = lt.Add(newLayer);
                            tr.AddNewlyCreatedDBObject(newLayer, true);
                        }
                        else
                        {
                            layerId = lt[layerName];
                        }

                        LinetypeTable ltTable = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);

                        // Load linetype if it doesn't already exist
                        if (!ltTable.Has(linetypeName))
                        {
                            ltTable.UpgradeOpen(); // Required before modifying the table
                            db.LoadLineTypeFile(linetypeName, linetypeFilePath);
                            ed.WriteMessage($"\nLinetype '{linetypeName}' loaded successfully.");
                        }
                        else
                        {
                            ed.WriteMessage($"\nLinetype '{linetypeName}' already exists.");
                        }

                        // Set the linetype as current
                        db.Celtype = ltTable[linetypeName];
                        ed.WriteMessage($"\nLinetype '{linetypeName}' is now set as current.");

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
                        if (pprNext.Status != PromptStatus.OK) break;

                        points.Add(pprNext.Value);
                        ppo.BasePoint = pprNext.Value;

                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                            Polyline polyline;

                            if (polylineId.IsNull)
                            {
                                // First time: create and store the polyline
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
                        db.Celtype = db.ByLayerLinetype;
                        if (ltTable.Has(linetypeName))
                        {
                            ObjectId ltId = ltTable[linetypeName];
                            LinetypeTableRecord ltRecord = (LinetypeTableRecord)tr.GetObject(ltId, OpenMode.ForWrite);


                            ltRecord.Erase();
                            ed.WriteMessage($"\nLinetype '{linetypeName}' deleted from table.");

                        }
                        else
                        {
                            ed.WriteMessage($"\nLinetype '{linetypeName}' does not exist in the table.");
                        }

                        tr.Commit();
                    }

                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\nError loading or setting linetype: {ex.Message}");
                }
                Trenches.radio5 = false;
            }
        }
        
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
