
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

        public override float getFitness()
        {
            var dxStart = _startPosX - _targetX;
            var dyStart = _startPosY - _targetY;

            var dx = _posX - _targetX;
            var dy = _posY - _targetY;

            float distTargetNow = (float)Math.Sqrt(dx * dx + dy * dy);

            float targetReachedPerc = 0;
            float targetActivatePerc = 0;
            if (distTargetNow < _radius)
                targetActivatePerc = 1 + (_settings.GoalTargetIterations - _targetIterationCount) / (float)_settings.GoalTargetIterations;
            else
            {
                float distTargetStart = (float)Math.Sqrt(dxStart * dxStart + dyStart * dyStart);
                targetReachedPerc = (distTargetStart - distTargetNow) / distTargetStart;
            }

            float targetPoints = 2 * _targetCount;
            float fitness = targetPoints + targetReachedPerc + targetActivatePerc;

            if (!_speedDeath)
                fitness += _speedBonusFitness;
            else
                fitness -= 3;

            return fitness;
        }

        protected float targetCountFactor()
        {
            return (float)Math.Pow(1.5, _targetCount);
            //return _targetCount + 1;
        }

        protected override void calculateFitness(float gain, float distTarget)
        {
            //_fitness += iteration;
            //_fitness++;

            var factorNTargets = targetCountFactor();

            float deltafFitnessTarget = factorNTargets;
            float deltaFitnessGain = 0.0f;
            float deltaFitnessZone = 0.0f;

            //var distTargetCurrent = getDistanceToTarget(targetX, targetY);

            _ellipse.RenderTransform = new TranslateTransform(_posX - _radius, _posY - _radius);



            float distZone = (float)(Math.Min(_radiusSquare / distTarget, 50));
            deltaFitnessZone = factorNTargets * distZone;

            //if (_targetCount < 2)
            {
                if (_settings.RandomTargets)
                {
                    //float distZone = (float)(Math.Min(_radiusSquare / distTarget, 10));
                    if (distZone < 0.1)
                    {
                        if (gain > 0 || gain < 0)
                        {
                            //float dFitness = gain * distZone;


                            //float dist = 1f / distZone - 0.5f;
                            //float vel = (float)Math.Sqrt(Math.Sqrt(_velX * _velX + _velY * _velY));
                            //float dsquare = 2 * dist * dist + 1;
                            //_fitness += gain * vel * dsquare;


                            deltaFitnessGain = 2 * gain * (float)Math.Sqrt(Math.Sqrt(_velX * _velX + _velY * _velY));
                        }
                    }
                    else
                        deltaFitnessZone = factorNTargets * distZone;
                }
                //else
                //{
                //    float distZone = (float)(Math.Min(_radiusSquare / distTarget, 50));
                //    deltaFitnessZone = factorNTargets * distZone;
                //}
            }

            //_fitness += deltafFitnessTarget;
            if (_targetCount < 2)
            {
                _fitness += deltaFitnessGain;

                if (gain > 0)
                    _fitness += deltaFitnessZone;
                else if (gain < 0)
                    _fitness += 0.5f * deltaFitnessZone;
            }
        }

        protected override Vector getAcceleration(Vector vecVel, Vector vecGoal)
        {
            var dist = NeuralNet.activate((float)vecGoal.Length - 10 * (float)_radius);
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

        protected override float calcFitnessMalusForLeavingTarget()
        {
            //return 100 * targetCountFactor() * _targetIterationCount * _targetCount;
            return 100 * targetCountFactor() * _targetIterationCount * _targetCount;
        }

        protected override float calcFitnessOnTarget()
        {
            float dFitness = 50 * targetCountFactor() * _targetIterationCount * _targetCount;

            if (TargetReached)
            {
                int timeLeft = (_settings.TurnsToTarget - _iterationsToTarget);
                dFitness += 500 * targetCountFactor() * timeLeft * timeLeft * _targetCount;
                _speedBonusFitness += (float)_iterationsToTarget / (float)_settings.TurnsToTarget;
                _startPosX = _posX;
                _startPosY = _posY;
            }

            return dFitness;
        }

        internal override void resetPos(float startX, float startY)
        {
            base.resetPos(startX, startY);
            _speedBonusFitness = 0;
        }
    }
}