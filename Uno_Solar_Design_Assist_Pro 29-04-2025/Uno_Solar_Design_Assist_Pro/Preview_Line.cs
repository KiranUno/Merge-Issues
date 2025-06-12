using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.GraphicsInterface;

namespace Uno_Solar_Design_Assist_Pro
{
    internal class Preview_Line : DrawJig
    {
        private Point3d _startPoint;
        private Point3d _endPoint;
        private bool _hasSecondPoint = false;

        public Preview_Line(Point3d startPoint)
        {
            _startPoint = startPoint;
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            PromptPointResult result = prompts.AcquirePoint("\nSelect next point (or press Enter to finish): ");

            if (result.Status != PromptStatus.OK)
                return SamplerStatus.Cancel;

            if (_endPoint == result.Value)
                return SamplerStatus.NoChange;

            _endPoint = result.Value;
            _hasSecondPoint = true;
            return SamplerStatus.OK;
        }

        protected override bool WorldDraw(WorldDraw draw)
        {
            if (_hasSecondPoint)
            {
                Line tempLine = new Line(_startPoint, _endPoint);
                draw.Geometry.Draw(tempLine);
            }
            return true;
        }
        public Point3d GetSecondPoint()
        {
            return _hasSecondPoint ? _endPoint : Point3d.Origin;
        }
    }
}
