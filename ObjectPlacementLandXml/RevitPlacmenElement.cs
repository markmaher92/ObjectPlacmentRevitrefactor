using Autodesk.Revit.DB;
using System;

namespace ObjectPlacementLandXml
{
    public class RevitPlacmenElement
    {

        public XYZ PlacementPoint { get; set; }
        public double Station { get; set; }
        public double TextStation { get; set; }
        public double RotationToAlignmentInX { get; set; }
        public Autodesk.Revit.DB.Line SimplifiedAlignmentAxis { get; set; }

        public RevitPlacmenElement(XYZ placementPoint, double station, Alignment alignment, double rotationToAlignmentInX, Autodesk.Revit.DB.Line simplifiedAlignmentAxis)
        {
            PlacementPoint = placementPoint;
            Station = Math.Round(station, 4);
            TextStation = station;
            RotationToAlignmentInX = rotationToAlignmentInX;
            SimplifiedAlignmentAxis = simplifiedAlignmentAxis;
            var PointElevation = LandXmlStationingObject.ExtractHeightForPoint(this.Station, alignment);
            this.PlacementPoint = new XYZ(PlacementPoint.X, PlacementPoint.Y, PointElevation);
        }
        public static XYZ ConvertPointToInternal(XYZ PointToConvert)
        {
            if (PointToConvert != null)
            {
                var ConvetedPoint = new XYZ(UnitUtils.ConvertToInternalUnits(PointToConvert.X, UnitTypeId.Meters), UnitUtils.ConvertToInternalUnits(PointToConvert.Y, UnitTypeId.Meters), UnitUtils.ConvertToInternalUnits(PointToConvert.Z, UnitTypeId.Meters));
                return ConvetedPoint;
            }
            return null;
        }
        public static double ConvertDoubleToInternal(double DoubleToConvert)
        {
            if (DoubleToConvert != default(double))
            {
                var ConvetedPoint = UnitUtils.ConvertToInternalUnits(DoubleToConvert, UnitTypeId.Meters);
                return ConvetedPoint;
            }
            return default(double);
        }
        public static double ConvertAngleToInternal(double AngleToConvert)
        {
            if (AngleToConvert != default(double))
            {
                var ConvetedPoint = UnitUtils.ConvertToInternalUnits(AngleToConvert, UnitTypeId.DegreesMinutes);
                return ConvetedPoint;
            }
            return default(double);
        }
        public void FillAttributes(FamilyInstance FamIns)
        {
            try
            {
                foreach (var Param in ParameterValues.ParamNames)
                {
                    OverrideParamterValue(FamIns, Param);
                }
                var StationParam = FamIns.LookupParameter("Text");
                if (StationParam != null)
                {
                    StationParam.Set(TextStation.ToString());
                }
            }
            catch (Exception)
            {

            }
        }

        private void OverrideParamterValue(FamilyInstance FamIns, ParameterElement Param)
        {
            try
            {
                Parameter ParamFou = FamIns.LookupParameter(Param.ParameterName);
                if (ParamFou != null && !string.IsNullOrEmpty(Param.ParameterValue))
                {
                    switch (ParamFou.StorageType)
                    {
                        case StorageType.None:
                            ParamFou.SetValueString(Param.ParameterValue.ToString());
                            break;
                        case StorageType.Integer:
                            ParamFou.SetValueString(Param.ParameterValue.ToString());
                            break;
                        case StorageType.Double:
                            ParamFou.SetValueString(Param.ParameterValue.ToString());
                            break;
                        case StorageType.String:
                            ParamFou.Set(Param.ParameterValue.ToString());
                            break;
                        //case StorageType.ElementId:
                        //    ParamFou.Set(Station.ToString());
                        //    break;
                        default:
                            ParamFou.Set(Station.ToString());
                            break;
                    }


                }


            }
            catch (Exception)
            {

            }
        }
        //private void FillParameters(FamilyInstance FamIns, double Stationtext)
        //{
        //    try
        //    {
        //        if (!string.IsNullOrWhiteSpace(this.ElevationTxt.Text))
        //        {
        //            var elevation = UnitUtils.ConvertToInternalUnits(double.Parse(this.ElevationTxt.Text), DisplayUnitType.DUT_MILLIMETERS);
        //            FamIns.LookupParameter("Elevation").Set(elevation);
        //        }

        //    }
        //    catch (Exception)
        //    {
        //    }
        //    try
        //    {
        //        var RoundedX = Math.Round(Stationtext, 3);
        //        FamIns.LookupParameter("Text").Set(RoundedX.ToString());
        //    }
        //    catch (Exception)
        //    {
        //    }
        //    try
        //    {
        //        var HorizontalDistance = UnitUtils.ConvertToInternalUnits(double.Parse(this.HorizontalDistancetext.Text), DisplayUnitType.DUT_MILLIMETERS);
        //        FamIns.LookupParameter("Horizontal Distance").Set(HorizontalDistance);
        //    }
        //    catch (Exception)
        //    {
        //    }
        //    try
        //    {
        //        var HorizontalDistance = UnitUtils.ConvertToInternalUnits(double.Parse(this.TextHeightTxt.Text), DisplayUnitType.DUT_MILLIMETERS);
        //        FamIns.LookupParameter("TextDepth").Set(HorizontalDistance);
        //    }
        //    catch (Exception)
        //    {
        //    }
        //    try
        //    {
        //        Double Inclinnation = UnitUtils.ConvertToInternalUnits(double.Parse(InclinationTxt.Text), DisplayUnitType.DUT_DEGREES_AND_MINUTES);
        //        FamIns.LookupParameter("InclinationAngle").Set(Inclinnation);


        //    }
        //    catch (Exception)
        //    {

        //    }
        //}
    }
}