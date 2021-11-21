using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NeuroNet
{
    internal class NeuralTrainer
    {
        private NeuralSettings _settings;
        private NeuBall[] _balls;
        private Random _rnd;
        private Brush[] _colors;
        private Brush _color;
        private int _generation;
        private double _actualHeight;
        private double _actualWidth;
        private int _maxIterationsEnd = 2500;
        private int _maxIterations = 25;

        private int _iteration;

        private int _targets;
        private int _targetsMax = 0;
        List<Point> _targetList = new List<Point>();

        private List<NeuBall> _nextGen;
        private int _targetRadius;
        private int[] _layerConfig;

        public NeuralTrainer(int seed, NeuralSettings neuralSettings, double actualWidth, double actualHeight, Brush[] colors, Brush trainerColor)
        {
            _actualHeight = actualHeight;
            _actualWidth = actualWidth;

            _rnd = new Random((int)DateTime.Now.Ticks + seed);
            _settings = neuralSettings;

            _colors = colors;
            _color = trainerColor;

            _layerConfig = new int[] { 8, 4, 2 };
        }


        public NeuralNet getActiveNet()
        {
            if (_balls.Length > 0)
                return _balls[0].Net;

            return null;
        }

        internal bool hasNextGen()
        {
            return _nextGen != null && _nextGen.Count > 0;
        }

        internal void init(UIElementCollection uiElements)
        {
            initBalls();

            for (int id = 0; id < _balls.Length; id++)
            {
                uiElements.Add(_balls[id].Ellipse);
            }

            drawTarget(uiElements);
        }

        internal void initNextGeneration(UIElementCollection uiElements)
        {
            var scale = (float)Math.Max(_actualWidth, _actualHeight);

            float centerX = 0.5f * (float)_actualHeight;
            float centerY = 0.5f * (float)_actualWidth;

            float startX;
            float startY;
            getRandomPoint(out startX, out startY);

            int noPerPrevious = _settings.NumberNets / _nextGen.Count;
            _balls = new NeuBall[noPerPrevious * _nextGen.Count];

            var variance = 0.02f * _maxIterationsEnd / _maxIterations;
            int chance = (int)(99f * ((float)_generation / (float)_maxIterationsEnd) + 1);

            int count = 0;
            foreach (var previousGen in _nextGen)
            {
                previousGen.resetPos(startX, startY);
                previousGen.Active = true;
                _balls[count * noPerPrevious] = previousGen;

                var generationColor = previousGen.SecondaryColor;
                if (_generation % 10 == 0)
                    generationColor = _colors[_rnd.Next(_colors.Length)];

                var stroke = _colors[_rnd.Next(_colors.Length)];

                for (int id = count * noPerPrevious + 1; id < (count + 1) * noPerPrevious; id++)
                {
                    _balls[id] = new NeuBall(_settings, startX, startY, centerX, centerY, scale, previousGen, chance, variance, _layerConfig);
                    _balls[id].setColors(_color, generationColor);
                    uiElements.Add(_balls[id].Ellipse);
                }

                count++;
            }

            _balls[0].highlight();

            for (int i = 0; i < _nextGen.Count; i++)
                uiElements.Add(_nextGen[_nextGen.Count - i - 1].Ellipse);

            drawTarget(uiElements);

            _nextGen = null;
        }

        internal void getNextIteration(UIElementCollection uiElements, ref string debug)
        {
            if (_balls == null)
                init(uiElements);

            if (_balls.Length > 0)
            {
                _iteration++;

                int activeCount = 0;
                for (int id = 0; id < _balls.Length; id++)
                {

                    NeuBall current = _balls[id];
                    if (current.Active)
                    {
                        activeCount++;

                        var targetX = (float)_targetList[current.TargetCount].X;
                        var targetY = (float)_targetList[current.TargetCount].Y;
                        current.doTimeStep(_iteration, targetX, targetY, (float)_actualWidth, (float)_actualHeight);

                        if(current.TargetReached)
                        {
                            if (current.TargetCount > _targetList.Count - 1)
                            {
                                addRandomTarget();
                                drawTarget(uiElements);
                            }
                        }
                    }
                }

                int noToChoose = _balls.Length / 20;
                if (_iteration > _maxIterations || activeCount < noToChoose)
                {
                    restartIteration();
                }

                debug += string.Format("Generation {0}\nIteration: {1} / {2}\nActive: {3}\nTargets best {4} \nTargetCount {5}\n\n",
                    _generation,
                    _iteration,
                    _maxIterations,
                    activeCount,
                    _targetsMax,
                    _targetList.Count);
            }
        }

        internal void setLayerConfig(int[] layerConfig)
        {
            _layerConfig = layerConfig;
        }

        internal void getUiElements(UIElementCollection uiElements)
        {
            foreach (var ball in _balls)
                uiElements.Add(ball.Ellipse);
        }

        internal void updateSettings(double actualWidth, double actualHeight)
        {
            _actualHeight = actualHeight;
            _actualWidth = actualWidth;

            _maxIterations = _settings.NumberIterationsStart;
            _generation = 0;
            _targetsMax = 0;
            _balls = null;
            addRandomTarget();
        }

        private void restartIteration()
        {
            //_maxIterations += _maxIterations / 5;
            _maxIterations++;
            if (_maxIterations > 100)
                _maxIterations += _maxIterations / 100;

            _maxIterations = Math.Min(_maxIterations, _maxIterationsEnd);

            _iteration = 0;

            var sorted = new SortedDictionary<float, NeuBall>();
            for (int i = 1; i < _balls.Length; i++)
            {
                if (!sorted.ContainsKey(-_balls[i].getFitness()))
                    sorted.Add(-_balls[i].getFitness(), _balls[i]);
            }

            int noToChoose = 20;// _balls.Length / 10;
            _nextGen = new List<NeuBall>();

            //if (pickNextGeneration(sorted, noToChoose, true) < noToChoose)
                pickNextGeneration(sorted, noToChoose, false);


            _generation++;
            _targets = 0;
            _targetList = new List<Point>();
            addRandomTarget();
        }

        private void initBalls()
        {
            var scale = (float)Math.Max(_actualWidth, _actualHeight);

            float centerX = 0.5f * (float)_actualHeight;
            float centerY = 0.5f * (float)_actualWidth;

            float startX;
            float startY;
            getRandomPoint(out startX, out startY);

            _balls = new NeuBall[_settings.NumberNets];

            for (int id = 0; id < _balls.Length; id++)
            {
                var seed = (int)DateTime.Now.Ticks % (id + 1000);
                _balls[id] = new NeuBall(_settings, seed, startX, startY, centerX, centerY, scale, _layerConfig);
                _balls[id].setColors(_color, _colors[_rnd.Next(_colors.Length)]);
            }

            _generation++;
            _targets = 0;
        }

        private int pickNextGeneration(SortedDictionary<float, NeuBall> sorted, int noToChoose, bool onlyActive)
        {
            int count = 0;

            foreach (var entry in sorted)
            {
                if (onlyActive && !entry.Value.Active)
                    continue;

                if (!_nextGen.Contains(entry.Value))
                {
                    _nextGen.Add(entry.Value);
                    entry.Value.Ellipse.Stroke = Brushes.Blue;
                    entry.Value.Ellipse.Fill = Brushes.Red;
                    entry.Value.Ellipse.Visibility = Visibility.Visible;
                    if (++count > noToChoose - 1)
                        break;
                }
            }

            return count;
        }

        private void getRandomPoint(out float pX, out float pY)
        {
            if (_settings.RandomTargets && _generation % 2 == 0)
            {
                pX = (float)((0.8 * _rnd.NextDouble() + 0.1) * _actualWidth);
                pY = (float)((0.8 * _rnd.NextDouble() + 0.1) * _actualHeight);
            }
            else
            {
                var f = 0.1 + 0.9 * Math.Min((float)(_iteration + 1) / _maxIterationsEnd, 0.4);
                pX = (float)((0.5 + f * (_rnd.NextDouble() - 0.5)) * _actualWidth);
                pY = (float)((0.5 + f * (_rnd.NextDouble() - 0.5)) * _actualHeight);
            }
        }

        private void drawTarget(UIElementCollection uiElements)
        {
            _targetRadius = 30;
            var ellipse = new Ellipse
            {
                Stroke = Brushes.LightBlue,
                StrokeThickness = 2,

                Width = 2 * _targetRadius,
                Height = 2 * _targetRadius,
            };

            var target = _targetList[_targetList.Count - 1];
            ellipse.RenderTransform = new TranslateTransform(target.X - _targetRadius, target.Y - _targetRadius);

            uiElements.Add(ellipse);
            _targets++;
            if (_targets > _targetsMax)
                _targetsMax = _targets;
        }

        private void addRandomTarget()
        {
            float px;
            float py;

            getRandomPoint(out px, out py);

            //if (_targetList.Count > 0)
            //{
            //    float distance = 0.0f;
            //    while (distance < 4 *_targetRadius * _targetRadius)
            //    {
            //        getRandomPoint(out px, out py);
            //        var prevX = (float)_targetList[_targetList.Count - 1].X;
            //        var prevY = (float)_targetList[_targetList.Count - 1].Y;

            //        var dx = px - prevX;
            //        var dy = py - prevY;

            //        distance = dx * dx + dy * dy;
            //    }
            //}

            _targetList.Add(new Point(px, py));
        }
    }
}