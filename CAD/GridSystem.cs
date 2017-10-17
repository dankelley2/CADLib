﻿using System;
using System.Collections.Generic;
using System.Drawing;

namespace CAD
{
    public class GridSystem
    {
        public float gridIncrements { get; set; }
        public float gridScale { get; set; }
        public PointF containerOrigin { get; set; }
        public PointF gridOrigin { get; set; }
        public PointF cursorPosition { get; set; }
        public bool showGrid   = true;
        public bool showOrigin = true;
        public bool showSnaps  = true;
        public bool showDims = true;
        public bool relativePositioning = false;
        public RectangleF gridBounds;
        public SizeF containerSize;
        public int subUnits = 4;
        private Pen gridPen = new Pen(Color.FromArgb(255,240,240,240));
        private Pen cursPen = new Pen(Color.FromArgb(100, Color.Black), 3);
        private Pen origPen = new Pen(Color.FromArgb(100, Color.Green), 3);
        public int DPI;
        //all sizes currently in inches
        
        public GridSystem(PointF containerOrigin, SizeF containerSize, float gridIncrements, int dpi)
        {
            this.gridIncrements = gridIncrements;
            this.containerOrigin = containerOrigin;
            this.containerSize = containerSize;
            this.cursorPosition = new PointF(0, 0);
            this.gridBounds = new RectangleF(containerOrigin, containerSize);
            this.DPI = dpi;
            this.gridScale = 1;
            this.gridOrigin = containerOrigin;
        }

        public bool toggleGrid()
        {
            showGrid = !(showGrid);
            return showGrid;
        }
        public bool toggleSnaps()
        {
            showSnaps = !(showSnaps);
            return showSnaps;
        }
        public bool toggleOrigin()
        {
            showOrigin = !(showOrigin);
            return showOrigin;
        }
        public bool toggleDims()
        {
            showDims = !(showDims);
            return showDims;
        }

        public float getGridIncrements()
        {
            return gridIncrements;
        }

        public bool setGridIncrements(float newSize)
        {
            if (newSize > 0)
            {
                gridIncrements = newSize;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void resizeGrid(SizeF newContainerSize)
        {
            this.gridBounds = new RectangleF(containerOrigin, newContainerSize);
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

        public float getZoomScale()
        {
            return gridScale;
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
        public PointF GetCursorReal()
        {
            return realizePoint(cursorPosition);
        }
        public bool CheckCircleLineIntersection(PointF CPoint, float CRad, PointF A, PointF B)
        {
            //Distance between two points
            var d = Math.Sqrt(Math.Pow((B.X - A.X), 2) +
                                    Math.Pow((B.Y - A.Y), 2));
            //Get nearest of C
            var a = (1 / Math.Pow(d, 2)) *
                    (
                        ((B.X - A.X) * (CPoint.X - A.X)) +
                        ((B.Y - A.Y) * (CPoint.Y - A.Y))
                    );
            PointF nearest = new PointF();
            nearest.X = (float)(A.X + (B.X - A.X) * a);
            nearest.Y = (float)(A.Y + (B.Y - A.Y) * a);

            //Check if point within circle
            var distToPoint = Math.Sqrt(Math.Pow((CPoint.X - nearest.X), 2) +
                                    Math.Pow((CPoint.Y - nearest.Y), 2));
            if(distToPoint <= CRad)
            {
                
                //line intersects, but check in line segment
                var ma = Math.Sqrt(Math.Pow((A.X - nearest.X), 2) +
                                        Math.Pow((A.Y - nearest.Y), 2));
                var mb = Math.Sqrt(Math.Pow((B.X - nearest.X), 2) +
                                        Math.Pow((B.Y - nearest.Y), 2));

                //line intersects, but check in line segment
                var ac = Math.Sqrt(Math.Pow((CPoint.X - nearest.X), 2) +
                                        Math.Pow((CPoint.Y - A.Y), 2));
                var bc = Math.Sqrt(Math.Pow((CPoint.X - nearest.X), 2) +
                                        Math.Pow((CPoint.Y - nearest.Y), 2));
                if (ma <= d && mb <= d)
                {
                    if (ac <= CRad || bc <= CRad)
                    {
                        return true;
                    }
                }

            }
            return false;
        }
        public void SnapOriginToPoint(PointF p1)
        {
            gridOrigin = GetNearestSnapPoint_Real(p1);
        }
        public bool IsWithinCurrentBounds(PointF p1)
        {
            return gridBounds.Contains(p1);
        }
        public PointF GetNearestSnapPoint(PointF p1)
        {
            float x = 0;
            float y = 0;
            float gridSpacing = (gridIncrements * (DPI * gridScale));
            float snapDistance = (gridSpacing / 2) + 1;
            PointF theoPoint = theorizePoint(p1);
            if (!(showSnaps) && !(showGrid)) { return theoPoint; }

            if (showSnaps)
            {
                foreach (PointF P in ShapeSystem.SnapPoints)
                {
                    if (Math.Abs(theoPoint.X - P.X) < .0625F && Math.Abs(theoPoint.Y - P.Y) < .0625F)
                    {
                        x = P.X;
                        y = P.Y;
                        return P;
                    }
                }
            }
            if (showGrid)
            {
                //Forwards X
                for (float i = (int)gridOrigin.X; i < (int)gridBounds.Right; i += gridSpacing)
                {
                    if (Math.Abs(p1.X - i) < snapDistance)
                    {
                        x = i;
                        break;
                    }
                }
                //Backwards X
                for (float i = (int)gridOrigin.X; i > (int)gridBounds.Left; i -= gridSpacing)
                {
                    if (Math.Abs(p1.X - i) < snapDistance)
                    {
                        x = i;
                        break;
                    }
                }
                //Forwards Y
                for (float i = (int)gridOrigin.Y; i < (int)gridBounds.Bottom; i += gridSpacing)
                {
                    if (Math.Abs(p1.Y - i) < snapDistance)
                    {
                        y = i;
                        break;
                    }
                }
                //Backwards Y
                for (float i = (int)gridOrigin.Y; i > (int)gridBounds.Top; i -= gridSpacing)
                {
                    if (Math.Abs(p1.Y - i) < snapDistance)
                    {
                        y = i;
                        break;
                    }
                }
            }
            return theorizePoint(new PointF(x, y));
        }
        public PointF GetNearestSnapPoint_Real(PointF p1)
        {
            float x = 0;
            float y = 0;
            float gridSpacing = ((gridIncrements * DPI) / 4);
            float snapDistance = (gridSpacing / 2) + 1;

            //Forwards X
            for (float i = (int)gridOrigin.X; i < (int)gridBounds.Right; i += gridSpacing)
            {
                if (Math.Abs(p1.X - i) < snapDistance)
                {
                    x = i;
                    break;
                }
            }
            //Backwards X
            for (float i = (int)gridOrigin.X; i > (int)gridBounds.Left; i -= gridSpacing)
            {
                if (Math.Abs(p1.X - i) < snapDistance)
                {
                    x = i;
                    break;
                }
            }
            //Forwards Y
            for (float i = (int)gridOrigin.Y; i < (int)gridBounds.Bottom; i += gridSpacing)
            {
                if (Math.Abs(p1.Y - i) < snapDistance)
                {
                    y = i;
                    break;
                }
            }
            //Backwards Y
            for (float i = (int)gridOrigin.Y; i > (int)gridBounds.Top; i -= gridSpacing)
            {
                if (Math.Abs(p1.Y - i) < snapDistance)
                {
                    y = i;
                    break;
                }
            }
            return new PointF(x, y);
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

        public void DrawGrid(Graphics g)
        {
            if (!(showGrid)) { return; }
            g.Clip = new Region(gridBounds);

            float gridSpacing = (gridIncrements * (DPI * gridScale));
            //X+ Lines
            for (float i = (int)gridOrigin.X; i < (int)gridBounds.Right; i += gridSpacing)
            {
                g.DrawLine(gridPen, new PointF(i, gridBounds.Y), new PointF(i, gridBounds.Height));
            }
            //Y+ Lines
            for (float i = (int)gridOrigin.Y; i < (int)gridBounds.Bottom; i += gridSpacing)
            {
                g.DrawLine(gridPen, new PointF(gridBounds.X, i), new PointF(gridBounds.Width, i));
            }
            //X- Lines
            for (float i = (int)gridOrigin.X; i > (int)gridBounds.Left; i -= gridSpacing)
            {
                g.DrawLine(gridPen, new PointF(i, gridBounds.Y), new PointF(i, gridBounds.Height));
            }
            //Y- Lines
            for (float i = (int)gridOrigin.Y; i > (int)gridBounds.Top; i -= gridSpacing)
            {
                g.DrawLine(gridPen, new PointF(gridBounds.X, i), new PointF(gridBounds.Width, i));
            }

            //Draw Origin
            PointF Gy1 = new PointF(gridOrigin.X, gridOrigin.Y - 50);
            PointF Gy2 = new PointF(gridOrigin.X, gridOrigin.Y + 50);
            PointF Gx1 = new PointF(gridOrigin.X - 50, gridOrigin.Y);
            PointF Gx2 = new PointF(gridOrigin.X + 50, gridOrigin.Y);

            g.DrawLine(origPen, Gy1, Gy2);
            g.DrawLine(origPen, Gx1, Gx2);
        }
        public void DrawOrigin(Graphics g)
        {
            if (!(showOrigin)) { return; }
            g.Clip = new Region(gridBounds);
            
            //Draw Origin
            PointF Gy1 = new PointF(gridOrigin.X, gridOrigin.Y - 50);
            PointF Gy2 = new PointF(gridOrigin.X, gridOrigin.Y + 50);
            PointF Gx1 = new PointF(gridOrigin.X - 50, gridOrigin.Y);
            PointF Gx2 = new PointF(gridOrigin.X + 50, gridOrigin.Y);

            g.DrawLine(origPen, Gy1, Gy2);
            g.DrawLine(origPen, Gx1, Gx2);
        }
        public void DrawCurs(Graphics g)
        {
            g.Clip = new Region(gridBounds);
            //Cursor
            PointF C = realizePoint(new PointF(cursorPosition.X, cursorPosition.Y));

            g.DrawLine(cursPen, new PointF(C.X - 25, C.Y), new PointF(C.X + 25, C.Y));
            g.DrawLine(cursPen, new PointF(C.X, C.Y - 25), new PointF(C.X, C.Y + 25));
        }
        public void DrawSnaps(Graphics g)
        {
            if (!(showSnaps)) { return; }
            g.Clip = new Region(gridBounds);
            //Cursor
            foreach (PointF S in ShapeSystem.GetSnapPoints())
            {
                PointF P = realizePoint(S);
                g.DrawLine(new Pen(Color.Orange), new PointF(P.X - 10, P.Y), new PointF(P.X + 10, P.Y));
                g.DrawLine(new Pen(Color.Orange), new PointF(P.X, P.Y - 10), new PointF(P.X, P.Y + 10));
            }
        }
        public void DrawLineToCursor(Graphics g,PointF start, PointF end)
        {
            g.DrawLine(cursPen, start, end);
        }


    }
}
