using AdvanceMath;
using CustomBodies;
using Interfaces;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using WorldControllers;

namespace Renderers
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class CanvasRenderer : UserControl, IDisposable, IRenderer
    {
        DrawingVisual _drawing;
        RenderTargetBitmap _bmp;
        WriteableBitmap _wrBmp;
        Brush _defaultBrush = new SolidColorBrush(Colors.WhiteSmoke);

        public CanvasRenderer()
        {
            InitializeComponent();

            _drawing = new DrawingVisual();

            _bmp = new RenderTargetBitmap(1024, 768, 96, 96, PixelFormats.Pbgra32);
            _wrBmp = BitmapFactory.New(1024, 768);

            RenderingImage.Source = _wrBmp;


            Representation.Instance.RegisterRenderer(this as IRenderer, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public void Dispose()
        {
            Representation.Instance.UnregisterRenderer(this as IRenderer);
        }

        private StreamGeometry BuildPolygonGeometry(BasePolygonBody body)
        {
            var geom = new StreamGeometry() { FillRule = FillRule.EvenOdd };

            using (var figCtx = geom.Open())
            {
                Vector2D start = body.Drawable.Polygon.Vertices[0];
                figCtx.BeginFigure(new Point(start.X, start.Y), true, true);

                foreach (var vertex in body.Drawable.Polygon.Vertices.Skip(1))
                {
                    figCtx.LineTo(new Point(vertex.X, vertex.Y), false, false);
                }
            }

            var mtx = body.Transformation;

            geom.Transform = new MatrixTransform(mtx.m00, mtx.m01, mtx.m10, mtx.m11, body.State.Position.X, body.State.Position.Y);

            geom.Freeze();

            return geom;
        }

        public void RenderWorld(IWorldSnapshot snapshot)
        {
            var op = _drawing.Dispatcher.InvokeAsync(() => {
                var polygonBodies = snapshot.Bodies.OfType<BasePolygonBody>();

                //var centerTransform = new TranslateTransform(_bmp.Width / 2, _bmp.Height / 2);
                //_drawing.Transform = centerTransform;

                // Retrieve the DrawingContext in order to create new drawing content.


                using (var drawCtx = _drawing.RenderOpen())
                {

                    foreach (var body in polygonBodies)
                    {
                        var geom = BuildPolygonGeometry(body);

                        drawCtx.DrawGeometry(_defaultBrush, new Pen(_defaultBrush, 2), geom);
                    }

                }

                _bmp.Clear();
                _bmp.Render(_drawing);
            });

            op.Wait();
        }
    }
}
