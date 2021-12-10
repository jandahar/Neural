
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace NeuroNet
{
    internal class NeuBall : NeuMoverBase
    {
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
            else
            {
                bool onTarget = NeuMoverBase.onTarget((float)distanceToTargetSquare);
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

            if (_ellipse != null)
                _ellipse.Visibility = Visibility.Visible;
        }

        public override string ToString()
        {
            return string.Format("Targets {0}, TargetReached {1}", TargetCount, TargetReached);
        }
    }
}