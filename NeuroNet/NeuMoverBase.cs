﻿using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace NeuroNet
{
    internal abstract class NeuMoverBase
    {
        private const double _radius = 10;
        protected const float _radiusSquare = (float)(_radius * _radius);
        protected Random _rnd = null;
        protected Point3D _position;
        protected NeuralNet _net;
        protected float _scale;
        protected float _scaleInv;
        protected float _velY;
        protected Ellipse _ellipse;
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

        public Ellipse Ellipse { get => _ellipse; private set => _ellipse = value; }
        public bool Active { get => _active; internal set => _active = value; }

        public abstract float getFitness(float factor);

        public NeuralNet Net { get => _net; private set => _net = value; }
        public bool TargetReached { get => _targetIterationCount >= _settings.GoalTargetIterations; private set => _targetIterationCount = 0; }
        public int TargetCount { get => _targetCount; set => _targetCount = value; }
        public Brush SecondaryColor { get => _secondaryColor; set => _secondaryColor = value; }

        public static double Radius => _radius;

        public float PosX { get => (float)_position.X; private set => _position = new Point3D(value, _position.Y, _position.Z); }
        public float PosY { get => (float)_position.Y; private set => _position = new Point3D(_position.X, value, _position.Z); }
        public float PosZ { get => (float)_position.Z; private set => _position = new Point3D(_position.X, _position.Y, value); }

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

            _ellipse = new Ellipse
            {
                Stroke = Brushes.Blue,
                Fill = Brushes.Blue,
                StrokeThickness = 2,
                Width = 2 * _radius,
                Height = 2 * _radius,
            };

            _ellipse.RenderTransform = new TranslateTransform((float)_position.X - _radius, (float)_position.Y- _radius);

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

        public virtual void setColors(Brush stroke, Brush fill)
        {
            _mainColor = stroke;
            _ellipse.Stroke = stroke;
            _ellipse.Fill = fill;
        }

        public override string ToString()
        {
            return base.ToString();
        }

        internal void highlight()
        {
            _ellipse.StrokeThickness = 5;
            _ellipse.Stroke = Brushes.Red;
        }

        internal virtual void resetPos(float startX, float startY)
        {
            _position = new Point3D(startX, startY, 0);
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

            if (_ellipse != null)
                _ellipse.Visibility = Visibility.Visible;
        }


        private void bounce(float maxX, float maxY)
        {
            if (_position.X < 0 || _position.X > maxX)
            {
                _maxHits--;
                _velX *= -1;
            }

            if (_position.Y < 0 || _position.Y > maxY)
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
                _ellipse.Visibility = Visibility.Hidden;
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

                _ellipse.Fill = Brushes.Green;
            }
            else
            {
                if (_targetIterationCount > 1)
                {
                    _targetIterationCount = 1;
                    _ellipse.Fill = _secondaryColor;
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
            Vector toTargetBefore = new Vector(_position.X - targetX, _position.Y - targetY);
            var vecVel = new Vector(_velX, _velY);
            _position = new Point3D(_position.X + _velX, _position.Y + _velY, 0);
            _ellipse.RenderTransform = new TranslateTransform(_position.X - _radius, _position.Y - _radius);

            Vector toTargetNorm = new Vector(toTargetBefore.X, toTargetBefore.Y);
            toTargetNorm.Normalize();

            Vector toTargetNow = new Vector(_position.X - targetX, _position.Y - targetY);

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
            var dx = (float)(_position.X - targetX);
            var dy = (float)(_position.Y - targetY);

            float distTarget = dx * dx + dy * dy;
            return distTarget;
        }
    }
}