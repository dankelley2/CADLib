using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace CAD
{
    [Serializable]
    public class ShapeSystem
    {

        static List<Shape> ShapeList = new List<Shape>();
        public static DataTable DT_ShapeList = new DataTable("DT_ShapeList");
        static Pen basicPen { get; set; }
        static Pen activePen { get; set; }
        static Pen dimPen { get; set; }
        static Pen dimArrowPen { get; set; }
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
        public bool RemoveShapeById(int Id)
        {
            Shape S = ShapeList.Where(s => s.IdShape == Id).FirstOrDefault();
            if (S != null)
            {
                ShapeList.Remove(S);
                return true;
            }
            return false;
        }
        public bool RemoveActiveShape()
        {
            Shape S = ShapeList.Where(s => s.isActiveShape == true).FirstOrDefault();
            if (S != null)
            {
                ShapeList.Remove(S);
                DT_ShapeList.Rows.Remove(DT_ShapeList.Rows.Find(S.IdShape));
                return true;
            }
            return false;
        }
        public Shape GetActiveShape()
        {
            Shape S = ShapeList.Where(s => s.isActiveShape == true).FirstOrDefault();
            return S;
        }
        public void DimensionActiveLine()
        {
            Shape S = GetActiveShape();
            if (S.MetaName == "Line")
            {
                S.Dimension();
                Console.WriteLine(S.MetaName + " " + S.IdShape.ToString() + " dimensioned.");
            }
            else
            {
                Console.WriteLine("Active object is not dimension-able.");
            }
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
            ShapeSystem.dimPen = new Pen(Color.CornflowerBlue, 1);
            ShapeSystem.dimArrowPen = new Pen(dimPen.Color, 1);
            AdjustableArrowCap dimPenCap = new AdjustableArrowCap(4, 5);
            dimArrowPen.CustomStartCap = dimPenCap;
            dimArrowPen.CustomEndCap = dimPenCap;
        }

        public ShapeSystem()
        {
            DataColumn IdShape = new DataColumn("IdShape");
            IdShape.DataType = System.Type.GetType("System.Int32");
            IdShape.Unique = true;
            DT_ShapeList.Columns.Add(IdShape);
            DT_ShapeList.PrimaryKey = new DataColumn[] { DT_ShapeList.Columns["IdShape"] };

            DataColumn IdParentShape = new DataColumn("IdParentShape");
            IdShape.DataType = System.Type.GetType("System.Int32");
            DT_ShapeList.Columns.Add(IdParentShape);

            DataColumn MetaName = new DataColumn("MetaName");
            MetaName.DataType = System.Type.GetType("System.String");
            DT_ShapeList.Columns.Add(MetaName);

            DataColumn MetaDesc = new DataColumn("MetaDesc");
            MetaDesc.DataType = System.Type.GetType("System.String");
            DT_ShapeList.Columns.Add(MetaDesc);

            DataColumn DisplayString = new DataColumn("DisplayString");
            MetaDesc.DataType = System.Type.GetType("System.String");
            DT_ShapeList.Columns.Add(DisplayString);

        }

        public static void AddShapeToDataSet(Shape S)
        {
            DataRow row = DT_ShapeList.NewRow();
            row["IdShape"] = S.IdShape;
            row["IdParentShape"] = S.ParentId;
            row["MetaName"] = S.MetaName;
            row["MetaDesc"] = S.MetaDesc;
            row["DisplayString"] = S.MetaName + " " + S.IdShape.ToString();
            DT_ShapeList.Rows.Add(row);
        }

        public abstract class Shape
        {
            public abstract void Draw(Graphics g);
            public abstract void Dimension();
            public bool isActiveShape { get; set; }
            public int ParentId;
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
        public class LineDimension : Shape
        {
            internal Line ParentLine;
            public float distanceFromLine { get; set; }
            public float leadingLineLength { get; set; }
            public float dimInsetFromLeadingLine { get; set; }
            public float dimLength { get; set; }
            public string TxDisplay { get; set; }
            public Font TxFont { get; set; }
            AdjustableArrowCap DisplayArrow = new AdjustableArrowCap(5, 5);

            public LineDimension(Line L, float dist)
            {
                this.ParentLine = L;
                this.ParentId = L.IdShape;
                float x1 = ParentLine.P1.X;
                float y1 = ParentLine.P1.Y;
                float x2 = ParentLine.P2.X;
                float y2 = ParentLine.P2.Y;
                this.TxFont = new System.Drawing.Font("Consolas", 8F, System.Drawing.FontStyle.Regular);
                this.dimLength = (float)Math.Sqrt((Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2)));
                this.MetaName = "Dim";
                object[] MetaDescArray = { this.ParentLine.MetaName, this.ParentLine.IdShape };
                this.MetaDesc = string.Format("for {0} {1}", MetaDescArray);
                this.distanceFromLine = .0625F;
                this.leadingLineLength = .5F;
                this.dimInsetFromLeadingLine = .125F;
                AddShapeToDataSet(this);

            }

            public override void Dimension()
            {
                throw new NotImplementedException();
            }

            public List<PointF> CalculateSidelines(PointF source, double slope, float dist, float len)
            {
                {
                    // m is the slope of line, and the 
                    // required Point lies distance l 
                    // away from the source Point

                    PointF P1 = new PointF();
                    PointF P2 = new PointF();
                    PointF P3 = new PointF();

                    float insetfromlen = len - dimInsetFromLeadingLine;

                    // slope is 0
                    if (slope == 0)
                    {
                        P1.X = source.X + dist;
                        P1.Y = source.Y;

                        P2.X = source.X + len;
                        P2.Y = source.Y;

                       //P3.X = P2.X - dimInsetFromLeadingLine;
                       //P3.Y = P2.Y;
                    }

                    // if slope is infinte
                    else if (Double.IsInfinity((double)slope))
                    {
                        P1.X = source.X;
                        P1.Y = source.Y + dist;

                        P2.X = source.X;
                        P2.Y = source.Y + len;

                        //P3.X = P2.X;
                        //P3.Y = P2.Y - dimInsetFromLeadingLine;
                    }
                    else
                    {
                        float distx = (float)(dist / Math.Sqrt(1 + (slope * slope)));
                        float disty = (float)slope * distx;
                        float lenx = (float)(len / Math.Sqrt(1 + (slope * slope)));
                        float leny = (float)slope * lenx;
                        //float insetx = (float)(insetfromlen / Math.Sqrt(1 + (slope * slope)));
                        //float insety = (float)slope * insetfromlen;
                        P1.X = source.X + distx;
                        P1.Y = source.Y + disty;
                        P2.X = source.X + lenx;
                        P2.Y = source.Y + leny;
                        //P3.X = source.X + (insetx);
                        //P3.Y = source.Y + (insety);
                    }

                    return new List<PointF>() { P1, P2, Line.GetFractionOfLine(P1,P2,.75F) };
                }
            }

            public override void Draw(Graphics g)
            {
                PointF P1 = ParentLine.P1;
                PointF P2 = ParentLine.P2;

                double dimSlope = (1 / ParentLine.slope) * -1;

                List<PointF> leaderLine1 = CalculateSidelines(P1, dimSlope, distanceFromLine, leadingLineLength);
                List<PointF> leaderLine2 = CalculateSidelines(P2, dimSlope, distanceFromLine, leadingLineLength);
                
                g.DrawLine(dimPen, gridSystem.realizePoint(leaderLine1[0]), gridSystem.realizePoint(leaderLine1[1]));
                g.DrawLine(dimPen, gridSystem.realizePoint(leaderLine2[0]), gridSystem.realizePoint(leaderLine2[1]));
                g.DrawLine(dimArrowPen, gridSystem.realizePoint(leaderLine1[2]), gridSystem.realizePoint(leaderLine2[2]));

                //dimLength string
                string dimLength = Math.Round((decimal)this.dimLength, 4).ToString() + "\"";
                SizeF dimLength_Size =  g.MeasureString(dimLength, this.TxFont);
                PointF drawingPoint = Line.GetFractionOfLine(gridSystem.realizePoint(leaderLine1[2]), gridSystem.realizePoint(leaderLine2[2]), .5F);
                drawingPoint.X = drawingPoint.X - (dimLength_Size.Width / 2);
                drawingPoint.Y = drawingPoint.Y - (dimLength_Size.Height / 2);
                g.FillRectangle(new SolidBrush(Color.White), new RectangleF(drawingPoint.X-2,drawingPoint.Y-2, dimLength_Size.Width+4,dimLength_Size.Height+4));
                g.DrawString(dimLength, this.TxFont, new SolidBrush(Color.Black),drawingPoint);
            }
        }

        [Serializable]
        public class Line : Shape
        {
            internal PointF P1;
            internal PointF P2;
            public double slope;
            public Line(PointF p1, PointF p2)
            {
                this.P1 = p1;
                this.P2 = p2;
                this.ParentId = -1;
                this.MetaName = "Line";
                object[] MetaDescArray = { this.P1.X, this.P1.Y, this.P2.X, this.P2.Y };
                this.MetaDesc = string.Format("{0},{1}:{2},{3}", MetaDescArray);
                AddShapeToDataSet(this);
                this.slope = (P2.Y - P1.Y) / (P2.X - P1.X);
            }

            public static PointF GetFractionOfLine(PointF p1, PointF p2, float frac)
            {
                return new PointF(p1.X + frac * (p2.X - p1.X),
                                   p1.Y + frac * (p2.Y - p1.Y));
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
                    Console.WriteLine("Line " + RetLine.IdShape.ToString() + " created as {0}",RetLine.MetaDesc);
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
                    Console.WriteLine("Line " + RetLine.IdShape.ToString() + " created as {0}", RetLine.MetaDesc);
                    gridSystem.cursorPosition = p2;
                    return;
                }
            }

            public override void Dimension()
            {
                new LineDimension(this, .5F);
            }

            public override void Draw(Graphics g)
            {
                if (isActiveShape)
                {
                    AdjustableArrowCap bigArrow = new AdjustableArrowCap(5, 5);
                    activePen.StartCap = LineCap.RoundAnchor;
                    activePen.CustomEndCap = bigArrow;
                    g.DrawLine(activePen, gridSystem.realizePoint(P1), gridSystem.realizePoint(P2));
                }
                else
                    g.DrawLine(basicPen, gridSystem.realizePoint(P1), gridSystem.realizePoint(P2));

            }
        }

    }

}
