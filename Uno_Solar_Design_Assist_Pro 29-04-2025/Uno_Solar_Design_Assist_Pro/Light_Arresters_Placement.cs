using System;
using System.Windows.Input;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Uno_Solar_Design_Assist_Pro
{
    internal class Light_Arresters_Placement : ICommand
    {
        private const double CircleRadius = 107;
        private const double GridSpacing = CircleRadius * 2 / 1.532; // Hexagonal spacing
        private const double CircleBlockSpacing = 160; // Circle block center-to-center distance
        
        public void Execute(object parameter)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Prompt user to select a boundary polyline
                PromptEntityOptions peo = new PromptEntityOptions("\nSelect inner boundary polyline: ");
                peo.SetRejectMessage("\nOnly polylines are allowed.");
                peo.AddAllowedClass(typeof(Polyline), true);

                PromptEntityResult per = ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK) return;

                Polyline boundary = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Polyline;
                if (boundary == null) return;

                // Get the bounding box or use vertex points to define a square
                Extents3d ext = boundary.GeometricExtents;
                Point3d minPt = ext.MinPoint;
                Point3d maxPt = ext.MaxPoint;

                // Generate circle centers within the square
                List<Point3d> circleCenters = new List<Point3d>();
                double dx = CircleBlockSpacing; // Adjusted distance
                double dy = CircleBlockSpacing;
                bool shiftRow = false;

                for (double y = minPt.Y - CircleRadius; y <= maxPt.Y + CircleRadius; y += dy)
                {
                    for (double x = minPt.X - CircleRadius + (shiftRow ? dx / 2 : 0); x <= maxPt.X + CircleRadius; x += dx)
                    {
                        Point3d candidateCenter = new Point3d(x, y, 0);
                        if (IsPointInsidePolyline(candidateCenter, boundary))
                        {
                            circleCenters.Add(candidateCenter);
                        }
                    }
                    shiftRow = !shiftRow;
                }

                // Create a block for the circles
                ObjectId blockId = CreateCircleBlock(db, tr);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                foreach (Point3d center in circleCenters)
                {
                    BlockReference blockRef = new BlockReference(center, blockId);
                    btr.AppendEntity(blockRef);
                    tr.AddNewlyCreatedDBObject(blockRef, true);
                }

                tr.Commit();
            }
        }

        private static ObjectId CreateCircleBlock(Database db, Transaction tr)
        {
            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
            BlockTableRecord btr = new BlockTableRecord
            {
                Name = "CircleBlock_" + DateTime.Now.Ticks
            };

            Circle circle = new Circle(Point3d.Origin, Vector3d.ZAxis, CircleRadius);
            btr.AppendEntity(circle);
            ObjectId btrId = bt.Add(btr);
            tr.AddNewlyCreatedDBObject(btr, true);

            return btrId;
        }

        private static bool IsPointInsidePolyline(Point3d point, Polyline poly)
        {
            int intersectCount = 0;
            Point2d testPoint = new Point2d(point.X, point.Y);

            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                Point2d v1 = poly.GetPoint2dAt(i);
                Point2d v2 = poly.GetPoint2dAt((i + 1) % poly.NumberOfVertices);

                if (((v1.Y <= testPoint.Y) && (v2.Y > testPoint.Y)) || ((v2.Y <= testPoint.Y) && (v1.Y > testPoint.Y)))
                {
                    double intersectX = (v2.X - v1.X) * (testPoint.Y - v1.Y) / (v2.Y - v1.Y) + v1.X;
                    if (intersectX > testPoint.X)
                    {
                        intersectCount++;
                    }
                }
            }

            return (intersectCount % 2) != 0;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
