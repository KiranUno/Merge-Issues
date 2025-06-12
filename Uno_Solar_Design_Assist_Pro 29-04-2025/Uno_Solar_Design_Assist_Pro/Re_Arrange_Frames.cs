using System;
using System.Linq;
using System.Windows.Input;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;

namespace Uno_Solar_Design_Assist_Pro
{
    internal class Re_Arrange_Frames : ICommand
    {
        public static double CentralVal = 0;
        List<double> wrechLinesX = new List<double>();
        public static Polyline bp = null;
        public static double minZ = 0;

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            doc.LockDocument();
            List<Polyline> selectedPolylines = new List<Polyline>();
            List<List<Point3d>> pnts = new List<List<Point3d>>();
            List<Line> wrenchLines = new List<Line>();
            List<double> wrenchLeft = new List<double>();
            List<double> wrenchRight = new List<double>();
            List<Polyline> meshRed = new List<Polyline>();

            double THeight = 0;
            double TWidth = 0;
            double HOffset = 0;
            double VOffset = 0;
            double BOffset = 0;
            double RoadWidth = 0;
            double TPitch = 0;

            //double THeight = Convert.ToDouble(Properties.Settings.Default.TableHeight);
            //double TWidth = Convert.ToDouble(Properties.Settings.Default.TableWidth);
            //double HOffset = Convert.ToDouble(Properties.Settings.Default.HOffset);
            //double VOffset = Convert.ToDouble(Properties.Settings.Default.VOffset);
            //double BOffset = Convert.ToDouble(Properties.Settings.Default.BOffset);
            //double RoadWidth = Convert.ToDouble(Properties.Settings.Default.RoadWidth);
            //double TPitch = Convert.ToDouble(Properties.Settings.Default.TablePitch);
            VOffset = TPitch - THeight;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord modelSpace = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                foreach (ObjectId objId in modelSpace)
                {
                    Entity ent = tr.GetObject(objId, OpenMode.ForRead) as Entity;

                    if (ent is Face face) // Check if it's a 3DFace
                    {
                        // if(ent.Layer=="Red" || ent.Layer == "Light_Green" || ent.Layer == "Yellow")
                        if (ent.Layer == "Red")
                        {
                            Point3d v0 = face.GetVertexAt(0);
                            Point3d v1 = face.GetVertexAt(1);
                            Point3d v2 = face.GetVertexAt(2);
                            Point3d v3 = face.GetVertexAt(3);
                            minZ = 0;
                            Point2d p0 = new Point2d(v0.X, v0.Y);
                            Point2d p1 = new Point2d(v1.X, v1.Y);
                            Point2d p2 = new Point2d(v2.X, v2.Y);
                            Point2d p3 = new Point2d(v3.X, v3.Y);

                            Polyline pline = new Polyline();
                            pline.AddVertexAt(0, p0, 0, 0, 0);
                            pline.AddVertexAt(1, p1, 0, 0, 0);
                            pline.AddVertexAt(2, p2, 0, 0, 0);
                            pline.AddVertexAt(3, p3, 0, 0, 0);
                            pline.Closed = true;
                            pline.Elevation = 0;
                            BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                            btr.AppendEntity(pline);
                            tr.AddNewlyCreatedDBObject(pline, true);
                            meshRed.Add(pline);
                            ed.WriteMessage("\nConverted 3DFace to Polyline.");
                        }
                    }
                }
                tr.Commit();
            }



            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord space = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                PromptEntityOptions peo = new PromptEntityOptions("\nSelect a closed polyline: ");
                peo.SetRejectMessage("\nMust be a closed polyline.");
                peo.AddAllowedClass(typeof(Polyline), false);

                PromptEntityResult res = ed.GetEntity(peo);
                if (res.Status != PromptStatus.OK) return;

                Polyline poly = tr.GetObject(res.ObjectId, OpenMode.ForRead) as Polyline;
                if (poly == null || !poly.Closed)
                {
                    ed.WriteMessage("\nSelected polyline must be closed.");
                    return;
                }

                //Obstacles Selection

                PromptSelectionResult selResult = ed.GetSelection();                    //Obstacles Selection
                if (selResult.Status == PromptStatus.OK)
                {
                    SelectionSet selSet = selResult.Value;
                    foreach (SelectedObject selObj in selSet)
                    {
                        Entity ent = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                        if (ent != null && ent is Polyline)
                        {
                            Polyline polyline = (Polyline)ent;
                            selectedPolylines.Add(polyline);
                        }
                        else
                        {
                            Polyline2d poly2d = tr.GetObject(ent.ObjectId, OpenMode.ForRead) as Polyline2d;
                            if (poly2d == null)
                            {
                                ed.WriteMessage("\nSelected entity is not a valid 2D Polyline.");
                                return;
                            }

                            // Convert 2D Polyline to Polyline
                            Polyline newPolyline = ConvertPolyline2d(poly2d, tr);
                            if (newPolyline != null)
                            {
                                selectedPolylines.Add(newPolyline);
                            }
                            else
                            {
                                ed.WriteMessage("\nConversion failed.");
                            }
                        }
                    }

                    foreach (Polyline polyline in selectedPolylines)
                    {

                        List<Point3d> vertices = new List<Point3d>();
                        for (int i = 0; i < polyline.NumberOfVertices; i++)
                        {
                            vertices.Add(polyline.GetPoint3dAt(i));
                        }

                        pnts.Add(vertices);
                    }
                }



                Polyline polylineOffset = tr.GetObject(res.ObjectId, OpenMode.ForRead) as Polyline;
                Polyline OffSet = null;

                if (polylineOffset != null)
                {
                    DBObjectCollection offsetCurves = polylineOffset.GetOffsetCurves(-6);

                    foreach (DBObject obj in offsetCurves)
                    {
                        Polyline offsetPolyline = obj as Polyline;
                        if (offsetPolyline != null)
                        {
                            space.AppendEntity(offsetPolyline);
                            tr.AddNewlyCreatedDBObject(offsetPolyline, true);
                        }
                        OffSet = offsetPolyline;
                        bp = offsetPolyline;
                    }
                }



                PromptPointResult p1Result = ed.GetPoint("\nSpecify first point for vertical center reference line: ");
                if (p1Result.Status != PromptStatus.OK) return;
                PromptPointOptions p2Options = new PromptPointOptions("\nSpecify second point for vertical center reference line: ")
                {
                    BasePoint = p1Result.Value,
                    UseBasePoint = true
                };

                CentralVal = p1Result.Value.X;
                wrenchRight.Add(CentralVal);
                wrenchLeft.Add(CentralVal);
                PromptPointResult p2Result = ed.GetPoint(p2Options);
                if (p2Result.Status != PromptStatus.OK) return;

                Line refLine = new Line(p1Result.Value, p2Result.Value);
                refLine.SetDatabaseDefaults();
                refLine.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1); // Red color

                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                space.AppendEntity(refLine);
                tr.AddNewlyCreatedDBObject(refLine, true);
                tr.Commit();
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord space = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                while (true)
                {
                    PromptPointResult startPointResult = ed.GetPoint("\nSelect first point: ");
                    if (startPointResult.Status != PromptStatus.OK) break;

                    Point3d startPoint = startPointResult.Value;

                    if (startPoint.X < CentralVal)
                    {
                        wrenchLeft.Add(startPoint.X);

                    }
                    else if (startPoint.X > CentralVal)
                    {
                        wrenchRight.Add(startPoint.X);
                    }

                    wrechLinesX.Add(startPoint.X);
                    Preview_Line Preview_Line = new Preview_Line(startPoint);

                    if (ed.Drag(Preview_Line).Status != PromptStatus.OK)
                        break;

                    Point3d endPoint = Preview_Line.GetSecondPoint();
                    if (endPoint.DistanceTo(startPoint) == 0)
                        break;

                    Line line = new Line(startPoint, endPoint);
                    space.AppendEntity(line);
                    tr.AddNewlyCreatedDBObject(line, true);
                    wrenchLines.Add(line);
                }

                tr.Commit();
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                if (bt == null) return;

                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;

                    if (btr == null || btr.IsLayout) continue;

                    foreach (ObjectId entId in btr.GetBlockReferenceIds(true, false))
                    {
                        Entity ent = tr.GetObject(entId, OpenMode.ForWrite) as Entity;
                        if (ent != null)
                        {
                            ent.Erase();
                        }
                    }
                }

                tr.Commit();
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                if (bt == null) return;

                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;

                    if (btr == null || btr.IsLayout) continue;

                    foreach (ObjectId entId in btr.GetBlockReferenceIds(true, false))
                    {
                        Entity ent = tr.GetObject(entId, OpenMode.ForWrite) as Entity;
                        if (ent != null)
                        {
                            ent.Erase();
                        }
                    }
                }

                tr.Commit();
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                if (bt == null) return;

                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;

                    if (btr == null || btr.IsLayout) continue;
                    foreach (ObjectId entId in btr.GetBlockReferenceIds(true, false))
                    {
                        Entity ent = tr.GetObject(entId, OpenMode.ForWrite) as Entity;
                        if (ent != null)
                        {
                            ent.Erase();
                        }
                    }
                }

                tr.Commit();
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord space = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                Entity ent = tr.GetObject(bp.ObjectId, OpenMode.ForRead) as Entity;
                if (ent != null && ent is Polyline)
                {
                    Polyline polyline = (Polyline)ent;
                    Extents3d ext = ent.GeometricExtents;
                    wrenchRight.Sort();
                    wrenchLeft = wrenchLeft.OrderByDescending(x => x).ToList();

                    //Select Horizontal road

                    // Prompt user to select a polyline
                    PromptEntityOptions peo = new PromptEntityOptions("\nSelect Horizontal road: ");
                    peo.SetRejectMessage("\nSelected entity is not a polyline.");
                    peo.AddAllowedClass(typeof(Polyline), true);
                    PromptEntityResult per = ed.GetEntity(peo);

                    if (per.Status != PromptStatus.OK)
                        return;

                    Polyline pline = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Polyline;

                    Point3d minPoint;
                    Point3d maxPoint;
                    if (pline != null)
                    {
                        Extents3d extents = pline.GeometricExtents;

                        minPoint = extents.MinPoint;
                        maxPoint = extents.MaxPoint;
                        CreateLinesRoadBased(ext, space, tr, polyline, db, bp, THeight, TWidth, HOffset, VOffset, BOffset, CentralVal, RoadWidth, wrenchRight, wrenchLeft, minPoint, maxPoint);

                    }
                }


                Polyline poly = tr.GetObject(bp.ObjectId, OpenMode.ForWrite) as Polyline;
                if (poly != null)
                {
                    poly.Erase();
                }
                tr.Commit();
            }

            string active_layername = null;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                if (btr != null)
                {
                    LayerTableRecord activeLayer = tr.GetObject(db.Clayer, OpenMode.ForRead) as LayerTableRecord;
                    active_layername = activeLayer.Name;
                }
            }

            PromptSelectionResult pmtSelRes = null;

            TypedValue[] typedVal = new TypedValue[1];

            typedVal[0] = new TypedValue((int)DxfCode.LayerName, active_layername);

            for (int i = 0; i < pnts.Count; i++)
            {
                Point3dCollection pntCol = new Point3dCollection();
                List<Point3d> l = pnts[i];
                for (int j = 0; j < l.Count; j++)
                {
                    pntCol.Add(l[j]);
                }

                SelectionFilter selFilter = new SelectionFilter(typedVal);
                pmtSelRes = ed.SelectCrossingPolygon(pntCol, selFilter);

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    if (pmtSelRes.Status == PromptStatus.OK)
                    {
                        foreach (ObjectId objId in pmtSelRes.Value.GetObjectIds())
                        {
                            Entity ent = tr.GetObject(objId, OpenMode.ForWrite) as Entity;
                            Polyline polyline = ent as Polyline;

                            if (polyline == null)
                            {
                                ent.Erase();
                                PlaceTwoBlocks(tr, db, ent, THeight, TWidth);
                                PlaceHalfBlock(tr, db, ent, THeight, TWidth);
                            }
                        }
                    }
                    tr.Commit();
                }
            }

            string active_layername2 = null;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                if (btr != null)
                {
                    LayerTableRecord activeLayer = tr.GetObject(db.Clayer, OpenMode.ForRead) as LayerTableRecord;
                    active_layername = activeLayer.Name;
                }
            }

            PromptSelectionResult pmtSelRes2 = null;

            TypedValue[] typedVal2 = new TypedValue[1];

            typedVal2[0] = new TypedValue((int)DxfCode.LayerName, active_layername);

            for (int i = 0; i < pnts.Count; i++)
            {
                Point3dCollection pntCol = new Point3dCollection();
                List<Point3d> l = pnts[i];
                for (int j = 0; j < l.Count; j++)
                {
                    pntCol.Add(l[j]);
                }

                SelectionFilter selFilter = new SelectionFilter(typedVal2);
                pmtSelRes2 = ed.SelectCrossingPolygon(pntCol, selFilter);

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    if (pmtSelRes2.Status == PromptStatus.OK)
                    {
                        foreach (ObjectId objId in pmtSelRes2.Value.GetObjectIds())
                        {
                            Entity ent = tr.GetObject(objId, OpenMode.ForWrite) as Entity;
                            Polyline polyline = ent as Polyline;

                            if (polyline == null)
                            {
                                ent.Erase();
                            }
                        }
                    }
                    tr.Commit();
                }
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    Polyline selectedPolyline = tr.GetObject(bp.ObjectId, OpenMode.ForWrite) as Polyline;
                    if (selectedPolyline != null)
                    {
                        double length = selectedPolyline.Length;
                        double segmentLength = length / 300.0;
                        List<Point3d> points = new List<Point3d>();

                        for (int i = 0; i < 300; i++)
                        {
                            points.Add(selectedPolyline.GetPointAtDist(i * segmentLength));
                        }
                        points.Add(selectedPolyline.StartPoint);

                        PromptSelectionResult s = ed.SelectFence(new Point3dCollection(points.ToArray()), new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, "INSERT") }));
                        if (s.Status == PromptStatus.OK)
                        {
                            List<ObjectId> add = new List<ObjectId>();
                            foreach (SelectedObject blockRef in s.Value)
                            {
                                Entity block = tr.GetObject(blockRef.ObjectId, OpenMode.ForWrite) as Entity;
                                if (block != null)
                                {
                                    block.Erase();
                                }
                            }
                        }
                    }

                    tr.Commit();
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\nError: {ex.Message}");
                }
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                List<ObjectId> blocksToDelete = new List<ObjectId>();

                foreach (Polyline polyline in meshRed)
                {
                    foreach (ObjectId entId in btr)
                    {
                        Entity ent = tr.GetObject(entId, OpenMode.ForWrite) as Entity;
                        if (ent is BlockReference blockRef)
                        {
                            Point3dCollection intersectionPoints = new Point3dCollection();
                            polyline.IntersectWith(blockRef, Intersect.ExtendThis, intersectionPoints, IntPtr.Zero, IntPtr.Zero);

                            if (intersectionPoints.Count > 0)
                            {
                                blocksToDelete.Add(blockRef.ObjectId);
                            }
                        }
                    }
                }

                foreach (ObjectId blockId in blocksToDelete)
                {
                    Entity block = tr.GetObject(blockId, OpenMode.ForWrite) as Entity;
                    Polyline polyline = block as Polyline;

                    if (polyline == null)
                    {
                        block.Erase();
                        PlaceTwoBlocks(tr, db, block, THeight, TWidth);
                        PlaceHalfBlock(tr, db, block, THeight, TWidth);
                    }
                }


                blocksToDelete.Clear();

                foreach (Polyline polyline in meshRed)
                {
                    foreach (ObjectId entId in btr)
                    {
                        Entity ent = tr.GetObject(entId, OpenMode.ForWrite) as Entity;
                        if (ent is BlockReference blockRef)
                        {
                            Point3dCollection intersectionPoints = new Point3dCollection();
                            polyline.IntersectWith(blockRef, Intersect.ExtendThis, intersectionPoints, IntPtr.Zero, IntPtr.Zero);

                            if (intersectionPoints.Count > 0)  // If intersection exists
                            {
                                blocksToDelete.Add(blockRef.ObjectId);
                            }
                        }
                    }
                }

                foreach (ObjectId blockId in blocksToDelete)
                {
                    Entity block = tr.GetObject(blockId, OpenMode.ForWrite) as Entity;
                    block.Erase();
                }

                foreach (Polyline polyline in meshRed)
                {
                    polyline.UpgradeOpen();
                    polyline.Erase();
                }


                tr.Commit();
                ed.WriteMessage($"\nDeleted {blocksToDelete.Count} blocks touching polylines.");
            }

        }

        private static Polyline ConvertPolyline2d(Polyline2d poly2d, Transaction tr)
        {
            Polyline polyline = new Polyline();
            int index = 0;

            foreach (ObjectId vertexId in poly2d)
            {
                Vertex2d vertex = tr.GetObject(vertexId, OpenMode.ForRead) as Vertex2d;
                if (vertex != null)
                {
                    polyline.AddVertexAt(index, new Point2d(vertex.Position.X, vertex.Position.Y), vertex.Bulge, 0, 0);
                    index++;
                }
            }

            polyline.Closed = poly2d.Closed;
            return polyline;
        }

        public static void CreateLinesRoadBased(Extents3d ext, BlockTableRecord space, Transaction tr, Polyline poly, Database db, Polyline OffSet, double THeight, double TWidth,
        double HOffset, double VOffset, double BOffset, double CenterVal, double RoadWidth, List<double> wrenchRight, List<double> wrenchLeft, Point3d minPointHRoad,
        Point3d maxPointHRoad)
        {

            Point3d min = ext.MinPoint;
            Point3d max = ext.MaxPoint;
            double xdist = max.X - min.X;
            double ydist = max.Y - min.Y;
            ydist = ydist - BOffset;
            double maxY = max.Y - (BOffset + THeight);


            ydist = minPointHRoad.Y - min.Y;
            maxY = minPointHRoad.Y - 0.1;

            double lineOffset = 0;
            try
            {
                double moovedist = THeight / 2;
                if (xdist > 0 && ydist > 0)
                {
                    while (ydist > (VOffset + THeight + BOffset))
                    {
                        maxY = maxY - moovedist;
                        Line newLine = new Line(new Point3d(min.X, maxY, min.Z), new Point3d(max.X, maxY, min.Z));
                        space.AppendEntity(newLine);
                        tr.AddNewlyCreatedDBObject(newLine, true);
                        Line line = tr.GetObject(newLine.ObjectId, OpenMode.ForWrite) as Line;
                        if (line == null) return;
                        Point3dCollection intersectionPoints = new Point3dCollection();
                        line.IntersectWith(poly, Intersect.ExtendThis, intersectionPoints, IntPtr.Zero, IntPtr.Zero);
                        List<Point3d> sortedPoints = new List<Point3d>(intersectionPoints.Cast<Point3d>());
                        sortedPoints.Sort((p1, p2) => p1.DistanceTo(line.StartPoint).CompareTo(p2.DistanceTo(line.StartPoint)));
                        Point3d p1 = sortedPoints[0];
                        Point3d p2 = sortedPoints[1];
                        PlacePanelsOnLineRight(p1, p2, tr, db, OffSet, THeight, TWidth, HOffset, VOffset, BOffset, CenterVal, RoadWidth, wrenchRight);
                        PlacePanelsOnLineLeft(p1, p2, tr, db, OffSet, THeight, TWidth, HOffset, VOffset, BOffset, CenterVal, RoadWidth, wrenchLeft);
                        Line lineTrimmed = new Line(p1, p2);
                        space.AppendEntity(lineTrimmed);
                        tr.AddNewlyCreatedDBObject(lineTrimmed, true);
                        newLine.Erase();
                        lineTrimmed.Erase();
                        ydist = ydist - (VOffset + THeight);
                        lineOffset = VOffset;
                        moovedist = VOffset + THeight;
                    }
                }
            }
            catch (System.ArgumentOutOfRangeException ex)
            {

            }

            //For top side placement
            maxY = minPointHRoad.Y + RoadWidth + 0.1;
            ydist = max.Y - maxY;

            try
            {
                double moovedist = THeight / 2;
                if (xdist > 0 && ydist > 0)
                {
                    while (ydist > (VOffset + THeight + BOffset))
                    {
                        maxY = maxY + moovedist; ;
                        Line newLine = new Line(new Point3d(min.X, maxY, min.Z), new Point3d(max.X, maxY, min.Z));
                        space.AppendEntity(newLine);
                        tr.AddNewlyCreatedDBObject(newLine, true);
                        Line line = tr.GetObject(newLine.ObjectId, OpenMode.ForWrite) as Line;
                        if (line == null) return;
                        Point3dCollection intersectionPoints = new Point3dCollection();
                        line.IntersectWith(poly, Intersect.ExtendThis, intersectionPoints, IntPtr.Zero, IntPtr.Zero);
                        List<Point3d> sortedPoints = new List<Point3d>(intersectionPoints.Cast<Point3d>());
                        sortedPoints.Sort((p1, p2) => p1.DistanceTo(line.StartPoint).CompareTo(p2.DistanceTo(line.StartPoint)));
                        Point3d p1 = sortedPoints[0];
                        Point3d p2 = sortedPoints[1];
                        PlacePanelsOnLineRight(p1, p2, tr, db, OffSet, THeight, TWidth, HOffset, VOffset, BOffset, CenterVal, RoadWidth, wrenchRight);
                        PlacePanelsOnLineLeft(p1, p2, tr, db, OffSet, THeight, TWidth, HOffset, VOffset, BOffset, CenterVal, RoadWidth, wrenchLeft);
                        Line lineTrimmed = new Line(p1, p2);
                        space.AppendEntity(lineTrimmed);
                        tr.AddNewlyCreatedDBObject(lineTrimmed, true);
                        newLine.Erase();
                        lineTrimmed.Erase();
                        ydist = ydist - (VOffset + THeight);
                        lineOffset = VOffset;
                        moovedist = VOffset + THeight;
                    }
                }
            }
            catch (System.ArgumentOutOfRangeException ex)
            {

            }


            //

        }

        public static void PlacePanelsOnLineRight(Point3d p1, Point3d p2, Transaction tr, Database db, Polyline OffSet, double THeight, double TWidth,
          double HOffset, double VOffset, double BOffset, double CenterVal, double RoadWidth, List<double> wrenchRight)
        {
            BlockTable acBlkTbl = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

            if (p2.X > CenterVal)
            {
                int num = 0;

                for (int i = 0; i < wrenchRight.Count; i++)
                {

                    double wrechdist = 0;

                    if (i == wrenchRight.Count - 1)
                    {

                        wrechdist = p2.X;
                    }
                    else
                    {
                        wrechdist = wrenchRight[i + 1];
                    }

                    double xdist = wrechdist - wrenchRight[i];
                    double blockStartX = wrenchRight[i] + RoadWidth / 2 + 0.1;
                    double blockStartY = p1.Y;

                    //Sample code for small panels


                    while (xdist >= (TWidth + HOffset) || xdist >= ((TWidth / 2) + HOffset))
                    {
                        if ((xdist >= (TWidth + HOffset) && (xdist >= (TWidth / 2 + HOffset)) && (blockStartX - p1.X) > HOffset))
                        {

                            if ((p2.X - blockStartX) >= (TWidth) && ((wrechdist - blockStartX) >= (TWidth)))
                            {
                                ObjectId blkRecId = ObjectId.Null;
                                using (BlockTableRecord acBlkTblRec = new BlockTableRecord())
                                {
                                    acBlkTblRec.Origin = new Point3d(blockStartX, blockStartY, minZ);

                                    using (Polyline rectangle = new Polyline())
                                    {
                                        rectangle.SetDatabaseDefaults();
                                        rectangle.AddVertexAt(0, new Point2d(blockStartX, blockStartY - THeight / 2), 0, 0, 0);
                                        rectangle.AddVertexAt(1, new Point2d(blockStartX + TWidth, blockStartY - THeight / 2), 0, 0, 0);
                                        rectangle.AddVertexAt(2, new Point2d(blockStartX + TWidth, blockStartY + THeight / 2), 0, 0, 0);
                                        rectangle.AddVertexAt(3, new Point2d(blockStartX, blockStartY + THeight / 2), 0, 0, 0);
                                        rectangle.Closed = true;
                                        acBlkTblRec.AppendEntity(rectangle);
                                        tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                                        acBlkTbl.Add(acBlkTblRec);
                                        tr.AddNewlyCreatedDBObject(acBlkTblRec, true);
                                    }

                                    blkRecId = acBlkTblRec.Id;

                                    if (blkRecId != ObjectId.Null)
                                    {
                                        using (BlockReference acBlkRef = new BlockReference(new Point3d(blockStartX, blockStartY, 0), blkRecId))
                                        {
                                            BlockTableRecord acCurSpaceBlkTblRec = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                            acCurSpaceBlkTblRec.AppendEntity(acBlkRef);
                                            tr.AddNewlyCreatedDBObject(acBlkRef, true);
                                            acBlkRef.Position = new Point3d(blockStartX, blockStartY, minZ);
                                            acBlkRef.RecordGraphicsModified(true);
                                        }
                                    }
                                }

                            }
                            else if ((p2.X - blockStartX) >= (TWidth / 2) && ((wrechdist - blockStartX) >= (TWidth / 2)))
                            {
                                ObjectId blkRecId = ObjectId.Null;
                                using (BlockTableRecord acBlkTblRec = new BlockTableRecord())
                                {
                                    acBlkTblRec.Origin = new Point3d(blockStartX, blockStartY, minZ);

                                    using (Polyline rectangle = new Polyline())
                                    {
                                        rectangle.SetDatabaseDefaults();
                                        rectangle.AddVertexAt(0, new Point2d(blockStartX, blockStartY - THeight / 2), 0, 0, 0);
                                        rectangle.AddVertexAt(1, new Point2d(blockStartX + TWidth / 2, blockStartY - THeight / 2), 0, 0, 0);
                                        rectangle.AddVertexAt(2, new Point2d(blockStartX + TWidth / 2, blockStartY + THeight / 2), 0, 0, 0);
                                        rectangle.AddVertexAt(3, new Point2d(blockStartX, blockStartY + THeight / 2), 0, 0, 0);
                                        rectangle.Closed = true;
                                        acBlkTblRec.AppendEntity(rectangle);
                                        tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                                        acBlkTbl.Add(acBlkTblRec);
                                        tr.AddNewlyCreatedDBObject(acBlkTblRec, true);
                                    }

                                    blkRecId = acBlkTblRec.Id;

                                    if (blkRecId != ObjectId.Null)
                                    {
                                        using (BlockReference acBlkRef = new BlockReference(new Point3d(blockStartX, blockStartY, minZ), blkRecId))
                                        {
                                            BlockTableRecord acCurSpaceBlkTblRec = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                            acCurSpaceBlkTblRec.AppendEntity(acBlkRef);
                                            tr.AddNewlyCreatedDBObject(acBlkRef, true);
                                            acBlkRef.Position = new Point3d(blockStartX, blockStartY, minZ);
                                            acBlkRef.RecordGraphicsModified(true);
                                        }
                                    }
                                }
                            }

                        }

                        xdist = xdist - (TWidth + HOffset);
                        blockStartX = blockStartX + TWidth + HOffset;
                    }
                }

                num++;
            }

        }

        public static void PlacePanelsOnLineLeft(Point3d p1, Point3d p2, Transaction tr, Database db, Polyline OffSet, double THeight, double TWidth,
          double HOffset, double VOffset, double BOffset, double CenterVal, double RoadWidth, List<double> wrenchRight)
        {
            BlockTable acBlkTbl = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

            if (p1.X < CenterVal)
            {
                int num = 0;

                for (int i = 0; i < wrenchRight.Count; i++)
                {

                    double wrechdist = 0;

                    if (i == wrenchRight.Count - 1)
                    {

                        wrechdist = p1.X;
                    }
                    else
                    {
                        wrechdist = wrenchRight[i + 1];
                    }

                    double d = wrenchRight[i];

                    double xdist = wrenchRight[i] - wrechdist;
                    double blockStartX = wrenchRight[i] - RoadWidth / 2 - 0.1;
                    double blockStartY = p1.Y;



                    while (xdist >= (TWidth + HOffset) || xdist >= ((TWidth / 2) + HOffset))
                    {
                        if ((xdist >= (TWidth + HOffset) && (xdist >= (TWidth / 2 + HOffset)) && (p2.X - blockStartX) > BOffset) && (blockStartX - TWidth - p1.X > BOffset))
                        {
                            ObjectId blkRecId = ObjectId.Null;
                            using (BlockTableRecord acBlkTblRec = new BlockTableRecord())
                            {
                                acBlkTblRec.Origin = new Point3d(blockStartX, blockStartY, minZ);

                                using (Polyline rectangle = new Polyline())
                                {
                                    rectangle.SetDatabaseDefaults();
                                    rectangle.AddVertexAt(0, new Point2d(blockStartX, blockStartY - THeight / 2), 0, 0, 0);
                                    rectangle.AddVertexAt(1, new Point2d(blockStartX - TWidth, blockStartY - THeight / 2), 0, 0, 0);
                                    rectangle.AddVertexAt(2, new Point2d(blockStartX - TWidth, blockStartY + THeight / 2), 0, 0, 0);
                                    rectangle.AddVertexAt(3, new Point2d(blockStartX, blockStartY + THeight / 2), 0, 0, 0);
                                    rectangle.Closed = true;
                                    acBlkTblRec.AppendEntity(rectangle);
                                    tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                                    acBlkTbl.Add(acBlkTblRec);
                                    tr.AddNewlyCreatedDBObject(acBlkTblRec, true);
                                }

                                blkRecId = acBlkTblRec.Id;

                                if (blkRecId != ObjectId.Null)
                                {
                                    using (BlockReference acBlkRef = new BlockReference(new Point3d(blockStartX, blockStartY, minZ), blkRecId))
                                    {
                                        BlockTableRecord acCurSpaceBlkTblRec = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                        acCurSpaceBlkTblRec.AppendEntity(acBlkRef);
                                        tr.AddNewlyCreatedDBObject(acBlkRef, true);
                                        acBlkRef.Position = new Point3d(blockStartX, blockStartY, minZ);
                                        acBlkRef.RecordGraphicsModified(true);
                                    }
                                }
                            }

                        }
                        else if (((xdist >= (TWidth / 2 + HOffset)) && (p2.X - blockStartX) > BOffset) && (blockStartX - TWidth / 2 - p1.X > BOffset))
                        {
                            ObjectId blkRecId = ObjectId.Null;
                            using (BlockTableRecord acBlkTblRec = new BlockTableRecord())
                            {
                                acBlkTblRec.Origin = new Point3d(blockStartX, blockStartY, minZ);

                                using (Polyline rectangle = new Polyline())
                                {
                                    rectangle.SetDatabaseDefaults();
                                    rectangle.AddVertexAt(0, new Point2d(blockStartX, blockStartY - THeight / 2), 0, 0, 0);
                                    rectangle.AddVertexAt(1, new Point2d(blockStartX - TWidth / 2, blockStartY - THeight / 2), 0, 0, 0);
                                    rectangle.AddVertexAt(2, new Point2d(blockStartX - TWidth / 2, blockStartY + THeight / 2), 0, 0, 0);
                                    rectangle.AddVertexAt(3, new Point2d(blockStartX, blockStartY + THeight / 2), 0, 0, 0);
                                    rectangle.Closed = true;
                                    acBlkTblRec.AppendEntity(rectangle);
                                    tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                                    acBlkTbl.Add(acBlkTblRec);
                                    tr.AddNewlyCreatedDBObject(acBlkTblRec, true);
                                }

                                blkRecId = acBlkTblRec.Id;

                                if (blkRecId != ObjectId.Null)
                                {
                                    using (BlockReference acBlkRef = new BlockReference(new Point3d(blockStartX, blockStartY, minZ), blkRecId))
                                    {
                                        BlockTableRecord acCurSpaceBlkTblRec = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                        acCurSpaceBlkTblRec.AppendEntity(acBlkRef);
                                        tr.AddNewlyCreatedDBObject(acBlkRef, true);
                                        acBlkRef.Position = new Point3d(blockStartX, blockStartY, minZ);
                                        acBlkRef.RecordGraphicsModified(true);
                                    }
                                }
                            }
                        }

                        xdist = xdist - (TWidth + HOffset);
                        blockStartX = blockStartX - TWidth - HOffset;
                    }
                }

                num++;
            }

        }

        public static void PlaceTwoBlocks(Transaction tr, Database db, Entity ent, double THeight, double TWidth)
        {
            BlockTable acBlkTbl = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
            ObjectId blkRecId = ObjectId.Null;
            ObjectId blkRecId2 = ObjectId.Null;

            Extents3d extents = ent.GeometricExtents;
            Point3d minPoint = extents.MinPoint;
            Point3d maxPoint = extents.MaxPoint;
            double blockDistance = maxPoint.X - minPoint.X;
            if (blockDistance >= TWidth - 1)
            {
                //blockDistance >= TWidth
                using (BlockTableRecord acBlkTblRec = new BlockTableRecord())
                {
                    acBlkTblRec.Origin = new Point3d(minPoint.X, minPoint.Y, minZ);

                    using (Polyline rectangle = new Polyline())
                    {
                        rectangle.SetDatabaseDefaults();
                        rectangle.AddVertexAt(0, new Point2d(minPoint.X, minPoint.Y), 0, 0, 0);
                        rectangle.AddVertexAt(1, new Point2d(minPoint.X + blockDistance / 2, minPoint.Y), 0, 0, 0);
                        rectangle.AddVertexAt(2, new Point2d(minPoint.X + blockDistance / 2, maxPoint.Y), 0, 0, 0);
                        rectangle.AddVertexAt(3, new Point2d(minPoint.X, maxPoint.Y), 0, 0, 0);
                        rectangle.Closed = true;
                        acBlkTblRec.AppendEntity(rectangle);
                        tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                        acBlkTbl.Add(acBlkTblRec);
                        tr.AddNewlyCreatedDBObject(acBlkTblRec, true);
                    }

                    blkRecId = acBlkTblRec.Id;

                    if (blkRecId != ObjectId.Null)
                    {
                        using (BlockReference acBlkRef = new BlockReference(new Point3d(minPoint.X, minPoint.Y, 0), blkRecId))
                        {
                            BlockTableRecord acCurSpaceBlkTblRec = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                            acCurSpaceBlkTblRec.AppendEntity(acBlkRef);
                            tr.AddNewlyCreatedDBObject(acBlkRef, true);
                            acBlkRef.Position = new Point3d(minPoint.X, minPoint.Y, minZ);
                            acBlkRef.RecordGraphicsModified(true);
                        }
                    }
                }
            }

        }

        public static void PlaceHalfBlock(Transaction tr, Database db, Entity ent, double THeight, double TWidth)
        {
            BlockTable acBlkTbl = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
            ObjectId blkRecId = ObjectId.Null;
            ObjectId blkRecId2 = ObjectId.Null;

            Extents3d extents = ent.GeometricExtents;
            Point3d minPoint = extents.MinPoint;
            Point3d maxPoint = extents.MaxPoint;
            double blockDistance = maxPoint.X - minPoint.X;


            if (blockDistance >= TWidth - 1)
            {
                using (BlockTableRecord acBlkTblRec = new BlockTableRecord())
                {
                    acBlkTblRec.Origin = new Point3d(minPoint.X, minPoint.Y, minZ);

                    using (Polyline rectangle = new Polyline())
                    {
                        rectangle.SetDatabaseDefaults();
                        rectangle.AddVertexAt(0, new Point2d(minPoint.X + blockDistance / 2, minPoint.Y), 0, 0, 0);
                        rectangle.AddVertexAt(1, new Point2d(minPoint.X + blockDistance, minPoint.Y), 0, 0, 0);
                        rectangle.AddVertexAt(2, new Point2d(minPoint.X + blockDistance, maxPoint.Y), 0, 0, 0);
                        rectangle.AddVertexAt(3, new Point2d(minPoint.X + blockDistance / 2, maxPoint.Y), 0, 0, 0);
                        rectangle.Closed = true;
                        acBlkTblRec.AppendEntity(rectangle);
                        tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                        acBlkTbl.Add(acBlkTblRec);
                        tr.AddNewlyCreatedDBObject(acBlkTblRec, true);
                    }

                    blkRecId = acBlkTblRec.Id;

                    if (blkRecId != ObjectId.Null)
                    {
                        using (BlockReference acBlkRef = new BlockReference(new Point3d(minPoint.X, minPoint.Y, 585.1255), blkRecId))
                        {
                            BlockTableRecord acCurSpaceBlkTblRec = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                            acCurSpaceBlkTblRec.AppendEntity(acBlkRef);
                            tr.AddNewlyCreatedDBObject(acBlkRef, true);
                            acBlkRef.Position = new Point3d(minPoint.X, minPoint.Y, minZ);
                            acBlkRef.RecordGraphicsModified(true);
                        }
                    }
                }
            }
        }

    }
}
