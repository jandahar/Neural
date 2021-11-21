using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NeuroNet
{
    internal class NeuralNetDisplay
    {
        private int[] _layers;
        private double _width;
        private double _height;
        private double _offset;
        private Point[][] _positions;
        private Ellipse[][] _neurons;
        private SolidColorBrush _color;
        private double _offX = 0.0;
        private double _offY = 0.0;
        private int _nodeDiameter = 20;

        public NeuralNetDisplay(int[] layers, double width, double height, double offset)
        {
            _color = Brushes.Yellow;

            _layers = layers;
            _width = width;
            _height = height;
            _offset = offset;

            init();
        }

        internal void init()
        {
            var offYMiddle = _offY + 0.5 * _height - _nodeDiameter;

            var spacingX = (_width - 2 * _nodeDiameter) / (_layers.Length - 1);

            _positions = new Point[_layers.Length][];
            _neurons = new Ellipse[_layers.Length][];

            for (int i = 0; i < _layers.Length; i++)
            {
                _positions[i] = new Point[_layers[i]];
                _neurons[i] = new Ellipse[_layers[i]];

                var posX = _offX + i * spacingX;

                var spacingY = (_height - 2 * _nodeDiameter) / (_layers[i] - 1);
                var realOffY = _offY;
                if (_layers[i] == 1)
                {
                    spacingY = 0;
                    realOffY = offYMiddle;
                }

                for (int j = 0; j < _layers[i]; j++)
                {
                    var posY = realOffY + j * spacingY;
                    Ellipse e = new Ellipse
                    {
                        Stroke = _color,
                        Fill = _color,
                        StrokeThickness = 5,
                    };
                    e.Width = _nodeDiameter;
                    e.Height = _nodeDiameter;

                    _positions[i][j] = new Point(posX, posY);
                    _neurons[i][j] = e;
                    e.RenderTransform = new TranslateTransform(posX + _nodeDiameter / 2, _offset + posY + _nodeDiameter / 2);
                }
            }
        }

        public void getDrawing(UIElementCollection uiElements)
        {
            var bounding = new Rectangle
            {
                Stroke = _color,
                StrokeThickness = 3,
                Width = _width,
                Height = _height
            };
            bounding.RenderTransform = new TranslateTransform(_offX, _offY + _offset);
            uiElements.Add(bounding);

            for (int i = 0; i < _layers.Length; i++)
                for (int j = 0; j < _layers[i]; j++)
                    uiElements.Add(_neurons[i][j]);
        }

        internal void drawNeurons(NeuralNet net)
        {
            var neurons = net.Neurons;
            for (int i = 0; i < neurons.Length; i++)
            {
                for (int j = 0; j < net.Layers[i]; j++)
                {
                    if (neurons[i][j] > 0)
                        _neurons[i][j].Stroke = Brushes.Green;
                    else
                        _neurons[i][j].Stroke = Brushes.Red;
                }
            }
        }
    }
}