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
    internal class Roads_Placement : ICommand
    {
        public static double CentralVal = 0;
        public static double VerticalCentralVal = 0;
        public static Polyline bp = null;
        public static List<Polyline> polylines = new List<Polyline>();
        public void Execute(object parameter)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            doc.LockDocument();
            List<Polyline> selectedPolylines = new List<Polyline>();
            List<List<Point3d>> pnts = new List<List<Point3d>>();

            List<Line> roadLines = new List<Line>();

            double RoadWidth = 2;
            double offsetDistance = RoadWidth / 2;

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

                Polyline polylineOffset = tr.GetObject(res.ObjectId, OpenMode.ForRead) as Polyline;
                bp = polylineOffset;


                Point3d nextPoint;
                do
                {
                    Point3d startPoint = ed.GetPoint("\nSelect first point: ").Value;


                    if (startPoint == null) return;

                    PromptPointResult result = ed.GetPoint("\nSelect next point (or press Enter to finish): ");

                    if (result.Status != PromptStatus.OK) break;
                    nextPoint = result.Value;
                    Line line = new Line(startPoint, nextPoint);
                    space.AppendEntity(line);
                    tr.AddNewlyCreatedDBObject(line, true);
                    roadLines.Add(line);

                } while (true);

                tr.Commit();
            }

            List<List<Line>> comb = new List<List<Line>>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord space = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                Entity ent = tr.GetObject(bp.ObjectId, OpenMode.ForRead) as Entity;

                if (ent != null && ent is Polyline)
                {
                    Polyline polyline = (Polyline)ent;
                    Extents3d ext = ent.GeometricExtents;

                    foreach (Line l in roadLines)
                    {
                        List<Line> offsetLines = RoadsCreation(ext, space, tr, bp, db, l, RoadWidth);
                        comb.Add(offsetLines);
                    }
                }

                tr.Commit();
            }


            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                for (int i = roadLines.Count - 1; i >= 0; i--) // Loop in reverse to prevent index shifting
                {
                    Line l = roadLines[i];

                    if (l.ObjectId.IsValid)
                    {
                        Line line = tr.GetObject(l.ObjectId, OpenMode.ForWrite) as Line;
                        if (line != null)
                        {
                            line.Erase();
                        }
                    }
                }
                tr.Commit();
            }

            roadLines.Clear();
        }

        private static bool IsPointInsidePolyline(Polyline poly, Point3d point)
        {
            return poly.GetClosestPointTo(point, false).DistanceTo(point) < 0.01;
        }
        private static void LoadLinetype(Database db, Transaction tr, string linetypeName)
        {
            LinetypeTable linetypeTable = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);

            if (!linetypeTable.Has(linetypeName))
            {
                db.LoadLineTypeFile(linetypeName, "acad.lin");
            }
        }
        public static List<Line> RoadsCreation(Extents3d ext, BlockTableRecord space, Transaction tr, Polyline poly, Database db, Line ln, double offsetDistance)
        {
            List<Line> lines = new List<Line>();

            List<Line> lns = new List<Line> { };


            DBObjectCollection offsetCurvesLeft = ln.GetOffsetCurves(offsetDistance);
            DBObjectCollection offsetCurvesRight = ln.GetOffsetCurves(-offsetDistance);

            Line left = AddOffsetLinesToModelSpace(space, tr, offsetCurvesLeft);
            Line right = AddOffsetLinesToModelSpace(space, tr, offsetCurvesRight);

            Point3d min = ext.MinPoint;
            Point3d max = ext.MaxPoint;

            Point2d l1start = new Point2d();
            Point2d l1end = new Point2d();

            Point2d l2start = new Point2d();
            Point2d l2end = new Point2d();

            Point2d l3start = new Point2d();
            Point2d l3end = new Point2d();

            try
            {
                Line line = new Line(new Point3d(ln.StartPoint.X, ln.StartPoint.Y, min.Z), new Point3d(ln.EndPoint.X, ln.EndPoint.Y, min.Z));
                // if (line == null) return;
                Point3dCollection intersectionPoints = new Point3dCollection();
                line.IntersectWith(poly, Intersect.ExtendThis, intersectionPoints, IntPtr.Zero, IntPtr.Zero);
                List<Point3d> sortedPoints = new List<Point3d>(intersectionPoints.Cast<Point3d>());
                sortedPoints.Sort((p1, p2) => p1.DistanceTo(line.StartPoint).CompareTo(p2.DistanceTo(line.StartPoint)));
                List<Line> newSegments = new List<Line>();
                bool inside = IsPointInsidePolyline(poly, ln.StartPoint);
                Point3d p1 = sortedPoints[0];
                Point3d p2 = sortedPoints[1];
                Line lineTrimmed = new Line(p1, p2);
                lineTrimmed.SetDatabaseDefaults();
                lineTrimmed.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1);
                LoadLinetype(db, tr, "DASHED");
                lineTrimmed.Linetype = "DASHED";
                lineTrimmed.LinetypeScale = 0.5;
                space.AppendEntity(lineTrimmed);
                //lns.Add(lineTrimmed);
                tr.AddNewlyCreatedDBObject(lineTrimmed, true);
                lns.Add(lineTrimmed);
                // l1start.X = 0; l1start.Y = 0;

                l1start = new Point2d(lineTrimmed.StartPoint.X, lineTrimmed.StartPoint.Y);
                l1end = new Point2d(lineTrimmed.EndPoint.X, lineTrimmed.EndPoint.Y);

                space.AppendEntity(line);
                tr.AddNewlyCreatedDBObject(line, true);
                line.Erase();
            }
            catch (System.ArgumentOutOfRangeException ex)
            {

            }

            try
            {
                Line line = new Line(new Point3d(left.StartPoint.X, left.StartPoint.Y, min.Z), new Point3d(left.EndPoint.X, left.EndPoint.Y, min.Z));
                //if (line == null) return;
                Point3dCollection intersectionPoints = new Point3dCollection();
                line.IntersectWith(poly, Intersect.ExtendThis, intersectionPoints, IntPtr.Zero, IntPtr.Zero);
                List<Point3d> sortedPoints = new List<Point3d>(intersectionPoints.Cast<Point3d>());
                sortedPoints.Sort((p1, p2) => p1.DistanceTo(line.StartPoint).CompareTo(p2.DistanceTo(line.StartPoint)));
                List<Line> newSegments = new List<Line>();
                bool inside = IsPointInsidePolyline(poly, ln.StartPoint);
                Point3d p1 = sortedPoints[0];
                Point3d p2 = sortedPoints[1];
                Line lineTrimmed = new Line(p1, p2);
                lineTrimmed.SetDatabaseDefaults();
                lineTrimmed.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1);
                lineTrimmed.LinetypeScale = 0.5;
                space.AppendEntity(lineTrimmed);
                tr.AddNewlyCreatedDBObject(lineTrimmed, true);
                lns.Add(lineTrimmed);

                l2start = new Point2d(lineTrimmed.StartPoint.X, lineTrimmed.StartPoint.Y);
                l2end = new Point2d(lineTrimmed.EndPoint.X, lineTrimmed.EndPoint.Y);

                space.AppendEntity(line);
                tr.AddNewlyCreatedDBObject(line, true);
                line.Erase();
                lines.Add(lineTrimmed);
            }
            catch (System.ArgumentOutOfRangeException ex)
            {

            }

            try
            {
                Line line = new Line(new Point3d(right.StartPoint.X, right.StartPoint.Y, min.Z), new Point3d(right.EndPoint.X, right.EndPoint.Y, min.Z));
                // if (line == null) return;
                Point3dCollection intersectionPoints = new Point3dCollection();
                line.IntersectWith(poly, Intersect.ExtendThis, intersectionPoints, IntPtr.Zero, IntPtr.Zero);
                List<Point3d> sortedPoints = new List<Point3d>(intersectionPoints.Cast<Point3d>());
                sortedPoints.Sort((p1, p2) => p1.DistanceTo(line.StartPoint).CompareTo(p2.DistanceTo(line.StartPoint)));
                List<Line> newSegments = new List<Line>();
                bool inside = IsPointInsidePolyline(poly, ln.StartPoint);
                Point3d p1 = sortedPoints[0];
                Point3d p2 = sortedPoints[1];
                Line lineTrimmed = new Line(p1, p2);
                lineTrimmed.SetDatabaseDefaults();
                lineTrimmed.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1);
                lineTrimmed.LinetypeScale = 0.5;
                space.AppendEntity(lineTrimmed);
                tr.AddNewlyCreatedDBObject(lineTrimmed, true);
                lns.Add(lineTrimmed);

                l3start = new Point2d(lineTrimmed.StartPoint.X, lineTrimmed.StartPoint.Y);
                l3end = new Point2d(lineTrimmed.EndPoint.X, lineTrimmed.EndPoint.Y);

                space.AppendEntity(line);
                tr.AddNewlyCreatedDBObject(line, true);
                line.Erase();
                lines.Add(lineTrimmed);
            }
            catch (System.ArgumentOutOfRangeException ex)
            {

            }

            List<Point2d> polylinePoints = new List<Point2d>();
            polylinePoints.Add(l2start);
            polylinePoints.Add(l1start);
            polylinePoints.Add(l3start);
            polylinePoints.Add(l3end);
            polylinePoints.Add(l1end);
            polylinePoints.Add(l2end);

            using (Polyline pline = new Polyline())
            {
                for (int i = 0; i < polylinePoints.Count; i++)
                {
                    pline.AddVertexAt(i, polylinePoints[i], 0, 0, 0);
                }

                pline.SetDatabaseDefaults();
                pline.Closed = true;
                pline.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1);
                pline.LinetypeScale = 0.5;

                space.AppendEntity(pline);
                tr.AddNewlyCreatedDBObject(pline, true);
            }

            left.Erase();
            right.Erase();

            return lines;
        }
        public static Line AddOffsetLinesToModelSpace(BlockTableRecord btr, Transaction tr, DBObjectCollection curves)
        {
            foreach (DBObject obj in curves)
            {
                if (obj is Curve curve)
                {
                    btr.AppendEntity(curve);
                    tr.AddNewlyCreatedDBObject(curve, true);

                    // If it's already a Line, return it
                    if (curve is Line line)
                    {
                        return line;
                    }
                    // If it's a Polyline with two vertices, convert it to a Line
                    else if (curve is Polyline polyline && polyline.NumberOfVertices == 2)
                    {
                        Point3d start = polyline.GetPoint3dAt(0);
                        Point3d end = polyline.GetPoint3dAt(1);
                        return new Line(start, end);
                    }
                    // If it's an Arc, approximate it with a Line from StartPoint to EndPoint
                    else if (curve is Arc arc)
                    {
                        return new Line(arc.StartPoint, arc.EndPoint);
                    }
                }
            }

            return null; // Return null if no valid Line was created
        }
                
        public event EventHandler CanExecuteChanged; 
        public bool CanExecute(object parameter)
        {
            return true;
        }
    }
}