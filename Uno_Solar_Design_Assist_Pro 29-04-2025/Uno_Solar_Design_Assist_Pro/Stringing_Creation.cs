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
    internal class Stringing_Creation : ICommand
    {
        private double BLOCK_LENGTH = 1.1300;
        private double BLOCK_HEIGHT = 2.244;

        private double HORIZONTAL_OFFSET = 0.35;
        private double VERTICAL_OFFSET = 0.35;

        private double VERTICAL_OFFSET_BETWEEN_SPLINES = 0.3;
        private double GroundLevel = 0;
        public TextVerticalMode TextVerticalMid { get; private set; }
        
        public void Execute(object parameter)
        {
            Stringing stringing = new Stringing();
            stringing.ShowDialog();

            if(stringing.DialogResult != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }
            //if (Global_Module.Stringing_Submitted != true)
            //{
            //    return;
            //}
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            using (DocumentLock docklock = doc.LockDocument())
            {
                using (Transaction tr = doc.TransactionManager.StartTransaction())
                {
                    string layerName = "UnoTEAM_STRINGING";
                    short lineWeight = (short)LineWeight.LineWeight000; // example
                    Color layerColor = Color.FromRgb(0, 165, 0); // Red as example

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
                    tr.Commit();
                }
            }
            
            if (Global_Module.Stringing_Category == "SINGLE ROW")
            {
                Map_SingleRow_Stringing(Global_Module.Stringing_Type);
            }
            else if (Global_Module.Stringing_Category == "U STRING")
            {
                Map_U_String_Stringing(Global_Module.Stringing_Type);
            }
            else if (Global_Module.Stringing_Category == "LEAP FROG")
            {
                Map_LeapFrog_Stringing(Global_Module.Stringing_Type);
            }
            else if (Global_Module.Stringing_Category == "CUSTOM")
            {
                Map_Custom_Stringing(Global_Module.Stringing_Type);
            }
        }

        public void Map_Custom_Stringing(string Stringing_Direction)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            using (DocumentLock doclock = doc.LockDocument())
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
#region Get_And_Set_Values
                    string layerName = "Stringing";

                    if (!layerTable.Has(layerName))
                    {
                        // Upgrade to write and create new layer
                        layerTable.UpgradeOpen();
                        LayerTableRecord newLayer = new LayerTableRecord
                        {
                            Name = layerName
                        };
                        layerTable.Add(newLayer);
                        tr.AddNewlyCreatedDBObject(newLayer, true);
                    }
                    // Set the layer as current
                    db.Clayer = layerTable[layerName];

                    List<BlockReference> selectedBlocks = SelectBlocks(ed, tr);
                    if (selectedBlocks.Count == 0)
                    {
                        ed.WriteMessage("\nNo blocks selected.");
                        return;
                    }

                    PromptDoubleOptions pdo = new PromptDoubleOptions("\n Please Enter Horizontal Offset Value");
                    pdo.AllowNegative = false;
                    pdo.AllowNone = false;
                    pdo.AllowZero = false;
                    PromptDoubleResult pdr = ed.GetDouble(pdo);

                    if (pdr.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage("\n Error Reading the Value");
                        return;
                    }
                    HORIZONTAL_OFFSET = pdr.Value;

                    pdo = new PromptDoubleOptions("\n Please Enter Vertical Offset Value");
                    pdr = ed.GetDouble(pdo);

                    if (pdr.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage("\n Error Reading the Value");
                        return;
                    }
                    VERTICAL_OFFSET = pdr.Value;
#endregion

                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    foreach (BlockReference block in selectedBlocks)
                    {
                        Get_Module_Dimensions(doc, block);

                        Extents3d ext = block.GeometricExtents;
                        double minx = ext.MinPoint.X;
                        double miny = ext.MinPoint.Y;
                        double maxx = ext.MaxPoint.X;
                        double maxy = ext.MaxPoint.Y;

                        double lower_midpoint_X = 0.0;
                        double upper_midpoint_X = 0.0;
                        double lower_midpoint_Y = miny + (BLOCK_HEIGHT / 2);
                        double upper_midpoint_Y = lower_midpoint_Y + VERTICAL_OFFSET + BLOCK_HEIGHT;

                        Point3d Lower_First_SubBlock_MidPoint = new Point3d();
                        Point3d Lower_Second_SubBlock_MidPoint = new Point3d();
                        Point3d Lower_Last_SubBlock_MidPoint = new Point3d();

                        Point3d Upper_First_SubBlock_MidPoint = new Point3d();
                        Point3d Upper_Second_SubBlock_MidPoint = new Point3d();
                        Point3d Upper_Last_SubBlock_MidPoint = new Point3d();

                        if (Stringing_Direction == "Left_To_Right")
                        {
                            lower_midpoint_X = minx + (BLOCK_LENGTH / 2);
                            upper_midpoint_X = maxx - (BLOCK_LENGTH / 2);

                            Lower_First_SubBlock_MidPoint = new Point3d(lower_midpoint_X, lower_midpoint_Y, GroundLevel);
                            Lower_Second_SubBlock_MidPoint = new Point3d(lower_midpoint_X + (BLOCK_LENGTH + HORIZONTAL_OFFSET), lower_midpoint_Y, GroundLevel);
                            Lower_Last_SubBlock_MidPoint = new Point3d(maxx - (BLOCK_LENGTH / 2), lower_midpoint_Y, GroundLevel);

                            Upper_First_SubBlock_MidPoint = new Point3d(upper_midpoint_X, upper_midpoint_Y, GroundLevel);
                            Upper_Second_SubBlock_MidPoint = new Point3d(upper_midpoint_X - (BLOCK_LENGTH + HORIZONTAL_OFFSET), upper_midpoint_Y, GroundLevel);
                            Upper_Last_SubBlock_MidPoint = new Point3d(minx + (BLOCK_LENGTH / 2), upper_midpoint_Y, GroundLevel);
                        }
                        else if (Stringing_Direction == "Right_To_Left")
                        {
                            lower_midpoint_X = maxx - (BLOCK_LENGTH / 2);
                            upper_midpoint_X = minx + (BLOCK_LENGTH / 2);

                            Lower_First_SubBlock_MidPoint = new Point3d(lower_midpoint_X, lower_midpoint_Y, GroundLevel);
                            Lower_Second_SubBlock_MidPoint = new Point3d(lower_midpoint_X - (BLOCK_LENGTH + HORIZONTAL_OFFSET), lower_midpoint_Y, GroundLevel);
                            Lower_Last_SubBlock_MidPoint = new Point3d(minx + (BLOCK_LENGTH / 2), lower_midpoint_Y, GroundLevel);

                            Upper_First_SubBlock_MidPoint = new Point3d(upper_midpoint_X, upper_midpoint_Y, GroundLevel);
                            Upper_Second_SubBlock_MidPoint = new Point3d(upper_midpoint_X + (BLOCK_LENGTH + HORIZONTAL_OFFSET), upper_midpoint_Y, GroundLevel);
                            Upper_Last_SubBlock_MidPoint = new Point3d(maxx - (BLOCK_LENGTH / 2), upper_midpoint_Y, GroundLevel);
                        }

                        // Insert polarity texts (Reversed for Right-to-Left)
                        InsertPolarityText(db, tr, btr, Lower_First_SubBlock_MidPoint, "\u2212", 1); // + at the start
                        InsertPolarityText(db, tr, btr, Lower_Second_SubBlock_MidPoint, "\uFF0B", 3); // - at the end

                        InsertPolarityText(db, tr, btr, Upper_First_SubBlock_MidPoint, "\uFF0B", 1); // + at the start                        
                        InsertPolarityText(db, tr, btr, Upper_Second_SubBlock_MidPoint, "\u2212", 3); // + at the end

                        Dictionary<Point3d, ObjectId> Spline1_coll;
                        Dictionary<Point3d, ObjectId> Spline2_coll;

                        if (Stringing_Direction == "Left_To_Right")
                        {
                            // Lower Splines Creation
                            Spline1_coll = Create_Splines_For_Custom_Stringing(db, tr, btr, Lower_First_SubBlock_MidPoint, Lower_Last_SubBlock_MidPoint, "Left_To_Right");
                            Spline2_coll = Create_Splines_For_Custom_Stringing(db, tr, btr, Lower_Second_SubBlock_MidPoint, Lower_Last_SubBlock_MidPoint, "Left_To_Right");

                            Point3d spline1_endPoint = Spline1_coll.ElementAt(0).Key;
                            Point3d spline2_endPoint = Spline2_coll.ElementAt(0).Key;

                            Point3dCollection Joint_Spline_P3D_Coll = new Point3dCollection();
                            Point3d Joint_Spline_MidPoint;

                            if (spline1_endPoint.X < spline2_endPoint.X)
                            {
                                Joint_Spline_MidPoint = new Point3d(spline1_endPoint.X + ((BLOCK_LENGTH / 2) + HORIZONTAL_OFFSET), spline1_endPoint.Y + VERTICAL_OFFSET_BETWEEN_SPLINES, GroundLevel);
                            }
                            else //if(spline1_endPoint.X > spline2_endPoint.X)
                            {
                                Joint_Spline_MidPoint = new Point3d(spline1_endPoint.X - ((BLOCK_LENGTH / 2) + HORIZONTAL_OFFSET), spline1_endPoint.Y + VERTICAL_OFFSET_BETWEEN_SPLINES, GroundLevel);
                            }

                            Joint_Spline_P3D_Coll.Add(spline1_endPoint);
                            Joint_Spline_P3D_Coll.Add(Joint_Spline_MidPoint);
                            Joint_Spline_P3D_Coll.Add(spline2_endPoint);

                            Spline Joint_Spline = new Spline(Joint_Spline_P3D_Coll, 3, 0.0);
                            ObjectId Joint_Spline_objid = btr.AppendEntity(Joint_Spline);
                            tr.AddNewlyCreatedDBObject(Joint_Spline, true);
                            Joint_Spline_P3D_Coll.Clear();

                            ObjectIdCollection Spline_Ids = new ObjectIdCollection();
                            Spline_Ids.Add(Joint_Spline_objid);
                            Spline_Ids.Add(Spline2_coll.ElementAt(0).Value);

                            Spline spline1 = (Spline)tr.GetObject(Spline1_coll.ElementAt(0).Value, OpenMode.ForWrite);

                            for (int i = 0; i < Spline_Ids.Count; i++)
                            {
                                Spline spline2 = (Spline)tr.GetObject(Spline_Ids[i], OpenMode.ForWrite);

                                spline1.JoinEntity(spline2);
                                spline2.UpgradeOpen();
                                spline2.Erase();
                            }

                            // Upper Splines Creation
                            Spline1_coll = Create_Splines_For_Custom_Stringing(db, tr, btr, Upper_First_SubBlock_MidPoint, Upper_Last_SubBlock_MidPoint, "Right_To_Left");
                            Spline2_coll = Create_Splines_For_Custom_Stringing(db, tr, btr, Upper_Second_SubBlock_MidPoint, Upper_Last_SubBlock_MidPoint, "Right_To_Left");

                            Spline_Ids.Clear();
                            Joint_Spline_P3D_Coll.Clear();

                            spline1_endPoint = Spline1_coll.ElementAt(0).Key;
                            spline2_endPoint = Spline2_coll.ElementAt(0).Key;

                            if (spline1_endPoint.X < spline2_endPoint.X)
                            {
                                Joint_Spline_MidPoint = new Point3d(spline1_endPoint.X + ((BLOCK_LENGTH / 2) + HORIZONTAL_OFFSET), spline1_endPoint.Y + VERTICAL_OFFSET_BETWEEN_SPLINES, GroundLevel);
                            }
                            else //if(spline1_endPoint.X > spline2_endPoint.X)
                            {
                                Joint_Spline_MidPoint = new Point3d(spline1_endPoint.X - ((BLOCK_LENGTH / 2) + HORIZONTAL_OFFSET), spline1_endPoint.Y + VERTICAL_OFFSET_BETWEEN_SPLINES, GroundLevel);
                            }

                            Joint_Spline_P3D_Coll.Add(spline1_endPoint);
                            Joint_Spline_P3D_Coll.Add(Joint_Spline_MidPoint);
                            Joint_Spline_P3D_Coll.Add(spline2_endPoint);

                            Joint_Spline = new Spline(Joint_Spline_P3D_Coll, 3, 0.0);
                            Joint_Spline_objid = btr.AppendEntity(Joint_Spline);
                            tr.AddNewlyCreatedDBObject(Joint_Spline, true);
                            Joint_Spline_P3D_Coll.Clear();

                            Spline_Ids.Add(Joint_Spline_objid);
                            Spline_Ids.Add(Spline2_coll.ElementAt(0).Value);

                            spline1 = (Spline)tr.GetObject(Spline1_coll.ElementAt(0).Value, OpenMode.ForWrite);

                            for (int i = 0; i < Spline_Ids.Count; i++)
                            {
                                Spline spline2 = (Spline)tr.GetObject(Spline_Ids[i], OpenMode.ForWrite);

                                spline1.JoinEntity(spline2);
                                spline2.UpgradeOpen();
                                spline2.Erase();
                            }
                        }
                        else if (Stringing_Direction == "Right_To_Left")
                        {
                            // Lower Splines Creation
                            Spline1_coll = Create_Splines_For_Custom_Stringing(db, tr, btr, Lower_First_SubBlock_MidPoint, Lower_Last_SubBlock_MidPoint, "Right_To_Left");
                            Spline2_coll = Create_Splines_For_Custom_Stringing(db, tr, btr, Lower_Second_SubBlock_MidPoint, Lower_Last_SubBlock_MidPoint, "Right_To_Left");

                            Point3d spline1_endPoint = Spline1_coll.ElementAt(0).Key;
                            Point3d spline2_endPoint = Spline2_coll.ElementAt(0).Key;

                            Point3dCollection Joint_Spline_P3D_Coll = new Point3dCollection();

                            Point3d Joint_Spline_MidPoint;

                            if (spline1_endPoint.X < spline2_endPoint.X)
                            {
                                Joint_Spline_MidPoint = new Point3d(spline1_endPoint.X + ((BLOCK_LENGTH / 2) + HORIZONTAL_OFFSET), spline1_endPoint.Y + VERTICAL_OFFSET_BETWEEN_SPLINES, GroundLevel);
                            }
                            else //if(spline1_endPoint.X > spline2_endPoint.X)
                            {
                                Joint_Spline_MidPoint = new Point3d(spline1_endPoint.X - ((BLOCK_LENGTH / 2) + HORIZONTAL_OFFSET), spline1_endPoint.Y + VERTICAL_OFFSET_BETWEEN_SPLINES, GroundLevel);
                            }

                            Joint_Spline_P3D_Coll.Add(spline1_endPoint);
                            Joint_Spline_P3D_Coll.Add(Joint_Spline_MidPoint);
                            Joint_Spline_P3D_Coll.Add(spline2_endPoint);

                            Spline Joint_Spline = new Spline(Joint_Spline_P3D_Coll, 3, 0.0);
                            ObjectId Joint_Spline_objid = btr.AppendEntity(Joint_Spline);
                            tr.AddNewlyCreatedDBObject(Joint_Spline, true);
                            Joint_Spline_P3D_Coll.Clear();

                            ObjectIdCollection Spline_Ids = new ObjectIdCollection();
                            Spline_Ids.Add(Joint_Spline_objid);
                            Spline_Ids.Add(Spline2_coll.ElementAt(0).Value);

                            Spline spline1 = (Spline)tr.GetObject(Spline1_coll.ElementAt(0).Value, OpenMode.ForWrite);

                            for (int i = 0; i < Spline_Ids.Count; i++)
                            {
                                Spline spline2 = (Spline)tr.GetObject(Spline_Ids[i], OpenMode.ForWrite);

                                spline1.JoinEntity(spline2);
                                spline2.UpgradeOpen();
                                spline2.Erase();
                            }

                            // Upper Splines Creation
                            Spline1_coll = Create_Splines_For_Custom_Stringing(db, tr, btr, Upper_First_SubBlock_MidPoint, Upper_Last_SubBlock_MidPoint, "Left_To_Right");
                            Spline2_coll = Create_Splines_For_Custom_Stringing(db, tr, btr, Upper_Second_SubBlock_MidPoint, Upper_Last_SubBlock_MidPoint, "Left_To_Right");

                            Spline_Ids.Clear();
                            Joint_Spline_P3D_Coll.Clear();

                            spline1_endPoint = Spline1_coll.ElementAt(0).Key;
                            spline2_endPoint = Spline2_coll.ElementAt(0).Key;

                            if (spline1_endPoint.X < spline2_endPoint.X)
                            {
                                Joint_Spline_MidPoint = new Point3d(spline1_endPoint.X + ((BLOCK_LENGTH / 2) + HORIZONTAL_OFFSET), spline1_endPoint.Y + VERTICAL_OFFSET_BETWEEN_SPLINES, GroundLevel);
                            }
                            else //if(spline1_endPoint.X > spline2_endPoint.X)
                            {
                                Joint_Spline_MidPoint = new Point3d(spline1_endPoint.X - ((BLOCK_LENGTH / 2) + HORIZONTAL_OFFSET), spline1_endPoint.Y + VERTICAL_OFFSET_BETWEEN_SPLINES, GroundLevel);
                            }

                            Joint_Spline_P3D_Coll.Add(spline1_endPoint);
                            Joint_Spline_P3D_Coll.Add(Joint_Spline_MidPoint);
                            Joint_Spline_P3D_Coll.Add(spline2_endPoint);

                            Joint_Spline = new Spline(Joint_Spline_P3D_Coll, 3, 0.0);
                            Joint_Spline_objid = btr.AppendEntity(Joint_Spline);
                            tr.AddNewlyCreatedDBObject(Joint_Spline, true);
                            Joint_Spline_P3D_Coll.Clear();

                            Spline_Ids.Add(Joint_Spline_objid);
                            Spline_Ids.Add(Spline2_coll.ElementAt(0).Value);

                            spline1 = (Spline)tr.GetObject(Spline1_coll.ElementAt(0).Value, OpenMode.ForWrite);

                            for (int i = 0; i < Spline_Ids.Count; i++)
                            {
                                Spline spline2 = (Spline)tr.GetObject(Spline_Ids[i], OpenMode.ForWrite);
                                spline1.JoinEntity(spline2);
                                spline2.UpgradeOpen();
                                spline2.Erase();
                            }
                        }
                    }
                    tr.Commit();
                }
            }
            
        }

        private Dictionary<Point3d, ObjectId> Create_Splines_For_Custom_Stringing(Database db, Transaction tr, BlockTableRecord btr, Point3d start, Point3d end, string Stringing_Direction)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            Point3dCollection points_Coll = new Point3dCollection();
            Point3d new_3DPoint = new Point3d();
            double X_val = start.X;
            int cnt = 1;

            if (Stringing_Direction == "Left_To_Right")
            {
                while (Math.Round(X_val, 5) <= Math.Round(end.X, 5))
                {
                    if (cnt % 2 == 1)
                    {
                        new_3DPoint = new Point3d(X_val, start.Y, 0);
                    }
                    else if (cnt % 2 == 0)
                    {
                        new_3DPoint = new Point3d(X_val, start.Y + VERTICAL_OFFSET_BETWEEN_SPLINES, 0);
                    }

                    points_Coll.Add(new_3DPoint);
                    X_val += BLOCK_LENGTH + HORIZONTAL_OFFSET;
                    cnt++;
                }
            }
            else if (Stringing_Direction == "Right_To_Left")
            {
                while (Math.Round(X_val, 5) >= Math.Round(end.X, 5))
                {
                    if (cnt % 2 == 1)
                    {
                        new_3DPoint = new Point3d(X_val, start.Y, 0);
                    }
                    else if (cnt % 2 == 0)
                    {
                        new_3DPoint = new Point3d(X_val, start.Y + VERTICAL_OFFSET_BETWEEN_SPLINES, 0);
                    }

                    points_Coll.Add(new_3DPoint);
                    X_val -= (BLOCK_LENGTH + HORIZONTAL_OFFSET);
                    cnt++;
                }
            }

            List<ObjectId> Spline_Ids = new List<ObjectId>();

            // Create multiple splines with overlapping segments
            for (int i = 0; i <= points_Coll.Count - 3; i += 2) // Increment by 2 for overlapping sections
            {
                Point3dCollection Vertices = new Point3dCollection
                {
                    points_Coll[i],
                    points_Coll[i + 1],
                    points_Coll[i + 2]
                };

                // Create Degree 3 spline
                Spline spline = new Spline(Vertices, 3, 0.0);
                spline.ColorIndex = 3;
                spline.LineWeight = LineWeight.LineWeight000;
                ObjectId splineId = btr.AppendEntity(spline);
                tr.AddNewlyCreatedDBObject(spline, true);
                Spline_Ids.Add(splineId);

                new_3DPoint = Vertices[2];
            }

            Spline Joined_spline = (Spline)tr.GetObject(Spline_Ids[0], OpenMode.ForWrite);

            for (int i = 1; i < Spline_Ids.Count; i++)
            {
                Spline spline2 = (Spline)tr.GetObject(Spline_Ids[i], OpenMode.ForWrite);
                Joined_spline.JoinEntity(spline2);
                spline2.UpgradeOpen();
                spline2.Erase();
            }

            Dictionary<Point3d, ObjectId> coll = new Dictionary<Point3d, ObjectId>();
            coll.Add(new_3DPoint, Joined_spline.ObjectId);
            return coll;
        }

        public void Map_LeapFrog_Stringing(string Stringing_Direction)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            using (DocumentLock doclock = doc.LockDocument())
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
#region Get_And_Set_Values
                    string layerName = "Stringing";

                    if (!layerTable.Has(layerName))
                    {
                        // Upgrade to write and create new layer
                        layerTable.UpgradeOpen();
                        LayerTableRecord newLayer = new LayerTableRecord
                        {
                            Name = layerName
                        };
                        layerTable.Add(newLayer);
                        tr.AddNewlyCreatedDBObject(newLayer, true);
                    }
                    // Set the layer as current
                    db.Clayer = layerTable[layerName];

                    List<BlockReference> selectedBlocks = SelectBlocks(ed, tr);
                    if (selectedBlocks.Count == 0)
                    {
                        ed.WriteMessage("\nNo blocks selected.");
                        return;
                    }

                    PromptDoubleOptions pdo = new PromptDoubleOptions("\n Please Enter Horizontal Offset Value");
                    pdo.AllowNegative = false;
                    pdo.AllowNone = false;
                    pdo.AllowZero = false;
                    PromptDoubleResult pdr = ed.GetDouble(pdo);

                    if (pdr.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage("\n Error Reading the Value");
                        return;
                    }
                    HORIZONTAL_OFFSET = pdr.Value;

                    pdo = new PromptDoubleOptions("\n Please Enter Vertical Offset Value");
                    pdr = ed.GetDouble(pdo);

                    if (pdr.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage("\n Error Reading the Value");
                        return;
                    }
                    VERTICAL_OFFSET = pdr.Value;
#endregion

                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    foreach (BlockReference block in selectedBlocks)
                    {
                        Get_Module_Dimensions(doc, block);

                        Extents3d ext = block.GeometricExtents;
                        double minx = ext.MinPoint.X;
                        double miny = ext.MinPoint.Y;
                        double maxx = ext.MaxPoint.X;
                        double maxy = ext.MaxPoint.Y;

                        double lower_midpoint_X = minx + (BLOCK_LENGTH / 2);
                        if (Stringing_Direction == "Right_To_Left")
                        {
                            lower_midpoint_X = maxx - (BLOCK_LENGTH / 2);
                        }
                        double upper_midpoint_X = lower_midpoint_X;

                        double lower_midpoint_Y = miny + (BLOCK_HEIGHT / 2);
                        double upper_midpoint_Y = lower_midpoint_Y + VERTICAL_OFFSET + BLOCK_HEIGHT;

                        // Lower Row Stringing points
                        Point3d First_Lower_SubBlock_MidPoint = new Point3d(lower_midpoint_X, lower_midpoint_Y, GroundLevel);
                        Point3d Second_Lower_SubBlock_MidPoint = new Point3d(lower_midpoint_X + (BLOCK_LENGTH + HORIZONTAL_OFFSET), lower_midpoint_Y, GroundLevel);    //Left_To_Right
                        if (Stringing_Direction == "Right_To_Left") //Right To Left
                        {
                            Second_Lower_SubBlock_MidPoint = new Point3d(lower_midpoint_X - (BLOCK_LENGTH + HORIZONTAL_OFFSET), lower_midpoint_Y, GroundLevel);
                        }

                        Point3d Last_Lower_SubBlock_MidPoint = new Point3d(maxx - (BLOCK_LENGTH / 2), lower_midpoint_Y, GroundLevel);       //Left_To_Right
                        if (Stringing_Direction == "Right_To_Left") //Right To Left
                        {
                            Last_Lower_SubBlock_MidPoint = new Point3d(minx + (BLOCK_LENGTH / 2), lower_midpoint_Y, GroundLevel);
                        }

                        // Upper Row Stringing points
                        Point3d First_Upper_SubBlock_MidPoint = new Point3d(upper_midpoint_X, upper_midpoint_Y, GroundLevel);
                        Point3d Second_Upper_SubBlock_MidPoint = new Point3d(upper_midpoint_X + (BLOCK_LENGTH + HORIZONTAL_OFFSET), upper_midpoint_Y, GroundLevel);    //Left_To_Right
                        if (Stringing_Direction == "Right_To_Left") //Right To Left
                        {
                            Second_Upper_SubBlock_MidPoint = new Point3d(upper_midpoint_X - (BLOCK_LENGTH + HORIZONTAL_OFFSET), upper_midpoint_Y, GroundLevel);
                        }

                        Point3d Last_Upper_SubBlock_MidPoint = new Point3d(maxx - (BLOCK_LENGTH / 2), upper_midpoint_Y, GroundLevel);       //Left_To_Right
                        if (Stringing_Direction == "Right_To_Left") //Right To Left
                        {
                            Last_Upper_SubBlock_MidPoint = new Point3d(minx + (BLOCK_LENGTH / 2), upper_midpoint_Y, GroundLevel);
                        }

                        InsertPolarityText(db, tr, btr, First_Lower_SubBlock_MidPoint, "\uFF0B", 1); // - at the start
                        InsertPolarityText(db, tr, btr, First_Upper_SubBlock_MidPoint, "\uFF0B", 1); // - at the start

                        InsertPolarityText(db, tr, btr, Second_Lower_SubBlock_MidPoint, "\u2212", 3); // + at the end
                        InsertPolarityText(db, tr, btr, Second_Upper_SubBlock_MidPoint, "\u2212", 3); // + at the end

                        // Lower Row Splines Creation
                        Dictionary<Point3d, ObjectId> Spline1_coll = Create_Splines_For_LeapFrog(db, tr, btr, First_Lower_SubBlock_MidPoint, Last_Lower_SubBlock_MidPoint, Stringing_Direction);
                        Dictionary<Point3d, ObjectId> Spline2_coll = Create_Splines_For_LeapFrog(db, tr, btr, Second_Lower_SubBlock_MidPoint, Last_Lower_SubBlock_MidPoint, Stringing_Direction);

                        Point3d spline1_endPoint = Spline1_coll.ElementAt(0).Key;
                        Point3d spline2_endPoint = Spline2_coll.ElementAt(0).Key;

                        Point3dCollection Joint_Spline_P3D_Coll = new Point3dCollection();
                        Point3d Joint_Spline_MidPoint;

                        //Left_To_Right
                        if (spline1_endPoint.X < spline2_endPoint.X)
                        {
                            Joint_Spline_MidPoint = new Point3d(spline1_endPoint.X + ((BLOCK_LENGTH / 2) + HORIZONTAL_OFFSET), spline1_endPoint.Y + VERTICAL_OFFSET_BETWEEN_SPLINES, GroundLevel);
                        }
                        else //if(spline1_endPoint.X > spline2_endPoint.X)
                        {
                            Joint_Spline_MidPoint = new Point3d(spline1_endPoint.X - ((BLOCK_LENGTH / 2) + HORIZONTAL_OFFSET), spline1_endPoint.Y + VERTICAL_OFFSET_BETWEEN_SPLINES, GroundLevel);
                        }

                        if (Stringing_Direction == "Right_To_Left")
                        {
                            if (spline1_endPoint.X > spline2_endPoint.X)
                            {
                                Joint_Spline_MidPoint = new Point3d(spline1_endPoint.X - ((BLOCK_LENGTH / 2) + HORIZONTAL_OFFSET), spline1_endPoint.Y + VERTICAL_OFFSET_BETWEEN_SPLINES, GroundLevel);
                            }
                            else //if(spline1_endPoint.X < spline2_endPoint.X)
                            {
                                Joint_Spline_MidPoint = new Point3d(spline1_endPoint.X + ((BLOCK_LENGTH / 2) + HORIZONTAL_OFFSET), spline1_endPoint.Y + VERTICAL_OFFSET_BETWEEN_SPLINES, GroundLevel);
                            }
                        }

                        Joint_Spline_P3D_Coll.Add(spline1_endPoint);
                        Joint_Spline_P3D_Coll.Add(Joint_Spline_MidPoint);
                        Joint_Spline_P3D_Coll.Add(spline2_endPoint);

                        Spline Joint_Spline = new Spline(Joint_Spline_P3D_Coll, 3, 0.0);
                        ObjectId Joint_Spline_objid = btr.AppendEntity(Joint_Spline);
                        tr.AddNewlyCreatedDBObject(Joint_Spline, true);
                        Joint_Spline_P3D_Coll.Clear();

                        ObjectIdCollection Spline_Ids = new ObjectIdCollection();
                        Spline_Ids.Add(Joint_Spline_objid);
                        Spline_Ids.Add(Spline2_coll.ElementAt(0).Value);

                        Spline spline1 = (Spline)tr.GetObject(Spline1_coll.ElementAt(0).Value, OpenMode.ForWrite);

                        for (int i = 0; i < Spline_Ids.Count; i++)
                        {
                            Spline spline2 = (Spline)tr.GetObject(Spline_Ids[i], OpenMode.ForWrite);

                            spline1.JoinEntity(spline2);
                            spline2.UpgradeOpen();
                            spline2.Erase();
                        }

                        // Upper Splines Creation
                        Spline1_coll = Create_Splines_For_LeapFrog(db, tr, btr, First_Upper_SubBlock_MidPoint, Last_Upper_SubBlock_MidPoint, Stringing_Direction);
                        Spline2_coll = Create_Splines_For_LeapFrog(db, tr, btr, Second_Upper_SubBlock_MidPoint, Last_Upper_SubBlock_MidPoint, Stringing_Direction);

                        Spline_Ids.Clear();
                        Joint_Spline_P3D_Coll.Clear();

                        spline1_endPoint = Spline1_coll.ElementAt(0).Key;
                        spline2_endPoint = Spline2_coll.ElementAt(0).Key;

                        //Left_To_Right
                        if (spline1_endPoint.X < spline2_endPoint.X)
                        {
                            Joint_Spline_MidPoint = new Point3d(spline1_endPoint.X + ((BLOCK_LENGTH / 2) + HORIZONTAL_OFFSET), spline1_endPoint.Y + VERTICAL_OFFSET_BETWEEN_SPLINES, GroundLevel);
                        }
                        else //if(spline1_endPoint.X > spline2_endPoint.X)
                        {
                            Joint_Spline_MidPoint = new Point3d(spline1_endPoint.X - ((BLOCK_LENGTH / 2) + HORIZONTAL_OFFSET), spline1_endPoint.Y + VERTICAL_OFFSET_BETWEEN_SPLINES, GroundLevel);
                        }

                        if (Stringing_Direction == "Right_To_Left")
                        {
                            if (spline1_endPoint.X > spline2_endPoint.X)
                            {
                                Joint_Spline_MidPoint = new Point3d(spline1_endPoint.X - ((BLOCK_LENGTH / 2) + HORIZONTAL_OFFSET), spline1_endPoint.Y + VERTICAL_OFFSET_BETWEEN_SPLINES, GroundLevel);
                            }
                            else //if(spline1_endPoint.X < spline2_endPoint.X)
                            {
                                Joint_Spline_MidPoint = new Point3d(spline1_endPoint.X + ((BLOCK_LENGTH / 2) + HORIZONTAL_OFFSET), spline1_endPoint.Y + VERTICAL_OFFSET_BETWEEN_SPLINES, GroundLevel);
                            }
                        }

                        Joint_Spline_P3D_Coll.Add(spline1_endPoint);
                        Joint_Spline_P3D_Coll.Add(Joint_Spline_MidPoint);
                        Joint_Spline_P3D_Coll.Add(spline2_endPoint);

                        Joint_Spline = new Spline(Joint_Spline_P3D_Coll, 3, 0.0);
                        Joint_Spline_objid = btr.AppendEntity(Joint_Spline);
                        tr.AddNewlyCreatedDBObject(Joint_Spline, true);
                        Joint_Spline_P3D_Coll.Clear();

                        Spline_Ids.Add(Joint_Spline_objid);
                        Spline_Ids.Add(Spline2_coll.ElementAt(0).Value);

                        spline1 = (Spline)tr.GetObject(Spline1_coll.ElementAt(0).Value, OpenMode.ForWrite);

                        for (int i = 0; i < Spline_Ids.Count; i++)
                        {
                            Spline spline2 = (Spline)tr.GetObject(Spline_Ids[i], OpenMode.ForWrite);
                            spline1.JoinEntity(spline2);
                            spline2.UpgradeOpen();
                            spline2.Erase();
                        }
                    }
                    tr.Commit();
                }
            }
            
        }

        private Dictionary<Point3d, ObjectId> Create_Splines_For_LeapFrog(Database db, Transaction tr, BlockTableRecord btr, Point3d start, Point3d end, string Stringing_Direction)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            Point3dCollection points_Coll = new Point3dCollection();
            Point3d new_3DPoint = new Point3d();
            double X_val = start.X;
            int cnt = 1;

            if (Stringing_Direction == "Left_To_Right")
            {
                while (Math.Round(X_val, 5) <= Math.Round(end.X, 5))
                {
                    if (cnt % 2 == 1)
                    {
                        new_3DPoint = new Point3d(X_val, start.Y, 0);
                    }
                    else if (cnt % 2 == 0)
                    {
                        new_3DPoint = new Point3d(X_val, start.Y + VERTICAL_OFFSET_BETWEEN_SPLINES, 0);
                    }

                    points_Coll.Add(new_3DPoint);
                    X_val += BLOCK_LENGTH + HORIZONTAL_OFFSET;
                    cnt++;
                }
            }
            else if (Stringing_Direction == "Right_To_Left")
            {
                while (Math.Round(X_val, 5) >= Math.Round(end.X, 5))
                {
                    if (cnt % 2 == 1)
                    {
                        new_3DPoint = new Point3d(X_val, start.Y, 0);
                    }
                    else if (cnt % 2 == 0)
                    {
                        new_3DPoint = new Point3d(X_val, start.Y + VERTICAL_OFFSET_BETWEEN_SPLINES, 0);
                    }

                    points_Coll.Add(new_3DPoint);
                    X_val -= (BLOCK_LENGTH + HORIZONTAL_OFFSET);
                    cnt++;
                }
            }

            List<ObjectId> Spline_Ids = new List<ObjectId>();

            // Create multiple splines with overlapping segments
            for (int i = 0; i <= points_Coll.Count - 3; i += 2) // Increment by 2 for overlapping sections
            {
                Point3dCollection Vertices = new Point3dCollection
                {
                    points_Coll[i],
                    points_Coll[i + 1],
                    points_Coll[i + 2]
                };

                // Create Degree 3 spline
                Spline spline = new Spline(Vertices, 3, 0.0);
                spline.ColorIndex = 3;
                spline.LineWeight = LineWeight.LineWeight000;
                ObjectId splineId = btr.AppendEntity(spline);
                tr.AddNewlyCreatedDBObject(spline, true);
                Spline_Ids.Add(splineId);

                new_3DPoint = Vertices[2];
            }

            Spline Joined_spline = (Spline)tr.GetObject(Spline_Ids[0], OpenMode.ForWrite);

            for (int i = 1; i < Spline_Ids.Count; i++)
            {
                Spline spline2 = (Spline)tr.GetObject(Spline_Ids[i], OpenMode.ForWrite);

                Joined_spline.JoinEntity(spline2);
                spline2.UpgradeOpen();
                spline2.Erase();
            }

            Dictionary<Point3d, ObjectId> coll = new Dictionary<Point3d, ObjectId>();
            coll.Add(new_3DPoint, Joined_spline.ObjectId);
            return coll;
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

        private void InsertPolarityText(Database db, Transaction tr, BlockTableRecord btr, Point3d position, string text, int colorIndex)
        {
            TextStyleTable textStyleTable = tr.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;

            using (DBText dbText = new DBText())
            {
                dbText.Position = position;
                dbText.Height = 1;
                dbText.TextString = text;
                if (textStyleTable.Has("Arial"))
                {
                    dbText.TextStyleId = textStyleTable["Arial"];
                }                    
                dbText.HorizontalMode = TextHorizontalMode.TextCenter;
                dbText.VerticalMode = TextVerticalMid;
                dbText.AlignmentPoint = position;
                dbText.Justify = AttachmentPoint.MiddleCenter;
                dbText.ColorIndex = colorIndex;
                btr.AppendEntity(dbText);
                tr.AddNewlyCreatedDBObject(dbText, true);
            }
        }

        public void Get_Module_Dimensions(Document doc, BlockReference block)
        {
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBObjectCollection exploded = new DBObjectCollection();
                block.Explode(exploded);

                foreach (DBObject obj in exploded)
                {
                    if (obj is Solid3d solid)
                    {
                        try
                        {
                            Extents3d extents = solid.GeometricExtents;

                            double length = extents.MaxPoint.X - extents.MinPoint.X;
                            double height = extents.MaxPoint.Y - extents.MinPoint.Y;
                            double thickness = extents.MaxPoint.Z - extents.MinPoint.Z;

                            BLOCK_LENGTH = length;
                            BLOCK_HEIGHT = height;
                        }
                        catch (Autodesk.AutoCAD.Runtime.Exception ex)
                        {
                            ed.WriteMessage($"\nError getting extents: {ex.Message}");
                        }
                    }
                }
                tr.Commit();
            }
        }

        public void Map_SingleRow_Stringing(string Stringing_Direction)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (DocumentLock doclock = doc.LockDocument())
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
#region Get_And_Set_Values
                    string layerName = "Stringing";

                    if (!layerTable.Has(layerName))
                    {
                        // Upgrade to write and create new layer
                        layerTable.UpgradeOpen();
                        LayerTableRecord newLayer = new LayerTableRecord
                        {
                            Name = layerName
                        };
                        layerTable.Add(newLayer);
                        tr.AddNewlyCreatedDBObject(newLayer, true);
                    }
                    // Set the layer as current
                    db.Clayer = layerTable[layerName];

                    List<BlockReference> selectedBlocks = SelectBlocks(ed, tr);
                    if (selectedBlocks.Count == 0)
                    {
                        ed.WriteMessage("\nNo blocks selected.");
                        return;
                    }

                    PromptDoubleOptions pdo = new PromptDoubleOptions("\n Please Enter Horizontal Offset Value");
                    pdo.AllowNegative = false;
                    pdo.AllowNone = false;
                    pdo.AllowZero = false;
                    PromptDoubleResult pdr = ed.GetDouble(pdo);

                    if (pdr.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage("\n Error Reading the Value");
                        return;
                    }
                    HORIZONTAL_OFFSET = pdr.Value;

                    pdo = new PromptDoubleOptions("\n Please Enter Vertical Offset Value");
                    pdr = ed.GetDouble(pdo);

                    if (pdr.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage("\n Error Reading the Value");
                        return;
                    }
                    VERTICAL_OFFSET = pdr.Value;
#endregion

                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);

                    foreach (BlockReference block in selectedBlocks)
                    {
                        Get_Module_Dimensions(doc, block);

                        Extents3d ext = block.GeometricExtents;
                        double minx = ext.MinPoint.X;
                        double miny = ext.MinPoint.Y;
                        double maxx = ext.MaxPoint.X;
                        double maxy = ext.MaxPoint.Y;

                        double Left_midpoint_X = minx + (BLOCK_LENGTH / 2);
                        double Right_midpoint_X = maxx - (BLOCK_LENGTH / 2);
                        double Lower_midpoint_Y = miny + (BLOCK_HEIGHT / 2);
                        double Upper_midpoint_Y = Lower_midpoint_Y + VERTICAL_OFFSET + BLOCK_HEIGHT;

                        if (Stringing_Direction == "Left_To_Right")
                        {
                            InsertPolarityText(db, tr, btr, new Point3d(Left_midpoint_X, Lower_midpoint_Y, 0), "\uFF0B", 1);
                            InsertPolarityText(db, tr, btr, new Point3d(Right_midpoint_X, Lower_midpoint_Y, 0), "\u2212", 3);

                            InsertPolarityText(db, tr, btr, new Point3d(Left_midpoint_X, Upper_midpoint_Y, 0), "\uFF0B", 1);
                            InsertPolarityText(db, tr, btr, new Point3d(Right_midpoint_X, Upper_midpoint_Y, 0), "\u2212", 3);

                            Create_SingleRow_Stringing(db, tr, new Point3d(Left_midpoint_X, Lower_midpoint_Y, GroundLevel), new Point3d(Right_midpoint_X, Lower_midpoint_Y, GroundLevel));   //Lower Line
                            Create_SingleRow_Stringing(db, tr, new Point3d(Left_midpoint_X, Upper_midpoint_Y, GroundLevel), new Point3d(Right_midpoint_X, Upper_midpoint_Y, GroundLevel));   //Upper Line                     
                        }
                        else if (Stringing_Direction == "Right_To_Left")
                        {
                            InsertPolarityText(db, tr, btr, new Point3d(Left_midpoint_X, Lower_midpoint_Y, 0), "\u2212", 3);
                            InsertPolarityText(db, tr, btr, new Point3d(Right_midpoint_X, Lower_midpoint_Y, 0), "\uFF0B", 1);

                            InsertPolarityText(db, tr, btr, new Point3d(Left_midpoint_X, Upper_midpoint_Y, 0), "\u2212", 3);
                            InsertPolarityText(db, tr, btr, new Point3d(Right_midpoint_X, Upper_midpoint_Y, 0), "\uFF0B", 1);

                            Create_SingleRow_Stringing(db, tr, new Point3d(Right_midpoint_X, Lower_midpoint_Y, GroundLevel), new Point3d(Left_midpoint_X, Lower_midpoint_Y, GroundLevel));   //Lower Line
                            Create_SingleRow_Stringing(db, tr, new Point3d(Right_midpoint_X, Upper_midpoint_Y, GroundLevel), new Point3d(Left_midpoint_X, Upper_midpoint_Y, GroundLevel));   //Upper Line
                        }
                    }
                    tr.Commit();
                }
            }

            
        }

        private void Create_SingleRow_Stringing(Database db, Transaction tr, Point3d start, Point3d end)
        {
            if (start.IsEqualTo(end))
                return;
            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

            using (Line stringing = new Line(start, end))
            {
                stringing.ColorIndex = 3;
                stringing.LineWeight = LineWeight.LineWeight000;
                btr.AppendEntity(stringing);
                tr.AddNewlyCreatedDBObject(stringing, true);
            }
        }

        public void Map_U_String_Stringing(string Stringing_Direction)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (DocumentLock doclock = doc.LockDocument())
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
#region Get_And_Set_Values
                    string layerName = "Stringing";

                    if (!layerTable.Has(layerName))
                    {
                        // Upgrade to write and create new layer
                        layerTable.UpgradeOpen();
                        LayerTableRecord newLayer = new LayerTableRecord
                        {
                            Name = layerName
                        };
                        layerTable.Add(newLayer);
                        tr.AddNewlyCreatedDBObject(newLayer, true);
                    }
                    // Set the layer as current
                    db.Clayer = layerTable[layerName];

                    List<BlockReference> selectedBlocks = SelectBlocks(ed, tr);
                    if (selectedBlocks.Count == 0)
                    {
                        ed.WriteMessage("\nNo blocks selected.");
                        return;
                    }

                    PromptDoubleOptions pdo = new PromptDoubleOptions("\n Please Enter Horizontal Offset Value");
                    pdo.AllowNegative = false;
                    pdo.AllowNone = false;
                    pdo.AllowZero = false;
                    PromptDoubleResult pdr = ed.GetDouble(pdo);

                    if (pdr.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage("\n Error Reading the Value");
                        return;
                    }
                    HORIZONTAL_OFFSET = pdr.Value;

                    pdo = new PromptDoubleOptions("\n Please Enter Vertical Offset Value");
                    pdr = ed.GetDouble(pdo);

                    if (pdr.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage("\n Error Reading the Value");
                        return;
                    }
                    VERTICAL_OFFSET = pdr.Value;
#endregion

                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);

                    foreach (BlockReference block in selectedBlocks)
                    {
                        Get_Module_Dimensions(doc, block);

                        Extents3d ext = block.GeometricExtents;
                        double minx = ext.MinPoint.X;
                        double miny = ext.MinPoint.Y;
                        double maxx = ext.MaxPoint.X;
                        double maxy = ext.MaxPoint.Y;

                        double Left_midpoint_X = minx + (BLOCK_LENGTH / 2);
                        double Right_midpoint_X = maxx - (BLOCK_LENGTH / 2);
                        double Lower_midpoint_Y = miny + (BLOCK_HEIGHT / 2);
                        double Upper_midpoint_Y = Lower_midpoint_Y + VERTICAL_OFFSET + BLOCK_HEIGHT;

                        Point3dCollection Poly_Points = new Point3dCollection();
                        Point3d Start_Point;
                        Point3d Joint_point1;
                        Point3d Joint_point2;
                        Point3d End_Point;

                        if (Stringing_Direction == "Left_To_Right")
                        {
                            InsertPolarityText(db, tr, btr, new Point3d(Left_midpoint_X, Lower_midpoint_Y, 0), "\u2212", 3);
                            InsertPolarityText(db, tr, btr, new Point3d(Left_midpoint_X, Upper_midpoint_Y, 0), "\uFF0B", 1);

                            Start_Point = new Point3d(Left_midpoint_X, Lower_midpoint_Y, GroundLevel);
                            Joint_point1 = new Point3d(Right_midpoint_X, Lower_midpoint_Y, GroundLevel);
                            Joint_point2 = new Point3d(Right_midpoint_X, Upper_midpoint_Y, GroundLevel);
                            End_Point = new Point3d(Left_midpoint_X, Upper_midpoint_Y, GroundLevel);

                            Poly_Points.Add(Start_Point);
                            Poly_Points.Add(Joint_point1);
                            Poly_Points.Add(Joint_point2);
                            Poly_Points.Add(End_Point);

                            Create_U_String_Stringing(db, tr, Poly_Points);
                        }
                        else if (Stringing_Direction == "Right_To_Left")
                        {
                            InsertPolarityText(db, tr, btr, new Point3d(Right_midpoint_X, Lower_midpoint_Y, 0), "\u2212", 3);
                            InsertPolarityText(db, tr, btr, new Point3d(Right_midpoint_X, Upper_midpoint_Y, 0), "\uFF0B", 1);

                            Start_Point = new Point3d(Right_midpoint_X, Lower_midpoint_Y, GroundLevel);
                            Joint_point1 = new Point3d(Left_midpoint_X, Lower_midpoint_Y, GroundLevel);
                            Joint_point2 = new Point3d(Left_midpoint_X, Upper_midpoint_Y, GroundLevel);
                            End_Point = new Point3d(Right_midpoint_X, Upper_midpoint_Y, GroundLevel);

                            Poly_Points.Add(Start_Point);
                            Poly_Points.Add(Joint_point1);
                            Poly_Points.Add(Joint_point2);
                            Poly_Points.Add(End_Point);

                            Create_U_String_Stringing(db, tr, Poly_Points);
                        }
                    }
                    tr.Commit();
                }
            }            
        }

        private void Create_U_String_Stringing(Database db, Transaction tr, Point3dCollection Poly_Points)
        {
            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

            using (Polyline pline = new Polyline())
            {
                for (int i = 0; i < Poly_Points.Count; i++)
                {
                    pline.AddVertexAt(i, new Point2d(Poly_Points[i].X, Poly_Points[i].Y), 0, 0, 0);
                }
                pline.ColorIndex = 3;
                pline.LineWeight = LineWeight.LineWeight000;
                btr.AppendEntity(pline);
                tr.AddNewlyCreatedDBObject(pline, true);
            }
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }
        
        public event EventHandler CanExecuteChanged;
    }
}
