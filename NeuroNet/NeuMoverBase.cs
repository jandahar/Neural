using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace NeuroNet
{
    internal abstract class NeuMoverBase
    {
        protected const double _radius = 10;
        protected const double _radiusSquare = _radius * _radius;
        protected Random _rnd = null;

        protected Point3D _position;
        protected Vector3D _velocity;
        protected Vector3D _acceleration;

        protected Point3D _target;

        private Point3D _startPos;

        protected NeuralNet _net;
        protected float _scale;
        protected float _scaleInv;
        protected bool _active = false;
        protected bool _speedDeath = false;

        protected int _maxHits;
        protected int _targetIterationCount = 0;
        protected int _iterationsToTarget;
        protected NeuralSettings _settings;
        protected Brush _mainColor;
        protected Brush _secondaryColor;
        private int _targetCount = 0;
        private float _speedBonusFitness = 0;
        private bool _isChampion;

        public bool Active { get => _active; internal set => _active = value; }

        public abstract void highlight();
        protected abstract void updatePosition();

        public NeuralNet Net { get => _net; private set => _net = value; }
        public bool TargetReached { get => _targetIterationCount >= _settings.GoalTargetIterations; private set => _targetIterationCount = 0; }
        public int TargetCount { get => _targetCount; private set => _targetCount = value; }
        public Brush SecondaryColor { get => _secondaryColor; set => _secondaryColor = value; }

        public static double Radius => _radius;

        public float PosX { get => (float)_position.X; private set => _position = new Point3D(value, _position.Y, _position.Z); }
        public float PosY { get => (float)_position.Y; private set => _position = new Point3D(_position.X, value, _position.Z); }
        public float PosZ { get => (float)_position.Z; private set => _position = new Point3D(_position.X, _position.Y, value); }
        public float SpeedBonusFitness { get => _speedBonusFitness; private set => _speedBonusFitness = value; }

        public bool Champion { get => _isChampion; internal set => _isChampion = value; }

        public NeuMoverBase(NeuralSettings settings, int seed, double X, double Y, float xM, float yM, float scale, int[] layerConfig)
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

        public NeuMoverBase(NeuralSettings settings, double x, double y, float xM, float yM, float scale, NeuBall previousGen, int chance, float variation, int[] layerConfig) :
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
        protected abstract Vector getAcceleration(Vector3D vecVel, Vector3D vecGoal);

        public void doTimeStep(int iteration, Point3D target, float maxX, float maxY)
        {
            _target = target;

            if (_active)
            {
                doMove(target);
                checkTargetHit(target);

                bounce(maxX, maxY);
            }
        }

        public float getFitness(float speedFactor)
        {
            var dxStart = _startPos.X - _target.X;
            var dyStart = _startPos.Z - _target.Z;

            var dx = _position.X - _target.X;
            var dy = _position.Z - _target.Z;

            float distTargetNow = (float)Math.Sqrt(dx * dx + dy * dy);

            float targetReachedPerc = 0;
            float targetActivatePerc = 0;
            if (distTargetNow < Radius)
                targetActivatePerc = 1 + (_targetIterationCount) / (float)_settings.GoalTargetIterations;
            else
            {
                float distTargetStart = (float)Math.Sqrt(dxStart * dxStart + dyStart * dyStart);

                if (Math.Abs(distTargetStart) > 1e-5)
                    targetReachedPerc = (distTargetStart - distTargetNow) / distTargetStart;
                else
                    targetReachedPerc = -distTargetNow;
            }

            float targetPoints = 3 * TargetCount;
            float fitness = targetPoints + targetReachedPerc + targetActivatePerc;

            if (_speedDeath)
                fitness -= 3;
            else
                fitness += speedFactor * SpeedBonusFitness;

            return fitness;
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


        internal virtual void resetPos(double startX, double startY)
        {
            _position = new Point3D(startX, 0, startY);
            _velocity = new Vector3D(0, 0, -0.01);
            _acceleration = new Vector3D();
            _targetIterationCount = 0;
            _maxHits = _settings.MaxHits;
            _active = true;
            _targetCount = 0;
            _iterationsToTarget = _settings.TurnsToTarget;
            //_rnd = new Random(_rnd.Next());
            _speedDeath = false;
            _speedBonusFitness = 0;
            _startPos = new Point3D(startX, 0, startY);
        }

        internal void setCurrentStartPos()
        {
            _startPos.X = _position.X;
            _startPos.Z = _position.Y;
        }

        private void bounce(float maxX, float maxY)
        {
            double damp = -0.9;

            if (_position.X < 0 || _position.X > maxX)
            {
                _maxHits--;
                _velocity = new Vector3D(-damp * _velocity.X, damp * _velocity.Y, damp * _velocity.Z);
            }

            if (_position.Y < 0 || _position.Y > maxY)
            {
                _velocity = new Vector3D(damp * _velocity.X, -damp * _velocity.Y, damp * _velocity.Z);
                _maxHits--;
            }

            if (_position.Z < 0 || _position.Z > maxY)
            {
                _velocity = new Vector3D(damp * _velocity.X, damp * _velocity.Y, -damp * _velocity.Z);
                _maxHits--;
            }

            if (_maxHits <= 0)
            {
                _active = false;
                _speedDeath = true;
            }
        }

        protected virtual double checkTargetHit(Point3D target)
        {
            if (_iterationsToTarget < 1)
            {
                _active = false;
                return 0.0f;
            }

            var distTarget = getDistanceToTarget(target);

            bool onTarget = NeuMoverBase.onTarget(distTarget);
            if (onTarget)
            {
                bool targetReached = TargetReached;
                if (targetReached)
                {
                    _targetCount++;
                    _speedBonusFitness += (float)_iterationsToTarget / _settings.TurnsToTarget;
                    _iterationsToTarget = _settings.TurnsToTarget;
                }
                else
                {
                    _targetIterationCount++;
                }
            }
            else
            {
                if (_targetIterationCount > 0)
                {
                    _targetIterationCount = 0;
                }

                _iterationsToTarget--;
            }

            return distTarget;
        }

        protected static bool onTarget(double distTarget)
        {
            return distTarget < _radius * _radius;
        }

        private float doMove(Point3D target)
        {
            var toTargetBefore = _position - target;
            var vecVel = _velocity;

            _velocity += 0.25 * _acceleration;
            _position += _velocity;

            updatePosition();

            var toTargetNorm = new Vector3D(toTargetBefore.X, toTargetBefore.Y, toTargetBefore.Z);
            toTargetNorm.Normalize();

            var toTargetNow = _position - target;

            var traveled = toTargetBefore - toTargetNow;
            traveled.Normalize();
            var gain = Vector3D.DotProduct(toTargetNorm, traveled);

            var vecGoal = _scaleInv * toTargetNow;

            var accel = getAcceleration(vecVel, vecGoal);

            if (accel.Length > 0.8)
            {
                accel.Normalize();
                accel *= 0.8;
            }

            if (_settings.Float)
                _acceleration = new Vector3D(accel.X, 0, Math.Min(accel.Y, 0));
            else
                _acceleration = new Vector3D(accel.X, 0, accel.Y);

            _acceleration.Z += 0.1;

            return (float)gain;
        }


        private double getDistanceToTarget(Point3D target)
        {
            var distTarget = (_position - target).LengthSquared;
            return distTarget;
        }
    }
}