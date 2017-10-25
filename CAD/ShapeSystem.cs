using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Xml.Serialization;

namespace CAD
{
    [Serializable]
    public class ShapeSystem
    {
        public static List<Shape> ShapeList = new List<Shape>();
        public static DataTable DT_ShapeList = new DataTable("DT_ShapeList");
        public static List<PointF> SnapPoints = new List<PointF>();
        public static Pen basicPen { get; set; }
        public static Pen activePen { get; set; }
        public static Pen dimPen { get; set; }
        public static Pen dimArrowPen { get; set; }
        public static Pen ctorLinePen { get; set; }
        public static GridSystem gridSystem { get; set; }
        
        public static int FindLineCircleIntersections(
            PointF P1, float radius,
            PointF point1, PointF point2,
            out PointF intersection1, out PointF intersection2)
        {
            float cx = P1.X;
            float cy = P1.Y;
            float dx, dy, A, B, C, det, t;

            dx = point2.X - point1.X;
            dy = point2.Y - point1.Y;

            A = dx * dx + dy * dy;
            B = 2 * (dx * (point1.X - cx) + dy * (point1.Y - cy));
            C = (point1.X - cx) * (point1.X - cx) +
                (point1.Y - cy) * (point1.Y - cy) -
                radius * radius;

            det = B * B - 4 * A * C;
            if ((A <= 0.0000001) || (det < 0))
            {
                // No real solutions.
                intersection1 = new PointF(float.NaN, float.NaN);
                intersection2 = new PointF(float.NaN, float.NaN);
                return 0;
            }
            else if (det == 0)
            {
                // One solution.
                t = -B / (2 * A);
                intersection1 =
                    new PointF(point1.X + t * dx, point1.Y + t * dy);
                intersection2 = new PointF(float.NaN, float.NaN);
                return 1;
            }
            else
            {
                // Two solutions.
                t = (float)((-B + Math.Sqrt(det)) / (2 * A));
                intersection1 =
                    new PointF(point1.X + t * dx, point1.Y + t * dy);
                t = (float)((-B - Math.Sqrt(det)) / (2 * A));
                intersection2 =
                    new PointF(point1.X + t * dx, point1.Y + t * dy);
                return 2;
            }
        }

        public static void FixParentAbandonmentIssues(int IdShape)
        {
            foreach (DataRow row in DT_ShapeList.Rows)
            {
                if (row["IdShape"].ToString() == IdShape.ToString())
                {
                    row["IdParentShape"] = -1;
                }
            }
        }

        public static void UpdateSnapPoints()
        {
            SnapPoints.Clear();
            foreach (Shape S in ShapeList)
            {
                if (S is iSnappable)
                {
                    iSnappable snapObj = (iSnappable)S;
                    SnapPoints = SnapPoints.Union((snapObj.GetSnapPoints()).OrderByDescending(p => p.X)).ToList();
                }
            }
        }

        public static List<PointF> GetSnapPoints()
        {
            return SnapPoints;
        }

        public void ClearData()
        {
            List<int> ShapeIds = new List<int>();
            foreach (Shape S in ShapeList)
            {
                ShapeIds.Add(S.IdShape);
            }
            foreach (int I in ShapeIds)
            {
                RemoveShapeById(I);
            }
            UpdateSnapPoints();
        }

        public void SetGrid(GridSystem grid)
        {
            gridSystem = grid;
        }

        public Shape GetShapeById(int Id)
        {
            Shape S = ShapeList.Where(s => s.IdShape == Id).FirstOrDefault();
            return S;
        }

        public bool ActivateShapeUnderPoint(PointF P)
        {
            P = gridSystem.theorizePoint(P);
            double distanceToPoint = 999;
            Shape newActive = null;
            foreach (Shape S in GetShapes())
            {
                if (!(S is iClickable)) // if it's not clickable continue
                {
                    continue;
                }

                iClickable C = (iClickable)S;

                if (C.IntersectsWithCircle(P, .1F))
                {
                    double D = C.GetDistanceFromPoint(P); //ITS LIKE A BAD ITCH EDDY, GET IT OFF
                    if (D < distanceToPoint)
                    {
                        distanceToPoint = D;
                        newActive = (Shape)C;
                    }
                }
            }
            if (newActive != null)
            {
                MakeActiveShape(GetShapeById(newActive.IdShape));
                return true;
            }
            return false;
        }

        public bool ActivateShapeUnderPoint<T>(PointF P) where T : iClickable
        {
            P = gridSystem.theorizePoint(P);
            double distanceToPoint = 999;
            Shape newActive = null;
            foreach (Shape S in GetShapes())
            {
                if (!(S is T)) // if it's not clickable continue
                {
                    continue;
                }

                iClickable C = (iClickable)S;

                if (C.IntersectsWithCircle(P, .1F))
                {
                    double D = C.GetDistanceFromPoint(P); //ITS LIKE A BAD ITCH EDDY, GET IT OFF
                    if (D < distanceToPoint)
                    {
                        distanceToPoint = D;
                        newActive = (Shape)C;
                    }
                }
            }
            if (newActive != null)
            {
                MakeActiveShape(GetShapeById(newActive.IdShape));
                return true;
            }
            return false;
        }

        public static double GetDistancePointToLine(PointF P, PointF A, PointF B)
        {
            //get Length of |AB|
            var d = Math.Sqrt(Math.Pow((B.X - A.X), 2) +
                                    Math.Pow((B.Y - A.Y), 2));
            //Get nearest point to P on |AB|
            var a = (1 / Math.Pow(d, 2)) *
                    (
                        ((B.X - A.X) * (P.X - A.X)) +
                        ((B.Y - A.Y) * (P.Y - A.Y))
                    );
            PointF nearest = new PointF();
            nearest.X = (float)(A.X + (B.X - A.X) * a);
            nearest.Y = (float)(A.Y + (B.Y - A.Y) * a);
            
            return (double)Math.Sqrt(Math.Pow((P.X - nearest.X), 2) +
                                    Math.Pow((P.Y - nearest.Y), 2));
            
        }

        public bool RemoveShapeById(int Id)
        {
            Shape S = ShapeList.Where(s => s.IdShape == Id).FirstOrDefault();
            if (S != null)
            {
                ShapeList.Remove(S);
                DT_ShapeList.Rows.Remove(DT_ShapeList.Rows.Find(S.IdShape));
                UpdateSnapPoints();
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
                UpdateSnapPoints();
                return true;
            }
            return false;
        }

        public void DeselectActiveShapes()
        {
            foreach (Shape S in ShapeList)
            {
                S.isActiveShape = false;
            }
        }

        public Shape GetActiveShape()
        {
            Shape S = ShapeList.Where(s => s.isActiveShape == true).FirstOrDefault();
            return S;
        }

        public void DimensionActiveLine()
        {
            Shape S = GetActiveShape();
            if (S == null)
            {
                Console.WriteLine("No object selected.");
                return;
            }
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

        public void AdjustDimByRealCursor(PointF RealCursor, int IdDim)
        {
            PointF cursor = gridSystem.theorizePoint(RealCursor);
            LinearDimension S = (LinearDimension)GetShapeById(IdDim);
            double dist = GetDistancePointToLine(cursor, S.P1, S.P2);
            S._leadingLineLength = (float)dist;
            if (cursor.X < GetFractionOfLine(S.P1, S.P2, .5F).X)
                S.direction = -1;
            else
                S.direction = 1;
        }

        public static void MakeActiveShape(Shape S)
        {
            foreach (Shape shape in ShapeList)
            {
                shape.isActiveShape = false;
            }
            S.isActiveShape = true;
        }

        public static void AddActiveShape(Shape S)
        {
            S.isActiveShape = true;
        }

        public static void SetShapeFillColor(List<float> Input)
        {
            Shape S = ShapeList.Where(s => s.IdShape == (int)Input[0]).FirstOrDefault();
            if (S is iFillable)
            {
                if (Input.Count == 4)
                {
                    ((iFillable)S).FillColor = Color.FromArgb(255, (int)Input[1], (int)Input[2], (int)Input[3]);
                }
                else if (Input.Count == 5)
                {
                    ((iFillable)S).FillColor = Color.FromArgb((int)Input[1], (int)Input[2], (int)Input[3], (int)Input[4]);
                }
            }
        }

        public void RefreshAll(Graphics g)
        {
            foreach (Shape S in ShapeList)
            {
                if (S is iFillable)
                {
                    ((iFillable)S).DrawFill(g);
                }
                S.Draw(g);
            }
        }

        public List<Shape> GetShapes()
        {
            return ShapeList;
        }

        public static void Draw2PTDim(PointF P1, PointF P2)
        {
            LinearDimension newDim = new LinearDimension(P1, P2);
        }

        public void setBaseColors(Pen basic, Pen active)
        {
            ShapeSystem.basicPen = basic;
            ShapeSystem.activePen = active;
            ShapeSystem.dimPen = new Pen(Color.CornflowerBlue, 1);
            ShapeSystem.dimArrowPen = new Pen(dimPen.Color, 1);
            ShapeSystem.ctorLinePen = new Pen(Color.FromArgb(150, Color.Orange),1);
            ShapeSystem.ctorLinePen.DashStyle = DashStyle.Dash;
            AdjustableArrowCap dimPenCap = new AdjustableArrowCap(4, 5);
            dimArrowPen.CustomStartCap = dimPenCap;
            dimArrowPen.CustomEndCap = dimPenCap;
        }

        public static PointF GetFractionOfLine(PointF p1, PointF p2, float frac)
        {
            return new PointF(p1.X + frac * (p2.X - p1.X),
                               p1.Y + frac * (p2.Y - p1.Y));
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

        public AdjustableArrowCap DisplayArrow = new AdjustableArrowCap(5, 5);


        public static void AddShapeToDataSet(Shape S)
        {
            DataRow row = DT_ShapeList.NewRow();
            row["IdShape"] = S.IdShape;
            row["IdParentShape"] = S.ParentId;
            row["MetaName"] = S.MetaName;
            row["MetaDesc"] = S.MetaDesc;
            row["DisplayString"] = S.MetaName + " " + S.IdShape.ToString() + " : " + S.MetaDesc;
            DT_ShapeList.Rows.Add(row);
        }

        public interface iSnappable
        {
            List<PointF> GetSnapPoints();
        }

        public interface iClickable
        {
            double GetDistanceFromPoint(PointF P);
            bool IntersectsWithCircle(PointF CPoint, float CRad);
        }

        public interface iFillable
        {
            Color FillColor { get; set; }
            void DrawFill(Graphics g);
        }

        [Serializable]
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
                IdShape = ShapeList.Max(m => m.IdShape) + 1;
                ShapeSystem.MakeActiveShape(this);
            }
        }

        [Serializable]
        public class LinearDimension : Shape , iClickable
        {
            public PointF P1 { get; set; }
            public PointF P2 { get; set; }
            public int direction = 1;
            public double slope { get
                {
                    return (P2.Y - P1.Y) / (P2.X - P1.X);
                }
            }
            public float distanceFromLine { get; set; }
            private float leadingLineLength { get; set; }
            public float _leadingLineLength {
                get
                {
                    return leadingLineLength;
                }
                set
                {
                    leadingLineLength = Math.Max(.25F, value);
                } }
            public float dimInsetFromLeadingLine { get; set; }
            public float dimLength { get; set; }
            public string TxDisplay { get; set; }
            public Font TxFont { get; set; }
            
            public LinearDimension()
            {

            }
            public LinearDimension(Line L, float dist)
            {
                this.P1 = L.P1;
                this.P2 = L.P2;
                this.ParentId = L.IdShape;
                float x1 = this.P1.X;
                float y1 = this.P1.Y;
                float x2 = this.P2.X;
                float y2 = this.P2.Y;
                this.TxFont = new System.Drawing.Font("Consolas", 8F, System.Drawing.FontStyle.Regular);
                this.dimLength = (float)Math.Sqrt((Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2)));
                this.MetaName = "Dim";
                object[] MetaDescArray = { L.MetaName, L.IdShape };
                this.MetaDesc = string.Format("{0} {1}", MetaDescArray);
                this.distanceFromLine = .0625F;
                this.leadingLineLength = dist;
                this.dimInsetFromLeadingLine = .125F;
                AddShapeToDataSet(this);

            }
            public LinearDimension(PointF P1, PointF P2)
            {
                this.P1 = P1;
                this.P2 = P2;
                this.ParentId = -1;
                float x1 = this.P1.X;
                float y1 = this.P1.Y;
                float x2 = this.P2.X;
                float y2 = this.P2.Y;
                this.TxFont = new System.Drawing.Font("Consolas", 8F, System.Drawing.FontStyle.Regular);
                this.dimLength = (float)Math.Sqrt((Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2)));
                this.MetaName = "Dim";
                object[] MetaDescArray = { P1.X, P1.Y, P2.X, P2.Y };
                this.MetaDesc = string.Format("({0},{1}):({2},{3})", MetaDescArray);
                this.distanceFromLine = .0625F;
                this.leadingLineLength = .5F;
                this.dimInsetFromLeadingLine = .125F;
                AddShapeToDataSet(this);

            }
            public LinearDimension(List<float> Input)
            {
                float x1 = Input[0];
                float y1 = Input[1];
                float x2 = Input[2];
                float y2 = Input[3];
                this.P1 = new PointF(x1, y1);
                this.P2 = new PointF(x2, y2);
                this.ParentId = -1;
                this.TxFont = new System.Drawing.Font("Consolas", 8F, System.Drawing.FontStyle.Regular);
                this.dimLength = (float)Math.Sqrt((Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2)));
                this.MetaName = "Dim";
                object[] MetaDescArray = { P1.X, P1.Y, P2.X, P2.Y };
                this.MetaDesc = string.Format("({0},{1}):({2},{3})", MetaDescArray);
                this.distanceFromLine = .0625F;
                this.leadingLineLength = .5F;
                this.dimInsetFromLeadingLine = .125F;
                AddShapeToDataSet(this);

            }
            public static void AddNewDim(List<float> Input)
            {
                LinearDimension retDim = new LinearDimension(Input);
            }
            public override void Dimension()
            {
                throw new NotImplementedException();
            }

            public static void AdjDim(List<float> Input)
            {
                Shape S = ShapeList.Where(s => s.IdShape == (int)Input[0]).FirstOrDefault();

                if (S is LinearDimension)
                {
                    ((LinearDimension)S)._leadingLineLength = Input[1];
                }
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
                        P1.X = source.X + (dist * direction);
                        P1.Y = source.Y;

                        P2.X = source.X + (len * direction);
                        P2.Y = source.Y;

                        P3.X = source.X + (len * direction);
                        P3.Y = source.Y;
                    }

                    // if slope is infinte
                    else if (Double.IsInfinity((double)slope))
                    {
                        P1.X = source.X;
                        P1.Y = source.Y +  (dist * direction);

                        P2.X = source.X;
                        P2.Y = source.Y +  (len * direction);

                        P3.X = source.X;
                        P3.Y = source.Y +  (len * direction);
                    }
                    else
                    {
                        float distx = (float)(dist / Math.Sqrt(1 + (slope * slope)));
                        float disty = (float)slope * distx;
                        float lenx = (float)(len / Math.Sqrt(1 + (slope * slope)));
                        float leny = (float)slope  * lenx;
                        float insx = (float)(.1F / Math.Sqrt(1 + (slope * slope)));
                        float insy = (float)slope  * .1F;
                        P1.X = source.X + (distx * direction);
                        P1.Y = source.Y + (disty * direction);
                        P2.X = source.X + (lenx  * direction);
                        P2.Y = source.Y + (leny  * direction);
                        P3.X = source.X + (lenx  * direction);
                        P3.Y = source.Y + (leny  * direction);
                    }

                    //return new List<PointF>() { P1, P2, GetFractionOfLine(P1, P2, .75F) };
                    return new List<PointF>() { P1, P2, P3 };
                }
            }

            public bool IntersectsWithCircle(PointF CPoint, float CRad)
            {
                double dimSlope = (1 / this.slope) * -1;
                PointF A = CalculateSidelines(this.P1, dimSlope, distanceFromLine, leadingLineLength)[2];
                PointF B = CalculateSidelines(this.P2, dimSlope, distanceFromLine, leadingLineLength)[2];
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
                if (distToPoint <= CRad)
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

            public double GetDistanceFromPoint(PointF P)
            {
                double dimSlope = (1 / this.slope) * -1;
                PointF A = CalculateSidelines(this.P1, dimSlope, distanceFromLine, leadingLineLength)[2];
                PointF B = CalculateSidelines(this.P2, dimSlope, distanceFromLine, leadingLineLength)[2];
                //Distance between two points
                var d = Math.Sqrt(Math.Pow((B.X - A.X), 2) +
                                        Math.Pow((B.Y - A.Y), 2));
                //Get nearest of |AB|
                var a = (1 / Math.Pow(d, 2)) *
                        (
                            ((B.X - A.X) * (P.X - A.X)) +
                            ((B.Y - A.Y) * (P.Y - A.Y))
                        );
                PointF nearest = new PointF();
                nearest.X = (float)(A.X + (B.X - A.X) * a);
                nearest.Y = (float)(A.Y + (B.Y - A.Y) * a);

                //Check distance from nearest point
                return (double)(Math.Sqrt(Math.Pow((P.X - nearest.X), 2) +
                                        Math.Pow((P.Y - nearest.Y), 2)));

            }

            public override void Draw(Graphics g)
            {
                double dimSlope = (1 / this.slope) * -1;

                List<PointF> leaderLine1 = CalculateSidelines(P1, dimSlope, distanceFromLine, leadingLineLength);
                List<PointF> leaderLine2 = CalculateSidelines(P2, dimSlope, distanceFromLine, leadingLineLength);

                if (isActiveShape)
                {
                    Pen AArrowPen = new Pen(activePen.Color);
                    AArrowPen.CustomStartCap = dimArrowPen.CustomStartCap;
                    AArrowPen.CustomEndCap = dimArrowPen.CustomEndCap;

                    g.DrawLine(new Pen(activePen.Color), gridSystem.realizePoint(leaderLine1[0]), gridSystem.realizePoint(leaderLine1[1]));
                    g.DrawLine(new Pen(activePen.Color), gridSystem.realizePoint(leaderLine2[0]), gridSystem.realizePoint(leaderLine2[1]));
                    g.DrawLine(AArrowPen, gridSystem.realizePoint(leaderLine1[2]), gridSystem.realizePoint(leaderLine2[2]));

                }
                else
                {
                    g.DrawLine(dimPen, gridSystem.realizePoint(leaderLine1[0]), gridSystem.realizePoint(leaderLine1[1]));
                    g.DrawLine(dimPen, gridSystem.realizePoint(leaderLine2[0]), gridSystem.realizePoint(leaderLine2[1]));
                    g.DrawLine(dimArrowPen, gridSystem.realizePoint(leaderLine1[2]), gridSystem.realizePoint(leaderLine2[2]));
                }
                //dimLength string
                string dimLength = Math.Round((decimal)this.dimLength, 4).ToString() + "\"";
                SizeF dimLength_Size = g.MeasureString(dimLength, this.TxFont);
                PointF drawingPoint = GetFractionOfLine(gridSystem.realizePoint(leaderLine1[2]), gridSystem.realizePoint(leaderLine2[2]), .5F);
                drawingPoint.X = drawingPoint.X - (dimLength_Size.Width / 2);
                drawingPoint.Y = drawingPoint.Y - (dimLength_Size.Height / 2);
                g.FillRectangle(new SolidBrush(Color.White), new RectangleF(drawingPoint.X - 2, drawingPoint.Y - 2, dimLength_Size.Width + 4, dimLength_Size.Height + 4));
                g.DrawString(dimLength, this.TxFont, new SolidBrush(Color.Black), drawingPoint);
            }
        }

        [Serializable]
        public class Line : Shape, iSnappable, iClickable
        {
            public PointF P1;
            public PointF P2;
            public double slope;

            public Line()
            {

            }
            public Line(PointF p1, PointF p2)
            {
                this.P1 = p1;
                this.P2 = p2;
                this.ParentId = -1;
                this.MetaName = "Line";
                object[] MetaDescArray = { this.P1.X, this.P1.Y, this.P2.X, this.P2.Y };
                this.MetaDesc = string.Format("({0},{1}):({2},{3})", MetaDescArray);
                AddShapeToDataSet(this);
                this.slope = (P2.Y - P1.Y) / (P2.X - P1.X);
            }
            //Line with Parent
            public Line(PointF p1, PointF p2, Shape parent)
            {
                this.P1 = p1;
                this.P2 = p2;
                this.ParentId = parent.IdShape;
                this.MetaName = "Line";
                object[] MetaDescArray = { this.P1.X, this.P1.Y, this.P2.X, this.P2.Y };
                this.MetaDesc = string.Format("({0},{1}):({2},{3})", MetaDescArray);
                AddShapeToDataSet(this);
                this.slope = (P2.Y - P1.Y) / (P2.X - P1.X);
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
                    Console.WriteLine("Line " + RetLine.IdShape.ToString() + " created as {0}", RetLine.MetaDesc);
                    ShapeSystem.UpdateSnapPoints();
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
                    ShapeSystem.UpdateSnapPoints();
                    return;
                }
            }

            public override void Dimension()
            {
                new LinearDimension(this, .5F);
            }

            public List<PointF> GetSnapPoints()
            {
                PointF P3 = GetFractionOfLine(P1, P2, .5F);
                return new List<PointF>() { P1, P2, P3 };
            }

            public bool IntersectsWithCircle(PointF CPoint, float CRad)
            {
                PointF A = P1;
                PointF B = P2;
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
                if (distToPoint <= CRad)
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

            public double GetDistanceFromPoint(PointF P)
            {
                PointF A = P1;
                PointF B = P2;
                //Distance between two points
                var d = Math.Sqrt(Math.Pow((B.X - A.X), 2) +
                                        Math.Pow((B.Y - A.Y), 2));
                //Get nearest of |AB|
                var a = (1 / Math.Pow(d, 2)) *
                        (
                            ((B.X - A.X) * (P.X - A.X)) +
                            ((B.Y - A.Y) * (P.Y - A.Y))
                        );
                PointF nearest = new PointF();
                nearest.X = (float)(A.X + (B.X - A.X) * a);
                nearest.Y = (float)(A.Y + (B.Y - A.Y) * a);

                //Check distance from nearest point
                return (double)(Math.Sqrt(Math.Pow((P.X - nearest.X), 2) +
                                        Math.Pow((P.Y - nearest.Y), 2)));

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
        
        [Serializable]
        public class ConstructionLine: Line
        {

        }

        [Serializable]
        public class Rect : Shape, iSnappable, iFillable
        {
            public Line AB;
            public Line BC;
            public Line CD;
            public Line DA;
            public Color FillColor { get; set; }

            public Rect()
            {

            }
            public Rect(PointF p1, PointF p2, PointF p3, PointF p4)
            {
                this.AB = new Line(p1, p2, (Shape)this);
                this.BC = new Line(p2, p3, (Shape)this);
                this.CD = new Line(p3, p4, (Shape)this);
                this.DA = new Line(p4, p1, (Shape)this);

                this.ParentId = -1;
                this.MetaName = "Rect";
                object[] MetaDescArray = { AB.IdShape, BC.IdShape, CD.IdShape, DA.IdShape };
                this.MetaDesc = string.Format("L{0}, L{1}, L{2}, L{3}", MetaDescArray);
                AddShapeToDataSet(this);
            }

            public override void Dimension()
            {
                throw new NotImplementedException();
            }

            public static void AddRect(List<float> Input)
            {
                //Width and height from cursor
                if (gridSystem.relativePositioning && Input.Count == 2)
                {
                    PointF cursor = new PointF(gridSystem.cursorPosition.X, gridSystem.cursorPosition.Y);
                    PointF p1 = new PointF(Math.Min(cursor.X, cursor.X + Input[0]), Math.Min(cursor.Y, cursor.Y + Input[1]));
                    PointF p2 = new PointF(Math.Max(cursor.X, cursor.X + Input[0]), Math.Min(cursor.Y, cursor.Y + Input[1]));
                    PointF p3 = new PointF(Math.Max(cursor.X, cursor.X + Input[0]), Math.Max(cursor.Y, cursor.Y + Input[1]));
                    PointF p4 = new PointF(Math.Min(cursor.X, cursor.X + Input[0]), Math.Max(cursor.Y, cursor.Y + Input[1]));
                    Rect RetRect = new Rect(p1, p2, p3, p4);
                    gridSystem.cursorPosition = cursor;
                    Console.WriteLine("Rectangle " + RetRect.IdShape.ToString() + " created as {0}", RetRect.MetaDesc);
                    ShapeSystem.UpdateSnapPoints();
                    return;

                }
                if (Input.Count != 4)
                {
                    Console.WriteLine("Invalid input.");
                    return;
                }
                else
                {
                    PointF pRef = new PointF(Input[0], Input[1]);
                    PointF p1 = new PointF(Math.Min(pRef.X, pRef.X + Input[2]), Math.Min(pRef.Y, pRef.Y + Input[3]));
                    PointF p2 = new PointF(Math.Max(pRef.X, pRef.X + Input[2]), Math.Min(pRef.Y, pRef.Y + Input[3]));
                    PointF p3 = new PointF(Math.Max(pRef.X, pRef.X + Input[2]), Math.Max(pRef.Y, pRef.Y + Input[3]));
                    PointF p4 = new PointF(Math.Min(pRef.X, pRef.X + Input[2]), Math.Max(pRef.Y, pRef.Y + Input[3]));

                    Rect RetRect = new Rect(p1, p2, p3, p4);
                    gridSystem.cursorPosition = pRef;
                    Console.WriteLine("Rectangle " + RetRect.IdShape.ToString() + " created as {0}", RetRect.MetaDesc);
                    ShapeSystem.UpdateSnapPoints();
                    return;
                }
            }

            public List<PointF> GetSnapPoints()
            {
                PointF Middle = GetFractionOfLine(AB.P1, BC.P2, .5F);
                return new List<PointF>() { Middle };
            }

            public void DrawFill(Graphics g)
            {
                if (this.FillColor == null) { return; }
                PointF p1 = gridSystem.realizePoint(AB.P1);
                PointF p2 = gridSystem.realizePoint(AB.P2);
                PointF p3 = gridSystem.realizePoint(CD.P2);
                RectangleF rect = new RectangleF(
                      p1.X
                    , p1.Y
                    , p2.X - p1.X
                    , p3.Y - p1.Y
                    );
                //path.CloseFigure();
                g.FillRectangle(new SolidBrush(FillColor), rect);
            }

            public override void Draw(Graphics g)
            {
                ;
            }
        }

        [Serializable]
        public class cadPoint : Shape, iSnappable, iClickable
        {
            public PointF P1;

            public cadPoint()
            {

            }
            public cadPoint(PointF p1)
            {
                this.P1 = p1;
                this.ParentId = -1;
                this.MetaName = "Point";
                object[] MetaDescArray = { this.P1.X, this.P1.Y };
                this.MetaDesc = string.Format("({0},{1})", MetaDescArray);
                AddShapeToDataSet(this);
            }
            public static void AddPoint(List<float> Input)
            {
                if (Input.Count == 0)
                {
                    PointF p1 = gridSystem.cursorPosition;
                    cadPoint newPoint = new cadPoint(p1);
                    Console.WriteLine("Point " + newPoint.IdShape.ToString() + " created as {0}", newPoint.MetaDesc);
                    ShapeSystem.UpdateSnapPoints();
                    return;
                }
                else if (Input.Count == 2)
                {
                    float x1 = (Input[0]);
                    float y1 = (Input[1]);
                    PointF p1 = new PointF(x1, y1);
                    cadPoint newPoint = new cadPoint(p1);
                    Console.WriteLine("Point " + newPoint.IdShape.ToString() + " created as {0}", newPoint.MetaDesc);
                    ShapeSystem.UpdateSnapPoints();
                    return;
                }
                else
                {
                    Console.WriteLine("Invalid input.");
                    return;
                }
            }

            public override void Dimension()
            {
                //new LineDimension(this, .5F);
            }

            public List<PointF> GetSnapPoints()
            {
                return new List<PointF>() { P1 };
            }

            public bool IntersectsWithCircle(PointF CPoint, float CRad)
            {
                //Distance between two points
                var dist = Math.Sqrt(Math.Pow((CPoint.X - P1.X), 2) +
                                        Math.Pow((CPoint.Y - P1.Y), 2));
                if (dist <= CRad + .0625)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public double GetDistanceFromPoint(PointF P)
            {
                //Check distance from nearest point
                return (double)(Math.Sqrt(Math.Pow((P.X - P1.X), 2) +
                                        Math.Pow((P.Y - P1.Y), 2)));
            }

            public override void Draw(Graphics g)
            {
                if (isActiveShape)
                {
                    //Cursor
                    PointF C = gridSystem.realizePoint(new PointF(P1.X, P1.Y));
                    g.DrawEllipse(new Pen(activePen.Color), new RectangleF(new PointF(C.X - 5, C.Y - 5), new SizeF(10, 10)));
                }
                else
                {
                    //Cursor
                    PointF C = gridSystem.realizePoint(new PointF(P1.X, P1.Y));
                    g.DrawEllipse(basicPen, new RectangleF(new PointF(C.X - 5, C.Y - 5), new SizeF(10, 10)));
                }

            }
        }
        
        [Serializable]
        public class Ellipse: Shape, iClickable, iSnappable
        {
            public PointF P1;
            public float R;
            public Ellipse()
            {

            }
            public Ellipse(PointF P1, float R)
            {
                this.P1 = P1;
                this.R = R;
                this.MetaName = "Ellipse";
                this.MetaDesc = P1.X.ToString() + "," + P1.Y.ToString() + "; R: " + R.ToString();
                AddShapeToDataSet(this);
            }
            public Ellipse(PointF P1, float R, Shape Parent)
            {
                this.P1 = P1;
                this.R = R;
                this.ParentId = Parent.IdShape;
                this.MetaName = "Ellipse";
                this.MetaDesc = P1.X.ToString() + "," + P1.Y.ToString() + "; R: " + R.ToString();
                AddShapeToDataSet(this);
            }
            public static void AddEllipse(List<float> Input)
            {
                if (Input.Count() == 1)
                {
                    PointF P1 = gridSystem.cursorPosition;
                    float R = Input[0];
                    Ellipse retCircle = new Ellipse(P1, R);
                    Console.WriteLine("Ellipse " + retCircle.IdShape.ToString() + " created as {0}", retCircle.MetaDesc);
                    ShapeSystem.UpdateSnapPoints();
                    return;
                }
                else if (Input.Count() == 3)
                {
                    PointF P1 = new PointF(Input[0], Input[1]);
                    float R = Input[2];
                    Ellipse retCircle = new Ellipse(P1, R);
                    Console.WriteLine("Ellipse " + retCircle.IdShape.ToString() + " created as {0}", retCircle.MetaDesc);
                    ShapeSystem.UpdateSnapPoints();
                    return;
                }
                else
                {
                    Console.WriteLine("Invalid input for Ellipse.");
                }
            }
            public override void Dimension()
            {
                throw new NotImplementedException();
            }
            public bool IntersectsWithCircle(PointF CPoint, float CRad)
            {
                //Distance between two points
                var dist = Math.Abs(Math.Sqrt(Math.Pow((CPoint.X - P1.X), 2) +
                                        Math.Pow((CPoint.Y - P1.Y), 2))-R);
                if (dist <= CRad + .03)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            public double GetDistanceFromPoint(PointF P)
            {
                //Check distance from nearest point
                return (double)Math.Abs(Math.Sqrt(Math.Pow((P.X - P1.X), 2) +
                                        Math.Pow((P.Y - P1.Y), 2)) - R);
            }
            public List<PointF> GetSnapPoints()
            {
                
                List<PointF> snaps = new List<PointF>();
                snaps.Add(new PointF(P1.X, P1.Y - R));
                snaps.Add(new PointF(P1.X, P1.Y + R));
                snaps.Add(new PointF(P1.X - R, P1.Y));
                snaps.Add(new PointF(P1.X + R, P1.Y));
                snaps.Add(P1);
                foreach (Shape S in ShapeList.Where(s => s is Line))
                {
                    Line L = (Line)S;
                    PointF potential1 = new PointF();
                    PointF potential2 = new PointF();
                    int result = ShapeSystem.FindLineCircleIntersections(P1, R, L.P1, L.P2, out potential1, out potential2);
                    if (result == 2) {
                        snaps.Add(potential1);
                        snaps.Add(potential2);
                    }
                    else if (result == 1)
                    {
                        snaps.Add(potential1);
                    }
                }
                return snaps;
            }
            public override void Draw(Graphics g)
            {
                PointF origin = gridSystem.realizePoint(new PointF(P1.X - R, P1.Y - R));
                SizeF size = gridSystem.realizeSize(new SizeF(R * 2,R*2));
                if (isActiveShape)
                {
                    g.DrawEllipse(activePen, new RectangleF(origin, size));
                }
                else
                {
                    g.DrawEllipse(basicPen, new RectangleF(origin, size));
                }
            }
        }

        [Serializable]
        public class Snapshot
        {
            public List<Line> list_Line = new List<Line>();
            public List<LinearDimension> list_Dim = new List<LinearDimension>();
            public List<cadPoint> list_cadPoint = new List<cadPoint>();
            public List<Rect> list_Rect = new List<Rect>();
            public List<Ellipse> list_Ellipse = new List<Ellipse>();
            public Snapshot()
            {
                foreach (Shape S in ShapeList)
                {
                    if (S is Line)
                    {
                        list_Line.Add((Line)S);
                    }
                    else if (S is LinearDimension)
                    {
                        list_Dim.Add((LinearDimension)S);
                    }
                    else if (S is cadPoint)
                    {
                        list_cadPoint.Add((cadPoint)S);
                    }
                    else if (S is Rect)
                    {
                        list_Rect.Add((Rect)S);
                    }
                    else if (S is Ellipse)
                    {
                        list_Ellipse.Add((Ellipse)S);
                    }
                }
            }

            public void Load()
            {
                foreach (Line L in list_Line)
                {
                    ShapeList.Add(L);
                    AddShapeToDataSet(L);
                }
                foreach (LinearDimension L in list_Dim)
                {
                    ShapeList.Add(L);
                    AddShapeToDataSet(L);
                }
                foreach (cadPoint L in list_cadPoint)
                {
                    ShapeList.Add(L);
                    AddShapeToDataSet(L);
                }
                foreach (Rect L in list_Rect)
                {
                    ShapeList.Add(L);
                    AddShapeToDataSet(L);
                }
                foreach (Ellipse L in list_Ellipse)
                {
                    ShapeList.Add(L);
                    AddShapeToDataSet(L);
                }
            }
        }

    }

}
