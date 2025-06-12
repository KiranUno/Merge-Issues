using System;
using System.Linq;
using System.Windows.Input;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.Colors;

namespace Uno_Solar_Design_Assist_Pro
{
    internal class Cabling_Creation_DC : ICommand
    {
        public void Execute(object parameter)
        {
            Cabling cabling = new Cabling();
            cabling.ShowDialog();

            if(cabling.DialogResult != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            Extents3d Table_Extents;
            double Table_Min_X = 0;
            double Table_Max_X = 0;
            double Table_Min_Y = 0;
            double Table_Max_Y = 0;

            Point3d Inverter_Point = Point3d.Origin;
            Point3d Trench_Point = Point3d.Origin;
            Point3dCollection Symbol_points;
            double Cable_Vertical_Offset = 0.25;
            int colorIndex = 0;

            using (DocumentLock doc_lock = doc.LockDocument())
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {                    
#region Get_Set_values
                    string layerName = "UnoTEAM_DC CABELS";
                    short lineWeight = (short)LineWeight.LineWeight000;
                    Color layerColor = Color.FromRgb(0, 0, 255);        // Blue

                    LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    ObjectId layerId;

                    if (layerTable.Has(layerName))
                    {
                        db.Clayer = layerTable[layerName];
                    }
                    else
                    {
                        layerTable.UpgradeOpen();

                        LayerTableRecord newLayer = new LayerTableRecord
                        {
                            Name = layerName,
                            Color = layerColor,
                            LineWeight = (LineWeight)lineWeight,
                            LinetypeObjectId = db.ContinuousLinetype
                        };

                        layerId = layerTable.Add(newLayer);
                        tr.AddNewlyCreatedDBObject(newLayer, true);
                    }
                    db.Clayer = layerTable[layerName];

                    List<BlockReference> selectedBlocks = SelectBlocks(ed, tr);

                    if(selectedBlocks.Count == 0)
                    {
                        ed.WriteMessage("\n No Solar Module Blocks are Selected");
                        return;
                    }

                    // Prompt user to select a single block reference (For Inverter)
                    PromptEntityOptions peo = new PromptEntityOptions("\nSelect an Inverter block: ");
                    peo.SetRejectMessage("\nOnly blocks are allowed.");
                    peo.AddAllowedClass(typeof(BlockReference), exactMatch: false);

                    PromptEntityResult per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage("\nNo Inverter block selected.");
                        return;
                    }
#endregion
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    BlockReference Inverter_Block = tr.GetObject(per.ObjectId, OpenMode.ForRead) as BlockReference;

                    if (Inverter_Block == null)
                    {
                        ed.WriteMessage("\n Incorrect INverter Block Choosen");
                        return;
                    }
                    else
                    {
                        Inverter_Point = Inverter_Block.Position;
                        ed.WriteMessage($"\nBlock origin: X={Inverter_Point.X}, Y={Inverter_Point.Y}, Z={Inverter_Point.Z}");

                        if (Inverter_Block.Name == "blue")
                            colorIndex = 5;
                        else if (Inverter_Block.Name == "red")
                            colorIndex = 1;
                        else if (Inverter_Block.Name == "green")
                            colorIndex = 3;
                        else if (Inverter_Block.Name == "magenta")
                            colorIndex = 6;
                        else
                            colorIndex = 2;
                    }

                    if (Global_Module.Trench_Line_Type == "Manual_Selection")
                    {
                        peo = new PromptEntityOptions("\nSelect a Trench Line: ");
                        peo.SetRejectMessage("\nOnly Lines are allowed.");
                        peo.AddAllowedClass(typeof(Line), exactMatch: false);

                        per = ed.GetEntity(peo);
                        if (per.Status != PromptStatus.OK)
                        {
                            ed.WriteMessage("\nNo Inverter block selected.");
                            return;
                        }
                        Line Trench_line = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Line;
                        Trench_Point = Trench_line.StartPoint;
                    }

                    foreach (BlockReference block in selectedBlocks)
                    {
                        if (block == null)
                        {
                            ed.WriteMessage("\n Error getting Selected Objects from Selection");
                            return;
                        }
                        #region Get_Table_Detals
                        //BlockReference Tabel_Block = tr.GetObject(obj.ObjectId, OpenMode.ForRead) as BlockReference;
                        BlockReference Tabel_Block = block;

                        if (Tabel_Block == null)
                        {
                            ed.WriteMessage($"\nError getting Table extents");
                            return;
                        }
                        try
                        {
                            Table_Extents = Tabel_Block.GeometricExtents;
                            Table_Min_X = Table_Extents.MinPoint.X;
                            Table_Min_Y = Table_Extents.MinPoint.Y;
                            Table_Max_X = Table_Extents.MaxPoint.X;
                            Table_Max_Y = Table_Extents.MaxPoint.Y;
                        }
                        catch (Autodesk.AutoCAD.Runtime.Exception ex)
                        {
                            ed.WriteMessage($"\nError getting extents: {ex.Message}");
                        }
#endregion
                        Symbol_points = Get_Symbol_Points(block.ObjectId);                        
                        Point3dCollection Poly_Points_Coll = new Point3dCollection();

                        //List<double> Module_Dimensions = GetModuleDimensions(obj.ObjectId);
                        //double Module_Length = Module_Dimensions[0];
                        //double Module_Height = Module_Dimensions[1];
                        ////double Horizonta_Offest = 0.35;
                        //double Vertical_Offest = 0.3386;

                        if (Symbol_points.Count != 2 && Symbol_points.Count != 4)
                        {
                            ed.WriteMessage("\n Error Getting Symbol points, Only 2 or 4 symbol points are acceptible");
                            return;
                        }

                        double Low_Y = Symbol_points.Cast<Point3d>().Min(p => p.Y);
                        double High_Y = Symbol_points.Cast<Point3d>().Max(p => p.Y);

                        double Low_X = Symbol_points.Cast<Point3d>().Min(p => p.X);
                        double High_X = Symbol_points.Cast<Point3d>().Max(p => p.X);

                        foreach (Point3d Symbol_point in Symbol_points)
                        {
                            if (Symbol_point.Y == High_Y)      // Top Point
                            {
                                Point3d p1 = Point3d.Origin;
                                Point3d p2 = Point3d.Origin;

                                if (Symbol_point.Y > Inverter_Point.Y)          // Inverter above Symbol
                                {
                                    p1 = new Point3d(Symbol_point.X, Table_Min_Y - Cable_Vertical_Offset, 0);
                                }
                                else if (Symbol_point.Y <= Inverter_Point.Y)    // Inverter below Symbol
                                {
                                    p1 = new Point3d(Symbol_point.X, Table_Max_Y + Cable_Vertical_Offset, 0);
                                }

                                if (Global_Module.Trench_Line_Type == "Nearest")
                                {
                                    Trench_Point = Get_TrenchLine_Intersection_Point(p1, Inverter_Point); ;
                                }
                                p2 = new Point3d(Trench_Point.X, p1.Y, 0);

                                Point3d Inverter_Before_Point = new Point3d(p2.X, Inverter_Point.Y, 0);

                                Poly_Points_Coll.Add(Symbol_point);
                                Poly_Points_Coll.Add(p1);
                                Poly_Points_Coll.Add(p2);
                                Poly_Points_Coll.Add(Inverter_Before_Point);
                                Poly_Points_Coll.Add(Inverter_Point);
                            }
                            else if (Symbol_point.Y == Low_Y)       // Bottom Point
                            {
                                Point3d p1 = Point3d.Origin;
                                Point3d p2 = Point3d.Origin;

                                if (Symbol_point.Y >= Inverter_Point.Y)         // Inverter above Symbol
                                {
                                    p1 = new Point3d(Symbol_point.X, Table_Min_Y - Cable_Vertical_Offset, 0);
                                }
                                else if (Symbol_point.Y < Inverter_Point.Y)     // Inverter below Symbol
                                {
                                    p1 = new Point3d(Symbol_point.X, Table_Max_Y + Cable_Vertical_Offset, 0);
                                }

                                if (Global_Module.Trench_Line_Type == "Nearest")
                                {
                                    Trench_Point = Get_TrenchLine_Intersection_Point(p1, Inverter_Point); ;
                                }
                                p2 = new Point3d(Trench_Point.X, p1.Y, 0);

                                Point3d Inverter_Before_Point = new Point3d(p2.X, Inverter_Point.Y, 0);

                                Poly_Points_Coll.Add(Symbol_point);
                                Poly_Points_Coll.Add(p1);
                                Poly_Points_Coll.Add(p2);
                                Poly_Points_Coll.Add(Inverter_Before_Point);
                                Poly_Points_Coll.Add(Inverter_Point);
                            }

                            //// Set point style to cross (style 2 = X-shaped cross)
                            //db.Pdmode = 2;       // 2 = Cross
                            //db.Pdsize = 0.5;     // Size of the point symbol (adjust as needed)

                            //foreach (Point3d position in Poly_Points_Coll)
                            //{
                            //    // Create and add the DBPoint
                            //    DBPoint point = new DBPoint(position);
                            //    point.ColorIndex = colorIndex;
                            //    btr.AppendEntity(point);
                            //    tr.AddNewlyCreatedDBObject(point, true);
                            //}

                            Create_Polylines(Poly_Points_Coll, colorIndex);
                            Poly_Points_Coll.Clear();
                        }
                    }
                    tr.Commit();
                }
            }            
        }

        private List<BlockReference> SelectBlocks(Editor ed, Transaction tr)
        {
            List<BlockReference> blocks = new List<BlockReference>();

            // Filter only INSERT entities (block references)
            SelectionFilter filter = new SelectionFilter(new TypedValue[]
            {
                new TypedValue((int)DxfCode.Start, "INSERT")
            });

            PromptSelectionOptions opts = new PromptSelectionOptions
            {
                MessageForAdding = "\nSelect single or multiple Solar Module Blocks:  "
            };
            PromptSelectionResult result = ed.GetSelection(opts, filter);

            if (result.Status == PromptStatus.OK)
            {
                foreach (SelectedObject selObj in result.Value)
                {
                    if (selObj != null)
                    {
                        Entity ent = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                        if (ent is BlockReference block)
                        {
                            BlockReference blkRef = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as BlockReference;
                            if (blkRef != null)
                            {
                                BlockTableRecord btr = tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                                string blockName = btr.Name;

                                if (blockName == "SolarTable_26x2" || blockName == "SolarTable_13x2" || blockName == "SolarTable_0")
                                {
                                    blocks.Add(blkRef);
                                }
                            }
                        }
                    }
                }
            }
            return blocks;
        }

        public void Create_Polylines(Point3dCollection Poly_Points_Coll, int color_index)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                Polyline3d cable = new Polyline3d(Poly3dType.SimplePoly, Poly_Points_Coll, false);
                cable.ColorIndex = color_index;

                btr.AppendEntity(cable);
                tr.AddNewlyCreatedDBObject(cable, true);
                tr.Commit();
            }
        }

        public Point3dCollection Get_Symbol_Points(ObjectId objid)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            string Plus_Symbol = "\uFF0B";
            string Minus_Symbol = "\u2212";

            Point3d Plus_Point1 = new Point3d(0, 0, 0);
            Point3d Plus_Point2 = new Point3d(0, 0, 0);
            Point3d Minus_Point1 = new Point3d(0, 0, 0);
            Point3d Minus_Point2 = new Point3d(0, 0, 0);
            Point3dCollection Symbol_points = new Point3dCollection();

            bool found1 = false;
            bool found2 = false;
            bool found3 = false;
            bool found4 = false;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference block = tr.GetObject(objid, OpenMode.ForRead) as BlockReference;

                if (block != null) //&& block.Name == ""
                {
                    ed.WriteMessage($"\nBlock selected: {block.Name} at {block.Position}");

                    Extents3d ext = block.GeometricExtents;
                    double minx = Math.Round(ext.MinPoint.X, 5);
                    double miny = Math.Round(ext.MinPoint.Y, 5);
                    double maxx = ext.MaxPoint.X;
                    double maxy = ext.MaxPoint.Y;

                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    foreach (ObjectId entId in btr)
                    {
                        Entity ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;
                        if (ent is DBText text)
                        {
                            if (text.TextString == Plus_Symbol)
                            {
                                Point3d Point_From_CAD = text.AlignmentPoint;
                                double x1 = Point_From_CAD.X;
                                double y1 = Point_From_CAD.Y;

                                if ((x1 > minx && x1 < maxx) && (y1 > miny && y1 < maxy))
                                {
                                    if (Plus_Point1 == new Point3d(0, 0, 0))
                                    {
                                        Plus_Point1 = text.AlignmentPoint;
                                        found1 = true;
                                        Symbol_points.Add(Plus_Point1);
                                    }
                                    else
                                    {
                                        Plus_Point2 = text.AlignmentPoint;
                                        found2 = true;
                                        Symbol_points.Add(Plus_Point2);
                                    }
                                }
                            }
                            else if (text.TextString == Minus_Symbol)
                            {
                                Point3d Point_From_CAD = text.AlignmentPoint;
                                double x1 = Point_From_CAD.X;
                                double y1 = Point_From_CAD.Y;

                                if ((x1 > minx && x1 < maxx) && (y1 > miny && y1 < maxy))
                                {
                                    if (Minus_Point1 == new Point3d(0, 0, 0))
                                    {
                                        Minus_Point1 = text.AlignmentPoint;
                                        found3 = true;
                                        Symbol_points.Add(Minus_Point1);
                                    }
                                    else
                                    {
                                        Minus_Point2 = text.AlignmentPoint;
                                        found4 = true;
                                        Symbol_points.Add(Minus_Point2);
                                    }
                                }
                            }
                        }
                        if (found1 && found2 && found3 && found4)
                        {
                            ed.WriteMessage("\n Plus Symbols at Points {0}, {1} and Minus Symbols at Points {2}, {3}", Plus_Point1, Plus_Point2, Minus_Point1, Minus_Point2);
                            break;
                        }
                    }

                    if (found1 == false || found3 == false)
                    {
                        ed.WriteMessage("\n Could not fin the Symbol Points");
                        return null;
                    }
                }
            }
            return Symbol_points;
        }
        
        public Point3d Get_TrenchLine_Intersection_Point(Point3d Cable_Point, Point3d Inverter_Point)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            string layerName = "UnoTEAM_TRENCHES";
            List<Line> Trench_Lines = new List<Line>();
            Point3dCollection Trench_points = new Point3dCollection();

            bool found = false;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                foreach (ObjectId objId in btr)
                {
                    Entity ent = tr.GetObject(objId, OpenMode.ForRead) as Entity;
                    if (ent is Line line && line.Layer == layerName)
                    {
                        Trench_Lines.Add(line);

                        Point3d start = line.StartPoint;
                        Point3d end = line.EndPoint;
                        Trench_points.Add(start);

                        if (Cable_Point.Y >= Math.Min(start.Y, end.Y) && Cable_Point.Y <= Math.Max(start.Y, end.Y))
                        {
                            if (start.X >= Math.Min(Cable_Point.X, Inverter_Point.X) && start.X <= Math.Max(Cable_Point.X, Inverter_Point.X))
                            {
                                found = true;
                                return new Point3d(line.StartPoint.X, Cable_Point.Y, 0);
                            }
                        }
                    }
                }
                tr.Commit();
            }

            if (found == false)
            {
                Point3d nearestPoint = Trench_points.Cast<Point3d>().OrderBy(p => p.DistanceTo(Inverter_Point)).FirstOrDefault();
                return nearestPoint;
            }

            return new Point3d();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        //public List<double> GetModuleDimensions(ObjectId objid)
        //{
        //    Document doc = Application.DocumentManager.MdiActiveDocument;
        //    Database db = doc.Database;
        //    Editor ed = doc.Editor;
        //    List<double> dimensions = new List<double>();

        //    using (Transaction tr = db.TransactionManager.StartTransaction())
        //    {
        //        BlockReference block = tr.GetObject(objid, OpenMode.ForRead) as BlockReference;

        //        DBObjectCollection exploded = new DBObjectCollection();
        //        block.Explode(exploded);

        //        foreach (DBObject obj in exploded)
        //        {
        //            if (obj is Solid3d solid)
        //            {
        //                try
        //                {
        //                    Extents3d extents = solid.GeometricExtents;

        //                    double length = extents.MaxPoint.X - extents.MinPoint.X;
        //                    double height = extents.MaxPoint.Y - extents.MinPoint.Y;
        //                    double thickness = extents.MaxPoint.Z - extents.MinPoint.Z;

        //                    dimensions.Add(length);
        //                    dimensions.Add(height);
        //                    //dimensions.Add(thickness);

        //                    ed.WriteMessage($"\nSolid3D Dimensions - Length: {length}, Height: {height}, Thockness: {thickness}");
        //                }
        //                catch (Autodesk.AutoCAD.Runtime.Exception ex)
        //                {
        //                    ed.WriteMessage($"\nError getting extents: {ex.Message}");
        //                }
        //            }
        //        }
        //    }
        //    return dimensions;
        //}
    }
}
