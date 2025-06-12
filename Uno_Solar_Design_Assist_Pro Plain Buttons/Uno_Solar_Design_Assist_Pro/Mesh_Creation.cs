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
using System.Linq;
using System.Threading.Tasks;

namespace Uno_Solar_Design_Assist_Pro
{
    public class Mesh_Creation : ICommand
    {
        public event EventHandler CanExecuteChanged;
        ObjectId squareId;
        ObjectId blockId;
        Point3d upperRight;
        Point3d lowerLeft;
        ObjectId polylineId;
        public static HashSet<string> polyline3dLayers = new HashSet<string>();
        public static HashSet<ObjectId> selectedPolyline3dIds = new HashSet<ObjectId>();
        public static List<double> percnt = new List<double>();

        public static int Lgreen = 0;
        public static int Yellow = 0;
        public static int Red = 0;
        public static int Green = 0;
        public static int totalFaces = 0;

        public static Dictionary<Color, int> colorCounts = new Dictionary<Color, int>();
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            Terrain_Mesh mesh = new Terrain_Mesh();
            Application.ShowModelessDialog(Application.MainWindow.Handle, mesh, false);
      

            if(mesh.DialogResult != DialogResult.OK)
            {
                return;
            }
        }
        public void selectContours()
        {

            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            bool continueSelecting = true;

            while (continueSelecting)
            {
                // === 1. User Selection ===
                SelectionFilter filter = new SelectionFilter(new TypedValue[]
                {
                new TypedValue((int)DxfCode.Start, "POLYLINE")
                });

                PromptSelectionOptions pso = new PromptSelectionOptions
                {
                    MessageForAdding = "\nSelect 3D Polylines: ",
                    MessageForRemoval = "\nRemove selection: "
                };
                HashSet<ObjectId> highlightedIds = new HashSet<ObjectId>();
                while (true)
                {

                    PromptEntityOptions peo = new PromptEntityOptions("\nSelect a 3D Polyline (Enter to exit): ");
                    peo.SetRejectMessage("\nOnly 3D Polylines allowed.");
                    peo.AddAllowedClass(typeof(Polyline3d), exactMatch: false);

                    PromptEntityResult per = ed.GetEntity(peo);

                    if (per.Status == PromptStatus.Cancel || per.Status == PromptStatus.None)
                    {
                        ed.WriteMessage("\nCommand ended.");
                        break;
                    }
                    using (doc.LockDocument())
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        Polyline3d selectedPolyline = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Polyline3d;

                        if (selectedPolyline != null)
                        {
                            string targetLayer = selectedPolyline.Layer;

                            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                            foreach (ObjectId objId in btr)
                            {
                                if (!highlightedIds.Contains(objId))
                                {
                                    Entity ent = tr.GetObject(objId, OpenMode.ForRead) as Entity;
                                    if (ent is Polyline3d poly && poly.Layer == targetLayer)
                                    {
                                        ent.UpgradeOpen();
                                        ent.Highlight();
                                        highlightedIds.Add(objId);
                                        polyline3dLayers.Add(ent.Layer);
                                        selectedPolyline3dIds.Add(ent.ObjectId);
                                    }
                                }
                            }
                        }

                        tr.Commit();
                    }
                }
                // === 3. Ask user to confirm or continue selecting ===
                PromptKeywordOptions confirmOptions = new PromptKeywordOptions("\nAre you satisfied with the highlighted selection?")
                {
                    AllowNone = false
                };
                confirmOptions.Keywords.Add("Yes");
                confirmOptions.Keywords.Add("No");
                confirmOptions.Keywords.Default = "Yes";

                PromptResult confirmResult = ed.GetKeywords(confirmOptions);
                continueSelecting = confirmResult.Status == PromptStatus.OK && confirmResult.StringResult == "No";
            }
            sqre();
            // doc.SendStringToExecute("sqre\n", true, false, true);
        }
        public void sqre()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            Point3d centroid;


            PromptEntityOptions polylineOptions = new PromptEntityOptions("\nSelect a closed polyline:");
            polylineOptions.SetRejectMessage("\nSelected entity must be a polyline.");
            polylineOptions.AddAllowedClass(typeof(Autodesk.AutoCAD.DatabaseServices.Polyline), true);
            PromptEntityResult polylineResult = ed.GetEntity(polylineOptions);

            if (polylineResult.Status != PromptStatus.OK)
                return;

            polylineId = polylineResult.ObjectId;

            using (Transaction tx = db.TransactionManager.StartTransaction())
            {

                Autodesk.AutoCAD.DatabaseServices.Polyline pl = tx.GetObject(polylineResult.ObjectId, OpenMode.ForRead) as Polyline;

                if (pl == null || !pl.Closed)
                {
                    ed.WriteMessage("\nThe selected polyline must be closed.");
                    return;
                }
                centroid = GetPolylineCentroid(pl);

                tx.Commit();
            }
            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity entity1 = tr.GetObject(polylineId, OpenMode.ForRead) as Entity;
                if (entity1.Layer != "BOUNDARY")
                {
                    System.Windows.MessageBox.Show("Please Select Boundry Layer outer Polyline");
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

                        //ObjectId squareId = tr.AddNewlyCreatedDBObject(square, true);
                        //squareId = btr.AppendEntity(square);
                        //tr.AddNewlyCreatedDBObject(square, true);


                        bt.UpgradeOpen();
                        bt.Add(btr);
                        tr.AddNewlyCreatedDBObject(btr, true);
                    }
                    double elevation = 0;
                    if (entity1 is Polyline poly)
                    {
                        elevation = poly.Elevation;
                    }

                    Point3d insertPoint = new Point3d(centroid.X, centroid.Y, elevation);

                    BlockTable bt1 = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr1 = (BlockTableRecord)tr.GetObject(bt1[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    BlockReference br = new BlockReference(insertPoint, bt1["SquareBlock"]);
                    btr1.AppendEntity(br);
                    tr.AddNewlyCreatedDBObject(br, true);
                    blockId = br.ObjectId;
                    Extents3d extents = br.GeometricExtents;

                    // Access the lower-left corner
                    lowerLeft = extents.MinPoint;

                    // Access the upper-right corner
                    upperRight = extents.MaxPoint;

                    //Entity entity = tr.GetObject(blockId, OpenMode.ForRead) as Entity;
                    //entity.Erase();
                    tr.Commit();

                    // doc.SendStringToExecute("CREATEMESH\n", true, false, true);
                    createmesh();
                }
            }
        }

        private Point3d GetPolylineCentroid(Autodesk.AutoCAD.DatabaseServices.Polyline pl)
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
        public async void createmesh()
        {
            int compactness = 0;
            if (Terrain_Mesh.Mesh_Dencity == 's')
            {
                compactness = 500;
            }
            else if (Terrain_Mesh.Mesh_Dencity == 'M')
            {
                compactness = 300;
            }
            else
            {
                compactness = 200;
            }
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {

                if (selectedPolyline3dIds.Count > 0)
                {
                    string lowerLeftStr = $"{lowerLeft.X},{lowerLeft.Y}";
                    string upperRightStr = $"{upperRight.X},{upperRight.Y}";


                    doc.SendStringToExecute("DRAPE\n", true, false, true);
                    ed.SetImpliedSelection(selectedPolyline3dIds.ToArray());
                    SelectionSet sset = ed.SelectImplied().Value;
                    ed.SetImpliedSelection(sset.GetObjectIds());
                    doc.SendStringToExecute("N\n", true, false, true);
                    doc.SendStringToExecute("Y\n", true, false, true);
                    doc.SendStringToExecute($"{lowerLeftStr}\n", true, false, true);
                    doc.SendStringToExecute($"{upperRightStr}\n", true, false, true);
                    doc.SendStringToExecute($"{compactness}\n", true, false, true);
                    doc.SendStringToExecute("\n", true, false, true);
                    doc.SendStringToExecute("\n", true, false, true);
                    await Task.Delay(200);

                }
                else
                {
                    ed.WriteMessage("\nNo 3D contour polylines found.");
                }
                tr.Commit();
                colap();

            }
        }
        public async void colap()
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
                using (doc.LockDocument())
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
                    await Task.Delay(200);
                    //doc.SendStringToExecute("REMOVEMESH\n", true, false, false);
                    tr.Commit();
                    Removemesh();

                }
            }
            else
            {
                ed.WriteMessage("\nNo blocks found on the specified layer.");
            }
        }
        public void Removemesh()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (doc.LockDocument())
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
                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\nError: " + ex.Message);
            }

            string[] layerNames = { "UnoTEAM_TOPOGRAPHY MESH" };
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
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Access the BlockTableRecord where entities are stored
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                int i = 0;
                // Loop through the entities in the BlockTableRecord
                foreach (ObjectId objId in btr)
                {
                    Entity ent = (Entity)tr.GetObject(objId, OpenMode.ForRead);

                    // Check if the entity matches the properties
                    if (ent is Line line && ent.Layer == "A-Area-Mass" && ent.Linetype == "HIDDEN2" && ent.Color.ColorIndex == 11)
                    {
                        i++;
                        // Upgrade the entity to writable and erase it
                        ent.UpgradeOpen();
                        ent.Erase();
                        if (i == 5)
                        {
                            break;
                        }
                    }
                }

                // Commit the transaction
                tr.Commit();
            }

            TerrainSlopeCalculator c = new TerrainSlopeCalculator();
            c.CalculateFaceSlope();
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
            public void CalculateFaceSlope()
            {

                Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                Editor ed = doc.Editor;
                Database db = doc.Database;
                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    List<Triangle> triangles = new List<Triangle>();
                    colorCounts.Clear();
                    // Iterate over all entities in Model Space
                    foreach (ObjectId objId in btr)
                    {
                        Entity entity = tr.GetObject(objId, OpenMode.ForWrite) as Entity;

                        // Check if the entity is a 3DFace
                        if (entity is Face face)
                        {

                            entity.LineWeight = LineWeight.LineWeight030;
                            Point3d v1 = face.GetVertexAt(0);
                            Point3d v2 = face.GetVertexAt(1);
                            Point3d v3 = face.GetVertexAt(2);

                            if (v1 != v2 && v1 != v3 && v2 != v3)
                            {
                                Triangle tri = new Triangle(v1, v2, v3);
                                triangles.Add(tri);

                                double slope = tri.CalculateSlope();
                                tri.ApplyColor(slope, tr, objId);
                            }
                        }
                    }


                    string report = $"Total Faces: {totalFaces}\n\n";

                    foreach (var pair in colorCounts)
                    {
                        string colorName = GetColorNameFromRGB(pair.Key);
                        double percentage = (double)pair.Value / totalFaces * 100;
                        percnt.Add(percentage);
                    }

                    tr.Commit();
                }
                string sourceLayer = "A-Area-Mass";
                string targetLayer = "UnoTEAM_TOPOGRAPHY MESH";

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                    if (!lt.Has(sourceLayer) || !lt.Has(targetLayer))
                    {
                        ed.WriteMessage("\nOne or both layers not found.");
                        return;
                    }

                    ObjectId sourceLayerId = lt[sourceLayer];
                    ObjectId targetLayerId = lt[targetLayer];

                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead);

                    foreach (ObjectId objId in btr)
                    {
                        Entity ent = tr.GetObject(objId, OpenMode.ForWrite) as Entity;
                        if (ent != null && ent.Layer == sourceLayer)
                        {
                            // Preserve the color visually if it's ByLayer
                            if (ent.Color.ColorMethod == Autodesk.AutoCAD.Colors.ColorMethod.ByLayer)
                            {
                                LayerTableRecord sourceLayerRec = (LayerTableRecord)tr.GetObject(sourceLayerId, OpenMode.ForRead);
                                ent.Color = sourceLayerRec.Color; // Apply actual color
                            }

                            ent.Layer = targetLayer;
                        }
                    }

                    // Optional: Delete the source layer after merging
                    try
                    {
                        LayerTableRecord layerToDelete = (LayerTableRecord)tr.GetObject(sourceLayerId, OpenMode.ForWrite);
                        if (!layerToDelete.IsErased && !layerToDelete.IsDependent)
                        {
                            layerToDelete.Erase(true);
                        }
                    }
                    catch
                    {
                        ed.WriteMessage("\nCould not delete the source layer. It may still be in use.");
                    }

                    tr.Commit();
                    ed.WriteMessage("\nLayer merge complete.");
                }
            }

            string GetColorNameFromRGB(Color color)
            {
                var knownColors = new Dictionary<string, (int R, int G, int B)>
                {
                    { "Dark Green", (0, 128, 0) },
                    { "Green", (85, 170, 0) },
                    { "Lime Green", (171, 213, 0) },
                    { "Bright Yellow", (255, 254, 0) },
                    { "Yellow Orange", (255, 223, 0) },
                    { "Golden Yellow", (255, 193, 0) },
                    { "Orange", (255, 161, 0) },
                    { "Orange Red", (255, 107, 0) },
                    { "Red Orange", (255, 53, 0) },
                    { "Red", (255, 0, 0) }
                };

                int r = color.Red;
                int g = color.Green;
                int b = color.Blue;

                string closestName = $"RGB({r},{g},{b})";
                int minDist = int.MaxValue;

                foreach (var kvp in knownColors)
                {
                    int dr = r - kvp.Value.R;
                    int dg = g - kvp.Value.G;
                    int db = b - kvp.Value.B;
                    int dist = dr * dr + dg * dg + db * db;

                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestName = kvp.Key;
                    }
                }

                if (minDist > 900)
                    return $"RGB({r},{g},{b})";

                return closestName;
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

                Autodesk.AutoCAD.Colors.Color faceColor = Autodesk.AutoCAD.Colors.Color.FromRgb(128, 128, 128); // Default gray

                for (int i = 0; i < Terrain_Mesh.angleMinList.Count; i++)
                {
                    if (slope >= Terrain_Mesh.angleMinList[i] && slope <= Terrain_Mesh.angleMaxList[i])
                    {
                        System.Drawing.Color color = Terrain_Mesh.colorList[i];
                        faceColor = Autodesk.AutoCAD.Colors.Color.FromRgb(color.R, color.G, color.B);
                        if (!colorCounts.ContainsKey(faceColor))
                            colorCounts[faceColor] = 1;
                        else
                            colorCounts[faceColor]++;
                        totalFaces++;

                        Entity ent = tr.GetObject(objId, OpenMode.ForWrite) as Entity;
                        if (ent != null)
                        {
                            ent.Color = faceColor;
                        }
                        break;
                    }
                }
            }
        }



    }
}
