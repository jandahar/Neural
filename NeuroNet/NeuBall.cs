
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

        protected override float targetCountFactor()
        {
            return (float)Math.Pow(1.5, _targetCount);
            //return _targetCount + 1;
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
            return 50 * targetCountFactor() * _targetIterationCount * _targetCount;
        }
    }
}