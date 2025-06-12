using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Controls;
using System.Windows.Forms;
using Control = System.Windows.Forms.Control;
using Image = System.Drawing.Image;
using Label = System.Windows.Forms.Label;
using TextBox = System.Windows.Controls.TextBox;

namespace Uno_Solar_Design_Assist_Pro
{
    internal class ZoomablePictureBox : PictureBox
    {
        private float _zoomFactor = 0.8f;
        private bool _isDraggingImage = false;
        private Point _lastMousePos;
        private const float MinZoomFactor = 0.8f;
        private const float MaxZoomFactor = 2.0f;
        public PointF ImageOffset { get; set; } = new PointF(0, 0);

        private Control _draggedLabel = null;
        private Point _labelDragStartMousePos;
        private Point _labelDragStartLabelPos;

        private Dictionary<Label, PointF> labelLogicalPositions = new();

        public float ZoomFactor
        {
            get => _zoomFactor;
            set
            {
                float oldZoom = _zoomFactor;
                _zoomFactor = Math.Max(MinZoomFactor, Math.Min(MaxZoomFactor, value));
                UpdateLabelPositions(oldZoom);
                Invalidate();
            }
        }

        public ZoomablePictureBox()
        {
            DoubleBuffered = true;
            SizeMode = PictureBoxSizeMode.Normal;
            BackColor = Color.White;

            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            MouseWheel += OnMouseWheel;
            //Resize += (s, e) => CenterImage();
            Resize += (s, e) => CenterImage();

        }

        public void RegisterDraggableLabel(Label label)
        {
            label.MouseDown += Label_MouseDown;
            label.MouseMove += Label_MouseMove;
            label.MouseUp += Label_MouseUp;
            label.Cursor = Cursors.Hand;

            var logical = ScreenToLogical(label.Location);
            labelLogicalPositions[label] = logical;
            Controls.Add(label);
            UpdateLabelPositions();
        }

        private void Label_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _draggedLabel = sender as Control;
                _labelDragStartMousePos = e.Location;
                _labelDragStartLabelPos = _draggedLabel.Location;
                Cursor = Cursors.SizeAll;
            }
        }

        private void Label_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedLabel != null && e.Button == MouseButtons.Left)
            {
                int dx = e.X - _labelDragStartMousePos.X;
                int dy = e.Y - _labelDragStartMousePos.Y;

                var newPos = new Point(
                    _labelDragStartLabelPos.X + dx,
                    _labelDragStartLabelPos.Y + dy
                );

                _draggedLabel.Location = newPos;

                if (_draggedLabel is Label lbl)
                {
                    labelLogicalPositions[lbl] = ScreenToLogical(lbl.Location);
                }
            }
        }

        private void Label_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _draggedLabel = null;
                //Cursor = Cursors.Default;
                Cursor = Cursors.SizeAll;
            }
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (Image == null || _draggedLabel != null) return;

            _isDraggingImage = true;
            _lastMousePos = e.Location;
            Cursor = Cursors.SizeAll;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingImage && Image != null)
            {
                float dx = e.X - _lastMousePos.X;
                float dy = e.Y - _lastMousePos.Y;

                var newOffset = new PointF(ImageOffset.X + dx, ImageOffset.Y + dy);
                ImageOffset = ClampImageOffset(newOffset);

                _lastMousePos = e.Location;

                UpdateLabelPositions();
                Invalidate();
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            _isDraggingImage = false;
            Cursor = Cursors.Default;
        }

        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            if (Image == null) return;

            float oldZoom = ZoomFactor;
            float zoomChange = e.Delta > 0 ? 1.1f : 0.8f;
            ZoomFactor *= zoomChange;

            float ratio = ZoomFactor / oldZoom;

            // Get the center of the control
            float cx = Width / 2f;
            float cy = Height / 2f;

            // Adjust offset so the zoom is centered from the control center
            var newOffset = new PointF(
                cx - (cx - ImageOffset.X) * ratio,
                cy - (cy - ImageOffset.Y) * ratio
            );

            ImageOffset = ClampImageOffset(newOffset);
            UpdateLabelPositions();
            Invalidate();
        }

        private void UpdateLabelPositions(float oldZoom = -1)
        {
            foreach (var pair in labelLogicalPositions)
            {
                var label = pair.Key;
                var logical = pair.Value;

                var screen = LogicalToScreen(logical);
                label.Location = new Point((int)screen.X, (int)screen.Y);
            }
        }

        private PointF LogicalToScreen(PointF logical)
        {
            return new PointF(
                logical.X * ZoomFactor + ImageOffset.X,
                logical.Y * ZoomFactor + ImageOffset.Y
            );
        }

        private PointF ScreenToLogical(Point screen)
        {
            return new PointF(
                (screen.X - ImageOffset.X) / ZoomFactor,
                (screen.Y - ImageOffset.Y) / ZoomFactor
            );
        }
        public void CenterImage()
        {
            if (Image != null)
            {
                var offset = new PointF(
                    (Width - Image.Width * ZoomFactor) / 2f,
                    (Height - Image.Height * ZoomFactor) / 2f
                );
                ImageOffset = offset;
                UpdateLabelPositions();
                Invalidate();
            }
        }


        //private void CenterImage()
        //{
        //    if (Image != null)
        //    {
        //        var offset = new PointF(
        //            (Width - Image.Width * ZoomFactor) / 2f,
        //            (Height - Image.Height * ZoomFactor) / 2f
        //        );
        //        ImageOffset = ClampImageOffset(offset);
        //        UpdateLabelPositions();
        //        Invalidate();
        //    }
        //}

        private PointF ClampImageOffset(PointF offset)
        {
            if (Image == null) return offset;

            float imgWidth = Image.Width * ZoomFactor;
            float imgHeight = Image.Height * ZoomFactor;

            // Only clamp when image is smaller than the container
            float minX = (imgWidth < Width) ? (Width - imgWidth) / 2 : Width - imgWidth;
            float maxX = (imgWidth < Width) ? (Width - imgWidth) / 2 : 0;

            float minY = (imgHeight < Height) ? (Height - imgHeight) / 2 : Height - imgHeight;
            float maxY = (imgHeight < Height) ? (Height - imgHeight) / 2 : 0;

            float clampedX = Math.Min(maxX, Math.Max(minX, offset.X));
            float clampedY = Math.Min(maxY, Math.Max(minY, offset.Y));

            return new PointF(clampedX, clampedY);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Image == null)
            {
                base.OnPaint(e);
                return;
            }
            var g = e.Graphics;
            g.TranslateTransform(ImageOffset.X, ImageOffset.Y);
            g.ScaleTransform(_zoomFactor, _zoomFactor);
            g.DrawImage(Image, 0, 0);
            base.OnPaint(e);
        }

    }
}




