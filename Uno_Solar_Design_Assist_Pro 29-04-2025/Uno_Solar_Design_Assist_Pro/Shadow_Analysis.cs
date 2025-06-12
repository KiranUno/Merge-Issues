using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace Uno_Solar_Design_Assist_Pro
{
    internal class Shadow_Analysis : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            Shadow f = new Shadow();
            Application.ShowModelessDialog(Application.MainWindow.Handle, f, false);
        }
        //public void start()
        //{

        //    for (int K = 0; K < 2; K++)
        //    {
        //        if (K == 0)
        //        {
        //            Document doc = Application.DocumentManager.MdiActiveDocument;
        //            Editor ed = doc.Editor;
        //            Database db = doc.Database;
        //            DateTime date = Shadow.datetime1;
        //            double latitude = Shadow.latitude;
        //            double longitude = Shadow.longitude;

        //            using (doc.LockDocument())
        //            {
        //                using (Transaction tr = db.TransactionManager.StartTransaction())
        //                {
        //                    Solid3d solid = tr.GetObject(Shadow.selectedObj, OpenMode.ForRead) as Solid3d;
        //                    Extents3d ext = solid.GeometricExtents;
        //                    Point3d objectStart = ext.MinPoint;
        //                    Point3d objectEnd = ext.MaxPoint;
        //                    double width = (ext.MaxPoint.X - ext.MinPoint.X) / 2;

        //                    Point3d apex = new Point3d(
        //                        (ext.MinPoint.X + ext.MaxPoint.X) / 2,
        //                        (ext.MinPoint.Y + ext.MaxPoint.Y) / 2,
        //                        ext.MaxPoint.Z);

        //                    // Base center
        //                    double radiusX = (ext.MaxPoint.X - ext.MinPoint.X) / 2;
        //                    double radiusY = (ext.MaxPoint.Y - ext.MinPoint.Y) / 2;
        //                    Point3d center = new Point3d(
        //                        (ext.MinPoint.X + ext.MaxPoint.X) / 2,
        //                        (ext.MinPoint.Y + ext.MaxPoint.Y) / 2,
        //                        ext.MinPoint.Z);

        //                    int segments = 30;

        //                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
        //                    BlockTableRecord btr = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

        //                    // From 8 AM to 5 PM
        //                    HashSet<Point3d> shadowPoints = new HashSet<Point3d>();
        //                    for (double hour = 8; hour <= 17; hour++)
        //                    {
        //                        DateTime currentTime = date.AddHours(hour);

        //                        Vector3d sunVector = GetSunDirection(currentTime, latitude);
        //                        Vector3d shadowDir = -sunVector;



        //                        for (int i = 0; i < segments; i++)
        //                        {
        //                            double angle = 2 * Math.PI * i / segments;
        //                            double x = center.X + radiusX * Math.Cos(angle);
        //                            double y = center.Y + radiusY * Math.Sin(angle);
        //                            double z = center.Z;

        //                            // Shadow projection
        //                            double t = -apex.Z / shadowDir.Z;
        //                            Point3d shadowPt = apex + shadowDir * t;
        //                            if (!double.IsNaN(shadowPt.X) && !double.IsNaN(shadowPt.Y) && !double.IsNaN(shadowPt.Z))
        //                            {
        //                                shadowPoints.Add(shadowPt);
        //                            }
        //                        }
        //                    }
        //                    if (shadowPoints.Count >= 2)
        //                    {

        //                        Polyline shadowPolyline = new Polyline();
        //                        int index = 0;

        //                        Double yvalue = 0;
        //                        foreach (Point3d point in shadowPoints)
        //                        {
        //                            yvalue = point.Y;
        //                            shadowPolyline.AddVertexAt(index++, new Point2d(point.X, point.Y), 0, 0, 0);
        //                        }
        //                        shadowPolyline.ColorIndex = 2;
        //                        shadowPolyline.Layer = "0";
        //                        shadowPolyline.Closed = false;
        //                        btr.AppendEntity(shadowPolyline);
        //                        tr.AddNewlyCreatedDBObject(shadowPolyline, true);

        //                        Polyline offsetPolyline = new Polyline();
        //                        int offsetIndex = 0;

        //                        if (yvalue > ext.MinPoint.Y)
        //                        {
        //                            foreach (Point3d point in shadowPoints)
        //                            {

        //                                double adjustedY = point.Y + width;

        //                                offsetPolyline.AddVertexAt(offsetIndex++, new Point2d(point.X, adjustedY), 0, 0, 0);
        //                            }
        //                        }
        //                        else
        //                        {
        //                            foreach (Point3d point in shadowPoints)
        //                            {

        //                                double adjustedY = point.Y - width;

        //                                offsetPolyline.AddVertexAt(offsetIndex++, new Point2d(point.X, adjustedY), 0, 0, 0);
        //                            }
        //                        }

        //                        Entity ent = tr.GetObject(shadowPolyline.Id, OpenMode.ForWrite) as Entity;
        //                        if (ent != null && ent is Polyline)
        //                        {
        //                            ent.Erase();
        //                        }
        //                        offsetPolyline.ColorIndex = 3;
        //                        offsetPolyline.Layer = "0";
        //                        offsetPolyline.Closed = false;

        //                        btr.AppendEntity(offsetPolyline);
        //                        tr.AddNewlyCreatedDBObject(offsetPolyline, true);

        //                        Point2d firstCorner = offsetPolyline.GetPoint2dAt(0);
        //                        Point2d lastCorner = offsetPolyline.GetPoint2dAt(offsetPolyline.NumberOfVertices - 1);

        //                        Point3d firstCorner3D = new Point3d(firstCorner.X, firstCorner.Y, center.Z);
        //                        Point3d lastCorner3D = new Point3d(lastCorner.X, lastCorner.Y, center.Z);

        //                        Line firstConnection = new Line(firstCorner3D, center);
        //                        Line lastConnection = new Line(lastCorner3D, center);

        //                        btr.AppendEntity(firstConnection);
        //                        btr.AppendEntity(lastConnection);
        //                        tr.AddNewlyCreatedDBObject(firstConnection, true);
        //                        tr.AddNewlyCreatedDBObject(lastConnection, true);

        //                    }
        //                    tr.Commit();
        //                }
        //            }


        //        }
        //        else
        //        {
        //            Document doc = Application.DocumentManager.MdiActiveDocument;
        //            Editor ed = doc.Editor;
        //            Database db = doc.Database;


        //            DateTime date = Shadow.datetime2;
        //            double latitude = Shadow.latitude;
        //            double longitude = Shadow.longitude;

        //            using (doc.LockDocument())
        //            {
        //                using (Transaction tr = db.TransactionManager.StartTransaction())
        //                {
        //                    Solid3d solid = tr.GetObject(Shadow.selectedObj, OpenMode.ForRead) as Solid3d;
        //                    Extents3d ext = solid.GeometricExtents;
        //                    Point3d objectStart = ext.MinPoint;
        //                    Point3d objectEnd = ext.MaxPoint;
        //                    double width = (ext.MaxPoint.X - ext.MinPoint.X) / 2;

        //                    Point3d apex = new Point3d(
        //                        (ext.MinPoint.X + ext.MaxPoint.X) / 2,
        //                        (ext.MinPoint.Y + ext.MaxPoint.Y) / 2,
        //                        ext.MaxPoint.Z);

        //                    // Base center
        //                    double radiusX = (ext.MaxPoint.X - ext.MinPoint.X) / 2;
        //                    double radiusY = (ext.MaxPoint.Y - ext.MinPoint.Y) / 2;
        //                    Point3d center = new Point3d(
        //                        (ext.MinPoint.X + ext.MaxPoint.X) / 2,
        //                        (ext.MinPoint.Y + ext.MaxPoint.Y) / 2,
        //                        ext.MinPoint.Z);

        //                    int segments = 30;

        //                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
        //                    BlockTableRecord btr = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

        //                    // From 8 AM to 5 PM
        //                    HashSet<Point3d> shadowPoints = new HashSet<Point3d>();
        //                    for (double hour = 8; hour <= 17; hour++)
        //                    {
        //                        DateTime currentTime = date.AddHours(hour);

        //                        Vector3d sunVector = GetSunDirection(currentTime, latitude);
        //                        Vector3d shadowDir = -sunVector;
        //                        for (int i = 0; i < segments; i++)
        //                        {
        //                            double angle = 2 * Math.PI * i / segments;
        //                            double x = center.X + radiusX * Math.Cos(angle);
        //                            double y = center.Y + radiusY * Math.Sin(angle);
        //                            double z = center.Z;

        //                            // Shadow projection
        //                            double t = -apex.Z / shadowDir.Z;
        //                            Point3d shadowPt = apex + shadowDir * t;
        //                            if (!double.IsNaN(shadowPt.X) && !double.IsNaN(shadowPt.Y) && !double.IsNaN(shadowPt.Z))
        //                            {
        //                                shadowPoints.Add(shadowPt);
        //                            }
        //                        }
        //                    }
        //                    if (shadowPoints.Count >= 2)
        //                    {

        //                        Polyline shadowPolyline = new Polyline();
        //                        int index = 0;

        //                        Double yvalue = 0;
        //                        foreach (Point3d point in shadowPoints)
        //                        {
        //                            yvalue = point.Y;
        //                            shadowPolyline.AddVertexAt(index++, new Point2d(point.X, point.Y), 0, 0, 0);
        //                        }

        //                        // Set polyline properties
        //                        shadowPolyline.ColorIndex = 2;
        //                        shadowPolyline.Layer = "0";
        //                        shadowPolyline.Closed = false;
        //                        btr.AppendEntity(shadowPolyline);
        //                        tr.AddNewlyCreatedDBObject(shadowPolyline, true);

        //                        Polyline offsetPolyline = new Polyline();
        //                        int offsetIndex = 0;

        //                        if (yvalue > ext.MinPoint.Y)
        //                        {
        //                            foreach (Point3d point in shadowPoints)
        //                            {

        //                                double adjustedY = point.Y + width;

        //                                offsetPolyline.AddVertexAt(offsetIndex++, new Point2d(point.X, adjustedY), 0, 0, 0);
        //                            }
        //                        }
        //                        else
        //                        {
        //                            foreach (Point3d point in shadowPoints)
        //                            {

        //                                double adjustedY = point.Y - width;

        //                                offsetPolyline.AddVertexAt(offsetIndex++, new Point2d(point.X, adjustedY), 0, 0, 0);
        //                            }
        //                        }

        //                        Entity ent = tr.GetObject(shadowPolyline.Id, OpenMode.ForWrite) as Entity;
        //                        if (ent != null && ent is Polyline)
        //                        {
        //                            ent.Erase(); // Erase polyline
        //                        }
        //                        // Set offset polyline properties
        //                        offsetPolyline.ColorIndex = 3;
        //                        offsetPolyline.Layer = "0";
        //                        offsetPolyline.Closed = false;

        //                        btr.AppendEntity(offsetPolyline);
        //                        tr.AddNewlyCreatedDBObject(offsetPolyline, true);



        //                        Point2d firstCorner = offsetPolyline.GetPoint2dAt(0);
        //                        Point2d lastCorner = offsetPolyline.GetPoint2dAt(offsetPolyline.NumberOfVertices - 1);

        //                        Point3d firstCorner3D = new Point3d(firstCorner.X, firstCorner.Y, center.Z);
        //                        Point3d lastCorner3D = new Point3d(lastCorner.X, lastCorner.Y, center.Z);

        //                        Line firstConnection = new Line(firstCorner3D, center);
        //                        Line lastConnection = new Line(lastCorner3D, center);

        //                        btr.AppendEntity(firstConnection);
        //                        btr.AppendEntity(lastConnection);
        //                        tr.AddNewlyCreatedDBObject(firstConnection, true);
        //                        tr.AddNewlyCreatedDBObject(lastConnection, true);

        //                    }
        //                    tr.Commit();
        //                }
        //            }
        //        }
        //    }
        //}
        //private Vector3d GetSunDirection(DateTime dateTime, double latitude)
        //{
        //    double hour = dateTime.TimeOfDay.TotalHours;
        //    double dayOfYear = dateTime.DayOfYear;

        //    // Solar declination angle (in degrees)
        //    double decl = 23.45 * Math.Sin(Math.PI / 180 * (360.0 / 365.0 * (dayOfYear - 81)));

        //    // Hour angle
        //    double ha = 15 * (hour - 12); // degrees

        //    double latRad = latitude * Math.PI / 180;
        //    double declRad = decl * Math.PI / 180;
        //    double haRad = ha * Math.PI / 180;

        //    // Solar altitude angle
        //    double alt = Math.Asin(Math.Sin(latRad) * Math.Sin(declRad) + Math.Cos(latRad) * Math.Cos(declRad) * Math.Cos(haRad));

        //    // Solar azimuth angle
        //    double azi = Math.Acos((Math.Sin(declRad) - Math.Sin(alt) * Math.Sin(latRad)) / (Math.Cos(alt) * Math.Cos(latRad)));
        //    if (hour > 12) azi = 2 * Math.PI - azi;

        //    // Convert to vector
        //    double x = Math.Cos(alt) * Math.Sin(azi);
        //    double y = Math.Cos(alt) * Math.Cos(azi);
        //    double z = Math.Sin(alt);

        //    return new Vector3d(x, y, z);
        //}



        //=================================================================

        Point3d firstLine;
        Point3d secondLine;
        Polyline offsetPolyline1 = new Polyline();

        public void start()
        {

            for (int K = 0; K < 2; K++)
            {
                if (K == 0)
                {
                    Document doc = Application.DocumentManager.MdiActiveDocument;
                    Editor ed = doc.Editor;
                    Database db = doc.Database;
                    DateTime date = Shadow.datetime1;
                    double latitude = Shadow.latitude;
                    double longitude = Shadow.longitude;
                    using (doc.LockDocument())
                    {
                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            Solid3d solid = tr.GetObject(Shadow.selectedObj, OpenMode.ForRead) as Solid3d;
                            Extents3d ext = solid.GeometricExtents;
                            Point3d objectStart = ext.MinPoint;
                            Point3d objectEnd = ext.MaxPoint;
                            double width = (ext.MaxPoint.X - ext.MinPoint.X) / 3;

                            Point3d apex = new Point3d(
                                (ext.MinPoint.X + ext.MaxPoint.X) / 2,
                                (ext.MinPoint.Y + ext.MaxPoint.Y) / 2,
                                ext.MaxPoint.Z);

                            // Base center
                            double radiusX = (ext.MaxPoint.X - ext.MinPoint.X) / 2;
                            double radiusY = (ext.MaxPoint.Y - ext.MinPoint.Y) / 2;
                            Point3d center = new Point3d(
                                (ext.MinPoint.X + ext.MaxPoint.X) / 2,
                                (ext.MinPoint.Y + ext.MaxPoint.Y) / 2,
                                ext.MinPoint.Z);

                            int segments = 30;

                            BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                            BlockTableRecord btr = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                            // From 8 AM to 5 PM
                            HashSet<Point3d> shadowPoints = new HashSet<Point3d>();
                            for (double hour = 9.0; hour <= 16.5; hour += 0.5)
                            {
                                DateTime currentTime = date.AddHours(hour);

                                Vector3d sunVector = GetSunDirection(currentTime, latitude);
                                Vector3d shadowDir = -sunVector;



                                for (int i = 0; i < segments; i++)
                                {
                                    double angle = 2 * Math.PI * i / segments;
                                    double x = center.X + radiusX * Math.Cos(angle);
                                    double y = center.Y + radiusY * Math.Sin(angle);
                                    double z = center.Z;

                                    // Shadow projection
                                    double t = -apex.Z / shadowDir.Z;
                                    Point3d shadowPt = apex + shadowDir * t;
                                    if (!double.IsNaN(shadowPt.X) && !double.IsNaN(shadowPt.Y) && !double.IsNaN(shadowPt.Z))
                                    {
                                        shadowPoints.Add(shadowPt);
                                    }
                                }
                            }
                            if (shadowPoints.Count >= 2)
                            {

                                Polyline shadowPolyline = new Polyline();
                                int index = 0;

                                Double yvalue = 0;
                                foreach (Point3d point in shadowPoints)
                                {
                                    yvalue = point.Y;
                                    shadowPolyline.AddVertexAt(index++, new Point2d(point.X, point.Y), 0, 0, 0);
                                }
                                shadowPolyline.ColorIndex = 2;
                                shadowPolyline.Layer = "0";
                                shadowPolyline.Closed = false;
                                btr.AppendEntity(shadowPolyline);
                                tr.AddNewlyCreatedDBObject(shadowPolyline, true);


                                int offsetIndex = 0;

                                if (yvalue > ext.MinPoint.Y)
                                {
                                    foreach (Point3d point in shadowPoints)
                                    {

                                        double adjustedY = point.Y + width;

                                        offsetPolyline1.AddVertexAt(offsetIndex++, new Point2d(point.X, adjustedY), 0, 0, 0);
                                    }
                                }
                                else
                                {
                                    foreach (Point3d point in shadowPoints)
                                    {

                                        double adjustedY = point.Y - width;

                                        offsetPolyline1.AddVertexAt(offsetIndex++, new Point2d(point.X, adjustedY), 0, 0, 0);
                                    }
                                }

                                Entity ent = tr.GetObject(shadowPolyline.Id, OpenMode.ForWrite) as Entity;
                                if (ent != null && ent is Polyline)
                                {
                                    ent.Erase();
                                }
                                offsetPolyline1.ColorIndex = 3;
                                offsetPolyline1.Layer = "0";
                                offsetPolyline1.Closed = false;

                                btr.AppendEntity(offsetPolyline1);
                                tr.AddNewlyCreatedDBObject(offsetPolyline1, true);

                                Point2d firstCorner = offsetPolyline1.GetPoint2dAt(0);
                                Point2d secondCorner = offsetPolyline1.GetPoint2dAt(offsetPolyline1.NumberOfVertices - 1);

                                firstLine = new Point3d(firstCorner.X, firstCorner.Y, center.Z);
                                secondLine = new Point3d(secondCorner.X, secondCorner.Y, center.Z);


                            }
                            tr.Commit();
                        }
                    }


                }
                else
                {
                    Document doc = Application.DocumentManager.MdiActiveDocument;
                    Editor ed = doc.Editor;
                    Database db = doc.Database;


                    DateTime date = Shadow.datetime2;
                    double latitude = Shadow.latitude;
                    double longitude = Shadow.longitude;

                    using (doc.LockDocument())
                    {
                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            Solid3d solid = tr.GetObject(Shadow.selectedObj, OpenMode.ForRead) as Solid3d;
                            Extents3d ext = solid.GeometricExtents;
                            Point3d objectStart = ext.MinPoint;
                            Point3d objectEnd = ext.MaxPoint;
                            double width = (ext.MaxPoint.X - ext.MinPoint.X) / 2;

                            Point3d apex = new Point3d(
                                (ext.MinPoint.X + ext.MaxPoint.X) / 2,
                                (ext.MinPoint.Y + ext.MaxPoint.Y) / 2,
                                ext.MaxPoint.Z);

                            // Base center
                            double radiusX = (ext.MaxPoint.X - ext.MinPoint.X) / 2;
                            double radiusY = (ext.MaxPoint.Y - ext.MinPoint.Y) / 2;
                            Point3d center = new Point3d(
                                (ext.MinPoint.X + ext.MaxPoint.X) / 2,
                                (ext.MinPoint.Y + ext.MaxPoint.Y) / 2,
                                ext.MinPoint.Z);

                            int segments = 30;

                            BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                            BlockTableRecord btr = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                            // From 8 AM to 5 PM
                            HashSet<Point3d> shadowPoints = new HashSet<Point3d>();
                            for (double hour = 9.0; hour <= 16.5; hour += 0.5)
                            {
                                DateTime currentTime = date.AddHours(hour);

                                Vector3d sunVector = GetSunDirection(currentTime, latitude);
                                Vector3d shadowDir = -sunVector;
                                for (int i = 0; i < segments; i++)
                                {
                                    double angle = 2 * Math.PI * i / segments;
                                    double x = center.X + radiusX * Math.Cos(angle);
                                    double y = center.Y + radiusY * Math.Sin(angle);
                                    double z = center.Z;

                                    // Shadow projection
                                    double t = -apex.Z / shadowDir.Z;
                                    Point3d shadowPt = apex + shadowDir * t;
                                    if (!double.IsNaN(shadowPt.X) && !double.IsNaN(shadowPt.Y) && !double.IsNaN(shadowPt.Z))
                                    {
                                        shadowPoints.Add(shadowPt);
                                    }
                                }
                            }
                            if (shadowPoints.Count >= 2)
                            {

                                Polyline shadowPolyline = new Polyline();
                                int index = 0;

                                Double yvalue = 0;
                                foreach (Point3d point in shadowPoints)
                                {
                                    yvalue = point.Y;
                                    shadowPolyline.AddVertexAt(index++, new Point2d(point.X, point.Y), 0, 0, 0);
                                }

                                // Set polyline properties
                                shadowPolyline.ColorIndex = 2;
                                shadowPolyline.Layer = "0";
                                shadowPolyline.Closed = false;
                                btr.AppendEntity(shadowPolyline);
                                tr.AddNewlyCreatedDBObject(shadowPolyline, true);

                                Polyline offsetPolyline = new Polyline();
                                int offsetIndex = 0;

                                if (yvalue > ext.MinPoint.Y)
                                {
                                    foreach (Point3d point in shadowPoints)
                                    {

                                        double adjustedY = point.Y + width;

                                        offsetPolyline.AddVertexAt(offsetIndex++, new Point2d(point.X, adjustedY), 0, 0, 0);
                                    }
                                }
                                else
                                {
                                    foreach (Point3d point in shadowPoints)
                                    {

                                        double adjustedY = point.Y - width;

                                        offsetPolyline.AddVertexAt(offsetIndex++, new Point2d(point.X, adjustedY), 0, 0, 0);
                                    }
                                }

                                Entity ent = tr.GetObject(shadowPolyline.Id, OpenMode.ForWrite) as Entity;
                                if (ent != null && ent is Polyline)
                                {
                                    ent.Erase(); // Erase polyline
                                }
                                // Set offset polyline properties
                                offsetPolyline.ColorIndex = 3;
                                offsetPolyline.Layer = "0";
                                offsetPolyline.Closed = false;

                                btr.AppendEntity(offsetPolyline);
                                tr.AddNewlyCreatedDBObject(offsetPolyline, true);



                                Point2d thirdCorner = offsetPolyline.GetPoint2dAt(0);
                                Point2d fourthCorner = offsetPolyline.GetPoint2dAt(offsetPolyline.NumberOfVertices - 1);

                                Point3d firstCorner3D = new Point3d(thirdCorner.X, thirdCorner.Y, center.Z);
                                Point3d lastCorner3D = new Point3d(fourthCorner.X, fourthCorner.Y, center.Z);

                                Line firstConnection = new Line(firstLine, firstCorner3D);
                                Line lastConnection = new Line(secondLine, lastCorner3D);

                                btr.AppendEntity(firstConnection);
                                btr.AppendEntity(lastConnection);
                                tr.AddNewlyCreatedDBObject(firstConnection, true);
                                tr.AddNewlyCreatedDBObject(lastConnection, true);


                                ObjectIdCollection hatchBoundary = new ObjectIdCollection();

                                // Assuming 'offsetPolyline' is already in the drawing
                                hatchBoundary.Add(offsetPolyline.ObjectId);       // 1. Offset polyline
                                hatchBoundary.Add(firstConnection.ObjectId);      // 2. Line from center to first offset point
                                hatchBoundary.Add(lastConnection.ObjectId);       // 3. Line from center to last offset point
                                hatchBoundary.Add(offsetPolyline1.ObjectId);        // 4. Original curve (like inner arc) if needed

                                // Create the hatch
                                Hatch hatch = new Hatch();
                                hatch.SetDatabaseDefaults();

                                btr.AppendEntity(hatch);
                                tr.AddNewlyCreatedDBObject(hatch, true);

                                hatch.PatternScale = 75;
                                hatch.SetHatchPattern(HatchPatternType.PreDefined, "ANSI31"); // You can change to your pattern
                                hatch.Associative = true;


                                // Append loop
                                hatch.AppendLoop(HatchLoopTypes.Default, hatchBoundary);
                                hatch.EvaluateHatch(true);

                            }
                            tr.Commit();
                        }
                    }
                }
            }

        }
        private Vector3d GetSunDirection(DateTime dateTime, double latitude)
        {
            double hour = dateTime.TimeOfDay.TotalHours;
            double dayOfYear = dateTime.DayOfYear;

            // Solar declination angle (in degrees)
            double decl = 23.45 * Math.Sin(Math.PI / 180 * (360.0 / 365.0 * (dayOfYear - 81)));

            // Hour angle
            double ha = 15 * (hour - 12); // degrees

            double latRad = latitude * Math.PI / 180;
            double declRad = decl * Math.PI / 180;
            double haRad = ha * Math.PI / 180;

            // Solar altitude angle
            double alt = Math.Asin(Math.Sin(latRad) * Math.Sin(declRad) + Math.Cos(latRad) * Math.Cos(declRad) * Math.Cos(haRad));

            // Solar azimuth angle
            double azi = Math.Acos((Math.Sin(declRad) - Math.Sin(alt) * Math.Sin(latRad)) / (Math.Cos(alt) * Math.Cos(latRad)));
            if (hour > 12) azi = 2 * Math.PI - azi;

            // Convert to vector
            double x = Math.Cos(alt) * Math.Sin(azi);
            double y = Math.Cos(alt) * Math.Cos(azi);
            double z = Math.Sin(alt);

            return new Vector3d(x, y, z);
        }


    }
}