
using System;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NeuroNet
{
    internal class NeuBall
    {
        private float _posX;
        private float _posY;
        private NeuralNet _net;
        private float _velY;
        private Ellipse _ellipse;
        private float _accelY;

        public float PosX { get => _posX; private set => _posX = value; }
        public float PosY { get => _posY; private set => _posY = value; }
        public float VelY { get => _velY; private set => _velY = value; }
        public Ellipse Ellipse { get => _ellipse; private set => _ellipse = value; }


        public NeuBall(float X, float Y, NeuralNet net)
        {
            _posX = X;
            _posY = Y;
            _net = net;

            _velY = 0;

            _ellipse = new Ellipse
            {
                Stroke = Brushes.Blue,
                Fill = Brushes.Blue,
                StrokeThickness = 5,
                Width = 20,
                Height = 20,
            };

            _ellipse.RenderTransform = new TranslateTransform(_posX, _posY);
        }

        public void doTimeStep()
        {
            var output = _net.FeedForward(new float[]{ _posY, _velY});
            setAccelY(output[0]);

            _velY += 0.01f + _accelY;
            _posY += _velY;

            _ellipse.RenderTransform = new TranslateTransform(_posX, _posY);
        }

        internal void setAccelY(float v)
        {
            _accelY = v;
        }

        public override string ToString()
        {
            return _net.ToString();
        }
    }
}