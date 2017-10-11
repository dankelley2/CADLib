using System;
using System.Collections.Generic;
using System.Drawing;

namespace CAD
{
    public class GridSystem
    {
        public float gridScale { get; set; }
        public PointF gridOrigin { get; set; }
        public PointF cursorPosition { get; set; }
        public bool relativePositioning = false;
        public RectangleF gridBounds;
        public SizeF containerSize;
        private Pen gridPen = new Pen(Color.LightGray);
        private Pen cursPen = new Pen(Color.FromArgb(50, Color.Black), 3);
        public int DPI;
        public GridSystem(PointF origin, SizeF containerSize, float scale, int dpi)
        {
            this.gridScale = scale;
            this.gridOrigin = origin;
            this.containerSize = containerSize;
            this.cursorPosition = new PointF(0, 0);
            this.gridBounds = new RectangleF(origin, containerSize);
            this.DPI = dpi;
        }

        public void resizeGrid(SizeF newContainerSize)
        {
            this.gridBounds = new RectangleF(gridOrigin, newContainerSize);
        }

        public void ZoomIn()
        {
            if (gridScale < 10)
                gridScale += .25F;
        }

        public void ZoomOut()
        {
            if (gridScale > .25F)
                gridScale -= .25F;
        }

        public PointF realizePoint(PointF P)
        {
            P.X = (P.X * (DPI * gridScale)) + gridOrigin.X;
            P.Y = (P.Y * (DPI * gridScale)) + gridOrigin.Y;
            return P;
        }
        public PointF theorizePoint(PointF P)
        {
            P.X = (P.X - gridOrigin.X) / (DPI * gridScale);
            P.Y = (P.Y - gridOrigin.Y) / (DPI * gridScale);
            return P;
        }
        public void SetCursor(List<float> position)
        {
            if (position.Count == 2)
            {
                if (relativePositioning)
                {
                    float x1 = position[0] + this.cursorPosition.X;
                    float y1 = position[1] + this.cursorPosition.Y;
                    cursorPosition = new PointF(x1, y1);
                    string[] newpos = new string[2] { position[0].ToString(), position[1].ToString() };
                    Console.WriteLine("Cursor moved with a delta of {0}:{1}", newpos);
                }
                else
                {
                    float x1 = position[0];
                    float y1 = position[1];
                    cursorPosition = new PointF(x1, y1);
                    string[] newpos = new string[2] { position[0].ToString(), position[1].ToString() };
                    Console.WriteLine("Cursor set to {0}:{1}", newpos);
                }
            }
            else
            {
                Console.WriteLine("Invalid cursor point.");
            }
        }
        public void SnapCursorToPoint(PointF p1)
        {
            cursorPosition = GetNearestSnapPoint(p1);
        }
        public bool IsWithinCurrentBounds(PointF p1)
        {
            return gridBounds.Contains(p1);
        }
        public PointF GetNearestSnapPoint(PointF p1)
        {
            float x = 0;
            float y = 0;
            int gridSpacing = (int)((gridScale * DPI) / 4);
            int snapDistance = (gridSpacing / 2) + 1;

            for (float i = (int)gridBounds.X; i < (int)gridBounds.Width; i += gridSpacing)
            {
                if (Math.Abs(p1.X - i) < snapDistance)
                {
                    x = i;
                    break;
                }
            }
            for (float i = (int)gridBounds.Y; i < (int)gridBounds.Height; i += gridSpacing)
            {
                if (Math.Abs(p1.Y - i) < snapDistance)
                {
                    y = i;
                    break;
                }
            }
            return theorizePoint(new PointF(x, y));
        }

        public void TogglePositioning()
        {
            relativePositioning = (!(relativePositioning));
            if (relativePositioning)
            {
                Console.WriteLine("Relative positioning is now active.");
            }
            else
            {
                Console.WriteLine("Global positioning is now active.");
            }
        }

        public void Draw(Graphics g)
        {
            int gridSpacing = (int)((gridScale * DPI) / 4);
            //X Lines
            for (float i = (int)gridBounds.X; i < (int)gridBounds.Width; i += gridSpacing)
            {
                g.DrawLine(gridPen, new PointF(i, gridBounds.Y), new PointF(i, gridBounds.Height));
            }
            //Y Lines
            for (float i = (int)gridBounds.Y; i < (int)gridBounds.Height; i += gridSpacing)
            {
                g.DrawLine(gridPen, new PointF(gridBounds.X, i), new PointF(gridBounds.Width, i));
            }
        }
        public void DrawCurs(Graphics g)
        {
            //Cursor
            PointF Gy1 = realizePoint(new PointF(cursorPosition.X, cursorPosition.Y - .5F));
            PointF Gy2 = realizePoint(new PointF(cursorPosition.X, cursorPosition.Y + .5F));
            PointF Gx1 = realizePoint(new PointF(cursorPosition.X - .5F, cursorPosition.Y));
            PointF Gx2 = realizePoint(new PointF(cursorPosition.X + .5F, cursorPosition.Y));

            g.DrawLine(cursPen, Gy1, Gy2);
            g.DrawLine(cursPen, Gx1, Gx2);
        }


    }
}
