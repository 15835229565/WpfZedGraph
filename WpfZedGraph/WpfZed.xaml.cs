using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZedGraph;
using Brush = System.Drawing.Brush;
using Brushes = System.Drawing.Brushes;
using Color = System.Drawing.Color;

namespace WpfZedGraph
{
    /// <summary>
    /// Interaction logic for WpfZed.xaml
    /// </summary>
    public partial class WpfZed : UserControl, INotifyPropertyChanged
    {
        public WpfZed()
        {
            InitializeComponent();
        }

        public int CurvesCount { get { return Source==null? 0: Source.Count; } }


        public ZedGraphControl Control { get { return GraphControl; } }


        public static DependencyProperty SourceProperty;
        public ObservableCollection<Curve> Source
        {
            get { return (ObservableCollection<Curve>)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        static WpfZed()
        {
            SourceProperty = DependencyProperty.Register("Source", typeof(ObservableCollection<Curve>), typeof(WpfZed), new FrameworkPropertyMetadata(null, OnItemsChanged));
        }

        private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var me = d as WpfZed;

            var old = e.OldValue as ObservableCollection<Curve>;

            if (old != null)
                old.CollectionChanged -= me.OnCollectionChanged;

            var n = e.NewValue as ObservableCollection<Curve>;

            if (n != null)
                n.CollectionChanged += me.OnCollectionChanged;

        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                // Clear and update entire collection
                GraphControl.GraphPane.CurveList.Clear();
            }

            if (e.NewItems != null)
            {
                foreach (Curve item in e.NewItems)
                {
                    item.PropertyChanged += OnItemChanged;
                    item.PointsAdded += PointsAdded;
                    item.Refreshed += PointsAdded;

                    var curve = GraphControl.GraphPane.AddCurve(item.Name, item.X, item.Y, item.Color, SymbolType.None);
                    curve.Tag = item.Tag;
                }
            }

            if (e.OldItems != null)
            {
                foreach (Curve item in e.OldItems)
                {
                    item.PropertyChanged -= OnItemChanged;
                    item.PointsAdded -= PointsAdded;
                    item.Refreshed -= PointsAdded;

                    var crv = GraphControl.GraphPane.CurveList.Single(x => (string)x.Tag == item.Tag);
                    if (crv == null) return;

                    GraphControl.GraphPane.CurveList.Remove(crv);
                }
            }

            OnPropertyChanged("CurvesCount");
            GraphControl.AxisChange();
            GraphControl.Invalidate();
        }


        private void OnItemChanged(object sender, PropertyChangedEventArgs e)
        {
            var crv = (Curve)sender;
            var curve = GraphControl.GraphPane.CurveList.Single(x => (string)x.Tag == crv.Tag);
            if (curve == null) return;

            switch (e.PropertyName)
            {
                case "Color": curve.Color = crv.Color;break;
                case "IsVisible": curve.IsVisible = crv.IsVisible; break;
                case "Type":
                case "Width":
                case "CurveBrush":
                case "LineVisible":
                case "SymbColor":
                case "SymbSize":
                    UpdateCurveForcely(curve, crv);
                    break;
                default:
                    break;
            }
            


            GraphControl.AxisChange();
            GraphControl.Invalidate();
        }

        private void UpdateCurveForcely(CurveItem curve, Curve crv)
        {
            GraphControl.GraphPane.CurveList.Remove(curve);
            var newCurve = GraphControl.GraphPane.AddCurve(crv.Name, crv.X, crv.Y, crv.Color, crv.Type);
            newCurve.Tag = crv.Tag;
            newCurve.IsVisible = crv.IsVisible;
            newCurve.Line.Width = crv.Width;
            newCurve.Line.IsVisible = crv.LineVisible;
            if(crv.CurveBrush.Count>=2) newCurve.Line.Fill = new Fill(crv.CurveBrush.ToArray());
            newCurve.Symbol.Size = crv.SymbSize;
            newCurve.Symbol.Fill.Color = crv.SymbColor;
            newCurve.Symbol.Fill.Type = FillType.Solid;
            

            GraphControl.AxisChange();
            GraphControl.Invalidate();
        }

        private void PointsAdded(object sender, RoutedEventArgs e)
        {
            var crv = (Curve)sender;

            var curve = GraphControl.GraphPane.CurveList.Single(x => (string)x.Tag == crv.Tag);
            if (curve == null) return;

            UpdateCurveForcely(curve, crv);
        }
        

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    [Serializable]
    public class Curve : INotifyPropertyChanged
    {
        public static Random Generator = new Random();
        public const string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        
        public static string GetRandomString(int length)
        {
            return new string(Enumerable.Repeat(CHARS, length).Select(s => s[Generator.Next(s.Length)]).ToArray());
        }

        private string _name = "New curve";
        public string Name { get { return _name; } set { _name = value; OnPropertyChanged("Name"); } }

        public string Tag { get; set; }

        public int PointsCount { get { return Points.Count; } }


        private ObservableCollection<Point> _points = new ObservableCollection<Point>();
        public ObservableCollection<Point> Points { get { return _points; } set { _points = value; OnPropertyChanged("Points"); OnPropertyChanged("PointsCount"); } }

        public double[] X { get { return _points.Select(x => x.X).ToArray(); } }


        public double[] Y { get { return _points.Select(x => x.Y).ToArray(); } }


        private System.Drawing.Color _color = System.Drawing.Color.Black;
        public System.Drawing.Color Color { get { return _color; } set { _color = value; OnPropertyChanged("Color"); } }

        private System.Drawing.Color _symbColor = System.Drawing.Color.Blue;
        public Color SymbColor { get { return _symbColor; } set { _symbColor = value; OnPropertyChanged("SymbColor"); } }

        private float _symbSize = 5;
        public float SymbSize { get { return _symbSize; } set { _symbSize = value; OnPropertyChanged("SymbSize"); } }



        private bool _isVisible = true;
        public bool IsVisible { get { return _isVisible; } set { _isVisible = value; OnPropertyChanged("IsVisible"); } }


        private bool _lineVisible = true;
        public bool LineVisible { get { return _lineVisible; } set { _lineVisible = value;OnPropertyChanged("LineVisible"); } }

        private float _width = 1f;
        public float Width { get { return _width; } set { _width = value;OnPropertyChanged("Width"); } }

        private SymbolType _type = SymbolType.None;
        public SymbolType Type { get { return _type; } set { _type = value;OnPropertyChanged("Type"); } }

        private ObservableCollection<Color> _brush = new ObservableCollection<Color>();
        public ObservableCollection<Color> CurveBrush { get { return _brush; } set { _brush = value; OnPropertyChanged("CurveBrush"); } }


        public Curve(double[] x, double[] y)
        {
            if (x == null || y == null) throw new NullReferenceException("Ссылка на массив не указана");

            if (x.Length != y.Length) throw new Exception("Длины массивов x и y не совпадают");

            Points.CollectionChanged += (a, b) => OnPropertyChanged("PointsCount");
            CurveBrush.CollectionChanged += (a, b) => Refreshed(this, new RoutedEventArgs());
            Tag = GetRandomString(10);

            for (int i = 0; i < x.Length; i++)
            {
                Points.Add(new Point(x[i], y[i]));
            }


        }

        public Curve(IEnumerable<Point> source)
        {
            if (source == null) throw new NullReferenceException("Ссылка на коллекцию не указана");
            Points.CollectionChanged += (a, b) => OnPropertyChanged("PointsCount");
            CurveBrush.CollectionChanged += (a, b) => Refreshed(this, new RoutedEventArgs());
            Tag = GetRandomString(10);

            foreach (var pt in source)
            {
                Points.Add(new Point(pt.X, pt.Y));
            }

        }

        public Curve(double[] y)
        {
            if (y == null) throw new NullReferenceException("Ссылка на массив не указана");

            Points.CollectionChanged += (a, b) => OnPropertyChanged("PointsCount");
            CurveBrush.CollectionChanged += (a, b) => Refreshed(this, new RoutedEventArgs());
            Tag = GetRandomString(10);

            for (int i = 0; i < y.Length; i++)
            {
                Points.Add(new Point(i, y[i]));
            }
        }

        public void AddPoints(IEnumerable<Point> points)
        {
            foreach (var pt in points)
            {
                Points.Add(new Point(pt.X, pt.Y));
            }
            if(PointsAdded!=null) PointsAdded(this, new RoutedEventArgs());
        }

        public void Refresh()
        {
            if (Refreshed != null) Refreshed(this, new RoutedEventArgs());
        }


        public delegate void PointsAddedDelegate(object sender, RoutedEventArgs e);
      
        [field: NonSerialized]
        public event PointsAddedDelegate PointsAdded;


        public delegate void RefreshDelegate(object sender, RoutedEventArgs e);
       
        [field: NonSerialized]
        public event RefreshDelegate Refreshed;


        [field:NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
