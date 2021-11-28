
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

        public override float getFitness(float speedFactor)
        {
            var dxStart = _startPosX - _targetX;
            var dyStart = _startPosY - _targetY;

            var dx = _posX - _targetX;
            var dy = _posY - _targetY;

            float distTargetNow = (float)Math.Sqrt(dx * dx + dy * dy);

            float targetReachedPerc = 0;
            float targetActivatePerc = 0;
            if (distTargetNow < Radius)
                targetActivatePerc = 1 + (_settings.GoalTargetIterations - _targetIterationCount) / (float)_settings.GoalTargetIterations;
            else
            {
                float distTargetStart = (float)Math.Sqrt(dxStart * dxStart + dyStart * dyStart);
                targetReachedPerc = (distTargetStart - distTargetNow) / distTargetStart;
            }

            float targetPoints = 2 * _targetCount;
            float fitness = targetPoints + targetReachedPerc + targetActivatePerc;

            if (_speedDeath)
                fitness -= 3;
            //else
            //    fitness += speedFactor * _speedBonusFitness;

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
        }
    }
}