using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Uno_Solar_Design_Assist_Pro
{
    internal class Light_Arresters_Placement : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => true;

        [CommandMethod("LA")]
        public void Execute(object parameter)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (DocumentLock docLock = doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Create or set layer
                string layerName = "UnoTEAM_LIGHTNING ARRESTER";
                // short lineWeight = (short)LineWeight.LineWeight030;
                Color layerColor = Color.FromRgb(255, 0, 0); // Red

                LayerTable layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                if (!layerTable.Has(layerName))
                {
                    layerTable.UpgradeOpen();

                    LayerTableRecord newLayer = new LayerTableRecord
                    {
                        Name = layerName,
                        Color = layerColor,
                        //  LineWeight = (LineWeight)lineWeight,
                        LinetypeObjectId = db.ContinuousLinetype
                    };

                    layerTable.Add(newLayer);
                    tr.AddNewlyCreatedDBObject(newLayer, true);
                }

                db.Clayer = layerTable[layerName];

                // Prompt user to select boundary polyline
                PromptEntityOptions peo = new PromptEntityOptions("\nSelect inner boundary polyline: ");
                peo.SetRejectMessage("\nOnly polylines are allowed.");
                peo.AddAllowedClass(typeof(Polyline), true);

                PromptEntityResult per = ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK)
                {
                    tr.Abort();
                    return;
                }

                Polyline boundary = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Polyline;
                if (boundary == null)
                {
                    tr.Abort();
                    return;
                }

                // Get bounding box
                Extents3d ext = boundary.GeometricExtents;
                Point3d minPt = ext.MinPoint;
                Point3d maxPt = ext.MaxPoint;

                const double CircleRadius = 107;
                const double CircleBlockSpacing = 160;
                List<Point3d> circleCenters = new List<Point3d>();
                double dx = CircleBlockSpacing;
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

                // Create block definition
                ObjectId blockId = CreateCircleBlockSafe(db, CircleRadius);
                if (blockId == ObjectId.Null)
                {
                    ed.WriteMessage("\nFailed to create block definition.");
                    tr.Abort();
                    return;
                }

                // Insert blocks
                BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                foreach (Point3d center in circleCenters)
                {
                    BlockReference blockRef = new BlockReference(center, blockId);
                    currentSpace.AppendEntity(blockRef);
                    tr.AddNewlyCreatedDBObject(blockRef, true);
                }

                tr.Commit();
            }
        }

        private static ObjectId CreateCircleBlockSafe(Database db, double radius)
        {
            ObjectId blockId = ObjectId.Null;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                string blockName = "CircleBlock_" + DateTime.Now.Ticks;

                if (bt.Has(blockName))
                {
                    blockId = bt[blockName];
                }
                else
                {
                    BlockTableRecord btr = new BlockTableRecord { Name = blockName };
                    Circle circle = new Circle(Point3d.Origin, Vector3d.ZAxis, radius);
                    btr.AppendEntity(circle);

                    bt.Add(btr);
                    tr.AddNewlyCreatedDBObject(btr, true);
                    blockId = btr.ObjectId;
                }

                tr.Commit();
            }

            return blockId;
        }

        private static bool IsPointInsidePolyline(Point3d point, Polyline poly)
        {
            int intersectCount = 0;
            Point2d testPoint = new Point2d(point.X, point.Y);

            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                Point2d v1 = poly.GetPoint2dAt(i);
                Point2d v2 = poly.GetPoint2dAt((i + 1) % poly.NumberOfVertices);

                if (((v1.Y <= testPoint.Y) && (v2.Y > testPoint.Y)) ||
                    ((v2.Y <= testPoint.Y) && (v1.Y > testPoint.Y)))
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
    }
}


