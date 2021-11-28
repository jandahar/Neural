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
            Circle,
            Fixed,
        }

        private NeuralSettings _settings;
        private NeuBall[] _balls;
        private Random _rnd;
        private Brush[] _colors;
        private Brush _color;
        private int _generation;
        private double _actualHeight;
        private double _actualWidth;
        private const int _generationConvergenceTarget = 2500;
        private int _maxIterationsEnd = 600;
        private int _maxIterations = 25;

        private int _iteration;

        private int _targets;
        private int _targetsMax = 0;
        List<Point> _targetList = new List<Point>();
        private int _noToChoose = 20;
        private List<NeuBall> _nextGen;
        private double _targetRadius = 2 * NeuMoverBase.Radius;
        private int[] _layerConfig;
        private int _maxTargetsSeen = 1;
        private int _increaseIterations = -1;
        private TargetingType _targeting;
        private int _increaseNumberBalls;
        private bool _allOfPreviousGenerationDied = false;
        private bool _disasterMutate = false;

        private string _debug = string.Empty;
        private float _speedFitnessFactor = 3;
        private List<Point> _fixedTargets = new List<Point>();

        public Brush Color { get => _color; internal set => _color = value; }
        public int MaxTargetsSeen { get => _maxTargetsSeen; private set => _maxTargetsSeen = value; }
        public int Generation { get => _generation; private set => _generation = value; }
        public int IncreaseIterations { get => _increaseIterations; set => _increaseIterations = value; }
        public int IncreaseNumberBalls { get => _increaseNumberBalls; internal set => _increaseNumberBalls = value; }
        public bool DisasterMutate { get => _disasterMutate; internal set => _disasterMutate = value; }
        internal TargetingType Targeting { get => _targeting; set => _targeting = value; }
        public int NoToChooseForNextGeneration { get => _noToChoose; set => _noToChoose = value; }
        public float SpeedFitnessFactor { get => _speedFitnessFactor; set => _speedFitnessFactor = value; }
        public List<Point> FixedTargets
        {
            get => _fixedTargets; internal set
            {
                _fixedTargets = value;
                //_targetList = _fixedTargets;
            }
        }

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

            addRandomTarget(0);
            drawTarget(uiElements);
        }

        private void initBalls()
        {
            var scale = (float)Math.Max(_actualWidth, _actualHeight);

            float centerX = 0.5f * (float)_actualHeight;
            float centerY = 0.5f * (float)_actualWidth;

            float startX;
            float startY;
            getStartPoint(out startX, out startY);

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

        internal int initNextGeneration(UIElementCollection uiElements)
        {
            _generation++;
            _targets = 0;
            _targetList = new List<Point>();

            addRandomTarget(0);
            drawTarget(uiElements);

            var scale = (float)Math.Max(_actualWidth, _actualHeight);

            float centerX = 0.5f * (float)_actualHeight;
            float centerY = 0.5f * (float)_actualWidth;

            float startX, startY;
            getStartPoint(out startX, out startY);

            int noPerPrevious = (_settings.NumberNets + _increaseNumberBalls) / _nextGen.Count;
            _balls = new NeuBall[noPerPrevious * _nextGen.Count];


            var variance = 0.01f * _generationConvergenceTarget / _generation;
            int chance = (int)(199f * ((float)_generation / (float)_generationConvergenceTarget) + 1);

            if (_disasterMutate && _allOfPreviousGenerationDied)
            {
                chance = (int)(0.7 * chance);
                variance = 1.75f * variance;
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

        private void getStartPoint(out float startX, out float startY)
        {
            switch (_targeting)
            {
                case TargetingType.Fixed:
                    if (_fixedTargets.Count > 0)
                    {
                        startX = (float)_fixedTargets[0].X;
                        startY = (float)_fixedTargets[0].Y;
                    }
                    else
                    {
                        startX = (float)(0.5 * _actualWidth);
                        startY = (float)(0.5 * _actualHeight);
                        break;
                    }
                    break;
                case TargetingType.Circle:
                    {
                        startX = (float)(0.5 * _actualWidth);
                        startY = (float)(0.5 * _actualHeight);
                        break;
                    }
                case TargetingType.Near:
                case TargetingType.Far:
                case TargetingType.Alternating:
                default:
                    getRandomPoint(out startX, out startY);
                    break;
            }
        }

        internal void getNextIteration(UIElementCollection uiElements, ref string debug)
        {
            if (_balls == null)
                init(uiElements);

            if (_balls.Length > 0)
            {
                _iteration++;
                bool maxIterationsReached = _iteration > _maxIterations;

                int activeCount = 0;
                for (int id = 0; id < _balls.Length; id++)
                {

                    NeuBall current = _balls[id];
                    if (current.Active)
                    {
                        //if (!maxIterationsReached)
                        activeCount++;

                        var targetX = (float)_targetList[current.TargetCount].X;
                        var targetY = (float)_targetList[current.TargetCount].Y;
                        current.doTimeStep(_iteration, targetX, targetY, (float)_actualWidth, (float)_actualHeight);

                        if (current.TargetReached)
                        {
                            if (current.TargetCount > _targetList.Count - 1)
                            {
                                addRandomTarget(current.TargetCount);
                                drawTarget(uiElements);
                            }
                            if (current.TargetCount > _maxTargetsSeen)
                                _maxTargetsSeen = current.TargetCount;

                            if (maxIterationsReached)
                                current.Active = false;
                        }
                        else if (current.TargetCount < 10 && maxIterationsReached)
                            current.Active = false;
                    }
                }

                //int noToRestart = _balls.Length / 20;
                int noToRestart = 0;
                _allOfPreviousGenerationDied = activeCount < noToRestart + 1 && !(maxIterationsReached);
                if (activeCount < noToRestart + 1)
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
        }

        private void restartIteration()
        {
            _debug = string.Empty;
            increaseMaxIterations();
            _iteration = 0;

            var sorted = new SortedDictionary<float, NeuBall>();
            for (int i = 1; i < _balls.Length; i++)
            {
                float fitness = -_balls[i].getFitness(_speedFitnessFactor);
                if (!sorted.ContainsKey(fitness))
                    sorted.Add(fitness, _balls[i]);
            }

            _nextGen = new List<NeuBall>();

            //if (pickNextGeneration(sorted, noToChoose, true) < noToChoose)
            var noToChooseOld = 20;
            if (_maxTargetsSeen > _increaseIterations && _noToChoose != noToChooseOld)
                noToChooseOld = _noToChoose;

            pickNextGeneration(sorted, noToChooseOld, false);
        }

        private void increaseMaxIterations()
        {
            if (_targeting == TargetingType.Fixed)
                _maxIterationsEnd = (_fixedTargets.Count - 1) * 100;

            //_maxIterations += _maxIterations / 5;
            _maxIterations++;
            var percIncrease = _disasterMutate ? 1 : 2;
            if (_maxIterations > 100)
                _maxIterations += percIncrease * _maxIterations / 100;

            if (_increaseIterations > 0 && _maxTargetsSeen > _increaseIterations)
                _maxIterations = Math.Max(_maxIterations, _settings.TurnsToTarget * _maxTargetsSeen);

            if (_allOfPreviousGenerationDied)
            {
                _maxIterations *= 2;
                _maxIterations /= 3;
            }

            _maxIterations = Math.Min(_maxIterations, _maxIterationsEnd);

            if (_targeting == TargetingType.Circle)
                _maxIterations = _settings.NumberIterationsStart;

            _debug += "Last # iterations: " + _iteration + "\n";
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
            getRandomPoint(out pX, out pY, _targeting);
        }

        public void getRandomPoint(out float pX, out float pY, TargetingType targeting)
        {
            switch (targeting)
            {
                case TargetingType.Far:
                    getRandomFarPoint(out pX, out pY);
                    break;
                case TargetingType.Circle:
                    getRandomeCirclePoint(out pX, out pY);
                    break;
                case TargetingType.Alternating:
                    if (_generation % 2 == 0)
                        getRandomFarPoint(out pX, out pY);
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

        private void getRandomFarPoint(out float pX, out float pY)
        {
            pX = (float)((0.8 * _rnd.NextDouble() + 0.1) * _actualWidth);
            pY = (float)((0.8 * _rnd.NextDouble() + 0.1) * _actualHeight);
        }

        private void drawTarget(UIElementCollection uiElements)
        {
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

        private void addRandomTarget(int nTarget)
        {
            float px;
            float py;

            if(_targeting == TargetingType.Fixed)
            {
                px = (float)_fixedTargets[nTarget % _fixedTargets.Count].X;
                py = (float)_fixedTargets[nTarget % _fixedTargets.Count].Y;
            }
            else
                getRandomPoint(out px, out py);

            _targetList.Add(new Point(px, py));
            _maxTargetsSeen = _targetList.Count;
        }
    }
}