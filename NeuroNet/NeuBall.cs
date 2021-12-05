
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace NeuroNet
{
    internal class NeuBall : NeuMoverBase
    {
        private float _startPosX;
        private float _startPosY;
        private float _speedBonusFitness = 0;
        private bool _isChampion;
        private Ellipse _ellipse;
        public Ellipse Ellipse { get => _ellipse; private set => _ellipse = value; }

        public bool Champion { get => _isChampion; internal set => _isChampion = value; }

        public NeuBall(NeuralSettings settings, int id, float X, float Y, float xM, float yM, float scale, int[] layerConfig) : base(settings, id, X, Y, xM, yM, scale, layerConfig)
        {
            init(X, Y);
        }

        public NeuBall(NeuralSettings settings, float x, float y, float xM, float yM, float scale, NeuBall previousGen, int chance, float variation, int[] layerConfig) : base(settings, x, y, xM, yM, scale, previousGen, chance, variation, layerConfig)
        {
            init(x, y);
        }

        private void init(float X, float Y)
        {
            _startPosX = X;
            _startPosY = Y;

            _ellipse = new Ellipse
            {
                Stroke = Brushes.Blue,
                Fill = Brushes.Blue,
                StrokeThickness = 2,
                Width = 2 * _radius,
                Height = 2 * _radius,
            };

            _ellipse.RenderTransform = new TranslateTransform((float)_position.X - _radius, (float)_position.Y - _radius);
        }

        public override void setColors(Brush mainColor, Brush secondaryColor)
        {
            base.setColors(mainColor, secondaryColor);

            _ellipse.Stroke = mainColor;
            _ellipse.Fill = secondaryColor;
            _ellipse.StrokeThickness = 5;
        }

        public override void highlight()
        {
            _ellipse.StrokeThickness = 5;
            _ellipse.Stroke = Brushes.Red;
        }

        protected override double checkTargetHit(Point3D target)
        {
            var distanceToTargetSquare = base.checkTargetHit(target);

            if (!_active)
            {
                _ellipse.Visibility = Visibility.Hidden;
            }
            else if (TargetReached)
            {
                _speedBonusFitness += _iterationsToTarget  / _settings.TurnsToTarget;
            }
            else
            {
                bool onTarget = NeuMoverBase.onTarget(distanceToTargetSquare);
                if (onTarget)
                    _ellipse.Fill = Brushes.Green;
                else
                    _ellipse.Fill = _secondaryColor;
            }

            return distanceToTargetSquare;
        }
        protected override void updatePosition()
        {
            _ellipse.RenderTransform = new TranslateTransform(_position.X - _radius, _position.Y - _radius);
        }

        public override float getFitness(float speedFactor)
        {
            var dxStart = _startPosX - _target.X;
            var dyStart = _startPosY - _target.Y;

            var dx = _position.X - _target.X;
            var dy = _position.Y - _target.Y;

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

            float targetPoints = 2 * _targetCount;
            float fitness = targetPoints + targetReachedPerc + targetActivatePerc;

            if (_speedDeath)
                fitness -= 3;
            else
                fitness += speedFactor * _speedBonusFitness;

            return fitness;
        }

        protected override Vector getAcceleration(Vector3D vecVel, Vector3D vecGoal)
        {
            var dist = NeuralNet.activate((float)vecGoal.Length - (float)Radius);
            vecGoal.Normalize();
            var nx = (float)vecGoal.X;
            var ny = (float)vecGoal.Y;

            var vel = (float)vecVel.Length;
            vecVel.Normalize();
            var vnx = (float)vecVel.X;
            var vny = (float)vecVel.Y;

            var output = _net.FeedForward(new float[] {
                dist,
                nx,
                ny,
                vel * vel,
                vnx,
                vny,
                (float)_acceleration.X,
                (float)_acceleration.Y
            });

            return new Vector(output[0], output[1]);
        }

        internal override void resetPos(float startX, float startY)
        {
            base.resetPos(startX, startY);
            _speedBonusFitness = 0;
            _startPosX = startX;
            _startPosY = startY;

            if (_ellipse != null)
                _ellipse.Visibility = Visibility.Visible;
        }

        internal void setCurrentStartPos()
        {
            _startPosX = (float)_position.X;
            _startPosY = (float)_position.Y;
        }

        public override string ToString()
        {
            return string.Format("Targets {0}, TargetReached {1}", _targetCount, TargetReached);
        }
    }
}