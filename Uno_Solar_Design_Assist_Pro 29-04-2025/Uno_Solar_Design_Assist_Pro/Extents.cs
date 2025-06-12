using System;
using System.Windows.Input;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using System.Collections.Generic;

namespace Uno_Solar_Design_Assist_Pro
{
    internal class Extents : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (DocumentLock docklock = doc.LockDocument())
            {
                using (Transaction tr = doc.TransactionManager.StartTransaction())
                {
                    List<BlockReference> selectedBlocks = SelectBlocks(ed, tr);

                    if(selectedBlocks.Count <= 0)
                    {
                        return;
                    }

                    foreach (BlockReference block in selectedBlocks)
                    {
                        try
                        {
                            Extents3d extents = block.GeometricExtents;

                            double length = extents.MaxPoint.X - extents.MinPoint.X;
                            double height = extents.MaxPoint.Y - extents.MinPoint.Y;
                            double thickness = extents.MaxPoint.Z - extents.MinPoint.Z;

                            ed.WriteMessage("\n MaxPoint = ( " + extents.MaxPoint.ToString() + " )");

                            ed.WriteMessage("\n MinPoint = ( " + extents.MinPoint.ToString() + " )");

                        }
                        catch (Autodesk.AutoCAD.Runtime.Exception ex)
                        {
                            ed.WriteMessage($"\nError getting extents: {ex.Message}");
                        }
                    }
                }
            }                
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

                            ed.WriteMessage("\n MaxPoint = ( " + extents.MaxPoint.ToString() + " )");

                            ed.WriteMessage("\n MinPoint = ( " + extents.MinPoint.ToString() + " )");

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
    }
}
