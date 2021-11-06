
using System;

namespace NeuroNet
{
    internal class NeuBall
    {
        private double _posX;
        private double _posY;
        private double _velY;
        private double _accelY;

        public NeuBall(double X, double Y)
        {
            _posX = X;
            _posY = Y;

            _velY = 0;
        }

        public double PosX { get => _posX; private set => _posX = value; }
        public double PosY { get => _posY; private set => _posY = value; }
        public double VelY { get => _velY; set => _velY = value; }

        public void doTimeStep()
        {
            _velY += 0.01 + _accelY;
            _posY += _velY;
        }

        internal void setAccelY(float v)
        {
            _accelY = v;
        }
    }
}