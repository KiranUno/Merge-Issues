using System;
using System.Windows.Input;
using System.Windows.Forms;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Uno_Solar_Design_Assist_Pro
{
    public class Mesh_Creation : ICommand
    {
        ObjectId blockId;
        Point3d upperRight;
        Point3d lowerLeft;
        List<ObjectId> contourIds = new List<ObjectId>();
        ObjectId polylineId;
        char Mesh_Dencity_Type;
         
        public void Execute(object parameter)
        {
            Terrain_Mesh mesh = new Terrain_Mesh();
            mesh.ShowDialog();

            if(mesh.DialogResult != DialogResult.OK)
            {
                return;
            }
            
            Mesh_Dencity_Type = mesh.Mesh_Dencity;

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions polylineOptions = new PromptEntityOptions("\nSelect a closed polyline:");
            polylineOptions.SetRejectMessage("\nSelected entity must be a polyline.");
            polylineOptions.AddAllowedClass(typeof(Polyline), true);
            PromptEntityResult polylineResult = ed.GetEntity(polylineOptions);

            if (polylineResult.Status != PromptStatus.OK)
                return;

            polylineId = polylineResult.ObjectId;

            using (DocumentLock docklock = doc.LockDocument())
            {
                using (Transaction tx = db.TransactionManager.StartTransaction())
                {

                    Polyline pl = tx.GetObject(polylineResult.ObjectId, OpenMode.ForRead) as Polyline;

                    if (pl == null || !pl.Closed)
                    {
                        ed.WriteMessage("\nThe selected polyline must be closed.");
                        return;
                    }
                    Point3d centroid = GetPolylineCentroid(pl);

                    Circle circle = new Circle(centroid, Vector3d.ZAxis, 5);
                    circle.Color = Color.FromColorIndex(ColorMethod.ByAci, 2);

                    BlockTableRecord btrr = (BlockTableRecord)tx.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                    btrr.AppendEntity(circle);
                    tx.AddNewlyCreatedDBObject(circle, true);
                    tx.Commit();
                }

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity entity1 = tr.GetObject(polylineId, OpenMode.ForRead) as Entity;
                    if (entity1.Layer.ToLower() != "boundary")
                    {
                        MessageBox.Show("Please Select Boundry Layer outer Polyline");
                        return;
                    }
                    else
                    {
                        BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                        if (!bt.Has("SquareBlock"))
                        {
                            BlockTableRecord btr = new BlockTableRecord
                            {
                                Name = "SquareBlock"
                            };

                            // Define a square
                            Polyline square = new Polyline(4);
                            square.AddVertexAt(0, new Point2d(-750, -750), 0, 0, 0);
                            square.AddVertexAt(1, new Point2d(750, -750), 0, 0, 0);
                            square.AddVertexAt(2, new Point2d(750, 750), 0, 0, 0);
                            square.AddVertexAt(3, new Point2d(-750, 750), 0, 0, 0);
                            square.Closed = true;

                            btr.AppendEntity(square);

                            bt.UpgradeOpen();
                            bt.Add(btr);
                            tr.AddNewlyCreatedDBObject(btr, true);
                        }
                        // Ask user to select a point to place the block
                        PromptPointResult ppr = ed.GetPoint("\nSelect a point inside the circle to place the block: ");
                        if (ppr.Status != PromptStatus.OK)
                            return;

                        BlockTable bt1 = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                        BlockTableRecord btr1 = (BlockTableRecord)tr.GetObject(bt1[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                        BlockReference br = new BlockReference(ppr.Value, bt1["SquareBlock"]);
                        btr1.AppendEntity(br);
                        tr.AddNewlyCreatedDBObject(br, true);
                        blockId = br.ObjectId;
                        Extents3d extents = br.GeometricExtents;

                        // Access the lower-left corner
                        lowerLeft = extents.MinPoint;

                        // Access the upper-right corner
                        upperRight = extents.MaxPoint;

                        ObjectId lastCircleId = ObjectId.Null;
                        foreach (ObjectId objId in btr1)
                        {
                            Entity ent = tr.GetObject(objId, OpenMode.ForRead) as Entity;
                            if (ent is Circle)
                            {
                                lastCircleId = objId; // Store last found circle
                            }
                        }

                        // If a circle was found, delete it
                        if (lastCircleId != ObjectId.Null)
                        {
                            Entity lastCircle = tr.GetObject(lastCircleId, OpenMode.ForWrite) as Entity;
                            lastCircle.Erase();
                            ed.WriteMessage("\nLast created circle removed.");
                        }
                        tr.Commit();
                        Create_Mesh();
                        //doc.SendStringToExecute("CREATEMESH\n", true, false, true);
                    }
                }
            }
        }

        private Point3d GetPolylineCentroid(Polyline pl)
        {
            double sumX = 0, sumY = 0;
            int numVerts = pl.NumberOfVertices;

            for (int i = 0; i < numVerts; i++)
            {
                Point2d vertex = pl.GetPoint2dAt(i);
                sumX += vertex.X;
                sumY += vertex.Y;
            }

            return new Point3d(sumX / numVerts, sumY / numVerts, 0);
        }
                
        public void Create_Mesh()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            Point3d minPoint = new Point3d(double.MaxValue, double.MaxValue, 0);
            Point3d maxPoint = new Point3d(double.MinValue, double.MinValue, 0);
            List<double> Zaxis = new List<double>();
            int Triangle_size = 200;

            if(Mesh_Dencity_Type.Equals('S'))
            {
                Triangle_size = 200;
            }
            else if(Mesh_Dencity_Type.Equals('M'))
            {
                Triangle_size = 300;
            }
            else if(Mesh_Dencity_Type.Equals('L'))
            {
                Triangle_size = 500;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                foreach (ObjectId objId in btr)
                {
                    Entity entity = tr.GetObject(objId, OpenMode.ForRead) as Entity;
                    if (entity is Polyline3d polyline3D || entity is Polyline polyline)
                    {
                        contourIds.Add(objId);

                    }
                }
                if (contourIds.Count > 0)
                {
                    string lowerLeftStr = $"{lowerLeft.X},{lowerLeft.Y}";
                    string upperRightStr = $"{upperRight.X},{upperRight.Y}";

                    doc.SendStringToExecute("DRAPE\n", true, false, true);
                    ed.SetImpliedSelection(contourIds.ToArray());
                    SelectionSet sset = ed.SelectImplied().Value;
                    ed.SetImpliedSelection(sset.GetObjectIds());
                    doc.SendStringToExecute("N\n", true, false, true);
                    doc.SendStringToExecute("Y\n", true, false, true);
                    doc.SendStringToExecute($"{lowerLeftStr}\n", true, false, true);
                    doc.SendStringToExecute($"{upperRightStr}\n", true, false, true);
                    doc.SendStringToExecute($"{Triangle_size}\n", true, false, true);
                    doc.SendStringToExecute("\n", true, false, true);
                    doc.SendStringToExecute("\n", true, false, true);
                }
                else
                {
                    ed.WriteMessage("\nNo 3D contour polylines found.");
                }
                Explode_Mesh();
                //doc.SendStringToExecute("COLAP\n", true, false, true);
                tr.Commit();
            }
        }
                
        public void Explode_Mesh()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            string targetLayer = "A-Area-Mass";

            List<ObjectId> blockIds = new List<ObjectId>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                foreach (ObjectId objId in btr)
                {
                    Entity entity = tr.GetObject(objId, OpenMode.ForRead) as Entity;

                    if (entity.Layer == targetLayer)
                    {
                        blockIds.Add(objId);
                    }

                }
                tr.Commit();
            }
            if (blockIds.Count > 0)
            {
                using (Transaction tr = doc.TransactionManager.StartTransaction())
                {
                    ObjectId objid = blockIds[0];
                    Autodesk.AutoCAD.DatabaseServices.Curve Selected_Obj = (Curve)tr.GetObject(objid, OpenMode.ForWrite);

                    DBObjectCollection db_Collection = new DBObjectCollection();
                    Selected_Obj.Explode(db_Collection);

                    // Add exploded entities to modelspace
                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    foreach (DBObject obj in db_Collection)
                    {
                        if (obj is Entity ent)
                        {
                            btr.AppendEntity(ent);
                            tr.AddNewlyCreatedDBObject(ent, true);
                        }
                    }

                    Selected_Obj.Erase();

                    foreach (object obj1 in db_Collection)
                    {
                        if (obj1 is BlockReference)
                        {
                            DBObjectCollection db_Collection1 = new DBObjectCollection();

                            BlockReference blockref = obj1 as BlockReference;
                            blockref.Explode(db_Collection1);

                            foreach (DBObject obj2 in db_Collection1)
                            {
                                if (obj2 is Entity ent)
                                {
                                    btr.AppendEntity(ent);
                                    tr.AddNewlyCreatedDBObject(ent, true);
                                }
                            }
                            blockref.Erase();
                        }
                    }
                    RemoveEntitiesOutsidePolyline();
                    //doc.SendStringToExecute("REMOVEMESH\n", true, false, false);
                    tr.Commit();
                }
            }
            else
            {
                ed.WriteMessage("\nNo blocks found on the specified layer.");
            }
        }
                
        public void RemoveEntitiesOutsidePolyline()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;


            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;

                // Open the ModelSpace for write
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                int removedCount = 0;

                // Step 2: Iterate through the entities in ModelSpace
                foreach (ObjectId objId in btr)
                {
                    Entity entity = tr.GetObject(objId, OpenMode.ForWrite) as Entity;

                    if (entity.Id == blockId)
                    {
                        entity.Erase();
                        break;
                    }
                }
                tr.Commit();
            }

            try
            {

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // Open the polyline for reading
                    Polyline polyline = tr.GetObject(polylineId, OpenMode.ForRead) as Polyline;

                    if (polyline == null || !polyline.Closed)
                    {
                        ed.WriteMessage("\nThe selected polyline must be closed.");
                        return;
                    }

                    // Open the block table for read
                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Open the ModelSpace for write
                    BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    int removedCount = 0;

                    // Step 2: Iterate through the entities in ModelSpace
                    foreach (ObjectId objId in btr)
                    {
                        Entity entity = tr.GetObject(objId, OpenMode.ForRead) as Entity;

                        if (entity is Face) // Check for 3D faces or other entities if needed
                        {
                            Face face = entity as Face;

                            // Get the vertices of the face
                            Point3d v1 = face.GetVertexAt(0);
                            Point3d v2 = face.GetVertexAt(1);
                            Point3d v3 = face.GetVertexAt(2);
                            Point3d v4 = face.IsPlanar ? face.GetVertexAt(3) : v3;

                            // Check if all vertices are inside the polyline
                            if (!IsPointInsidePolyline1(polyline, v1) || !IsPointInsidePolyline1(polyline, v2) ||
                                !IsPointInsidePolyline1(polyline, v3) || (face.IsPlanar && !IsPointInsidePolyline1(polyline, v4)))
                            {
                                // If any vertex is outside, remove the entity
                                entity.UpgradeOpen();
                                entity.Erase();
                                removedCount++;
                            }
                        }

                    }

                    // Commit the transaction
                    doc.SendStringToExecute("Slope\n", true, false, false);
                    tr.Commit();
                    ed.WriteMessage($"\n{removedCount} entities were removed.");


                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\nError: " + ex.Message);
            }

            string[] layerNames = { "Dark_Green", "Light_Green", "Yellow", "Red" };
            short colorIndex = 1;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                foreach (string layName in layerNames)
                {
                    bool validLayerName = false;

                    do
                    {
                        try
                        {
                            // Validate the initial layer name
                            SymbolUtilityServices.ValidateSymbolName(layName, false);

                            if (lt.Has(layName))
                            {
                                ed.WriteMessage("\nA layer with this name already exists.");
                                validLayerName = false;
                            }
                            else
                            {
                                validLayerName = true;
                            }
                        }
                        catch
                        {
                            ed.WriteMessage("\nInvalid layer name.");
                            validLayerName = false;
                        }

                        if (!validLayerName)
                        {
                            // Optionally, handle the case where the layer name is invalid
                            // For example, assign a new valid name or take other actions
                        }

                    } while (!validLayerName);

                    // Create our new layer table record
                    LayerTableRecord ltr = new LayerTableRecord();

                    // Set its properties
                    ltr.Name = layName;
                    ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex);

                    lt.UpgradeOpen();
                    ObjectId ltId = lt.Add(ltr);
                    tr.AddNewlyCreatedDBObject(ltr, true);

                    // Optionally set the layer to be current for this drawing
                    db.Clayer = ltId;

                    // Increment color index for the next layer
                    colorIndex++;

                    // Report what we've done
                    ed.WriteMessage("\nCreated layer named \"{0}\" with a color index of {1}.", layName, colorIndex);
                }

                // Commit the transaction
                tr.Commit();
            }
        }
        private bool IsPointInsidePolyline1(Polyline polyline, Point3d point)
        {
            Point2d point2d = new Point2d(point.X, point.Y);
            bool isInside = false;
            int numVertices = polyline.NumberOfVertices;
            Point2d lastVertex = polyline.GetPoint2dAt(numVertices - 1);

            for (int i = 0; i < numVertices; i++)
            {
                Point2d currentVertex = polyline.GetPoint2dAt(i);

                // Ray-casting logic
                if ((currentVertex.Y > point2d.Y) != (lastVertex.Y > point2d.Y) &&
                    (point2d.X < (lastVertex.X - currentVertex.X) * (point2d.Y - currentVertex.Y) /
                     (lastVertex.Y - currentVertex.Y) + currentVertex.X))
                {
                    isInside = !isInside;
                }

                lastVertex = currentVertex;
            }

            return isInside;
        }

        public class TerrainSlopeCalculator
        {
            public static int Lgreen = 0;
            public static int Yellow = 0;
            public static int Red = 0;
            public static int Green = 0;
            public static int totalFaces = 0;

            [CommandMethod("Slope")]
            public void CalculateFaceSlope()
            {

                Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                Editor ed = doc.Editor;
                Database db = doc.Database;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                    List<Triangle> triangles = new List<Triangle>();

                    // Iterate over all entities in Model Space
                    foreach (ObjectId objId in btr)
                    {
                        Entity entity = tr.GetObject(objId, OpenMode.ForWrite) as Entity;

                        // Check if the entity is a 3DFace
                        if (entity is Face face)
                        {

                            entity.LineWeight = LineWeight.LineWeight030;
                            // Extract the vertices of the 3DFace
                            Point3d v1 = face.GetVertexAt(0);
                            Point3d v2 = face.GetVertexAt(1);
                            Point3d v3 = face.GetVertexAt(2);

                            // If the face is not degenerate, create a triangle
                            if (v1 != v2 && v1 != v3 && v2 != v3)
                            {
                                Triangle tri = new Triangle(v1, v2, v3); // Create triangle object
                                triangles.Add(tri);

                                double slope = tri.CalculateSlope();
                                tri.ApplyColor(slope, tr, objId);

                            }
                        }
                    }

                    double lightGreenPercent = (Green / (double)totalFaces) * 100;
                    double cyanPercent = (Lgreen / (double)totalFaces) * 100;
                    double lightBrownPercent = (Yellow / (double)totalFaces) * 100;
                    double greenPercent = (Red / (double)totalFaces) * 100;

                    string message = "DarkGreen : " + lightGreenPercent.ToString() + "\n" +
                     "Light Green : " + cyanPercent.ToString() + "\n" +
                     "Yellow : " + lightBrownPercent.ToString() + "\n" +
                     "Red : " + greenPercent.ToString();

                    MessageBox.Show(message);
                    tr.Commit();
                }
            }
        }

        public class Triangle
        {
            public Point3d P1, P2, P3;

            public Triangle(Point3d p1, Point3d p2, Point3d p3)
            {
                P1 = p1; P2 = p2; P3 = p3;
            }

            public double CalculateSlope()
            {
                double d12 = P1.DistanceTo(P2);
                double d13 = P1.DistanceTo(P3);
                double d23 = P2.DistanceTo(P3);

                double dz12 = Math.Abs(P2.Z - P1.Z);
                double dz13 = Math.Abs(P3.Z - P1.Z);
                double dz23 = Math.Abs(P3.Z - P2.Z);

                double maxSlope = Math.Max(Math.Atan(dz12 / d12), Math.Atan(dz13 / d13));
                maxSlope = Math.Max(maxSlope, Math.Atan(dz23 / d23));

                return maxSlope * (180 / Math.PI); // Convert to degrees
            }

            internal void ApplyColor(double slope, Transaction tr, ObjectId objId)
            {
                Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                Editor ed = doc.Editor;
                Database db = doc.Database;

                Autodesk.AutoCAD.Colors.Color faceColor;
                bool a = false;
                bool b = false;
                bool c = false;
                bool d = false;
                if (slope >= 0 && slope <= 3)
                {
                    faceColor = Autodesk.AutoCAD.Colors.Color.FromRgb(34, 139, 34);
                    TerrainSlopeCalculator.Green++;
                    a = true;
                }
                else if (slope > 3 && slope <= 5)
                {
                    faceColor = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 0);
                    TerrainSlopeCalculator.Lgreen++;
                    b = true;
                }
                else if (slope > 5 && slope <= 10)
                {
                    faceColor = Autodesk.AutoCAD.Colors.Color.FromRgb(255, 255, 0);
                    TerrainSlopeCalculator.Yellow++;
                    c = true;
                }
                else
                {
                    faceColor = Autodesk.AutoCAD.Colors.Color.FromRgb(255, 0, 0);
                    TerrainSlopeCalculator.Red++;
                    d = true;
                }
                TerrainSlopeCalculator.totalFaces++;
                // Open the entity for writing
                Entity entity = tr.GetObject(objId, OpenMode.ForWrite) as Entity;
                if (entity != null)
                {
                    LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                    entity.Color = faceColor;
                    if (a == true)
                    {
                        entity.Layer = "Dark_Green";
                    }
                    else if (b == true)
                    {
                        entity.Layer = "Light_Green";
                    }
                    else if (c == true)
                    {
                        entity.Layer = "Yellow";
                    }
                    else if (d == true)
                    {
                        entity.Layer = "Red";
                    }

                }
            }
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
