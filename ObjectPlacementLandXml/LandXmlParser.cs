using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
namespace ObjectPlacementLandXml
{
    class LandXmlParser
    {
        public static List<HeightElements> LandxmlHeighElements;
        public static LandXML Deserialize(string path)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(path);
            XmlSerializer serializer = new XmlSerializer(typeof(LandXML));
            StreamReader reader = new StreamReader(path);
            LandXML Schema = (LandXML)serializer.Deserialize(reader);
            reader.Close();
            return Schema;
        }
        internal static List<RevitPlacmenElement> ParseLandXml(string LandXmlPath)
        {
            LandXML Landx = Deserialize(LandXmlPath);
            return ExtractPointPlacment(Landx);

        }

        private static List<RevitPlacmenElement> ExtractPointPlacment(LandXML Landx)
        {
            List<RevitPlacmenElement> RevitPlacementPoints = new List<RevitPlacmenElement>();

            foreach (Alignments Alignments in Landx.Items.OfType<Alignments>())
            {
                foreach (var Alignment in Alignments.Alignment)
                {
                    //stationing
                    List<double> Stations = CreateStationing(ObjectPlacement.TransForm.DistanceBetweenStations, Alignment);
                    LandxmlHeighElements = ExtractHeightElemenets(Alignment);

                    List<LandXmlStationingObject> LandXmlAlignmentObjects = ExtractStationingObjects(Alignment, Stations);

                    //Placment
                    ExtractPlacementPoints(Alignment, RevitPlacementPoints, Stations, LandXmlAlignmentObjects);

                    using (Transaction T = new Transaction(Command.uidoc.Document, "Create Groups"))
                    {
                        T.Start();

                        var ids = LandXmlAlignmentObjects.Select(E => E.RevitModelCurve?.Id);
                        var FilteredIds = ids.Where(E => E != null);
                        //ids.ToList().RemoveAll(EE => EE == null && EE.GetType() != typeof(ElementId));
                        if (FilteredIds.Any())
                        {
                            Group G = Command.uidoc.Document.Create.NewGroup(FilteredIds.ToList());
                            G.GroupType.Name = Alignment.name;
                           // G.Name = Alignment.name;
                        }
                        T.Commit();
                    }
                }
            }

            return RevitPlacementPoints;
        }
        private static XYZ ExtractPVIPoint(string[] ProfileElement)
        {
            //var PviPoint = (ProfileElement as PVI).Text;
            var Tx = ProfileElement[0].Split(' ');
            Double PVIX;
            Double PVIZ;

            double.TryParse(Tx[0], out PVIX);
            double.TryParse(Tx[1], out PVIZ);

            var HeightPOint = (new XYZ(PVIX, 0, PVIZ));
            return HeightPOint;
        }
        private static List<HeightElements> ExtractHeightElemenets(Alignment alignment)
        {
            List<HeightElements> HeightElements = new List<HeightElements>();
            foreach (var Prof in alignment.Items.OfType<Profile>())
            {
                foreach (var Profilealign in Prof.Items.OfType<ProfAlign>())
                {
                    for (int i = 0; i < Profilealign.Items.Count() - 1; i++)
                    {
                        var CurrentPViPoint = Profilealign.Items[i];
                        ExtractHeightElements(HeightElements, Profilealign, i, CurrentPViPoint);
                    }

                }
            }

            XYZ EndPoint = HeightElements.Last().SegmentElement.GetEndPoint(1);
            XYZ ProfileEnd = new XYZ(alignment.length, EndPoint.Y, EndPoint.Z);

            var Distance = EndPoint.DistanceTo(ProfileEnd);
            if (Math.Round(EndPoint.X, 7) != Math.Round(ProfileEnd.X, 7))
            {
                var L = Autodesk.Revit.DB.Line.CreateBound(EndPoint, ProfileEnd);
                HeightElements.Add(new HeightElements((HeightElements.Last().Range.Item2, alignment.length), L));
            }
            return HeightElements;

        }

        private static void ExtractHeightElements(List<HeightElements> HeightElements, ProfAlign Profilealign, int i, object CurrentPViPoint)
        {
            if (CurrentPViPoint is PVI)
            {
                XYZ PVIPoint = ExtractPVIPoint((CurrentPViPoint as PVI).Text);
                var NextPVIPointLand = Profilealign.Items[i + 1];

                (double, double) Range = (0, 0);
                Range.Item1 = PVIPoint.X;

                Autodesk.Revit.DB.Line LineElement = GetLineElementForPVI(PVIPoint, NextPVIPointLand, ref Range);
                HeightElements.Add(new HeightElements(Range, LineElement));
            }
            else if (CurrentPViPoint is CircCurve)
            {
                (double, double) ARcRange, LineAfterRange;
                XYZ ArcPointEnd, NextPviPoint;
                Arc ARC;

                GetarcElementPVI(Profilealign, i, CurrentPViPoint, out ARcRange, out ArcPointEnd, out LineAfterRange, out NextPviPoint, out ARC);

                HeightElements.Add(new HeightElements(ARcRange, ARC));

                var LineAfterCurve = Autodesk.Revit.DB.Line.CreateBound(ArcPointEnd, NextPviPoint);
                HeightElements.Add(new HeightElements(LineAfterRange, LineAfterCurve));

            }
        }

        private static void GetarcElementPVI(ProfAlign Profilealign, int i, object CurrentPViPoint, out (double, double) ARcRange, out XYZ ArcPointEnd, out (double, double) LineAfterRange, out XYZ NextPviPoint, out Arc ARC)
        {
            XYZ PVIPoint = ExtractPVIPoint((CurrentPViPoint as CircCurve).Text);
            ARcRange = (0, 0);
            var HalfArchLength = (CurrentPViPoint as CircCurve).length / 2;
            ARcRange.Item1 = PVIPoint.X - HalfArchLength;
            ARcRange.Item2 = PVIPoint.X + HalfArchLength;

            XYZ ArcPointStart = null;
            ArcPointEnd = null;
            var PreviousPointLand = Profilealign.Items[i - 1];

            XYZ PreviousPVI = null;
            if (PreviousPointLand is PVI)
            {
                PreviousPVI = ExtractPVIPoint((PreviousPointLand as PVI).Text);
            }
            else if (PreviousPointLand is CircCurve)
            {
                PreviousPVI = ExtractPVIPoint((PreviousPointLand as CircCurve).Text);
            }
            var L = Autodesk.Revit.DB.Line.CreateBound(PreviousPVI, PVIPoint);
            var Value = (PVIPoint.X - HalfArchLength);
            XYZ PointToProject = new XYZ(Value, 0, 0);

            var Zray = Autodesk.Revit.DB.Line.CreateUnbound(PointToProject, XYZ.BasisZ);
            ArcPointStart = L.Project(PointToProject).XYZPoint;

            IntersectionResultArray Intersections = null;
            var REsult = L.Intersect(Zray, out Intersections);
            ArcPointStart = Intersections.get_Item(0).XYZPoint;
            //review Arc


            LineAfterRange = (0, 0);
            LineAfterRange.Item1 = PVIPoint.X + HalfArchLength;
            var NextPVIPointLand = Profilealign.Items[i + 1];
            NextPviPoint = null;
            if (NextPVIPointLand is PVI)
            {
                NextPviPoint = ExtractPVIPoint((NextPVIPointLand as PVI).Text);
                LineAfterRange.Item2 = NextPviPoint.X;

            }
            else if (NextPVIPointLand is CircCurve)
            {
                NextPviPoint = ExtractPVIPoint((NextPVIPointLand as CircCurve).Text);
                var HalfNextArchLength = (NextPVIPointLand as CircCurve).length / 2;
                LineAfterRange.Item2 = NextPviPoint.X - HalfNextArchLength;

            }
            var L2 = Autodesk.Revit.DB.Line.CreateBound(PVIPoint, NextPviPoint);
            var Value2 = (PVIPoint.X + HalfArchLength);
            XYZ PointToProject2 = new XYZ(Value2, 0, 0);
            // ArcPointEnd = L2.Project(PointToProject2).XYZPoint;
            var Zray2 = Autodesk.Revit.DB.Line.CreateUnbound(PointToProject2, XYZ.BasisZ);
            IntersectionResultArray Intersections2 = null;
            var REsult2 = L2.Intersect(Zray2, out Intersections2);
            ArcPointEnd = Intersections2.get_Item(0).XYZPoint;


            ARC = LandXmlStationingObject.CreateArcFromCircCurve(ArcPointStart, ArcPointEnd, (CurrentPViPoint as CircCurve).radius, (bool)false);

            //midpoint is not correct 
            //ARC = Autodesk.Revit.DB.Arc.Create(ArcPointStart, ArcPointEnd, (ArcPointStart + ArcPointEnd / 2));
        }

        private static Autodesk.Revit.DB.Line GetLineElementForPVI(XYZ PVIPoint, object NextPVIPointLand, ref (double, double) Range)
        {
            XYZ NextPviPoint = null;
            if (NextPVIPointLand is PVI)
            {
                NextPviPoint = ExtractPVIPoint((NextPVIPointLand as PVI).Text);
                Range.Item2 = NextPviPoint.X;
            }
            else if (NextPVIPointLand is CircCurve)
            {
                NextPviPoint = ExtractPVIPoint((NextPVIPointLand as CircCurve).Text);
                var ArcStartStation = (NextPVIPointLand as CircCurve).length / 2;
                Range.Item2 = NextPviPoint.X - ArcStartStation;
            }
            var LineElement = Autodesk.Revit.DB.Line.CreateBound(PVIPoint, NextPviPoint);
            return LineElement;
        }

        private static void ExtractPlacementPoints(Alignment alignment, List<RevitPlacmenElement> RevitPlacementPoint, List<double> Stations, List<LandXmlStationingObject> LandXmlAlignmentObjects)
        {
            var StationsToStudy = Stations.Distinct().ToList();
            StationsToStudy.Sort();

            foreach (var LandXmlObject in LandXmlAlignmentObjects)
            {
                if (LandXmlObject.Alignment == alignment)
                {
                    for (int i = 0; i < StationsToStudy.Count; i++)
                    {
                        var Station = StationsToStudy[i];

                        if (ObjectPlacement.TransForm.CreateStationsAtEndAndStartCheck == false)
                        {
                            if (Station == LandXmlObject.Station && i != 0)
                            {
                                continue;
                            }
                            if (Station == (LandXmlObject.Station + LandXmlObject.GetLength()) && i != StationsToStudy.Count - 1)
                            {
                                continue;
                            }
                        }
                        if (Station >= LandXmlObject.Station && Station <= (LandXmlObject.Station + LandXmlObject.GetLength()))
                        {
                            var PointAtStatation = LandXmlObject.GetPointAtStation(Station);
                            RevitPlacementPoint.Add(PointAtStatation);
                            continue;
                        }

                    }
                }
            }
            //RevitPlacementPoint.Add(LandXmlAlignmentObjects.Last().GetEndPoint());
        }

        //Stationing 
        private static List<double> CreateStationing(double StationIncrement, Alignment Alignment)
        {
            List<double> Stations = new List<double>();
            var StartStationX = Alignment.staStart;

            for (double i = StartStationX; i <= Alignment.length + StartStationX; i += StationIncrement)
            {
                Stations.Add(i);
            }

            return Stations;
        }
        private static List<LandXmlStationingObject> ExtractStationingObjects(Alignment Alignment, List<double> Stations)
        {
            var ObjectStation = Alignment.staStart;
            List<LandXmlStationingObject> Objects = new List<LandXmlStationingObject>();
            using (Transaction T = new Transaction(Command.uidoc.Document, "Create RevitElements"))
            {
                T.Start();
                foreach (CoordGeom CoordGeom in Alignment.Items.OfType<CoordGeom>())
                {
                    foreach (object CoordGeoItem in CoordGeom.Items)
                    {
                        if (Alignment.name.ToLower() == "NL_SMA_Pilotlager_Zugang".ToLower())
                        {

                        }
                        var LandXmlAlignMentObj = new LandXmlStationingObject(ObjectStation, CoordGeoItem, Alignment);
                        Objects.Add(LandXmlAlignMentObj);

                        ObjectStation = LandXmlAlignMentObj.GetEndStation();
                        Stations.Add(ObjectStation);
                    }

                }
                T.Commit();
            }
            Stations = Stations.Distinct().ToList();
            return Objects;
        }


    }
}
