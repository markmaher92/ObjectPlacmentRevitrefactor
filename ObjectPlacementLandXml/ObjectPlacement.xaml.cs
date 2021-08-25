using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ObjectPlacementLandXml
{
    /// <summary>
    /// Interaction logic for ObjectPlacement.xaml
    /// </summary>
    public partial class ObjectPlacement : Window
    {
        public static ElementTransformParams TransForm { get; set; }
        public List<RevitPlacmenElement> RevitPlaceMentPoints { get; set; }
        public ObjectPlacement()
        {
            InitializeComponent();
        }

        private void Run_click(object sender, RoutedEventArgs e)
        {
            TransForm = ExtractTransformParameters();
            RevitPlaceMentPoints = LandXmlParser.ParseLandXml(LandXmlPath.Text);

            ParameterValues W = new ParameterValues(RevitPlaceMentPoints, FamilyPath.Text, TransForm);
            W.ShowDialog();
            //RevitHelper.PlaceRevitFamilies(RevitPlaceMentPoints, uiDoc, FamilyPath.Text);
        }

        private ElementTransformParams ExtractTransformParameters()
        {
            ElementTransformParams TransFormParams = new ElementTransformParams();
            if (!string.IsNullOrEmpty(this.HorizontalDistancetext.Text))
            {
                TransFormParams.HorizontalDistance = double.Parse(this.HorizontalDistancetext.Text);
            }
            if (!string.IsNullOrEmpty(this.ElevationTxt.Text))
            {
                TransFormParams.ElevationFromAlignment = double.Parse(this.ElevationTxt.Text);
            }
            if (!string.IsNullOrEmpty(this.DegreesTxt.Text))
            {
                TransFormParams.RotationAngleInPlane = double.Parse(this.DegreesTxt.Text);
            }
            if (!string.IsNullOrEmpty(this.InclinationTxt.Text))
            {
                TransFormParams.InclinationAngleInXZPlane = double.Parse(this.InclinationTxt.Text);
            }
            if (!string.IsNullOrEmpty(this.StationDistanceTxt.Text))
            {
                TransFormParams.DistanceBetweenStations = double.Parse(this.StationDistanceTxt.Text);
            }
            TransFormParams.RotateWithAlignment = RotateWithAlignment.IsChecked;
            TransFormParams.CreateAlignment = CreateAlignmentInModelCheck.IsChecked;
            TransFormParams.CreateStationsAtEndAndStartCheck = CreateStationsAtEndAndStartCheck.IsChecked;

            TransFormParams.StationToStartFrom = ExtractStationPlacmentStart();
            TransFormParams.StationToEndAt = ExtractStationPlacmentEnd();

            return TransFormParams;
        }


        public double? ExtractStationPlacmentStart()
        {
            double StationPlaceMentStart = default(double);


            return StationPlaceMentStart;
        }
        public double? ExtractStationPlacmentEnd()
        {
            double StationPlaceMentEnd = default(double);
            if (!string.IsNullOrEmpty(this.PlacmentEndStationText.Text))
            {
                StationPlaceMentEnd = double.Parse(this.PlacmentEndStationText.Text);
            }
            return StationPlaceMentEnd;
        }
        private void LandXmlPathBut(object sender, RoutedEventArgs e)
        {
            WindowDialogs.LandXmlOpenDialog(LandXmlPath);
        }

        private void RevitBrowserClick(object sender, RoutedEventArgs e)
        {
            WindowDialogs.OpenDialogRev(FamilyPath);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TransForm = ExtractTransformParameters();
            RevitPlaceMentPoints = LandXmlParser.ParseLandXml(LandXmlPath.Text);
        }
    }
}
