
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NeuroNet
{
    internal class NeuBall : NeuMoverBase
    {
        public NeuBall(NeuralSettings settings, int id, float X, float Y, float xM, float yM, float scale) : base(settings, id, X, Y, xM, yM, scale)
        {
        }

        public NeuBall(NeuralSettings settings, float x, float y, float xM, float yM, float scale, NeuBall previousGen, int chance, float variation) : base(settings, x, y, xM, yM, scale, previousGen, chance, variation)
        {
        }

        protected override int[] getLayerConfig()
        {
            return new int[] { 8, 12, 8, 4, 2 };
            //int[] layerconfig = new int[] { 8, 6, 4, 2 };
            //int[] layerConfig = new int[] { 8, 12, 12, 12, 4, 2 };

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
            _fitness += deltaFitnessGain;

            if (gain > 0)
                _fitness += deltaFitnessZone;
            else if (gain < 0)
                _fitness += 0.5f * deltaFitnessZone;
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
            return 100 * targetCountFactor() * _targetIterationCount * _targetCount;
        }

        protected override float calcFitnessOnTarget()
        {
            float dFitness = 50 * targetCountFactor() * _targetIterationCount * _targetCount;

            if (TargetReached)
                dFitness += 500 * targetCountFactor() * (_settings.TurnsToTarget - _iterationsToTarget) * _targetCount;

            return dFitness;
        }
    }
}