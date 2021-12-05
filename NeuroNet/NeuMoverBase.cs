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
        protected float _accelX;
        protected float _accelY;
        protected bool _active = false;
        protected bool _speedDeath = false;
        protected float _velX;

        protected int _maxHits;
        protected int _targetIterationCount = 1;
        protected int _iterationsToTarget;
        protected NeuralSettings _settings;
        protected Brush _mainColor;
        protected Brush _secondaryColor;
        protected int _targetCount = 0;
        protected float _targetX;
        protected float _targetY;

        public bool Active { get => _active; internal set => _active = value; }

        public abstract float getFitness(float factor);
        public abstract void highlight();
        protected abstract void updatePosition();

        public NeuralNet Net { get => _net; private set => _net = value; }
        public bool TargetReached { get => _targetIterationCount >= _settings.GoalTargetIterations; private set => _targetIterationCount = 0; }
        public int TargetCount { get => _targetCount; set => _targetCount = value; }
        public Brush SecondaryColor { get => _secondaryColor; set => _secondaryColor = value; }

        public static double Radius => _radius;

        public float PosX { get => _posX; private set => _posX = value; }
        public float PosY { get => _posY; private set => _posY = value; }

        public NeuMoverBase(NeuralSettings settings, int seed, float X, float Y, float xM, float yM, float scale, int[] layerConfig)
        {
            _settings = settings;
            if (_rnd == null)
            {
                DateTime now = DateTime.Now;
                _rnd = new Random(seed);
            }

            _net = new NeuralNet(_rnd.Next(), layerConfig);

            resetPos(X, Y);
            _scale = scale;
            _scaleInv = 1 / scale;

            _active = true;

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
        protected abstract Vector getAcceleration(Vector vecVel, Vector vecGoal);

        public void doTimeStep(int iteration, float targetX, float targetY, float maxX, float maxY)
        {
            _targetX = targetX;
            _targetY = targetY;

            if (_active)
            {
                doMove(targetX, targetY);
                checkTargetHit(targetX, targetY);

                bounce(maxX, maxY);
            }
        }

        public virtual void setColors(Brush mainColor, Brush secondaryColor)
        {
            _mainColor = mainColor;
            _secondaryColor = secondaryColor;
        }

        public override string ToString()
        {
            return base.ToString();
        }


        internal virtual void resetPos(float startX, float startY)
        {
            _posX = startX;
            _posY = startY;
            _velY = -.01f;
            _velX = 0;
            _accelY = 0;
            _targetIterationCount = 1;
            _maxHits = _settings.MaxHits;
            _active = true;
            _targetCount = 0;
            _iterationsToTarget = _settings.TurnsToTarget;
            //_rnd = new Random(_rnd.Next());
            _speedDeath = false;
        }


        private void bounce(float maxX, float maxY)
        {
            if (_posX < 0 || _posX > maxX)
            {
                _maxHits--;
                _velX *= -1;
            }

            if (_posY < 0 || _posY > maxY)
            {
                _velY *= -1;
                _maxHits--;
            }

            if (_maxHits <= 0)
            {
                _active = false;
                _speedDeath = true;
            }
        }

        protected virtual float checkTargetHit(float targetX, float targetY)
        {
            if (_iterationsToTarget < 1)
            {
                _active = false;
                return 0.0f;
            }

            float distTarget = getDistanceToTarget(targetX, targetY);

            bool onTarget = NeuMoverBase.onTarget(distTarget);
            if (onTarget)
            {
                bool targetReached = TargetReached;
                if (targetReached)
                {
                    _targetCount++;
                    _iterationsToTarget = _settings.TurnsToTarget;
                }
                else
                {
                    _targetIterationCount++;
                }
            }
            else
            {
                if (_targetIterationCount > 1)
                {
                    _targetIterationCount = 1;
                }

                _iterationsToTarget--;
            }

            return distTarget;
        }

        protected static bool onTarget(float distTarget)
        {
            return distTarget < _radius * _radius;
        }

        private float doMove(float targetX, float targetY)
        {
            Vector toTargetBefore = new Vector(_posX - targetX, _posY - targetY);
            var vecVel = new Vector(_velX, _velY);
            _posX += _velX;
            _posY += _velY;
            updatePosition();

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