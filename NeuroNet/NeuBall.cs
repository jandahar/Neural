
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NeuroNet
{
    internal class NeuBall : NeuMoverBase
    {
        private float _startPosX;
        private float _startPosY;
        private float _speedBonusFitness = 0;
        private bool _isChampion;

        public bool Champion { get => _isChampion; internal set => _isChampion = value; }

        public NeuBall(NeuralSettings settings, int id, float X, float Y, float xM, float yM, float scale, int[] layerConfig) : base(settings, id, X, Y, xM, yM, scale, layerConfig)
        {
            _startPosX = X;
            _startPosY = Y;
        }

        public NeuBall(NeuralSettings settings, float x, float y, float xM, float yM, float scale, NeuBall previousGen, int chance, float variation, int[] layerConfig) : base(settings, x, y, xM, yM, scale, previousGen, chance, variation, layerConfig)
        {
        }

        public override void setColors(Brush mainColor, Brush secondaryColor)
        {
            _mainColor = mainColor;
            _secondaryColor = secondaryColor;
            _ellipse.Stroke = mainColor;
            _ellipse.Fill = secondaryColor;
            _ellipse.StrokeThickness = 5;
        }

        protected override float checkTargetHit(float targetX, float targetY)
        {
            float distanceToTargetSquare = base.checkTargetHit(targetX, targetY);

            if(TargetReached)
            {
                _speedBonusFitness += _iterationsToTarget  / _settings.TurnsToTarget;
            }

            return distanceToTargetSquare;
        }

        public override float getFitness(float speedFactor)
        {
            var dxStart = _startPosX - _targetX;
            var dyStart = _startPosY - _targetY;

            var dx = _position.X - _targetX;
            var dy = _position.Y - _targetY;

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

        protected override Vector getAcceleration(Vector vecVel, Vector vecGoal)
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
                _accelX,
                _accelY
            });

            Vector accel = new Vector(output[0], output[1]);
            return accel;
        }

        internal override void resetPos(float startX, float startY)
        {
            base.resetPos(startX, startY);
            _speedBonusFitness = 0;
            _startPosX = startX;
            _startPosY = startY;
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