
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NeuroNet
{
    internal class NeuBall
    {
        static Random _rnd = null;
        private const double _radius = 10;

        private float _posX;
        private float _posY;
        private NeuralNet _net;
        private float _velY;
        private Ellipse _ellipse;
        private float _accelX;
        private float _accelY;
        private bool _active = false;
        private float _velX;

        private float _fitness;
        private int _maxHits;
        private float _distTraveled;
        private int _targetIterationCount = 0;

        public float PosX { get => _posX; set => _posX = value; }
        public float PosY { get => _posY; set => _posY = value; }
        public float VelY { get => _velY; private set => _velY = value; }
        public Ellipse Ellipse { get => _ellipse; private set => _ellipse = value; }
        public bool Active { get => _active; internal set => _active = value; }
        public float Fitness { get => _fitness; internal set => _fitness = value; }
        public NeuralNet Net { get => _net; private set => _net = value; }
        public bool TargetReached { get => _targetIterationCount > 25; private set => _targetIterationCount = 0; }

        public NeuBall(float X, float Y, NeuralNet net)
        {
            if (_rnd == null)
                _rnd = new Random();

            resetPos(X, Y);
            _net = net;

            _ellipse = new Ellipse
            {
                Stroke = Brushes.Blue,
                Fill = Brushes.Blue,
                StrokeThickness = 5,
                Width = 2 * _radius,
                Height = 2 * _radius,
            };

            _ellipse.RenderTransform = new TranslateTransform(_posX - _radius, _posY - _radius);

            _active = true;

            _fitness = (float)_rnd.NextDouble();
        }

        public NeuBall(float x, float y, NeuBall previousGen, int chance, float variation) : this(x, y, net: null)
        {
            _net = previousGen.clone();
            _net.Mutate(chance, variation);

            resetPos(x, y);
        }

        private NeuralNet clone()
        {
            return _net.clone();
        }

        public void doTimeStep(float distX, float distY, float maxX, float maxY)
        {
            if (_active)
            {
                doMove(distX, distY);

                _ellipse.RenderTransform = new TranslateTransform(_posX - _radius, _posY - _radius);

                float distTarget = distX * distX + distY * distY;
                checkTargetHit(distTarget);

                _fitness++;

                float distZone = (float)(_radius * _radius / distTarget);
                _fitness += distZone;

                bounce(maxX, maxY);
            }
        }

        private void bounce(float maxX, float maxY)
        {
            if (_posX < 0 || _posX > maxX)
            {
                _maxHits--;
                _velX *= -1;
                _fitness -= _velX * _velX;
            }

            if (_posY < 0 || _posY > maxY)
            {
                _velY *= -1;
                _maxHits--;
                _fitness -= _velY * _velY;
            }

            if (_maxHits == 0)
            {
                _active = false;
            }
        }

        private void doMove(float distX, float distY)
        {
            var output = _net.FeedForward(new float[] { distX, _velX, _accelX, distY, _velY, _accelY });
            Vector accel = new Vector(output[0], output[1]);

            if (accel.Length > 0.3)
            {
                accel.Normalize();
                accel *= 0.4;
            }

            _accelX = (float)accel.X;
            _accelY = (float)accel.Y;

            
            _velX += _accelX;

            _velY += 0.01f;
            _velY += _accelY;

            _posX += _velX;
            _posY += _velY;

            _distTraveled += _velX * _velX + _velY * _velY;
        }

        private void checkTargetHit(float distTarget)
        {
            bool onTarget = distTarget < _radius * _radius;
            if (onTarget)
            {
                _fitness += _targetIterationCount;
                _targetIterationCount++;
                _ellipse.Fill = Brushes.Green;
            }
            else
            {
                _targetIterationCount = 0;
                _ellipse.Fill = Brushes.Blue;
            }
        }

        public override string ToString()
        {
            return string.Format("Fitness: {0}", _fitness);
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
            _targetIterationCount = 0;
            _fitness = (float)_rnd.NextDouble();
            _maxHits = 1;
            _active = true;
        }
    }
}