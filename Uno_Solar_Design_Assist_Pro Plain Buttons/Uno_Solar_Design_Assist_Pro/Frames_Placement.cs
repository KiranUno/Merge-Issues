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
    internal class Frames_Placement : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public static double CentralVal = 0;
        public static double VerticalCentralVal = 0;
        public static Polyline bp = null;

        public void Execute(object parameter)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            doc.LockDocument();
            List<Polyline> selectedPolylines = new List<Polyline>();
            List<List<Point3d>> pnts = new List<List<Point3d>>();

            Polyline offPolyline = null;
            double THeight = 0;
            double TWidth = 0;
            double HOffset = 0;
            double VOffset = 0;
            double BOffset = 0;
            double RoadWidth = 0;
            double TPitch = 0;
            Frames form = new Frames();
            form.ShowDialog();

            BOffset = form.BorderOffset;
            THeight = form.TableHeight;
            TWidth = form.TableWidth;
            HOffset = form.HorizontalOffset;
            VOffset = form.VerticalOffset;
            RoadWidth = form.RoadWidth;
            TPitch = form.TablePitch;
            VOffset = TPitch - THeight;
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

                //Road creation vertically

                PromptPointResult p1Result = ed.GetPoint("\nSpecify first point for vertical center reference line: ");
                if (p1Result.Status != PromptStatus.OK) return;
                PromptPointOptions p2Options = new PromptPointOptions("\nSpecify second point for vertical center reference line: ")
                {
                    BasePoint = p1Result.Value,
                    UseBasePoint = true
                };

                PromptPointResult p2Result = ed.GetPoint(p2Options);
                if (p2Result.Status != PromptStatus.OK) return;

                double offsetDistance = RoadWidth / 2;

                Line refLine = new Line(p1Result.Value, p2Result.Value);
                //  space.AppendEntity(refLine);
                refLine.SetDatabaseDefaults();
                refLine.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1); // Red color

                LoadLinetype(db, tr, "DASHED");

                refLine.Linetype = "DASHED";  // You can also use "DOT"
                refLine.LinetypeScale = 0.5;  // Adjust scale to make dashes visible

                CentralVal = p1Result.Value.X;

                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                space.AppendEntity(refLine);
                tr.AddNewlyCreatedDBObject(refLine, true);

                Polyline polylineOffset = tr.GetObject(res.ObjectId, OpenMode.ForRead) as Polyline;
                Polyline OffSet = null;

                if (polylineOffset != null)
                {
                    DBObjectCollection offsetCurves = polylineOffset.GetOffsetCurves(offsetDistance);

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

                if (res.Status == PromptStatus.OK)
                {
                    Entity ent = tr.GetObject(OffSet.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent != null && ent is Polyline)
                    {
                        Polyline polyline = (Polyline)ent;
                        Extents3d ext = ent.GeometricExtents;
                        CreateLines(ext, space, tr, polyline, db, OffSet, THeight, TWidth, HOffset, VOffset, BOffset, CentralVal, RoadWidth);
                    }

                }
                offPolyline = polylineOffset;
                refLine.Erase();
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

            //Removing small blocks

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
                    Polyline selectedPolyline = tr.GetObject(offPolyline.ObjectId, OpenMode.ForWrite) as Polyline;
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

            Polyline borderPolyline = null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    Polyline selectedPolyline = tr.GetObject(offPolyline.ObjectId, OpenMode.ForWrite) as Polyline;
                    if (selectedPolyline != null)
                    {
                        double newZValue = 0;
                        selectedPolyline.Elevation = newZValue;

                    }
                    tr.Commit();
                    borderPolyline = selectedPolyline;
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\nError: {ex.Message}");
                }
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    Polyline selectedPolyline = borderPolyline;
                    if (selectedPolyline != null)
                    {
                        double length = selectedPolyline.Length;
                        double segmentLength = length / 300.0;
                        List<Point3d> points = new List<Point3d>();
                        for (int i = 0; i < 300; i++)
                        {
                            Point3d pt = selectedPolyline.GetPointAtDist(i * segmentLength);
                            points.Add(new Point3d(pt.X, pt.Y, 0)); // Flatten Z
                        }
                        points.Add(new Point3d(selectedPolyline.StartPoint.X, selectedPolyline.StartPoint.Y, 0));


                        PromptSelectionResult s = ed.SelectFence(new Point3dCollection(points.ToArray()),
                            new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, "INSERT") }));

                        if (s.Status == PromptStatus.OK && s.Value != null)
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
                        else
                        {
                            ed.WriteMessage("\nNo objects selected.");
                        }
                    }

                    tr.Commit();
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\nError: {ex.Message}");
                }
            }
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        private static bool IsPointInsidePolyline(Polyline poly, Point3d point)
        {
            return poly.GetClosestPointTo(point, false).DistanceTo(point) < 0.01;
        }
        public static void CreateLines(Extents3d ext, BlockTableRecord space, Transaction tr, Polyline poly, Database db, Polyline OffSet, double THeight, double TWidth,
            double HOffset, double VOffset, double BOffset, double CenterVal, double RoadWidth)
        {

            Point3d min = ext.MinPoint;
            Point3d max = ext.MaxPoint;
            double xdist = max.X - min.X;
            double ydist = max.Y - min.Y;
            ydist = ydist - BOffset;
            double maxY = max.Y - (BOffset + THeight);

            double lineOffset = 0;
            try
            {
                if (xdist > 0 && ydist > 0)
                {
                    while (ydist > (VOffset + THeight + BOffset))
                    {
                        maxY = maxY - THeight - lineOffset;
                        Line newLine = new Line(new Point3d(min.X, maxY, min.Z), new Point3d(max.X, maxY, min.Z));
                        space.AppendEntity(newLine);
                        tr.AddNewlyCreatedDBObject(newLine, true);
                        Line line = tr.GetObject(newLine.ObjectId, OpenMode.ForWrite) as Line;
                        if (line == null) return;
                        Point3dCollection intersectionPoints = new Point3dCollection();
                        line.IntersectWith(poly, Intersect.ExtendThis, intersectionPoints, IntPtr.Zero, IntPtr.Zero);
                        List<Point3d> sortedPoints = new List<Point3d>(intersectionPoints.Cast<Point3d>());
                        sortedPoints.Sort((p1, p2) => p1.DistanceTo(line.StartPoint).CompareTo(p2.DistanceTo(line.StartPoint)));
                        List<Line> newSegments = new List<Line>();
                        bool inside = IsPointInsidePolyline(poly, line.StartPoint);
                        Point3d p1 = sortedPoints[0];
                        Point3d p2 = sortedPoints[1];
                        PlacePanelsOnLine(p1, p2, tr, db, OffSet, THeight, TWidth, HOffset, VOffset, BOffset, CenterVal, RoadWidth);
                        PlacePanelsOnLineToLeft(p1, p2, tr, db, OffSet, THeight, TWidth, HOffset, VOffset, BOffset, CenterVal, RoadWidth);
                        Line lineTrimmed = new Line(p1, p2);
                        space.AppendEntity(lineTrimmed);
                        tr.AddNewlyCreatedDBObject(lineTrimmed, true);
                        newLine.Erase();
                        lineTrimmed.Erase();
                        ydist = ydist - (VOffset + THeight);
                        lineOffset = VOffset;
                    }
                }
            }
            catch (System.ArgumentOutOfRangeException ex)
            {

            }

        }


        public static void PlacePanelsOnLine(Point3d p1, Point3d p2, Transaction tr, Database db, Polyline OffSet, double THeight, double TWidth,
            double HOffset, double VOffset, double BOffset, double CenterVal, double RoadWidth)
        {
            BlockTable acBlkTbl = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
            double xdist = p2.X - CenterVal;
            double blockStartX = CenterVal + RoadWidth / 2 + 0.5;
            xdist = xdist - RoadWidth / 2 - 0.5;
            double blockStartY = p1.Y;
            BlockReference copy = null;

            double tempHOffset = HOffset;
            int tables = 0;

            if (CenterVal > p1.X)
            {
                while (xdist >= (BOffset + TWidth + HOffset) || xdist >= (BOffset + (TWidth / 2) + HOffset))
                {
                    if (xdist >= (BOffset + TWidth + HOffset))
                    {
                        ObjectId blkRecId = ObjectId.Null;
                        using (BlockTableRecord acBlkTblRec = new BlockTableRecord())
                        {
                            acBlkTblRec.Origin = new Point3d(blockStartX, blockStartY, p1.Z);

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
                                using (BlockReference acBlkRef = new BlockReference(new Point3d(blockStartX, blockStartY, 585.1255), blkRecId))
                                {
                                    BlockTableRecord acCurSpaceBlkTblRec = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                    acCurSpaceBlkTblRec.AppendEntity(acBlkRef);
                                    tr.AddNewlyCreatedDBObject(acBlkRef, true);
                                    acBlkRef.Position = new Point3d(blockStartX, blockStartY, 585.1255);
                                    acBlkRef.RecordGraphicsModified(true);

                                    copy = acBlkRef;
                                }
                            }
                        }

                        if (tables % 2 == 0)
                        {
                            xdist = xdist - TWidth - HOffset;
                            blockStartX = blockStartX + TWidth + HOffset;
                        }
                        else
                        {
                            xdist = xdist - TWidth - 2.1;
                            blockStartX = blockStartX + TWidth + 2.1;
                        }
                    }
                    else if (xdist >= (BOffset + (TWidth / 2) + HOffset))
                    {
                        ObjectId blkRecId = ObjectId.Null;
                        using (BlockTableRecord acBlkTblRec = new BlockTableRecord())
                        {
                            acBlkTblRec.Origin = new Point3d(blockStartX, blockStartY, p1.Z);

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
                                using (BlockReference acBlkRef = new BlockReference(new Point3d(blockStartX, blockStartY, 585.1255), blkRecId))
                                {
                                    BlockTableRecord acCurSpaceBlkTblRec = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                    acCurSpaceBlkTblRec.AppendEntity(acBlkRef);
                                    tr.AddNewlyCreatedDBObject(acBlkRef, true);
                                    acBlkRef.Position = new Point3d(blockStartX, blockStartY, 585.1255);
                                    acBlkRef.RecordGraphicsModified(true);

                                    copy = acBlkRef;
                                }
                            }
                        }
                        xdist = xdist - (TWidth / 2) - HOffset;
                        blockStartX = blockStartX + (TWidth / 2) + HOffset;
                    }
                    tables++;
                }
            }
            else
            {
                double gap = p1.X - CenterVal;
                double forward = gap / (TWidth + HOffset);
                int tableNo = 0;

                if (forward < 1)
                {
                    xdist = xdist - (BOffset + TWidth + HOffset);
                    blockStartX = blockStartX + (TWidth + HOffset);
                    while (xdist >= (BOffset + TWidth + HOffset) || xdist >= (BOffset + (TWidth / 2) + HOffset))
                    {
                        if (xdist >= (BOffset + TWidth + HOffset))
                        {
                            ObjectId blkRecId = ObjectId.Null;
                            using (BlockTableRecord acBlkTblRec = new BlockTableRecord())
                            {
                                acBlkTblRec.Origin = new Point3d(blockStartX, blockStartY, p1.Z); // Set origin with Z value

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
                                    using (BlockReference acBlkRef = new BlockReference(new Point3d(blockStartX, blockStartY, 585.1255), blkRecId))
                                    {
                                        BlockTableRecord acCurSpaceBlkTblRec = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                        acCurSpaceBlkTblRec.AppendEntity(acBlkRef);
                                        tr.AddNewlyCreatedDBObject(acBlkRef, true);
                                        acBlkRef.Position = new Point3d(blockStartX, blockStartY, 585.1255);
                                        acBlkRef.RecordGraphicsModified(true);

                                        copy = acBlkRef;
                                    }
                                }
                            }

                            if (tableNo % 2 != 0)
                            {
                                xdist = xdist - TWidth - HOffset;
                                blockStartX = blockStartX + TWidth + HOffset;
                            }
                            else
                            {
                                xdist = xdist - TWidth - 2.1;
                                blockStartX = blockStartX + TWidth + 2.1;
                            }
                        }
                        else if (xdist >= (BOffset + (TWidth / 2) + HOffset))
                        {
                            ObjectId blkRecId = ObjectId.Null;
                            using (BlockTableRecord acBlkTblRec = new BlockTableRecord())
                            {
                                acBlkTblRec.Origin = new Point3d(blockStartX, blockStartY, p1.Z); // Set origin with Z value

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
                                    using (BlockReference acBlkRef = new BlockReference(new Point3d(blockStartX, blockStartY, 585.1255), blkRecId))
                                    {
                                        BlockTableRecord acCurSpaceBlkTblRec = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                        acCurSpaceBlkTblRec.AppendEntity(acBlkRef);
                                        tr.AddNewlyCreatedDBObject(acBlkRef, true);
                                        acBlkRef.Position = new Point3d(blockStartX, blockStartY, 585.1255);
                                        acBlkRef.RecordGraphicsModified(true);

                                        copy = acBlkRef;
                                    }
                                }
                            }
                            xdist = xdist - TWidth / 2 - HOffset;
                            blockStartX = blockStartX + TWidth / 2 + HOffset;
                        }
                        tableNo++;
                    }
                    tables++;
                }
                else
                {
                    int result = (int)forward + 1;

                    gap = p1.X - CenterVal;
                    forward = gap / TWidth;
                    int f = ((int)(forward)) + 1;

                    if (f % 2 == 0)
                    {
                        blockStartX = blockStartX + f * TWidth + (f / 2) * HOffset + (f / 2) * 2.1;
                        xdist = p2.X - blockStartX;
                    }
                    else
                    {
                        blockStartX = blockStartX + f * TWidth + ((f / 2) + 1) * HOffset + (f / 2) * 2.1;
                        xdist = p2.X - blockStartX;
                    }


                    while (xdist >= (BOffset + TWidth + HOffset) || xdist >= (BOffset + (TWidth / 2) + HOffset))
                    {
                        if (xdist >= (BOffset + TWidth + HOffset))
                        {
                            ObjectId blkRecId = ObjectId.Null;
                            using (BlockTableRecord acBlkTblRec = new BlockTableRecord())
                            {
                                acBlkTblRec.Origin = new Point3d(blockStartX, blockStartY, p1.Z); // Set origin with Z value

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
                                    using (BlockReference acBlkRef = new BlockReference(new Point3d(blockStartX, blockStartY, 585.1255), blkRecId))
                                    {
                                        BlockTableRecord acCurSpaceBlkTblRec = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                        acCurSpaceBlkTblRec.AppendEntity(acBlkRef);
                                        tr.AddNewlyCreatedDBObject(acBlkRef, true);
                                        acBlkRef.Position = new Point3d(blockStartX, blockStartY, 585.1255);
                                        acBlkRef.RecordGraphicsModified(true);

                                        copy = acBlkRef;
                                    }
                                }
                            }

                            if (result % 2 == 0)
                            {

                                xdist = xdist - TWidth - HOffset;
                                blockStartX = blockStartX + TWidth + HOffset;
                            }
                            else
                            {
                                xdist = xdist - TWidth - 2.1;
                                blockStartX = blockStartX + TWidth + 2.1;
                            }
                        }
                        else if (xdist >= (BOffset + (TWidth / 2) + HOffset))
                        {
                            ObjectId blkRecId = ObjectId.Null;
                            using (BlockTableRecord acBlkTblRec = new BlockTableRecord())
                            {
                                acBlkTblRec.Origin = new Point3d(blockStartX, blockStartY, p1.Z); // Set origin with Z value

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
                                    using (BlockReference acBlkRef = new BlockReference(new Point3d(blockStartX, blockStartY, 585.1255), blkRecId))
                                    {
                                        BlockTableRecord acCurSpaceBlkTblRec = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                        acCurSpaceBlkTblRec.AppendEntity(acBlkRef);
                                        tr.AddNewlyCreatedDBObject(acBlkRef, true);
                                        acBlkRef.Position = new Point3d(blockStartX, blockStartY, 585.1255);
                                        acBlkRef.RecordGraphicsModified(true);

                                        copy = acBlkRef;
                                    }
                                }
                            }
                            xdist = xdist - TWidth / 2 - HOffset;
                            blockStartX = blockStartX + TWidth / 2 + HOffset;
                        }
                        tables++;
                        result++;
                    }
                }
            }

        }

        public static bool IntersectsBlock(Polyline polyline, BlockReference blockRef, Transaction tr)
        {

            Extents3d extents;
            try
            {
                extents = blockRef.GeometricExtents;
            }
            catch
            {
                return false;
            }

            if (!IntersectsBoundingBox(polyline, extents))
                return false;
            BlockTableRecord blockDef = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;

            if (blockDef == null)
                return false;

            foreach (ObjectId objId in blockDef)
            {
                Entity ent = tr.GetObject(objId, OpenMode.ForRead) as Entity;
                if (ent is Curve curveEntity)
                {
                    if (Intersects(polyline, curveEntity))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        public static bool IntersectsBoundingBox(Polyline polyline, Extents3d extents)
        {
            Polyline bbox = new Polyline();
            bbox.AddVertexAt(0, new Point2d(extents.MinPoint.X, extents.MinPoint.Y), 0, 0, 0);
            bbox.AddVertexAt(1, new Point2d(extents.MaxPoint.X, extents.MinPoint.Y), 0, 0, 0);
            bbox.AddVertexAt(2, new Point2d(extents.MaxPoint.X, extents.MaxPoint.Y), 0, 0, 0);
            bbox.AddVertexAt(3, new Point2d(extents.MinPoint.X, extents.MaxPoint.Y), 0, 0, 0);
            bbox.Closed = true;

            return Intersects(polyline, bbox);
        }
        public static bool Intersects(Curve polyline, Curve entity)
        {
            Point3dCollection intersectionPoints = new Point3dCollection();
            polyline.IntersectWith(entity, Intersect.OnBothOperands, intersectionPoints, IntPtr.Zero, IntPtr.Zero);
            return intersectionPoints.Count > 0;
        }
        public static bool isTouchingPolyline(Polyline p, BlockReference block)
        {
            if (p == null || block == null) return false;

            try
            {
                Point3dCollection intersectionPoints = new Point3dCollection();
                using (Transaction tr = p.Database.TransactionManager.StartTransaction())
                {
                    BlockReference blk = tr.GetObject(block.ObjectId, OpenMode.ForRead) as BlockReference;
                    Polyline pline = tr.GetObject(p.ObjectId, OpenMode.ForRead) as Polyline;

                    if (blk == null || pline == null) return false;

                    if (intersectionPoints.Count > 0)
                    {
                        List<Point3d> sortedPoints = new List<Point3d>(intersectionPoints.Cast<Point3d>());
                        sortedPoints.Sort((p1, p2) => p1.DistanceTo(blk.Position).CompareTo(p2.DistanceTo(blk.Position)));
                        return true;
                    }
                }
            }
            catch (System.AccessViolationException ex)
            {
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(
                    "\nAccess violation error: " + ex.Message);
                return false;
            }
            return false;
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

        public static void PlacePanelsOnLineToLeft(Point3d p1, Point3d p2, Transaction tr, Database db, Polyline OffSet, double THeight, double TWidth,
            double HOffset, double VOffset, double BOffset, double CenterVal, double RoadWidth)
        {
            BlockTable acBlkTbl = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
            double xdist = CenterVal - p1.X;
            double BlockDistance = 1;
            double blockStartX = CenterVal - RoadWidth / 2 - 0.5;
            xdist = xdist - RoadWidth / 2 - 0.5;
            double blockStartY = p1.Y;
            BlockReference copy = null;

            int num = 1;

            if (CenterVal > p1.X)
            {
                while ((xdist >= (BOffset + TWidth + HOffset) || (xdist >= (BOffset + (TWidth / 2) + HOffset)) && (p2.X - (CenterVal + BOffset) > BOffset)))
                {
                    if (xdist >= (BOffset + TWidth + HOffset))
                    {
                        ObjectId blkRecId = ObjectId.Null;
                        using (BlockTableRecord acBlkTblRec = new BlockTableRecord())
                        {
                            acBlkTblRec.Origin = new Point3d(blockStartX, blockStartY, p1.Z); // Set origin with Z value

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
                                using (BlockReference acBlkRef = new BlockReference(new Point3d(blockStartX, blockStartY, 585.1255), blkRecId))
                                {
                                    BlockTableRecord acCurSpaceBlkTblRec = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                    acCurSpaceBlkTblRec.AppendEntity(acBlkRef);
                                    tr.AddNewlyCreatedDBObject(acBlkRef, true);
                                    acBlkRef.Position = new Point3d(blockStartX, blockStartY, 585.1255);
                                    acBlkRef.RecordGraphicsModified(true);

                                    copy = acBlkRef;
                                }
                            }
                        }

                        if (num % 2 == 0)
                        {
                            xdist = xdist - TWidth - 2.1;
                            blockStartX = blockStartX - TWidth - 2.1;
                        }
                        else
                        {
                            xdist = xdist - TWidth - HOffset;
                            blockStartX = blockStartX - TWidth - HOffset;
                        }
                    }
                    else if (xdist >= (BOffset + (TWidth / 2) + HOffset))
                    {
                        ObjectId blkRecId = ObjectId.Null;
                        using (BlockTableRecord acBlkTblRec = new BlockTableRecord())
                        {
                            acBlkTblRec.Origin = new Point3d(blockStartX, blockStartY, p1.Z); // Set origin with Z value

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
                                using (BlockReference acBlkRef = new BlockReference(new Point3d(blockStartX, blockStartY, 585.1255), blkRecId))
                                {
                                    BlockTableRecord acCurSpaceBlkTblRec = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                    acCurSpaceBlkTblRec.AppendEntity(acBlkRef);
                                    tr.AddNewlyCreatedDBObject(acBlkRef, true);
                                    acBlkRef.Position = new Point3d(blockStartX, blockStartY, 585.1255);
                                    acBlkRef.RecordGraphicsModified(true);

                                    copy = acBlkRef;
                                }
                            }
                        }
                        xdist = xdist - (TWidth / 2) - HOffset;
                        blockStartX = blockStartX - (TWidth / 2) - HOffset;


                    }

                    num++;
                }
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
                    acBlkTblRec.Origin = new Point3d(minPoint.X, minPoint.Y, minPoint.Z);

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
                        using (BlockReference acBlkRef = new BlockReference(new Point3d(minPoint.X, minPoint.Y, 585.1255), blkRecId))
                        {
                            BlockTableRecord acCurSpaceBlkTblRec = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                            acCurSpaceBlkTblRec.AppendEntity(acBlkRef);
                            tr.AddNewlyCreatedDBObject(acBlkRef, true);
                            acBlkRef.Position = new Point3d(minPoint.X, minPoint.Y, 585.1255);
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
                    acBlkTblRec.Origin = new Point3d(minPoint.X, minPoint.Y, minPoint.Z);

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
                            acBlkRef.Position = new Point3d(minPoint.X, minPoint.Y, 585.1255);
                            acBlkRef.RecordGraphicsModified(true);
                        }
                    }
                }
            }
        }

        public static void VerticalRoadCreation(Polyline polyline, Editor ed, Database db, Transaction tr, double RoadWidth, BlockTableRecord space)
        {
            PromptPointResult p1Result = ed.GetPoint("\nSpecify first point for vertical road reference line: ");
            if (p1Result.Status != PromptStatus.OK) return;
            PromptPointOptions p2Options = new PromptPointOptions("\nSpecify second point for vertical road reference line: ")
            {
                BasePoint = p1Result.Value,
                UseBasePoint = true
            };

            PromptPointResult p2Result = ed.GetPoint(p2Options);
            if (p2Result.Status != PromptStatus.OK) return;

            double offsetDistance = RoadWidth / 2;

            Line refLine = new Line(p1Result.Value, p2Result.Value);
            //  space.AppendEntity(refLine);
            refLine.SetDatabaseDefaults();
            refLine.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1); // Red color

            LoadLinetype(db, tr, "DASHED");

            refLine.Linetype = "DASHED";  // You can also use "DOT"
            refLine.LinetypeScale = 0.5;  // Adjust scale to make dashes visible

            CentralVal = p1Result.Value.X;

            BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
            BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

            space.AppendEntity(refLine);
            tr.AddNewlyCreatedDBObject(refLine, true);

            TrimLine(polyline, db, tr, refLine);

            Polyline polyRefLine = new Polyline();
            polyRefLine.AddVertexAt(0, new Point2d(p1Result.Value.X, p1Result.Value.Y), 0, 0, 0);
            polyRefLine.AddVertexAt(1, new Point2d(p2Result.Value.X, p2Result.Value.Y), 0, 0, 0);
            polyRefLine.SetDatabaseDefaults();

            DBObjectCollection offsetLinesLeft = polyRefLine.GetOffsetCurves(offsetDistance);
            DBObjectCollection offsetLinesRight = polyRefLine.GetOffsetCurves(-offsetDistance);

            foreach (Entity ent in offsetLinesLeft)
            {
                Polyline leftPolyline = ent as Polyline;
                leftPolyline.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1);
                space.AppendEntity(leftPolyline);
                tr.AddNewlyCreatedDBObject(leftPolyline, true);
            }

            foreach (Entity ent in offsetLinesRight)
            {
                Polyline rightPolyline = ent as Polyline;
                rightPolyline.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1);
                space.AppendEntity(rightPolyline);
                tr.AddNewlyCreatedDBObject(rightPolyline, true);
            }
            refLine.Erase();
        }

        private static void LoadLinetype(Database db, Transaction tr, string linetypeName)
        {
            LinetypeTable linetypeTable = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);

            if (!linetypeTable.Has(linetypeName))
            {
                db.LoadLineTypeFile(linetypeName, "acad.lin");
            }
        }


        public static void TrimLine(Polyline poly, Database db, Transaction tr, Line line)
        {
            if (line == null) return;
            Point3dCollection intersectionPoints = new Point3dCollection();
            line.IntersectWith(poly, Intersect.ExtendThis, intersectionPoints, IntPtr.Zero, IntPtr.Zero);
            List<Point3d> sortedPoints = new List<Point3d>(intersectionPoints.Cast<Point3d>());
            sortedPoints.Sort((p1, p2) => p1.DistanceTo(line.StartPoint).CompareTo(p2.DistanceTo(line.StartPoint)));
            List<Line> newSegments = new List<Line>();
            bool inside = IsPointInsidePolyline(poly, line.StartPoint);
            Point3d p1 = sortedPoints[0];
            Point3d p2 = sortedPoints[1];
            Line lineTrimmed = new Line(p1, p2);
            LoadLinetype(db, tr, "DASHED");
            lineTrimmed.SetDatabaseDefaults();
            lineTrimmed.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1); // Red color

            lineTrimmed.Linetype = "DASHED";  // You can also use "DOT"
            lineTrimmed.LinetypeScale = 0.5;  // Adjust scale to make dashes visible
            BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
            BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

            btr.AppendEntity(lineTrimmed);
            tr.AddNewlyCreatedDBObject(lineTrimmed, true);
        }

        public static void CreateRoads(BlockTableRecord space, Transaction tr, Polyline poly, Line l)
        {
            Extents3d extents = l.GeometricExtents;
            Point3d min = extents.MinPoint;
            Point3d max = extents.MaxPoint;
            Line newLine = new Line(new Point3d(min.X, max.Y, min.Z), new Point3d(max.X, max.Y, min.Z));
            space.AppendEntity(newLine);
            tr.AddNewlyCreatedDBObject(newLine, true);
            Line line = tr.GetObject(newLine.ObjectId, OpenMode.ForWrite) as Line;
            if (line == null) return;
            Point3dCollection intersectionPoints = new Point3dCollection();
            line.IntersectWith(poly, Intersect.ExtendThis, intersectionPoints, IntPtr.Zero, IntPtr.Zero);
            List<Point3d> sortedPoints = new List<Point3d>(intersectionPoints.Cast<Point3d>());
            sortedPoints.Sort((p1, p2) => p1.DistanceTo(line.StartPoint).CompareTo(p2.DistanceTo(line.StartPoint)));
            List<Line> newSegments = new List<Line>();
            bool inside = IsPointInsidePolyline(poly, line.StartPoint);
            Point3d p1 = sortedPoints[0];
            Point3d p2 = sortedPoints[1];
            Line lineTrimmed = new Line(p1, p2);
            space.AppendEntity(lineTrimmed);
            tr.AddNewlyCreatedDBObject(lineTrimmed, true);
        }

        public static void RoadsCreation(Extents3d ext, BlockTableRecord space, Transaction tr, Polyline poly, Database db, double CentralVal, double RoadWidth)
        {
            Point3d min = ext.MinPoint;
            Point3d max = ext.MaxPoint;
            //Vertical Central line
            try
            {
                Line newLine = new Line(new Point3d(CentralVal, min.Y, min.Z), new Point3d(CentralVal, max.Y, min.Z));
                space.AppendEntity(newLine);
                tr.AddNewlyCreatedDBObject(newLine, true);
                Line line = tr.GetObject(newLine.ObjectId, OpenMode.ForWrite) as Line;
                if (line == null) return;
                Point3dCollection intersectionPoints = new Point3dCollection();
                line.IntersectWith(poly, Intersect.ExtendThis, intersectionPoints, IntPtr.Zero, IntPtr.Zero);
                List<Point3d> sortedPoints = new List<Point3d>(intersectionPoints.Cast<Point3d>());
                sortedPoints.Sort((p1, p2) => p1.DistanceTo(line.StartPoint).CompareTo(p2.DistanceTo(line.StartPoint)));
                List<Line> newSegments = new List<Line>();
                bool inside = IsPointInsidePolyline(poly, line.StartPoint);
                Point3d p1 = sortedPoints[0];
                Point3d p2 = sortedPoints[1];
                Line lineTrimmed = new Line(p1, p2);
                lineTrimmed.SetDatabaseDefaults();
                lineTrimmed.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1);
                LoadLinetype(db, tr, "DASHED");
                lineTrimmed.Linetype = "DASHED";
                lineTrimmed.LinetypeScale = 0.5;
                space.AppendEntity(lineTrimmed);
                tr.AddNewlyCreatedDBObject(lineTrimmed, true);
                newLine.Erase();
            }
            catch (System.ArgumentOutOfRangeException ex)
            {

            }

            //Left offset

            try
            {
                Line newLine = new Line(new Point3d(CentralVal - RoadWidth / 2, min.Y, min.Z), new Point3d(CentralVal - RoadWidth / 2, max.Y, min.Z));
                space.AppendEntity(newLine);
                tr.AddNewlyCreatedDBObject(newLine, true);
                Line line = tr.GetObject(newLine.ObjectId, OpenMode.ForWrite) as Line;
                if (line == null) return;
                Point3dCollection intersectionPoints = new Point3dCollection();
                line.IntersectWith(poly, Intersect.ExtendThis, intersectionPoints, IntPtr.Zero, IntPtr.Zero);
                List<Point3d> sortedPoints = new List<Point3d>(intersectionPoints.Cast<Point3d>());
                sortedPoints.Sort((p1, p2) => p1.DistanceTo(line.StartPoint).CompareTo(p2.DistanceTo(line.StartPoint)));
                List<Line> newSegments = new List<Line>();
                bool inside = IsPointInsidePolyline(poly, line.StartPoint);
                Point3d p1 = sortedPoints[0];
                Point3d p2 = sortedPoints[1];
                Line lineTrimmed = new Line(p1, p2);
                lineTrimmed.SetDatabaseDefaults();
                lineTrimmed.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1);
                lineTrimmed.LinetypeScale = 0.5;
                space.AppendEntity(lineTrimmed);
                tr.AddNewlyCreatedDBObject(lineTrimmed, true);
                newLine.Erase();
            }
            catch (System.ArgumentOutOfRangeException ex)
            {

            }

            //

            //Right Offset

            try
            {
                Line newLine = new Line(new Point3d(CentralVal + RoadWidth / 2, min.Y, min.Z), new Point3d(CentralVal + RoadWidth / 2, max.Y, min.Z));
                space.AppendEntity(newLine);
                tr.AddNewlyCreatedDBObject(newLine, true);
                Line line = tr.GetObject(newLine.ObjectId, OpenMode.ForWrite) as Line;
                if (line == null) return;
                Point3dCollection intersectionPoints = new Point3dCollection();
                line.IntersectWith(poly, Intersect.ExtendThis, intersectionPoints, IntPtr.Zero, IntPtr.Zero);
                List<Point3d> sortedPoints = new List<Point3d>(intersectionPoints.Cast<Point3d>());
                sortedPoints.Sort((p1, p2) => p1.DistanceTo(line.StartPoint).CompareTo(p2.DistanceTo(line.StartPoint)));
                List<Line> newSegments = new List<Line>();
                bool inside = IsPointInsidePolyline(poly, line.StartPoint);
                Point3d p1 = sortedPoints[0];
                Point3d p2 = sortedPoints[1];
                Line lineTrimmed = new Line(p1, p2);
                lineTrimmed.SetDatabaseDefaults();
                lineTrimmed.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1);
                lineTrimmed.LinetypeScale = 0.5;
                space.AppendEntity(lineTrimmed);
                tr.AddNewlyCreatedDBObject(lineTrimmed, true);
                newLine.Erase();
            }
            catch (System.ArgumentOutOfRangeException ex)
            {

            }

        }

        public static void RoadsCreationHorizontal(Extents3d ext, BlockTableRecord space, Transaction tr, Polyline poly, Database db, double CentralVal, double RoadWidth)
        {
            Point3d min = ext.MinPoint;
            Point3d max = ext.MaxPoint;
            //Vertical Central line
            try
            {
                Line newLine = new Line(new Point3d(min.X, CentralVal, min.Z), new Point3d(max.X, CentralVal, min.Z));
                space.AppendEntity(newLine);
                tr.AddNewlyCreatedDBObject(newLine, true);
                Line line = tr.GetObject(newLine.ObjectId, OpenMode.ForWrite) as Line;
                if (line == null) return;
                Point3dCollection intersectionPoints = new Point3dCollection();
                line.IntersectWith(poly, Intersect.ExtendThis, intersectionPoints, IntPtr.Zero, IntPtr.Zero);
                List<Point3d> sortedPoints = new List<Point3d>(intersectionPoints.Cast<Point3d>());
                sortedPoints.Sort((p1, p2) => p1.DistanceTo(line.StartPoint).CompareTo(p2.DistanceTo(line.StartPoint)));
                List<Line> newSegments = new List<Line>();
                bool inside = IsPointInsidePolyline(poly, line.StartPoint);
                Point3d p1 = sortedPoints[0];
                Point3d p2 = sortedPoints[1];
                Line lineTrimmed = new Line(p1, p2);
                lineTrimmed.SetDatabaseDefaults();
                lineTrimmed.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1);
                LoadLinetype(db, tr, "DASHED");
                lineTrimmed.Linetype = "DASHED";
                lineTrimmed.LinetypeScale = 0.5;
                space.AppendEntity(lineTrimmed);
                tr.AddNewlyCreatedDBObject(lineTrimmed, true);
                newLine.Erase();
            }
            catch (System.ArgumentOutOfRangeException ex)
            {

            }

            //Left offset

            try
            {
                Line newLine = new Line(new Point3d(min.X, CentralVal - RoadWidth / 2, min.Z), new Point3d(max.X, CentralVal - RoadWidth / 2, min.Z));
                space.AppendEntity(newLine);
                tr.AddNewlyCreatedDBObject(newLine, true);
                Line line = tr.GetObject(newLine.ObjectId, OpenMode.ForWrite) as Line;
                if (line == null) return;
                Point3dCollection intersectionPoints = new Point3dCollection();
                line.IntersectWith(poly, Intersect.ExtendThis, intersectionPoints, IntPtr.Zero, IntPtr.Zero);
                List<Point3d> sortedPoints = new List<Point3d>(intersectionPoints.Cast<Point3d>());
                sortedPoints.Sort((p1, p2) => p1.DistanceTo(line.StartPoint).CompareTo(p2.DistanceTo(line.StartPoint)));
                List<Line> newSegments = new List<Line>();
                bool inside = IsPointInsidePolyline(poly, line.StartPoint);
                Point3d p1 = sortedPoints[0];
                Point3d p2 = sortedPoints[1];
                Line lineTrimmed = new Line(p1, p2);
                lineTrimmed.SetDatabaseDefaults();
                lineTrimmed.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1);
                lineTrimmed.LinetypeScale = 0.5;
                space.AppendEntity(lineTrimmed);
                tr.AddNewlyCreatedDBObject(lineTrimmed, true);
                newLine.Erase();
            }
            catch (System.ArgumentOutOfRangeException ex)
            {

            }

            //

            //Right Offset

            try
            {
                Line newLine = new Line(new Point3d(min.X, CentralVal + RoadWidth / 2, min.Z), new Point3d(max.X, CentralVal + RoadWidth / 2, min.Z));
                space.AppendEntity(newLine);
                tr.AddNewlyCreatedDBObject(newLine, true);
                Line line = tr.GetObject(newLine.ObjectId, OpenMode.ForWrite) as Line;
                if (line == null) return;
                Point3dCollection intersectionPoints = new Point3dCollection();
                line.IntersectWith(poly, Intersect.ExtendThis, intersectionPoints, IntPtr.Zero, IntPtr.Zero);
                List<Point3d> sortedPoints = new List<Point3d>(intersectionPoints.Cast<Point3d>());
                sortedPoints.Sort((p1, p2) => p1.DistanceTo(line.StartPoint).CompareTo(p2.DistanceTo(line.StartPoint)));
                List<Line> newSegments = new List<Line>();
                bool inside = IsPointInsidePolyline(poly, line.StartPoint);
                Point3d p1 = sortedPoints[0];
                Point3d p2 = sortedPoints[1];
                Line lineTrimmed = new Line(p1, p2);
                lineTrimmed.SetDatabaseDefaults();
                lineTrimmed.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1);
                lineTrimmed.LinetypeScale = 0.5;
                space.AppendEntity(lineTrimmed);
                tr.AddNewlyCreatedDBObject(lineTrimmed, true);
                newLine.Erase();
            }
            catch (System.ArgumentOutOfRangeException ex)
            {

            }
        }

    }
}
