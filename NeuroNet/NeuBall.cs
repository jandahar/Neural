
using System;
using System.Windows;
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
        private float _accelX;
        private float _accelY;
        private bool _active = false;
        private float _velX;

        private float _distTraveled;

        public float PosX { get => _posX; set => _posX = value; }
        public float PosY { get => _posY; set => _posY = value; }
        public float VelY { get => _velY; private set => _velY = value; }
        public Ellipse Ellipse { get => _ellipse; private set => _ellipse = value; }
        public bool Active { get => _active; internal set => _active = value; }
        public float Fitness { get => _net._fitness; internal set => _net._fitness = value; }
        public NeuralNet Net { get => _net; private set => _net = value; }
        public float DistTraveled { get => _net._fitness * _distTraveled; private set => _distTraveled = value; }

        public NeuBall(float X, float Y, NeuralNet net)
        {
            resetPos(X, Y);
            _net = net;

            _ellipse = new Ellipse
            {
                Stroke = Brushes.Blue,
                Fill = Brushes.Blue,
                StrokeThickness = 5,
                Width = 20,
                Height = 20,
            };

            _ellipse.RenderTransform = new TranslateTransform(_posX, _posY);

            _active = true;
        }

        public NeuBall(float x, float y, NeuBall previousGen, int chance, float variation) : this(x, y, net: null)
        {
            _net = previousGen.clone();
            _net.Mutate(chance, variation);
        }

        private NeuralNet clone()
        {
            return _net.clone();
        }

        public void doTimeStep(float distX, float distY, float maxX, float maxY)
        {
            if (_active)
            {
                var output = _net.FeedForward(new float[] { distX, _velX, _accelX, distY, _velY, _accelY });
                _accelX = output[0];
                _accelY = output[1];
                _velX += _accelX;

                _velY += 0.01f;
                _velY += _accelY;

                _posX += _velX;
                _posY += _velY;

                _distTraveled += _velX * _velX + _velY * _velY;

                _ellipse.RenderTransform = new TranslateTransform(_posX, _posY);

                if (_posX < 0 || _posX > maxX)
                    _velX *= -1;

                if (_posY < 0 || _posY > maxX)
                    _velY *= -1;
            }
        }

        public override string ToString()
        {
            return _net.ToString() + (_active ? " Active" : " Inactive");
        }

        internal void hide()
        {
            _ellipse.Visibility = Visibility.Hidden;
        }

        internal void resetPos(float startX, float startY)
        {
            _posX = startX;
            _posY = startY;
            _velY = 0;
            _velX = 0;
            _accelY = 0; 
            _distTraveled = 0.0f;
        }
    }
}