using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace NeuroNet
{
    internal class NeuralTrainer2D : NeuralTrainer
    {
        private List<Line> _spurLines = new List<Line>();

        public NeuralTrainer2D(int seed, NeuralSettings neuralSettings, double actualWidth, double actualHeight, Brush[] colors, Brush trainerColor) : base(seed, neuralSettings, actualWidth, actualHeight, colors, trainerColor)
        {
        }

        protected override NeuMoverBase createMover(float scale, float centerX, float centerY, Point3D start, int id, int seed)
        {
            return new NeuBall(_settings, seed, start, centerX, centerY, scale, _layerConfig);
        }
        protected override NeuMoverBase createMoverFromPreviousGen(float scale, float centerX, float centerY, Point3D start, float variance, int chance, NeuMoverBase previousGen)
        {
            return new NeuBall(_settings, start, centerX, centerY, scale, (NeuBall)previousGen, chance, variance, _layerConfig);
        }

        internal override int initNextGeneration()
        {
            _spurLines = new List<Line>();

            return base.initNextGeneration();
        }

        internal override void initUiElements()
        {
            base.initUiElements();

            _spurLines = new List<Line>();
        }

        internal override void getUiElements(UIElementCollection uiElements)
        {
            base.getUiElements(uiElements);

            foreach (var l in _spurLines)
                uiElements.Add(l);
        }

        protected override void visualizeMovement(NeuMoverBase current, Point posStart)
        {
            if (_settings.DrawLines &&
                _iteration > 0 &&
                current.Champion &&
                !current.Hidden)
            {
                Line line = new Line
                {
                    X1 = posStart.X,
                    Y1 = posStart.Y,
                    X2 = current.PosX,
                    Y2 = current.PosZ,
                    StrokeThickness = 1,
                    Stroke = _color
                };

                _spurLines.Add(line);
                _newUiElements.Add(line);
            }
        }

        protected override void drawTarget(Point3D target)
        {
            var ellipse = new Ellipse
            {
                Stroke = _color,
                StrokeThickness = 2,

                Width = 2 * _levels[_currentLevel].TargetRadius,
                Height = 2 * _levels[_currentLevel].TargetRadius,
            };

            ellipse.RenderTransform = new TranslateTransform(target.X - _levels[_currentLevel].TargetRadius, target.Z - _levels[_currentLevel].TargetRadius);

            _newUiElements.Add(ellipse);
        }
    }
    internal class NeuralTrainer3D : NeuralTrainer
    {
        private List<Line> _spurLines = new List<Line>();

        public NeuralTrainer3D(int seed, NeuralSettings neuralSettings, double actualWidth, double actualHeight, Brush[] colors, Brush trainerColor) : base(seed, neuralSettings, actualWidth, actualHeight, colors, trainerColor)
        {
            _layerConfig = new int[] { 11, 8, 3 };
        }

        protected override NeuMoverBase createMover(float scale, float centerX, float centerY, Point3D start, int id, int seed)
        {
            return new NeuBall3D(_settings, seed, start, centerX, centerY, scale, _layerConfig);
        }
        protected override NeuMoverBase createMoverFromPreviousGen(float scale, float centerX, float centerY, Point3D start, float variance, int chance, NeuMoverBase previousGen)
        {
            return new NeuBall3D(_settings, start, centerX, centerY, scale, (NeuBall3D)previousGen, chance, variance, _layerConfig);
        }

        internal override int initNextGeneration()
        {
            _spurLines = new List<Line>();

            return base.initNextGeneration();
        }

        internal override void initUiElements()
        {
            base.initUiElements();

            _spurLines = new List<Line>();
        }

        internal override void getUiElements(UIElementCollection uiElements)
        {
            base.getUiElements(uiElements);

            foreach (var l in _spurLines)
                uiElements.Add(l);
        }

        protected override void visualizeMovement(NeuMoverBase current, Point posStart)
        {
            if (_settings.DrawLines &&
                _iteration > 0 &&
                current.Champion &&
                !current.Hidden)
            {
                Line line = new Line
                {
                    X1 = posStart.X,
                    Y1 = posStart.Y,
                    X2 = current.PosX,
                    Y2 = current.PosZ,
                    StrokeThickness = 1,
                    Stroke = _color
                };

                _spurLines.Add(line);
                _newUiElements.Add(line);
            }
        }

        protected override void drawTarget(Point3D target)
        {
            var ellipse = new Ellipse
            {
                Stroke = _color,
                StrokeThickness = 2,

                Width = 2 * _levels[_currentLevel].TargetRadius,
                Height = 2 * _levels[_currentLevel].TargetRadius,
            };

            ellipse.RenderTransform = new TranslateTransform(target.X - _levels[_currentLevel].TargetRadius, target.Z - _levels[_currentLevel].TargetRadius);

            _newUiElements.Add(ellipse);
        }
    }
}