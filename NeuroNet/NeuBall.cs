
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
        private const float _radiusSquare = (float)(_radius * _radius);
        private float _posX;
        private float _posY;
        private NeuralNet _net;
        private float _scale;
        private float _scaleInv;
        private float _xPosZero;
        private float _yPosZero;
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
        private int _id;
        private NeuralSettings _settings;
        private Brush _mainColor;

        public float PosX { get => _posX; set => _posX = value; }
        public float PosY { get => _posY; set => _posY = value; }
        public float VelY { get => _velY; private set => _velY = value; }
        public Ellipse Ellipse { get => _ellipse; private set => _ellipse = value; }
        public bool Active { get => _active; internal set => _active = value; }
        public float Fitness { get => _fitness; internal set => _fitness = value; }
        public NeuralNet Net { get => _net; private set => _net = value; }
        public bool TargetReached { get => _targetIterationCount > 25; private set => _targetIterationCount = 0; }
        public Brush MainColor { get => _mainColor; set => _mainColor = value; }

        public NeuBall(NeuralSettings settings, int id, float X, float Y, float xM, float yM, float scale)
        {
            _settings = settings;
            if (_rnd == null)
                _rnd = new Random((int)DateTime.Now.Ticks + id);

            _id = id;
            if (_id == 0)
                _id = _rnd.Next(10000);

            //int[] layerConfig = new int[] { 6, 4, 2 };
            int[] layerConfig = new int[] { 6, 8, 4, 2 };
            //int[] layerConfig = new int[] { 6, 16, 8, 4, 2 };

            //var rnd = _rnd.Next(0, 100);
            //if (rnd < 70)
            //{
            //    layerConfig = new int[] { 6, 4, 2 };
            //}
            //else if (rnd < 20)
            //{
            //    layerConfig = new int[] { 6, 8, 4, 2 };
            //}
            //else if(rnd < 2)
            //{
            //    layerConfig = new int[] { 6, 16, 8, 4, 2 };
            //}



            _net = new NeuralNet(_rnd.Next(), layerConfig);

            resetPos(X, Y);
            _scale = scale;
            _scaleInv = 1 / scale;

            _xPosZero = xM;
            _yPosZero = yM;

            _ellipse = new Ellipse
            {
                Stroke = Brushes.Blue,
                Fill = Brushes.Blue,
                StrokeThickness = 2,
                Width = 2 * _radius,
                Height = 2 * _radius,
            };

            _ellipse.RenderTransform = new TranslateTransform(_posX - _radius, _posY - _radius);

            _active = true;

            _fitness = (float)_rnd.NextDouble();
        }

        public NeuBall(NeuralSettings settings, float x, float y, float xM, float yM, float scale, NeuBall previousGen, int chance, float variation) : 
            this(settings, 0, x, y, xM, yM, scale)
        {
            _net = previousGen.clone();
            _net.Mutate(chance, variation);

            resetPos(x, y);
        }

        private NeuralNet clone()
        {
            return _net.clone();
        }

        public void setColors(Brush stroke, Brush fill)
        {
            _mainColor = fill;
            _ellipse.Stroke = stroke;
            _ellipse.Fill = fill;
        }

        public void doTimeStep(int iteration, float targetX, float targetY, float maxX, float maxY)
        {
            if (_active)
            {
                //_fitness += iteration;

                var distTargetCurrent = getDistanceToTarget(targetX, targetY);
                doMove(targetX, targetY);

                _ellipse.RenderTransform = new TranslateTransform(_posX - _radius, _posY - _radius);

                var distTarget = checkTargetHit(targetX, targetY);
                float distChange = (distTargetCurrent - distTarget) / _radiusSquare;
                _fitness += distChange;

                //float distZone = (float)(_radiusSquare / distTarget);
                //_fitness += distZone;

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
                _fitness *= 0.01f;
                _active = false;
            }
        }

        private void doMove(float targetX, float targetY)
        {
            var goalX = _scaleInv * (_posX - targetX);
            var goalY = _scaleInv * (_posY - targetY);

            var output = _net.FeedForward(new float[] { 
                goalX,
                _velX,
                1e3f * _scaleInv *_accelX, 
                goalY,
                _velY,
                1e3f * _scaleInv *_accelY
            });

            Vector accel = new Vector(output[0], output[1]);

            if (accel.Length > 0.8)
            {
                accel.Normalize();
                accel *= 0.4;
            }

            accel *= 1e3 * _scaleInv;

            _accelX =  (float)accel.X;

            if(_settings.Float)
                _accelY = Math.Min((float)accel.Y, 0);
            else
                _accelY = (float)accel.Y;

            _velX += _accelX;

            _velY += 0.25f;
            _velY += _accelY;

            _posX += _velX;
            _posY += _velY;

            _distTraveled += _velX * _velX + _velY * _velY;


        }

        private float checkTargetHit(float targetX, float targetY)
        {
            float distTarget = getDistanceToTarget(targetX, targetY);

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
                _ellipse.Fill = _mainColor;
            }

            return distTarget;
        }

        private float getDistanceToTarget(float targetX, float targetY)
        {
            var dx = _posX - targetX;
            var dy = _posY - targetY;

            float distTarget = dx * dx + dy * dy;
            return distTarget;
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
            _maxHits = _settings.MaxHits;
            _active = true;
        }
    }
}