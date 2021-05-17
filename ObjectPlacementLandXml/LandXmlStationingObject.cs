using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arc = Autodesk.Revit.DB.Arc;
using Transform = Autodesk.Revit.DB.Transform;

namespace ObjectPlacementLandXml
{
    class LandXmlStationingObject
    {
        public double Station { get; set; }
        public double EndStation { get; set; }
        public object AlignmentSegmentElement { get; set; }
        public Autodesk.Revit.DB.Curve RevitSegmentElement { get; set; }

        public Alignment Alignment { get; set; }


        public LandXmlStationingObject(double station, object alignmentElement, Alignment alignment)
        {
            Station = station;
            AlignmentSegmentElement = alignmentElement;
            EndStation = station + this.GetLength();
            Alignment = alignment;
            CreateRevitElement();
        }

        private void CreateRevitElement()
        {
            if (this.AlignmentSegmentElement is Line)
            {
                Autodesk.Revit.DB.Line L = Autodesk.Revit.DB.Line.CreateBound(this.GetStartPoint(), this.GetEndPoint());
                RevitSegmentElement = L;

                if ((bool)ObjectPlacement.TransForm.CreateAlignment)
                {
                    try
                    {
                        var ConvertedPointStart = RevitPlacmenElement.ConvertPointToInternal(this.GetStartPoint());
                        var ConvertedEndPoint = RevitPlacmenElement.ConvertPointToInternal(this.GetEndPoint());
                        Autodesk.Revit.DB.Line ConvrtedLine = Autodesk.Revit.DB.Line.CreateBound(ConvertedPointStart, ConvertedEndPoint);
                        CreateRevitElementInRevit(ConvrtedLine);
                    }
                    catch (Exception)
                    {

                    }
                    // Create a ModelArc element using the created geometry arc and sketch plane
                }
            }
            if (this.AlignmentSegmentElement is IrregularLine)
            {
                // Autodesk.Revit.DB.Line L = Autodesk.Revit.DB.Line.CreateBound(this.GetStartPoint().PlacementPoint, this.GetEndPoint().PlacementPoint);
                //  RevitSegmentElement = L;

            }
            if (this.AlignmentSegmentElement is Curve)
            {
                var StartPoint = this.GetStartPoint();
                var EndPoint = this.GetEndPoint();
                var Radius = this.GetCurveRadius();

                Arc C = CreateArc(StartPoint, EndPoint, Radius, (bool)false);
                RevitSegmentElement = C;

                if ((bool)ObjectPlacement.TransForm.CreateAlignment)
                {
                    try
                    {
                        var ConvertedPointStart = RevitPlacmenElement.ConvertPointToInternal(this.GetStartPoint());
                        var ConvertedEndPoint = RevitPlacmenElement.ConvertPointToInternal(this.GetEndPoint());
                        var ConvertedRadius = RevitPlacmenElement.ConvertDoubleToInternal(Radius);
                        Arc ConcertedCurve = CreateArc(ConvertedPointStart, ConvertedEndPoint, ConvertedRadius, (bool)false);
                        CreateRevitElementInRevit(ConcertedCurve);
                    }
                    catch (Exception)
                    {

                    }
                    // Create a ModelArc element using the created geometry arc and sketch plane
                }

            }
            if (this.AlignmentSegmentElement is Spiral)
            {
                var Sp = (this.AlignmentSegmentElement as Spiral);
                var NurbsSpline = CreateaSpiral(Sp);
                RevitSegmentElement = NurbsSpline;

                if ((bool)ObjectPlacement.TransForm.CreateAlignment)
                {
                    try
                    {
                        List<XYZ> ConvertedPoints = new List<XYZ>();
                        foreach (XYZ item in NurbsSpline.CtrlPoints)
                        {
                            var ConvertedPointStart = RevitPlacmenElement.ConvertPointToInternal(item);

                            ConvertedPoints.Add(ConvertedPointStart);
                        }
                        List<double> Weights = Enumerable.Repeat(1.0, ConvertedPoints.Count).ToList();
                        var ConvertedNurbsCurve = NurbSpline.CreateCurve(ConvertedPoints, Weights);
                        CreateRevitElementInRevit(ConvertedNurbsCurve);
                    }
                    catch (Exception)
                    {

                    }
                    // Create a ModelArc element using the created geometry arc and sketch plane
                }

            }
            if (this.AlignmentSegmentElement is Chain)
            {
                //Review 
                //return ExtractPoint((this.AlignmentElement as Chain).Text);
            }

        }

        private static void CreateRevitElementInRevit(Autodesk.Revit.DB.Curve geomLine)
        {
            // var OriginShit = geomLine.ComputeDerivatives(0, true);
            //var normal = OriginShit.BasisZ;
            // var origin = OriginShit.Origin;
            // XYZ origin = new XYZ(0, 0, 0);
            // XYZ normal = new XYZ(0, 1, 0);
            var Origin = geomLine.GetEndPoint(0);

            Plane geomPlane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, Origin);
            // Create a sketch plane in current document
            SketchPlane sketch = SketchPlane.Create(Command.uidoc.Document, geomPlane);

            // Create a ModelLine element using the created geometry line and sketch plane
            var line = Command.uidoc.Document.Create.NewModelCurve(geomLine, sketch);
        }

        private Autodesk.Revit.DB.NurbSpline CreateaSpiral(Spiral Sp)
        {
            var Splength = Sp.length;
            var spEndRadius = Sp.radiusEnd;
            var SpStartRad = Sp.radiusStart;
            var SpType = Sp.spiType;
            var SpTheta = Sp.theta;
            var SpTotalx = Sp.totalX;
            var SpTotaly = Sp.totalY;
            var spTanLong = Sp.tanLong;
            var spTanShort = Sp.tanShort;
            var Rot = Sp.rot;

            var startPoint = ExtractPoint(Sp.Items[0]);
            var EndPoint = ExtractPoint(Sp.Items[2]);
            var PiPoint = ExtractPoint(Sp.Items[1]);

            double Radius = default(double);

            bool StraightPartAtStart = false;
            if (double.IsInfinity(SpStartRad))
            {
                Radius = spEndRadius;
            }
            else
            {
                Radius = SpStartRad;
                StraightPartAtStart = true;
            }

            var A = Math.Sqrt(Radius * Splength);
            var tao = Math.Pow(A, 2) / (2 * Math.Pow(Radius, 2));

            // double SubDivisions = Math.Round((Splength / ObjectPlacement.Stationincrement));
            //Change
            double SubDivisions = Math.Round((Splength * 20));
            var step = tao / SubDivisions;

            List<XYZ> ControlPoints = new List<XYZ>();



            for (double i = 0.0; i < tao; i = i + step)
            {
                var x = A * Math.Sqrt(2 * i) * (1 - (Math.Pow(i, 2) / 10) + (Math.Pow(i, 4) / 216));
                var y = A * Math.Sqrt(2 * i) * ((i / 3) - (Math.Pow(i, 3) / 42) + (Math.Pow(i, 5) / 1320));

                //var Point = new XYZ(x, y, 0);           
                if (Rot == clockwise.ccw)
                {
                    var Point = new XYZ(y, x, 0) + EndPoint;
                    ControlPoints.Add(Point);

                }
                else
                {
                    var Point = new XYZ(y, x, 0) + startPoint;
                    ControlPoints.Add(Point);
                }
            }

            var V1 = (ControlPoints.Last() - ControlPoints.First()).Normalize();
            var V2 = (EndPoint - startPoint).Normalize();
            var Angle = V2.AngleTo(V1);
            Angle = ((Math.PI / 2) - Angle);
            List<double> Weights = Enumerable.Repeat(1.0, ControlPoints.Count).ToList();
            var P = NurbSpline.CreateCurve(ControlPoints, Weights);


            NurbSpline RotatedCurve = null;
            if (Rot != clockwise.ccw)
            {
                var TransForm = Transform.CreateRotationAtPoint(XYZ.BasisZ, (Angle - Math.PI / 2), startPoint);
                RotatedCurve = (NurbSpline)P.CreateTransformed(TransForm);
            }
            else
            {
                var TransForm = Transform.CreateRotationAtPoint(XYZ.BasisZ, (Angle + Math.PI / 2), EndPoint);
                RotatedCurve = (NurbSpline)P.CreateTransformed(TransForm);

                var PointsReversed = RotatedCurve.CtrlPoints.Reverse();
                RotatedCurve = (NurbSpline)NurbSpline.CreateCurve(PointsReversed.ToList(), Weights);
            }



            RevitSegmentElement = RotatedCurve;
            return RotatedCurve;
        }

        public XYZ GetStartPoint()
        {
            XYZ StartPoint = null;
            if (this.AlignmentSegmentElement is Line)
            {
                StartPoint = ExtractPoint((this.AlignmentSegmentElement as Line).Start);
            }
            if (this.AlignmentSegmentElement is IrregularLine)
            {
                //StartPoint = ExtractPoint((this.AlignmentSegmentElement as IrregularLine).Start);
            }
            if (this.AlignmentSegmentElement is Curve)
            {
                StartPoint = ExtractPoint((this.AlignmentSegmentElement as Curve).Items[0] as PointType);
            }
            if (this.AlignmentSegmentElement is Spiral)
            {
                //review    
                StartPoint = ExtractPoint((this.AlignmentSegmentElement as Spiral).Items[0] as PointType);
            }
            if (this.AlignmentSegmentElement is Chain)
            {
                //Review 
                //StartPoint = ExtractPoint((this.AlignmentSegmentElement as Chain).Text);
            }
            // var StartPointPlacement = new RevitPlacmenElement(StartPoint, Station, this.Alignment);

            return StartPoint;
        }
        public XYZ GetEndPoint()
        {
            XYZ EndPoint = null;

            if (this.AlignmentSegmentElement is Line)
            {
                EndPoint = ExtractPoint((this.AlignmentSegmentElement as Line).End);
            }
            if (this.AlignmentSegmentElement is IrregularLine)
            {
                //  EndPoint = ExtractPoint((this.AlignmentSegmentElement as IrregularLine).End);
            }
            if (this.AlignmentSegmentElement is Curve)
            {
                EndPoint = ExtractPoint((this.AlignmentSegmentElement as Curve).Items[2] as PointType);
            }
            if (this.AlignmentSegmentElement is Spiral)
            {
                //review    
                EndPoint = ExtractPoint((this.AlignmentSegmentElement as Spiral).Items[2] as PointType);
            }
            if (this.AlignmentSegmentElement is Chain)
            {
                //Review 
                // EndPoint = ExtractPoint((this.AlignmentSegmentElement as Chain).Text);
            }

            return EndPoint;

            //return new RevitPlacmenElement(EndPoint, EndStation, this.Alignment);
        }
        public double GetEndStation()
        {
            if (this.AlignmentSegmentElement is Line)
            {
                return (this.Station + (this.AlignmentSegmentElement as Line).length);
            }
            if (this.AlignmentSegmentElement is IrregularLine)
            {
                //return (this.Station + (this.AlignmentSegmentElement as IrregularLine).length);
            }
            if (this.AlignmentSegmentElement is Curve)
            {
                return (this.Station + (this.AlignmentSegmentElement as Curve).length);
            }
            if (this.AlignmentSegmentElement is Spiral)
            {
                return (this.Station + (this.AlignmentSegmentElement as Spiral).length);
            }
            if (this.AlignmentSegmentElement is Chain)
            {
                //Review 
                //return (this.Station + (this.AlignmentSegmentElement as Chain).station);
            }

            return default(double);
        }
        private static XYZ ExtractPoint(PointType PointType)
        {
            var Point = PointType.Text[0].Split(' ');
            Double X;
            Double Y;
            double.TryParse(Point[0], out X);
            double.TryParse(Point[1], out Y);
            XYZ PointStart = new XYZ(Y, X, 0);
            return PointStart;
        }
        private static XYZ ExtractPoint(string[] PointText)
        {
            var Point = PointText[0].Split(' ');
            Double X;
            Double Y;
            double.TryParse(Point[0], out X);
            double.TryParse(Point[1], out Y);
            XYZ PointStart = new XYZ(Y, X, 0);
            return PointStart;
        }
        public RevitPlacmenElement GetPointAtStation(double StationToStudy)
        {
            RevitPlacmenElement PointElement = null;
            if (this.AlignmentSegmentElement is Line)
            {
                XYZ Point = (this.RevitSegmentElement as Autodesk.Revit.DB.Line).Evaluate(StationToStudy - Station, false);
                XYZ NextPoint = (this.RevitSegmentElement as Autodesk.Revit.DB.Line).Evaluate(StationToStudy + 0.0001 - Station, false);
                double AngleToXAxis = ExtractAngleInX(Point, NextPoint);

                XYZ AxisRotationPoint = (this.RevitSegmentElement as Autodesk.Revit.DB.Line).Evaluate(StationToStudy + 0.01 - Station, false);
                var SimplfiedAxis = Autodesk.Revit.DB.Line.CreateBound(Point, AxisRotationPoint);
                PointElement = new RevitPlacmenElement(Point, StationToStudy, this.Alignment, AngleToXAxis, SimplfiedAxis);

            }
            if (this.AlignmentSegmentElement is IrregularLine)
            {
                // XYZ Point = (this.RevitSegmentElement as Autodesk.Revit.DB.Line).Evaluate(StationToStudy - Station, false);
                //  PointElement = new RevitPlacmenElement(Point, StationToStudy, this.Alignment);

            }
            if (this.AlignmentSegmentElement is Curve)
            {
                #region
                //double StationParam;
                //StationParam = 1 - (((StationToStudy - this.Station)) / this.GetLength());
                //XYZ Point = (this.RevitSegmentElement as Autodesk.Revit.DB.Arc).Evaluate(StationParam, true);
                //try
                //{
                //    var NextStationPar = (((StationToStudy + 0.01 - this.Station)) / this.GetLength());
                //    var NextStationParam = 1 - NextStationPar;
                //    var NextPoint = (this.RevitSegmentElement as Autodesk.Revit.DB.Arc).Evaluate(NextStationParam, true);

                //    double AngleToXAxis = ExtractAngleInX(Point, NextPoint);
                //    var AxisStationParam = 1 - (((StationToStudy + 0.01 - this.Station)) / this.GetLength());
                //    var AxisRotationPoint = (this.RevitSegmentElement as Autodesk.Revit.DB.Arc).Evaluate(AxisStationParam, true);
                //    var SimplfiedAxis = Autodesk.Revit.DB.Line.CreateBound(Point, AxisRotationPoint);

                //    PointElement = new RevitPlacmenElement(Point, StationToStudy, this.Alignment, AngleToXAxis, SimplfiedAxis);
                //}
                //catch (Exception)
                //{

                //}

                double StationParam;
                StationParam = 1- (((StationToStudy - this.Station)) / this.GetLength());
                XYZ Point = (this.RevitSegmentElement as Autodesk.Revit.DB.Arc).Evaluate(StationParam, true);
                var NextStationParam = 1-(((StationToStudy + 0.01 - this.Station)) / this.GetLength());
                if (NextStationParam < 0)
                {
                    NextStationParam = -NextStationParam;
                }
                var NextPoint = (this.RevitSegmentElement as Autodesk.Revit.DB.Arc).Evaluate(NextStationParam, true);

                double AngleToXAxis = ExtractAngleInX(Point, NextPoint);

                var AxisStationParam = 1 - (((StationToStudy + 0.01 - this.Station)) / this.GetLength());
                if (AxisStationParam < 0)
                {
                     AxisStationParam = -AxisStationParam;
                }
                var AxisRotationPoint = (this.RevitSegmentElement as Autodesk.Revit.DB.Arc).Evaluate(AxisStationParam, true);
                var SimplfiedAxis = Autodesk.Revit.DB.Line.CreateBound(Point, AxisRotationPoint);

                PointElement = new RevitPlacmenElement(Point, StationToStudy, this.Alignment, AngleToXAxis, SimplfiedAxis);

                #endregion



            }
            if (this.AlignmentSegmentElement is Spiral)
            {
                double StationParam = (StationToStudy - this.Station) / this.GetLength();
                XYZ Point = (this.RevitSegmentElement as NurbSpline).Evaluate((StationToStudy - this.Station), false);

                double StationParamNext = (StationToStudy + 0.01 - this.Station) / this.GetLength();
                XYZ NextPoint = (this.RevitSegmentElement as NurbSpline).Evaluate(StationParamNext, false);

                double AngleToXAxis = ExtractAngleInX(Point, NextPoint);


                double AxisStationParam = (StationToStudy + 0.01 - this.Station) / this.GetLength();
                XYZ AxisPoint = (this.RevitSegmentElement as NurbSpline).Evaluate(AxisStationParam, false);
                var SimplfiedAxis = Autodesk.Revit.DB.Line.CreateBound(Point, AxisPoint);

                PointElement = new RevitPlacmenElement(Point, StationToStudy, this.Alignment, AngleToXAxis, SimplfiedAxis);
            }
            if (this.AlignmentSegmentElement is Chain)
            {
                //Review 
                //return ExtractPoint((this.AlignmentElement as Chain).Text);
            }

            return PointElement;
        }

        private double ExtractAngleInX(XYZ CurrentPoint, XYZ NextPoint)
        {

            XYZ NormalVector = (NextPoint - CurrentPoint).Normalize();
            if (NextPoint.Y > CurrentPoint.Y)
            {
                double Angle = NormalVector.AngleTo(XYZ.BasisX) + (Math.PI / 2);
                return Angle;
            }
            else if (NextPoint.Y < CurrentPoint.Y)
            {
                double Angle = (Math.PI / 2) - NormalVector.AngleTo(XYZ.BasisX);
                return Angle;
            }
            else if (NextPoint.Y == CurrentPoint.Y)
            {
                double Angle = NormalVector.AngleTo(XYZ.BasisX);
                return Angle;
            }

            return 0;
        }

        private Arc CreateArc(XYZ PointStart, XYZ PointEnd, double radius, bool largeSagitta)
        {
            XYZ midPointChord = 0.5 * (PointStart + PointEnd);

            XYZ v = null;
            if (!(bool)this.GetArcRotationAntiClockWise())
            {
                v = PointEnd - PointStart;
            }
            else
            {
                v = PointStart - PointEnd;
            }
            double d = 0.5 * v.GetLength(); // half chord length

            // Small and large circle sagitta:
            // http://www.mathopenref.com/sagitta.html

            double s = largeSagitta
              ? radius + Math.Sqrt(radius * radius - d * d) // sagitta large
              : radius - Math.Sqrt(radius * radius - d * d); // sagitta small

            var PX = Transform.CreateRotation(XYZ.BasisZ, 0.5 * Math.PI);
            var PX2 = v.Normalize();
            var PX3 = v.Normalize().Multiply(s);
            XYZ midPointArc = midPointChord + Transform.CreateRotation(XYZ.BasisZ, 0.5 * Math.PI).OfVector(v.Normalize().Multiply(s));


            return Arc.Create(PointEnd, PointStart, midPointArc);

        }
        public static Arc CreateArcFromCircCurve(XYZ PointStart, XYZ PointEnd, double radius, bool largeSagitta)
        {
            PointStart = new XYZ(PointStart.X, PointStart.Z, 0);
            PointEnd = new XYZ(PointEnd.X, PointEnd.Z, 0);

            XYZ midPointChord = 0.5 * (PointStart + PointEnd);

            XYZ v = null;
            //if (!(bool)this.GetArcRotationAntiClockWise())
            //{
            //    v = PointEnd - PointStart;
            //}
            //else
            //{
            v = PointStart - PointEnd;
            //}            //}
            double d = 0.5 * v.GetLength(); // half chord length

            // Small and large circle sagitta:
            // http://www.mathopenref.com/sagitta.html

            double s = largeSagitta
              ? radius + Math.Sqrt(radius * radius - d * d) // sagitta large
              : radius - Math.Sqrt(radius * radius - d * d); // sagitta small

            var PX = Transform.CreateRotation(XYZ.BasisZ, 0.5 * Math.PI);
            var PX2 = v.Normalize();
            var PX3 = v.Normalize().Multiply(s);
            XYZ midPointArc = midPointChord + Transform.CreateRotation(XYZ.BasisZ, 0.5 * Math.PI).OfVector(v.Normalize().Multiply(s));

            var PointARcEnd = new XYZ(PointEnd.X, PointEnd.Z, PointEnd.Y);
            var PointArcStart = new XYZ(PointStart.X, PointStart.Z, PointStart.Y);
            var PointArcMid = new XYZ(midPointArc.X, midPointArc.Z, midPointArc.Y);
            return Arc.Create(PointARcEnd, PointArcStart, PointArcMid);
        }
        public static double ExtractHeightForPoint(double station, Alignment alignment)
        {
            double Height = default(double);
            if (alignment != null)
            {
                foreach (var HeightElements in LandXmlParser.LandxmlHeighElements)
                {
                    if (station >= HeightElements.Range.Item1 && station <= HeightElements.Range.Item2)
                    {
                        var Zray = Autodesk.Revit.DB.Line.CreateUnbound(new XYZ(station, 0, 0), XYZ.BasisZ);

                        IntersectionResultArray Intersections = null;
                        var REsult = HeightElements.SegmentElement.Intersect(Zray, out Intersections);
                        var IntersectionPoint = Intersections.get_Item(0).XYZPoint;
                        return IntersectionPoint.Z;
                    }
                }
            }
            return default(double);


        }

        public double GetLength()
        {
            double Length = 0;
            if (this.AlignmentSegmentElement is Line)
            {
                Length = (this.AlignmentSegmentElement as Line).length;
            }
            if (this.AlignmentSegmentElement is IrregularLine)
            {
                Length = (this.AlignmentSegmentElement as IrregularLine).length;

            }
            if (this.AlignmentSegmentElement is Curve)
            {
                Length = (this.AlignmentSegmentElement as Curve).length;

            }
            if (this.AlignmentSegmentElement is Spiral)
            {
                //review    
                Length = (this.AlignmentSegmentElement as Spiral).length;

            }
            if (this.AlignmentSegmentElement is Chain)
            {
                //Review 
                //Length = (this.AlignmentElement as Chain).length;
            }
            return Length;
        }

        public XYZ GetPointPI()
        {
            if (this.AlignmentSegmentElement is Curve)
            {
                return ExtractPoint((this.AlignmentSegmentElement as Curve).Items[3] as PointType);
            }
            if (this.AlignmentSegmentElement is Spiral)
            {
                return ExtractPoint((this.AlignmentSegmentElement as Spiral).Items[1] as PointType);
            }
            return null;
        }
        public XYZ GetPointAtCurveCenter()
        {
            if (this.AlignmentSegmentElement is Curve)
            {
                return ExtractPoint((this.AlignmentSegmentElement as Curve).Items[1] as PointType);
            }
            if (this.AlignmentSegmentElement is Spiral)
            {
                //return ExtractPoint((this.AlignmentElement as Spiral).Items[1] as PointType);
            }
            return null;
        }
        public double GetCurveRadius()
        {
            if (this.AlignmentSegmentElement is Curve)
            {
                return (this.AlignmentSegmentElement as Curve).radius;
            }
            if (this.AlignmentSegmentElement is Spiral)
            {
                //return ExtractPoint((this.AlignmentElement as Spiral).Items[1] as PointType);
            }
            return default(double);
        }
        public double GetCurveStartAngle()
        {
            if (this.AlignmentSegmentElement is Curve)
            {
                return (this.AlignmentSegmentElement as Curve).dirStart;
            }
            if (this.AlignmentSegmentElement is Spiral)
            {
                //return ExtractPoint((this.AlignmentElement as Spiral).Items[1] as PointType);
            }
            return default(double);
        }
        public double GetCurveEndAngle()
        {
            if (this.AlignmentSegmentElement is Curve)
            {
                return (this.AlignmentSegmentElement as Curve).dirEnd;
            }
            if (this.AlignmentSegmentElement is Spiral)
            {
                //return ExtractPoint((this.AlignmentElement as Spiral).Items[1] as PointType);
            }
            return default(double);
        }


        public bool? GetArcRotationAntiClockWise()
        {
            if (this.AlignmentSegmentElement is Curve)
            {
                var ArcRot = (this.AlignmentSegmentElement as Curve).rot;
                if (ArcRot == clockwise.cw)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            return null;
        }
    }
}
