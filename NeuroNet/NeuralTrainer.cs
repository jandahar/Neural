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
        internal enum TargetingType
        {
            Near,
            Far,
            Alternating,
            Circle
        }

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
        private int _maxTargetsSeen = 1;
        private int _increaseIterations = -1;
        private TargetingType _targeting;
        private int _increaseNumberBalls;
        private bool _allOfPreviousGenerationDied = false;
        private bool _disasterMutate = false;

        private string _debug = string.Empty;

        public Brush Color { get => _color; internal set => _color = value; }
        public int MaxTargetsSeen { get => _maxTargetsSeen; private set => _maxTargetsSeen = value; }
        public int Generation { get => _generation; private set => _generation = value; }
        public int IncreaseIterations { get => _increaseIterations; set => _increaseIterations = value; }
        public int IncreaseNumberBalls { get => _increaseNumberBalls; internal set => _increaseNumberBalls = value; }
        public bool DisasterMutate { get => _disasterMutate; internal set => _disasterMutate = value; }
        internal TargetingType Targeting { get => _targeting; set => _targeting = value; }

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

        internal int initNextGeneration(UIElementCollection uiElements)
        {
            _generation++;
            _targets = 0;
            _targetList = new List<Point>();
            addRandomTarget();
            drawTarget(uiElements);

            var scale = (float)Math.Max(_actualWidth, _actualHeight);

            float centerX = 0.5f * (float)_actualHeight;
            float centerY = 0.5f * (float)_actualWidth;

            float startX;
            float startY;
            if (_targeting == TargetingType.Circle)
            {
                startX = (float)(0.5 * _actualWidth);
                startY = (float)(0.5 * _actualHeight);
            }
            else
                getRandomPoint(out startX, out startY);

            int noPerPrevious = (_settings.NumberNets + _increaseNumberBalls) / _nextGen.Count;
            _balls = new NeuBall[noPerPrevious * _nextGen.Count];

            var variance = 0.02f * _maxIterationsEnd / _maxIterations;
            int chance = (int)(99f * ((float)_generation / (float)_maxIterationsEnd) + 1);

            if (_disasterMutate && _allOfPreviousGenerationDied)
            {
                chance /= 2;
                variance *= 20;
                _allOfPreviousGenerationDied = false;
            }

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


            _nextGen = null;

            return _maxTargetsSeen;
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

                        if (current.TargetReached)
                        {
                            if (current.TargetCount > _targetList.Count - 1)
                            {
                                addRandomTarget();
                                drawTarget(uiElements);
                            }
                        }
                    }
                }

                //int noToRestart = _balls.Length / 20;
                int noToRestart = 0;
                _allOfPreviousGenerationDied = activeCount < noToRestart + 1;
                if (_iteration > _maxIterations || _allOfPreviousGenerationDied)
                {
                    restartIteration();

                    if (_allOfPreviousGenerationDied)
                        _debug += "Catastrophic\n";
                    else
                        _debug += "Target iterations reached\n";
                }

                debug += string.Format("Generation {0}\nIteration: {1} / {2}\nActive: {3}\nTargets best {4} \nTargetCount {5}\n{6}\n",
                    _generation,
                    _iteration,
                    _maxIterations,
                    activeCount,
                    _targetsMax - 1,
                    _targetList.Count - 1,
                    _debug);
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

            foreach (var target in _targetList)
                addEllipse(uiElements, target);
        }

        internal void updateSettings(double actualWidth, double actualHeight)
        {
            _actualHeight = actualHeight;
            _actualWidth = actualWidth;

            _maxIterations = _settings.NumberIterationsStart;
            _generation = 0;
            _targetsMax = 0;
            _balls = null;

            switch (_settings.Targeting)
            {
                case "Far":
                    _targeting = TargetingType.Far;
                    break;
                case "Circle":
                    _targeting = TargetingType.Circle;
                    break;
                case "Near":
                default:
                    _targeting = TargetingType.Near;
                    break;
            }

            addRandomTarget();
        }

        private void restartIteration()
        {
            _debug = string.Empty;
            //_maxIterations += _maxIterations / 5;
            _maxIterations++;
            var percIncrease = _disasterMutate ? 1 : 2;
            if (_maxIterations > 100)
                _maxIterations += percIncrease * _maxIterations / 100;

            if (_increaseIterations > 0 && _maxTargetsSeen > _increaseIterations)
                _maxIterations = Math.Max(_maxIterations, _settings.TurnsToTarget * _maxTargetsSeen);

            if (_allOfPreviousGenerationDied)
                _maxIterations /= 3;

            _maxIterations = Math.Min(_maxIterations, _maxIterationsEnd);

            if (_targeting == TargetingType.Circle)
                _maxIterations = _settings.NumberIterationsStart;

            _debug += "Last # iterations: " + _iteration + "\n";
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
        }

        private void initBalls()
        {
            var scale = (float)Math.Max(_actualWidth, _actualHeight);

            float centerX = 0.5f * (float)_actualHeight;
            float centerY = 0.5f * (float)_actualWidth;

            float startX;
            float startY;

            if (_targeting == TargetingType.Circle)
            {
                startX = (float)(0.5 * _actualWidth);
                startY = (float)(0.5 * _actualHeight);
            }
            else
                getRandomPoint(out startX, out startY);

            _balls = new NeuBall[_settings.NumberNets + _increaseNumberBalls];

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
            switch (_targeting)
            {
                case TargetingType.Far:
                    getRandomeFarPoint(out pX, out pY);
                    break;
                case TargetingType.Circle:
                    getRandomeCirclePoint(out pX, out pY);
                    break;
                case TargetingType.Alternating:
                    if (_settings.RandomTargets && _generation % 2 == 0)
                        getRandomeFarPoint(out pX, out pY);
                    else
                        getRandomNearPoint(out pX, out pY);
                    break;
                default:
                case TargetingType.Near:
                    getRandomNearPoint(out pX, out pY);
                    break;
            }
        }

        private void getRandomeCirclePoint(out float pX, out float pY)
        {
            var f = 0.1 + 0.9 * Math.Min((float)(_iteration + 1) / _maxIterationsEnd, 0.4);
            var radius = (0.4 + 0.1 * _rnd.NextDouble()) * Math.Min(_actualHeight, _actualWidth);
            var phi = 360 * _rnd.NextDouble();
            pX = (float)(0.5 * _actualWidth + radius * Math.Cos(phi));
            pY = (float)(0.5 * _actualHeight + radius * Math.Sin(phi));
        }

        private void getRandomNearPoint(out float pX, out float pY)
        {
            var f = 0.1 + 0.9 * Math.Min((float)(_iteration + 1) / _maxIterationsEnd, 0.4);
            pX = (float)((0.5 + f * (_rnd.NextDouble() - 0.5)) * _actualWidth);
            pY = (float)((0.5 + f * (_rnd.NextDouble() - 0.5)) * _actualHeight);
        }

        private void getRandomeFarPoint(out float pX, out float pY)
        {
            pX = (float)((0.8 * _rnd.NextDouble() + 0.1) * _actualWidth);
            pY = (float)((0.8 * _rnd.NextDouble() + 0.1) * _actualHeight);
        }

        private void drawTarget(UIElementCollection uiElements)
        {
            _targetRadius = 30;
            var target = _targetList[_targetList.Count - 1];

            addEllipse(uiElements, target);
            _targets++;
            if (_targets > _targetsMax)
                _targetsMax = _targets;
        }

        private void addEllipse(UIElementCollection uiElements, Point target)
        {
            var ellipse = new Ellipse
            {
                Stroke = _color,
                StrokeThickness = 2,

                Width = 2 * _targetRadius,
                Height = 2 * _targetRadius,
            };

            ellipse.RenderTransform = new TranslateTransform(target.X - _targetRadius, target.Y - _targetRadius);

            uiElements.Add(ellipse);
        }

        private void addRandomTarget()
        {
            float px;
            float py;

            getRandomPoint(out px, out py);

            _targetList.Add(new Point(px, py));
            _maxTargetsSeen = _targetList.Count;
        }
    }
}