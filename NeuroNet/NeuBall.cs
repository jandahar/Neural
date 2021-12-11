
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace NeuroNet
{
    internal class NeuBall : NeuMoverBase
    {
        private Ellipse _ellipse;
        public Ellipse Ellipse { get => _ellipse; private set => _ellipse = value; }

        public NeuBall(NeuralSettings settings, int id, Point3D pos, float xM, float yM, float scale, int[] layerConfig) : base(settings, id, pos, xM, yM, scale, layerConfig)
        {
            init();
        }

        public NeuBall(NeuralSettings settings, Point3D pos, float xM, float yM, float scale, NeuBall previousGen, int chance, float variation, int[] layerConfig) : base(settings, pos, xM, yM, scale, previousGen, chance, variation, layerConfig)
        {
            init();
        }

        private void init()
        {
            _ellipse = new Ellipse
            {
                Stroke = Brushes.Blue,
                Fill = Brushes.Blue,
                StrokeThickness = 2,
                Width = 2 * _radius,
                Height = 2 * _radius,
            };

            _ellipse.RenderTransform = new TranslateTransform(_position.X - _radius, _position.Z - _radius);
        }

        public override void setColors(Brush mainColor, Brush secondaryColor)
        {
            base.setColors(mainColor, secondaryColor);

            _ellipse.Stroke = mainColor;
            _ellipse.Fill = secondaryColor;
            _ellipse.StrokeThickness = 5;
        }

        public override void markWinner()
        {
            _ellipse.StrokeThickness = 5;
            _ellipse.Stroke = Brushes.Red;
        }

        public override void markChampion()
        {
            _ellipse.Stroke = Brushes.Blue;
            _ellipse.Fill = Brushes.Red;
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
            _ellipse.RenderTransform = new TranslateTransform(_position.X - _radius, _position.Z - _radius);
        }

        protected override Vector3D getAcceleration(Vector3D vecVel, Vector3D vecGoal)
        {
            var dist = NeuralNet.activate((float)vecGoal.Length - (float)Radius);
            vecGoal.Normalize();
            var nx = (float)vecGoal.X;
            var ny = (float)vecGoal.Z;

            var vel = (float)vecVel.Length;
            vecVel.Normalize();
            var vnx = (float)vecVel.X;
            var vny = (float)vecVel.Z;

            var output = _net.FeedForward(new float[] {
                dist,
                nx,
                ny,
                vel * vel,
                vnx,
                vny,
                (float)_acceleration.X,
                (float)_acceleration.Z
            });

            return new Vector3D(output[0], 0.0, output[1]);
        }

        internal override void resetPos(Point3D pos)
        {
            base.resetPos(pos);

            if (_ellipse != null)
                _ellipse.Visibility = Visibility.Visible;
        }

        public override string ToString()
        {
            return string.Format("Targets {0}, TargetReached {1}", TargetCount, TargetReached);
        }

        public override void getUiElements(List<UIElement> uiElements)
        {
            uiElements.Add(_ellipse);
        }

        public override void hide(bool hide = true)
        {
            base.hide(hide);
            _ellipse.Visibility = hide ? Visibility.Hidden : Visibility.Visible;
        }
    }

    internal class NeuBall3D : NeuMoverBase
    {
        private Ellipse _ellipse;
        public Ellipse Ellipse { get => _ellipse; private set => _ellipse = value; }

        public NeuBall3D(NeuralSettings settings, int id, Point3D pos, float xM, float yM, float scale, int[] layerConfig) : base(settings, id, pos, xM, yM, scale, layerConfig)
        {
            init();
        }

        public NeuBall3D(NeuralSettings settings, Point3D pos, float xM, float yM, float scale, NeuBall3D previousGen, int chance, float variation, int[] layerConfig) : base(settings, pos, xM, yM, scale, previousGen, chance, variation, layerConfig)
        {
            init();
        }

        private void init()
        {
            _ellipse = new Ellipse
            {
                Stroke = Brushes.Blue,
                Fill = Brushes.Blue,
                StrokeThickness = 2,
                Width = 2 * _radius,
                Height = 2 * _radius,
            };

            _ellipse.RenderTransform = new TranslateTransform(_position.X - _radius, _position.Z - _radius);
        }

        public override void setColors(Brush mainColor, Brush secondaryColor)
        {
            base.setColors(mainColor, secondaryColor);

            _ellipse.Stroke = mainColor;
            _ellipse.Fill = secondaryColor;
            _ellipse.StrokeThickness = 5;
        }

        public override void markWinner()
        {
            _ellipse.StrokeThickness = 5;
            _ellipse.Stroke = Brushes.Red;
        }

        public override void markChampion()
        {
            _ellipse.Stroke = Brushes.Blue;
            _ellipse.Fill = Brushes.Red;
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
            _ellipse.RenderTransform = new TranslateTransform(_position.X - _radius, _position.Z - _radius);
        }

        protected override Vector3D getAcceleration(Vector3D vecVel, Vector3D vecGoal)
        {
            var dist = NeuralNet.activate((float)vecGoal.Length - (float)Radius);
            vecGoal.Normalize();

            var vel = (float)vecVel.Length;
            vecVel.Normalize();

            var output = _net.FeedForward(new float[] {
                dist,
                (float)vecGoal.X,
                (float)vecGoal.Y,
                (float)vecGoal.Z,
                vel * vel,
                (float)vecVel.X,
                (float)vecVel.Y,
                (float)vecVel.Z,
                (float)_acceleration.X,
                (float)_acceleration.Y,
                (float)_acceleration.Z
            });

            return new Vector3D(output[0], output[1], output[2]);
        }

        internal override void resetPos(Point3D pos)
        {
            base.resetPos(pos);

            if (_ellipse != null)
                _ellipse.Visibility = Visibility.Visible;
        }

        public override string ToString()
        {
            return string.Format("Targets {0}, TargetReached {1}", TargetCount, TargetReached);
        }

        public override void getUiElements(List<UIElement> uiElements)
        {
            uiElements.Add(_ellipse);
        }

        public override void hide(bool hide = true)
        {
            base.hide(hide);
            _ellipse.Visibility = hide ? Visibility.Hidden : Visibility.Visible;
        }
    }
}