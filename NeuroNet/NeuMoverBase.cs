using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NeuroNet
{
    internal abstract class NeuMoverBase
    {
        protected const double _radius = 10;
        protected const float _radiusSquare = (float)(_radius * _radius);
        protected Random _rnd = null;
        protected float _posX;
        protected float _posY;
        protected NeuralNet _net;
        protected float _scale;
        protected float _scaleInv;
        protected float _velY;
        protected Ellipse _ellipse;
        protected float _accelX;
        protected float _accelY;
        protected bool _active = false;
        protected float _velX;

        protected float _fitness;
        protected int _maxHits;
        protected int _targetIterationCount = 1;
        protected int _iterationsToTarget;
        protected int _id;
        protected NeuralSettings _settings;
        protected Brush _mainColor;
        protected Brush _secondaryColor;
        protected int _targetCount = 0;
        protected float _targetX;
        protected float _targetY;

        public float PosX { get => _posX; set => _posX = value; }
        public float PosY { get => _posY; set => _posY = value; }
        public float VelY { get => _velY; private set => _velY = value; }
        public Ellipse Ellipse { get => _ellipse; private set => _ellipse = value; }
        public bool Active { get => _active; internal set => _active = value; }

        public virtual float getFitness()
        {
            return _fitness;
        }

        public NeuralNet Net { get => _net; private set => _net = value; }
        public bool TargetReached { get => _targetIterationCount > _settings.GoalTargetIterations; private set => _targetIterationCount = 0; }
        public Brush MainColor { get => _mainColor; set => _mainColor = value; }
        public int TargetCount { get => _targetCount; set => _targetCount = value; }
        public Brush SecondaryColor { get => _secondaryColor; set => _secondaryColor = value; }

        public NeuMoverBase(NeuralSettings settings, int id, float X, float Y, float xM, float yM, float scale, int[] layerConfig)
        {
            _settings = settings;
            if (_rnd == null)
                _rnd = new Random((int)DateTime.Now.Ticks + id);

            _id = id;
            if (_id == 0)
                _id = _rnd.Next(10000);

            _net = new NeuralNet(_rnd.Next(), layerConfig);

            resetPos(X, Y);
            _scale = scale;
            _scaleInv = 1 / scale;

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

            _iterationsToTarget = _settings.TurnsToTarget;
        }

        public NeuMoverBase(NeuralSettings settings, float x, float y, float xM, float yM, float scale, NeuBall previousGen, int chance, float variation, int[] layerConfig) :
            this(settings, 0, x, y, xM, yM, scale, layerConfig)
        {
            _net = previousGen.clone();
            _net.Mutate(chance, variation);

            resetPos(x, y);
        }


        protected NeuralNet clone()
        {
            return _net.clone();
        }

        protected abstract float calcFitnessMalusForLeavingTarget();
        protected abstract float calcFitnessOnTarget();
        protected abstract Vector getAcceleration(Vector vecVel, Vector vecGoal);

        public void doTimeStep(int iteration, float targetX, float targetY, float maxX, float maxY)
        {
            _targetX = targetX;
            _targetY = targetY;

            if (_active)
            {
                var gain = doMove(targetX, targetY);
                var distTarget = checkTargetHit(targetX, targetY);

                calculateFitness(gain, distTarget);

                bounce(maxX, maxY);
            }
        }

        protected abstract void calculateFitness(float gain, float distTarget);

        public virtual void setColors(Brush stroke, Brush fill)
        {
            _mainColor = stroke;
            _ellipse.Stroke = stroke;
            _ellipse.Fill = fill;
        }

        public override string ToString()
        {
            return string.Format("Fitness: {0}", _fitness);
        }

        internal void hide()
        {
            _ellipse.Visibility = Visibility.Hidden;
        }

        internal void highlight()
        {
            _ellipse.StrokeThickness = 5;
            _ellipse.Stroke = Brushes.Red;
        }

        internal void resetPos(float startX, float startY)
        {
            _posX = startX;
            _posY = startY;
            _velY = -.01f;
            _velX = 0;
            _accelY = 0;
            _targetIterationCount = 1;
            _fitness = (float)_rnd.NextDouble();
            _maxHits = _settings.MaxHits;
            _active = true;
            _targetCount = 0;
            _iterationsToTarget = _settings.TurnsToTarget;
            _rnd = new Random((int)DateTime.Now.Ticks + _rnd.Next(1000));

            if (_ellipse != null)
                _ellipse.Visibility = Visibility.Visible;
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

            if (_maxHits <= 0)
            {
                if (_fitness > 0)
                    _fitness *= 0.01f;
                else
                    _fitness *= 1e2f;
                _active = false;
            }
        }

        private float checkTargetHit(float targetX, float targetY)
        {
            if (_iterationsToTarget < 1)
            {
                _active = false;
                _ellipse.Visibility = Visibility.Hidden;
                return 0.0f;
            }

            float distTarget = getDistanceToTarget(targetX, targetY);

            bool onTarget = distTarget < _radius * _radius;
            if (onTarget)
            {
                bool targetReached = TargetReached;
                if (targetReached)
                    _targetCount++;

                _fitness += calcFitnessOnTarget();

                if (targetReached)
                    _iterationsToTarget = _settings.TurnsToTarget;

                _targetIterationCount++;
                _ellipse.Fill = Brushes.Green;
            }
            else
            {
                if (_targetIterationCount > 1)
                {
                    _fitness -= calcFitnessMalusForLeavingTarget();
                    _targetIterationCount = 1;
                    _ellipse.Fill = _secondaryColor;
                }

                _iterationsToTarget--;
            }

            return distTarget;
        }

        private float doMove(float targetX, float targetY)
        {
            Vector toTargetBefore = new Vector(_posX - targetX, _posY - targetY);
            var vecVel = new Vector(_velX, _velY);
            _posX += _velX;
            _posY += _velY;

            Vector toTargetNorm = new Vector(toTargetBefore.X, toTargetBefore.Y);
            toTargetNorm.Normalize();

            Vector toTargetNow = new Vector(_posX - targetX, _posY - targetY);

            Vector traveled = toTargetBefore - toTargetNow;
            traveled.Normalize();
            var gain = toTargetNorm * traveled;

            var goalX = _scaleInv * (float)toTargetNow.X;
            var goalY = _scaleInv * (float)toTargetNow.Y;
            var vecGoal = new Vector(goalX, goalY);

            Vector accel = getAcceleration(vecVel, vecGoal);

            if (accel.Length > 0.8)
            {
                accel.Normalize();
                accel *= 0.8;
            }

            _accelX = (float)accel.X;

            if (_settings.Float)
                _accelY = Math.Min((float)accel.Y, 0);
            else
                _accelY = (float)accel.Y;

            _velX += _accelX / 2;

            _velY += 0.25f;
            _velY += _accelY / 2;

            return (float)gain;
        }

        private float getDistanceToTarget(float targetX, float targetY)
        {
            var dx = _posX - targetX;
            var dy = _posY - targetY;

            float distTarget = dx * dx + dy * dy;
            return distTarget;
        }
    }
}