using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Uno_Solar_Design_Assist_Pro
{
    internal class Grounding_Creation : ICommand
    {
        private Editor ed;
        private double Horizontal_width = 0;
        private double Vertical_width = 0;
        public Color Vertical_Color { get; private set; }
        public object Horizontal_Color { get; private set; }

        public static Color Horizontal_color = Color.FromRgb((byte)Grounding.red, (byte)Grounding.green, (byte)Grounding.blue);
        public static Color Vertical_color = Color.FromRgb((byte)Grounding.red1, (byte)Grounding.green1, (byte)Grounding.blue1);

        public string Layer_Name = "UnoTEAM_GROUNDING";

        public void Initialize()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc != null)
            {
                ed = doc.Editor;
                ed.WriteMessage("\nSolar Wiring Module Loaded. Commands: Draw_Grounding_Strips");
            }
        }

        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object parameter) => true;

        [CommandMethod("Draw_Grounding_Strips")]
        public void Execute(object parameter)
        {
            Grounding f1 = new Grounding();
            if (f1.ShowDialog() != DialogResult.OK) return;

            Horizontal_width = f1.Horizontal_Strip_Weight;
            Vertical_width = f1.Vertical_Strip_Weight;

            Vertical_Color = Vertical_color;
            Horizontal_Color = Horizontal_color;

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            using (DocumentLock docLock = doc.LockDocument())
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Polyline boundary = SelectBoundaryPolyline(ed, tr);
                    if (boundary == null) return;

                    BlockTableRecord ms = tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite) as BlockTableRecord;
                    List<BlockReference> blocks = CollectBlocksInBoundary(tr, ms, boundary, ed);

                    var groupedRows = GroupBlocksByRow(blocks);

                    Create_Validate_Layer(db, Layer_Name, Horizontal_color);
                    Create_Validate_Layer(db, Layer_Name, Vertical_color);

                    double horizontalExtension = 1.6;
                    double Module_Half_Height = 2.45;
                    double maxLength = 6.0;

                    List<Point3d> verticalLeftPoints = new();
                    List<Point3d> verticalRightPoints = new();
                    int cnt = 1;

                    foreach (var row in groupedRows)
                    {
                        if (row.Count == 0) continue;

                        BlockReference firstBlock = row[0];
                        BlockReference lastBlock = row[row.Count - 1];

                        Point3d leftMid = GetBlockLeftMiddlePoint(firstBlock);
                        Point3d leftEnd = new(leftMid.X - horizontalExtension, leftMid.Y, 0);
                        Create_PolyLine(tr, ms, leftMid, leftEnd, Horizontal_color, Horizontal_width);
                        verticalLeftPoints.AddRange(GetVerticalSegments(leftEnd, verticalLeftPoints.LastOrDefault(), Module_Half_Height, cnt));

                        Point3d rightMid = GetBlockRightMiddlePoint(lastBlock);
                        Point3d rightEnd = new(rightMid.X + horizontalExtension, rightMid.Y, 0);
                        Create_PolyLine(tr, ms, rightMid, rightEnd, Horizontal_color, Horizontal_width);
                        verticalRightPoints.AddRange(GetVerticalSegments(rightEnd, verticalRightPoints.LastOrDefault(), Module_Half_Height, cnt));
                        cnt++;

                        for (int i = 0; i < row.Count - 1; i++)
                        {
                            Point3d left = GetBlockRightMiddlePoint(row[i]);
                            Point3d right = GetBlockLeftMiddlePoint(row[i + 1]);

                            double distance = right.X - left.X;
                            if (distance > maxLength)
                            {
                                Point3d mid1 = new(left.X + horizontalExtension, left.Y, 0);
                                Point3d mid2 = new(right.X - horizontalExtension, right.Y, 0);
                                Create_PolyLine(tr, ms, left, mid1, Horizontal_color, Horizontal_width);
                                Create_PolyLine(tr, ms, right, mid2, Horizontal_color, Horizontal_width);
                            }
                            else
                            {
                                Create_PolyLine(tr, ms, left, right, Horizontal_color, Horizontal_width);
                            }
                        }
                    }

                    Draw_Boundary_Strips(tr, ms, verticalLeftPoints, Vertical_color, Vertical_width);
                    Draw_Boundary_Strips(tr, ms, verticalRightPoints, Vertical_color, Vertical_width);

                    List<Point3d> TrenchLine_Points = Get_TrenchLine_Points();
                    Dictionary<Point3d, List<Point3d>> Needed_Points = new();
                    foreach (Point3d Trench_Point in TrenchLine_Points)
                    {
                        List<Point3d> Intersection_Points = new List<Point3d>();
                        int row_cnt = 0;

                        foreach (var row in groupedRows)
                        {
                            row_cnt++;
                            if (row.Count > 1)
                            {
                                for (int i = 0; i < row.Count - 1; i++)
                                {
                                    BlockReference block1 = row[i];
                                    BlockReference block2 = row[i + 1];

                                    Point3d FirstBlock_Right_MidPoint = GetBlockRightMiddlePoint(block1);
                                    Point3d SecondBlock_Left_MidPoint = GetBlockLeftMiddlePoint(block2);

                                    Point3d Mid_Point = new Point3d((FirstBlock_Right_MidPoint.X + SecondBlock_Left_MidPoint.X) / 2, SecondBlock_Left_MidPoint.Y, 0);
                                    if (Math.Abs(Mid_Point.X - Trench_Point.X) < 0.5)
                                    {
                                        Intersection_Points.Add(Mid_Point);
                                    }
                                }
                            }
                        }
                        ed.WriteMessage($"\nTrench at X={Trench_Point.X} has {Intersection_Points.Count} intersection points.");

                        if (!Needed_Points.ContainsKey(Trench_Point))
                        {
                            Needed_Points[Trench_Point] = Intersection_Points;
                        }
                    }
                    foreach (var kvp in Needed_Points)
                    {
                        List<Point3d> polyPoints = kvp.Value;

                        if (polyPoints.Count >= 2)
                        {
                            Point2d ptStart = new Point2d(polyPoints[0].X, polyPoints[0].Y);
                            Point2d ptEnd = new Point2d(polyPoints[^1].X, polyPoints[^1].Y);

                            // Check for existing matching line and erase it if found
                            foreach (ObjectId objId in ms)
                            {
                                Entity ent = tr.GetObject(objId, OpenMode.ForWrite) as Entity;
                                if (ent is Polyline pl && pl.NumberOfVertices == 2)
                                {
                                    Point2d v0 = pl.GetPoint2dAt(0);
                                    Point2d v1 = pl.GetPoint2dAt(1);

                                    bool isSame =
                                        (IsCloseTo(v0, ptStart) && IsCloseTo(v1, ptEnd)) ||
                                        (IsCloseTo(v0, ptEnd) && IsCloseTo(v1, ptStart));

                                    if (isSame)
                                    {
                                        pl.Erase();
                                        break; // Assuming only one match exists
                                    }
                                }
                            }

                            // Create new polyline
                            using (Polyline poly = new Polyline())
                            {
                                poly.AddVertexAt(0, ptStart, 0, Vertical_width, Vertical_width);
                                poly.AddVertexAt(1, ptEnd, 0, Vertical_width, Vertical_width);
                                poly.Color = Color.FromRgb((byte)Grounding.red1, (byte)Grounding.green1, (byte)Grounding.blue1);

                                ms.AppendEntity(poly);
                                tr.AddNewlyCreatedDBObject(poly, true);
                            }
                        }
                    }
                    tr.Commit();
                }
            }
        }
        private Polyline SelectBoundaryPolyline(Editor ed, Transaction tr)
        {
            PromptEntityOptions peo = new PromptEntityOptions("\nSelect boundary polyline: ");
            peo.SetRejectMessage("\nOnly closed polylines allowed.");
            peo.AddAllowedClass(typeof(Polyline), true); // Allow selecting polylines only
            PromptEntityResult per = ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK) return null;

            Polyline boundary = null;
            try
            {
                boundary = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Polyline;
                if (boundary == null || !boundary.Closed)
                {
                    ed.WriteMessage("\nInvalid or non-closed polyline selected.");
                    return null; // Return null if not valid
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError reading selected boundary: {ex.Message}");
                return null;
            }
            return boundary;
        }

        // Collects specified blocks within the boundary
        private List<BlockReference> CollectBlocksInBoundary(Transaction tr, BlockTableRecord ms, Polyline boundary, Editor ed)
        {
            List<BlockReference> blocks = new List<BlockReference>();
            foreach (ObjectId id in ms)
            {
                if (id.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(BlockReference))))
                {
                    BlockReference br = tr.GetObject(id, OpenMode.ForRead, false, true) as BlockReference;
                    if (br != null && IsPointInside(boundary, br.Position))
                    {
                        Entity ent = br as Entity; // Cast to check layer
                                                   //if (ent != null && (ent.Layer == "PVcase PV Modules (full frames)" ||
                                                   //                     ent.Layer == "PVcase PV Modules (half frames)") || ent.Layer == "UnoTEAM_MODULES" || ent.Layer == "UnoTEAM_MODULES" || ent.Layer == "BlueLayer")
                                                   //{
                        if (ent != null && (ent.Layer == "UnoTEAM_MODULES"))
                        {
                            blocks.Add(br);
                        }
                    }
                }
            }
            ed.WriteMessage($"\nFound {blocks.Count} matching blocks inside boundary.");
            return blocks;
        }

        // Ray casting algorithm to check if a point is inside a polyline (Corrected Version)
        private bool IsPointInside(Polyline poly, Point3d pt)
        {
            // Basic validation of input polyline
            if (poly == null || poly.IsDisposed || !poly.Closed || poly.NumberOfVertices < 3)
            {

                return false;
            }

            Point2d test = new Point2d(pt.X, pt.Y);
            int count = poly.NumberOfVertices;
            int crossings = 0;

            // Use the crossing number algorithm (ray casting)
            for (int i = 0; i < count; i++)
            {
                Point3d p1_3d = poly.GetPoint3dAt(i);
                Point3d p2_3d = poly.GetPoint3dAt((i + 1) % count); // Wrap around for last segment

                // Use 2D points for reliable check in XY plane
                Point2d p1 = new Point2d(p1_3d.X, p1_3d.Y);
                Point2d p2 = new Point2d(p2_3d.X, p2_3d.Y);

                // Check if the horizontal ray (positive X direction) from 'test' crosses the segment (p1, p2)
                if (((p1.Y <= test.Y && test.Y < p2.Y) || // Upward crossing
                     (p2.Y <= test.Y && test.Y < p1.Y)) && // Downward crossing
                                                           // Point must be to the left of the edge intersection
                    (test.X < (p2.X - p1.X) * (test.Y - p1.Y) / (p2.Y - p1.Y) + p1.X))
                {
                    crossings++;
                }
            }

            bool inside = (crossings % 2 == 1); // Odd number of crossings means the point is inside

            // If ray casting is inconclusive (point might be on boundary), check proximity
            if (!inside)
            {
                try
                {
                    // Find the closest point ON the polyline TO the test point pt.
                    Point3d closestPoint = poly.GetClosestPointTo(pt, false); // Don't extend segments

                    // Calculate the distance between the test point and the closest point found.
                    double dist = pt.DistanceTo(closestPoint);

                    // Use a small tolerance to consider points very close to the boundary as inside.
                    double boundaryTolerance = Tolerance.Global.EqualPoint; // Use AutoCAD's tolerance
                    if (dist < boundaryTolerance)
                    {
                        inside = true; // Point is on or very near the boundary
                    }
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    // Log error if checking proximity fails
                    ed.WriteMessage($"\nWarning: Error checking proximity to boundary for point {pt}: {ex.Message}");
                    // Keep 'inside' as false if check fails
                }
                catch (System.Exception sysEx)
                {
                    ed.WriteMessage($"\nSystem Error checking proximity to boundary for point {pt}: {sysEx.Message}");
                }
            }
            return inside;
        }

        // Groups blocks into rows based on Y-coordinate
        private List<List<BlockReference>> GroupBlocksByRow(List<BlockReference> blocks)
        {
            double yTolerance = 3.14; // Tolerance for grouping by Y coordinate
            var groupedRows = blocks
                .GroupBy(b => System.Math.Round(b.Position.Y / yTolerance) * yTolerance)
                .OrderByDescending(g => g.Key) // Order rows top-to-bottom
                .Select(g => g.OrderBy(b => b.Position.X).ToList()) // Order blocks left-to-right
                .ToList();
            return groupedRows;
        }

        private void Create_Validate_Layer(Database db, string layerName, Color color)
        {

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                if (!layerTable.Has(layerName))
                {
                    LayerTableRecord newLayer = new LayerTableRecord
                    {
                        Name = layerName,
                        Color = color
                    };
                    layerTable.UpgradeOpen();
                    layerTable.Add(newLayer);
                    tr.AddNewlyCreatedDBObject(newLayer, true);
                }
                else
                {
                    db.Clayer = layerTable[layerName];
                }
                tr.Commit();
            }
        }
        private void Create_PolyLine(Transaction tr, BlockTableRecord ms, Point3d start, Point3d end, Color Horizontal_Color, double Strip_Weight)
        {
            Point2d ptStart = new Point2d(start.X, start.Y);
            Point2d ptEnd = new Point2d(end.X, end.Y);

            // Define color from Grounding_UI values
            Color color = Color.FromRgb((byte)Grounding.red, (byte)Grounding.green, (byte)Grounding.blue);

            // Delete any existing polyline between these points
            foreach (ObjectId objId in ms)
            {
                Entity ent = tr.GetObject(objId, OpenMode.ForWrite) as Entity;
                if (ent is Polyline pl && pl.NumberOfVertices == 2)
                {
                    Point2d v0 = pl.GetPoint2dAt(0);
                    Point2d v1 = pl.GetPoint2dAt(1);

                    bool isSame =
                        (IsCloseTo(v0, ptStart) && IsCloseTo(v1, ptEnd)) ||
                        (IsCloseTo(v0, ptEnd) && IsCloseTo(v1, ptStart));

                    if (isSame)
                    {
                        pl.Erase();
                        break; // Assuming only one such line exists
                    }
                }
            }

            // Create new polyline
            Polyline newPline = new Polyline();
            newPline.AddVertexAt(0, ptStart, 0, Strip_Weight, Strip_Weight);
            newPline.AddVertexAt(1, ptEnd, 0, Strip_Weight, Strip_Weight);
            newPline.Color = Color.FromRgb((byte)Grounding.red, (byte)Grounding.green, (byte)Grounding.blue);

            ms.AppendEntity(newPline);
            tr.AddNewlyCreatedDBObject(newPline, true);
        }

        private bool IsCloseTo(Point2d pt1, Point2d pt2, double tolerance = 1e-6) //changed the default tolerance.
        {
            return pt1.GetDistanceTo(pt2) < tolerance;
        }
        // Helper method: returns vertical segments based on conditions
        private List<Point3d> GetVerticalSegments(Point3d current, Point3d prev, double height, int cnt)
        {
            List<Point3d> pts = new();
            if (cnt > 1 && current.X != prev.X)
            {
                pts.Add(new(prev.X, prev.Y - height, prev.Z));
                pts.Add(new(current.X, prev.Y - height, prev.Z));
            }
            pts.Add(current);
            return pts;
        }

        private void Draw_Boundary_Strips(Transaction tr, BlockTableRecord ms, List<Point3d> pts, Color Vertical_Color, double Horizontal_width)
        {
            if (pts.Count < 2) return;

            // Sort points top-to-bottom
            pts = pts.OrderByDescending(p => p.Y).ToList();

            // Convert points to 2D
            List<Point2d> point2Ds = pts.Select(p => new Point2d(p.X, p.Y)).ToList();

            // Define color from Grounding_UI
            Color color = Color.FromRgb((byte)Grounding.red1, (byte)Grounding.green1, (byte)Grounding.blue1);

            // Erase any existing matching polyline
            foreach (ObjectId objId in ms)
            {
                Entity ent = tr.GetObject(objId, OpenMode.ForWrite) as Entity;
                if (ent is Polyline pl && pl.NumberOfVertices == point2Ds.Count)
                {
                    bool allMatch = true;
                    for (int i = 0; i < point2Ds.Count; i++)
                    {
                        if (!IsCloseTo(pl.GetPoint2dAt(i), point2Ds[i], 1e-6)) // Use a tolerance of 1e-6
                        {
                            allMatch = false;
                            break;
                        }
                    }

                    if (allMatch)
                    {
                        pl.Erase();
                        break;
                    }
                }
            }

            // Create new polyline
            Polyline Poly = new Polyline
            {
                Color = Color.FromRgb((byte)Grounding.red1, (byte)Grounding.green1, (byte)Grounding.blue1)
            };

            for (int i = 0; i < point2Ds.Count; i++)
            {
                Poly.AddVertexAt(i, point2Ds[i], 0, Horizontal_width, Horizontal_width);
            }

            ms.AppendEntity(Poly);
            tr.AddNewlyCreatedDBObject(Poly, true);
        }


        // Gets the midpoint of the left edge based on GeometricExtents
        private Point3d GetBlockLeftMiddlePoint(BlockReference br)
        {
            Extents3d ext = br.GeometricExtents;
            return new Point3d(ext.MinPoint.X, (ext.MinPoint.Y + ext.MaxPoint.Y) / 2, 0);
        }

        // Gets the midpoint of the right edge based on GeometricExtents
        private Point3d GetBlockRightMiddlePoint(BlockReference br)
        {
            Extents3d ext = br.GeometricExtents;
            return new Point3d(ext.MaxPoint.X, (ext.MinPoint.Y + ext.MaxPoint.Y) / 2, 0);
        }

        private List<Point3d> Get_TrenchLine_Points()
        {
            List<Point3d> trenchPts = new List<Point3d>();

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                foreach (ObjectId objId in btr)
                {
                    if (objId.ObjectClass == RXObject.GetClass(typeof(Polyline)))
                    {
                        Polyline pline = (Polyline)tr.GetObject(objId, OpenMode.ForRead);
                        if (pline.Layer == "UnoTEAM_TRENCHES" && pline.NumberOfVertices > 0)
                        {
                            trenchPts.Add(pline.StartPoint);
                        }
                    }
                }

                tr.Commit();
            }

            return trenchPts;
        }

    }
}



 