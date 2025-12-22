using NAudio.Dsp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace MusicPlayer_ovh
{
    /// <summary>
    /// Interaction logic for Mixer.xaml
    /// </summary>
    public partial class Mixer : Window
    {
        AudioPlayer Player;
        double[] eqGains = new double[6] {0,0,0,0,0,0};

        PointCollection points;

        public Mixer(AudioPlayer player)
        {
            InitializeComponent();
            Player = player;
            points = new PointCollection();
            loadGains();
            setEQ();

            for (int i = 0; i < 6; i++)
            {
                Slider slider = (Slider)this.FindName("slider" + (i+1));
                slider.Value = eqGains[i];
            }

            this.Loaded += (s, e) =>
            {
                UpdatePoints();
                CreateBG();
                UpdateVisualCurve(eqGains);
            };

            

        }

        private void CloseButton(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();

            }
        }

        private void SliderMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                e.Handled = true;

                slider.CaptureMouse();

                UpdateValueToMouse(slider, e.GetPosition(slider));

            }
        }
        private void SliderMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider && slider.IsMouseCaptured)
            {
                e.Handled = true;
                slider.ReleaseMouseCapture();

                int id = int.Parse(slider.Tag.ToString());
                
                Player.UpdateEQ(id, (float)slider.Value);


            }
        }
        private void SliderMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is Slider slider && slider.IsMouseCaptured)
            {
                e.Handled = true;
                UpdateValueToMouse(slider, e.GetPosition(slider));
                
            }
        }
        private void UpdateValueToMouse(Slider slider, Point mousePos)
        {
            double ratio = mousePos.Y / slider.ActualHeight;

            ratio = Math.Max(0, Math.Min(1, ratio));

            double invertedRatio = 1 - ratio;

            slider.Value = invertedRatio * (slider.Maximum - slider.Minimum) + slider.Minimum;


            int id = int.Parse(slider.Tag.ToString());
            eqGains[id] = (double)slider.Value;
            UpdateVisualCurve(eqGains);


        }


        public void UpdateVisualCurve(double[] gains)
        {
            // adding points based on value of sliders to array
            points.Clear();
            double GraphWidth = PathArea.ActualWidth;
            double GraphHeight = PathArea.ActualHeight;

            double segmentWidth = GraphWidth / 5;

            UpdatePoints();


            PathFigure lineFigure = new PathFigure { StartPoint = points[0], IsClosed = false };

            for (int i = 0; i < points.Count - 1; i++)
            {
                Point p1 = points[i];
                Point p2 = points[i + 1];

                // midpoint for the curve anchor
                double tension = 0.3;

                Point control1 = new Point(p1.X + (p2.X - p1.X) * tension, p1.Y);
                Point control2 = new Point(p2.X - (p2.X - p1.X) * tension, p2.Y);

                lineFigure.Segments.Add(new BezierSegment(control1, control2, p2, true));
            }

            lineFigure.Segments.Add(new LineSegment(points[points.Count - 1], true));

            

            // apply to the Line Path
            PathGeometry lineGeo = new PathGeometry();
            lineGeo.Figures.Add(lineFigure);
            PathLine.Data = lineGeo;

            // fill area
            PathFigure areaFigure = lineFigure.Clone();

            areaFigure.Segments.Add(new LineSegment(new Point(GraphWidth, GraphHeight), false));

            areaFigure.Segments.Add(new LineSegment(new Point(0, GraphHeight), false));
            areaFigure.IsClosed = true;

            PathGeometry areaGeo = new PathGeometry();
            areaGeo.Figures.Add(areaFigure);
            PathArea.Data = areaGeo;
        }
        private void CreateBG()
        {
            double GraphWidth = PathArea.ActualWidth;
            double GraphHeight = PathArea.ActualHeight;
            // max, min, mid lines
            PathGeometry lineGeoBg = new PathGeometry();

            PathFigure maxLine = new PathFigure { StartPoint = new Point(0, MapGainToY(12)), IsClosed = false };
            maxLine.Segments.Add(new LineSegment(new Point(GraphWidth, MapGainToY(12)), true));
            PathFigure minLine = new PathFigure { StartPoint = new Point(0, MapGainToY(-12)), IsClosed = false };
            minLine.Segments.Add(new LineSegment(new Point(GraphWidth, MapGainToY(-12)), true));
            PathFigure midLine = new PathFigure { StartPoint = new Point(0, MapGainToY(0)), IsClosed = false };
            midLine.Segments.Add(new LineSegment(new Point(GraphWidth, MapGainToY(0)), true));

            lineGeoBg.Figures.Add(maxLine);
            lineGeoBg.Figures.Add(minLine);
            lineGeoBg.Figures.Add(midLine);
            PathBackground.Data = lineGeoBg;
            // columns
            PathGeometry lineGeoCol = new PathGeometry();

            for(int i = 1; i < points.Count - 1; i++)
            {
                PathFigure col1 = new PathFigure { StartPoint = new Point(points[i].X, (MapGainToY(12) + 10)), IsClosed = false };
                col1.Segments.Add(new LineSegment(new Point(points[i].X, (MapGainToY(-12) - 10)), true));
                lineGeoCol.Figures.Add(col1);
            }
            PathBackgroundVertical.Data = lineGeoCol;
        }
        private void UpdatePoints()
        {
            double GraphWidth = PathArea.ActualWidth;
            double GraphHeight = PathArea.ActualHeight;
            points.Add(new Point(0, MapGainToY(eqGains[0])));

            for (int i = 0; i < eqGains.Length; i++)
            {
                double x = (i * (GraphWidth / 6)) + ((GraphWidth / 6) / 2);
                double y = MapGainToY(eqGains[i]);

                points.Add(new Point(x, y));
            }
            points.Add(new Point(GraphWidth, MapGainToY(eqGains[eqGains.Length - 1])));
        }
        private double MapGainToY(double gain)
        {

            double canvasHeight = PathArea.ActualHeight;
            double midY = canvasHeight / 2;
            double ThumbMargin = 10;
            double usableHeight = canvasHeight - (2 * ThumbMargin);

            double scale = usableHeight / 24.0;

            return midY - (gain * scale);
        }

        private void loadGains()
        {
            if (Properties.Settings.Default.Gains != null && Properties.Settings.Default.Gains.Count == 6)
            {
                for (int i = 0; i < 6; i++)
                {
                    eqGains[i] = double.Parse(Properties.Settings.Default.Gains[i]);
                    Slider slider = (Slider)this.FindName("slider" + (i+1));
                    slider.Value = eqGains[i];
                }
            }
            else
            {
                saveGains();
            }
        }
        private void saveGains()
        {
            var gainStrings = new System.Collections.Specialized.StringCollection();
            for(int i = 0; i < 6; i++)
            {
                gainStrings.Add(eqGains[i].ToString());
            }
            Properties.Settings.Default.Gains = gainStrings;
        }

        private void setEQ()
        {
            for (int i = 0; i < 6; i++)
            {
                Player.UpdateEQ(i, (float)eqGains[i]);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            saveGains();

            Properties.Settings.Default.Save();

            this.Hide();
            e.Cancel = true;
        }
    }

}
