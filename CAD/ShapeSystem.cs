using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CAD
{
    [Serializable]
    public class ShapeSystem
    {

        static List<Shape> ShapeList = new List<Shape>();
        static Pen basicPen { get; set; }
        static Pen activePen { get; set; }
        static GridSystem gridSystem { get; set; }
        public void SetGrid(GridSystem grid)
        {
            gridSystem = grid;
        }
        public Shape GetShapeById(int Id)
        {
            Shape S = ShapeList.Where(s => s.IdShape == Id).FirstOrDefault();
            return S;
        }
        public Shape GetActiveShape()
        {
            Shape S = ShapeList.Where(s => s.isActiveShape == true).FirstOrDefault();
            return S;
        }
        public static void MakeActiveShape(Shape S)
        {
            foreach (Shape shape in ShapeList)
            {
                shape.isActiveShape = false;
            }
            S.isActiveShape = true;
        }
        public void RefreshAll(Graphics g)
        {
            foreach (Shape S in ShapeList)
            {
                S.Draw(g);
            }
        }
        public List<Shape> GetShapes()
        {
            return ShapeList;
        }
        public void setBaseColors(Pen basic, Pen active)
        {
            ShapeSystem.basicPen = basic;
            ShapeSystem.activePen = active;
        }

        public abstract class Shape
        {
            public abstract void Draw(Graphics g);
            public bool isActiveShape { get; set; }
            public string MetaName;
            public string MetaDesc;
            public bool needsRedraw;
            public int IdShape;
            public Shape()
            {
                ShapeList.Add(this);
                IdShape = ShapeList.Count();
                ShapeSystem.MakeActiveShape(this);
            }
        }

        [Serializable]
        public class Line : Shape
        {
            internal PointF P1;
            internal PointF P2;
            public Line(PointF p1, PointF p2)
            {
                this.P1 = p1;
                this.P2 = p2;
                this.MetaName = "Line";
                object[] MetaDescArray = { this.P1.X, this.P1.Y, this.P2.X, this.P2.Y };
                this.MetaDesc = string.Format("{0},{1}:{2},{3}", MetaDescArray);
            }

            public static void AddLine(List<float> Input)
            {
                if (gridSystem.relativePositioning && Input.Count == 2)
                {
                    PointF p1 = new PointF(gridSystem.cursorPosition.X, gridSystem.cursorPosition.Y);

                    float x2 = (Input[0] + p1.X);
                    float y2 = (Input[1] + p1.Y);
                    PointF p2 = new PointF(x2, y2);
                    Line RetLine = new Line(p1, p2);
                    gridSystem.cursorPosition = p2;
                    return;
                }
                if (Input.Count != 4)
                {
                    Console.WriteLine("Invalid input.");
                    return;
                }
                else
                {
                    float x1 = (Input[0]);
                    float y1 = (Input[1]);
                    float x2 = (Input[2]);
                    float y2 = (Input[3]);
                    PointF p1 = new PointF(x1, y1);
                    PointF p2 = new PointF(x2, y2);
                    Line RetLine = new Line(p1, p2);
                    gridSystem.cursorPosition = p2;
                    return;
                }
            }

            public override void Draw(Graphics g)
            {
                if (isActiveShape)
                    g.DrawLine(activePen, gridSystem.realizePoint(P1), gridSystem.realizePoint(P2));
                else
                    g.DrawLine(basicPen, gridSystem.realizePoint(P1), gridSystem.realizePoint(P2));

            }
        }

    }

}
