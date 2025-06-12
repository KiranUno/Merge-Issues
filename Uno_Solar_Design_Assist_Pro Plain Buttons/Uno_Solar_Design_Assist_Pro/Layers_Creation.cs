using System;
using System.Windows.Input;
using Autodesk.AutoCAD.Colors;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Uno_Solar_Design_Assist_Pro
{
    public class LayerInfo
    {
        public string Name { get; set; }
        public Color Color { get; set; }
        public string Linetype { get; set; }
        public LineWeight Lineweight { get; set; }
    }

    internal class Layers_Creation : ICommand
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
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                    if (layerTable != null)
                    {
                        List<LayerInfo> layers = new List<LayerInfo>
                        {
                            new LayerInfo { Name = "UnoTEAM_TOPOGRAPHY MESH",   Color = Color.FromRgb(34, 139, 34),     Linetype = "Continuous", Lineweight = LineWeight.LineWeight030 }, // Forest Green
                            new LayerInfo { Name = "UnoTEAM_FRAMES",            Color = Color.FromRgb(0, 255, 255),     Linetype = "Continuous", Lineweight = LineWeight.LineWeight000 }, // Cyan
                            new LayerInfo { Name = "UnoTEAM_LIGHTNING ARRESTER", Color = Color.FromRgb(255, 0, 0),      Linetype = "Continuous", Lineweight = LineWeight.LineWeight000 }, // Red
                            new LayerInfo { Name = "UnoTEAM_ROADS",             Color = Color.FromRgb(255, 0, 0),       Linetype = "Continuous", Lineweight = LineWeight.LineWeight000 }, // Red
                            new LayerInfo { Name = "UnoTEAM_TRENCHES",          Color = Color.FromRgb(255, 255, 255),   Linetype = "Continuous", Lineweight = LineWeight.LineWeight000 }, // White
                            new LayerInfo { Name = "UnoTEAM_MODULES",           Color = Color.FromRgb(0, 124, 165),     Linetype = "Continuous", Lineweight = LineWeight.LineWeight000 }, // Blue Sapphire
                            new LayerInfo { Name = "UnoTEAM_POLES",             Color = Color.FromRgb(255, 0, 0),       Linetype = "Continuous", Lineweight = LineWeight.LineWeight000 }, // Red
                            new LayerInfo { Name = "UnoTEAM_POLES COORDINATES", Color = Color.FromRgb(0, 0, 0),         Linetype = "Text",       Lineweight = LineWeight.LineWeight000 }, // Black
                            new LayerInfo { Name = "UnoTEAM_TABLE NUMBER",      Color = Color.FromRgb(255, 255, 255),   Linetype = "Text",       Lineweight = LineWeight.LineWeight000 }, // White
                            new LayerInfo { Name = "UnoTEAM_POLARITY SYMBOLS",  Color = Color.FromRgb(0, 165, 0),       Linetype = "Text",       Lineweight = LineWeight.LineWeight000 }, // Emerald Green
                            new LayerInfo { Name = "UnoTEAM_STRINGING",         Color = Color.FromRgb(0, 165, 0),       Linetype = "Continuous", Lineweight = LineWeight.LineWeight000 }, // Emerald Green
                            new LayerInfo { Name = "UnoTEAM_AC CABLES",         Color = Color.FromRgb(0, 127, 255),     Linetype = "Continuous", Lineweight = LineWeight.LineWeight000 }, // Deep Sky Blue
                            new LayerInfo { Name = "UnoTEAM_DC CABELS",         Color = Color.FromRgb(0, 0, 255),       Linetype = "Continuous", Lineweight = LineWeight.LineWeight000 }, // Blue
                            new LayerInfo { Name = "UnoTEAM_TRANSFORMER BLOCK", Color = Color.FromRgb(255, 255, 255),   Linetype = "Block",      Lineweight = LineWeight.LineWeight000 }, // White
                            new LayerInfo { Name = "UnoTEAM_INVERTER BLOCK",    Color = Color.FromRgb(255, 255, 255),   Linetype = "Block",      Lineweight = LineWeight.LineWeight000 }, // White
                            new LayerInfo { Name = "UnoTEAM_GROUPING",          Color = Color.FromRgb(0, 165, 82),      Linetype = "Continuous", Lineweight = LineWeight.LineWeight000 }, // Green Cyan
                            new LayerInfo { Name = "UnoTEAM_GROUNDING",         Color = Color.FromRgb(255, 128, 0),     Linetype = "Continuous", Lineweight = LineWeight.LineWeight000 }, // Dark Orange
                            new LayerInfo { Name = "UnoTEAM_DC TRENCH",         Color = Color.FromRgb(0, 255, 0),       Linetype = "DC TRENCH",  Lineweight = LineWeight.LineWeight000 }, // Lime
                            new LayerInfo { Name = "UnoTEAM_2AC CABEL TRENCH",  Color = Color.FromRgb(255, 0, 255),     Linetype = "2AC CABEL TRENCH", Lineweight = LineWeight.LineWeight000 }, // Magenta
                            new LayerInfo { Name = "UnoTEAM_3AC CABEL TRENCH",  Color = Color.FromRgb(255, 0, 0),       Linetype = "3AC CABEL TRENCH", Lineweight = LineWeight.LineWeight000 }, // Red
                            new LayerInfo { Name = "UnoTEAM_4AC CABEL TRENCH",  Color = Color.FromRgb(0, 255, 255),     Linetype = "4AC CABEL TRENCH", Lineweight = LineWeight.LineWeight000 }, // Cyan
                            new LayerInfo { Name = "UnoTEAM_5AC CABEL TRENCH",  Color = Color.FromRgb(0, 255, 255),     Linetype = "5AC CABEL TRENCH", Lineweight = LineWeight.LineWeight000 } // Cyan
                        };

                        foreach (var layerInfo in layers)
                        {
                            if (!layerTable.Has(layerInfo.Name))
                            {
                                layerTable.UpgradeOpen();

                                LayerTableRecord newLayer = new LayerTableRecord
                                {
                                    Name = layerInfo.Name,
                                    Color = layerInfo.Color,
                                    LineWeight = layerInfo.Lineweight
                                };

                                // Set Linetype
                                LinetypeTable linetypeTable = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                                if (linetypeTable.Has(layerInfo.Linetype))
                                {
                                    newLayer.LinetypeObjectId = linetypeTable[layerInfo.Linetype];
                                }
                                else
                                {
                                    // Fallback if linetype not found
                                    newLayer.LinetypeObjectId = db.ContinuousLinetype;
                                }

                                layerTable.Add(newLayer);
                                tr.AddNewlyCreatedDBObject(newLayer, true);
                            }
                        }
                    }
                    tr.Commit();
                }
            }            
        }
    }
}
