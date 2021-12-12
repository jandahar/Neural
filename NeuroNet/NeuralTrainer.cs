using Power3D;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace NeuroNet
{
    internal enum TargetingType
    {
        Near,
        Far,
        Alternating,
        Circle,
        Fixed,
    }

    internal class NeuralTrainerLevel
    {
        public int MaxIterationsStart = 50;
        public int MaxIterationsEnd = 55;
        public Point3D? StartPoint = null;
        public List<Point3D> TargetList = new List<Point3D>();
        public TargetingType Targeting;
        public double TargetRadius = 2 * NeuMoverBase.Radius;
        public int LevelTries = 3;

        public float WinPercentage = 0.05f;
        public int GenerationsToComplete = 20;
    }

    internal abstract class NeuralTrainer
    {
        protected NeuralSettings _settings;
        protected NeuMoverBase[] _balls;
        private Random _rnd;
        private SolidColorBrush[] _colors;
        protected SolidColorBrush _color;
        private int _generation;
        private double _actualHeight;
        private double _actualWidth;

        protected List<UIElement> _newUiElements = new List<UIElement>();
        protected List<P3dMesh> _newMeshes = new List<P3dMesh>();

        protected int _iteration;
        private int _maxIterations = 25;

        protected List<NeuralTrainerLevel> _levels;
        protected int _currentLevel = 0;
        private int _currentLevelGoal = 1;

        private List<Point3D> _targets = new List<Point3D>();
        private int _targetsMax = 0;
        private int _noToChoose = 20;
        private List<NeuMoverBase> _nextGen;
        protected int[] _layerConfig;
        private int _maxTargetsSeen = 1;
        private int _increaseIterations = -1;
        private int _increaseNumberBalls;
        private bool _allOfPreviousGenerationDied = false;
        private float _lastPercentComplete = 0;
        private bool _disasterMutate = false;

        private string _debug = string.Empty;
        private float _speedFitnessFactor = 3;
        //private List<Point> _fixedTargets = new List<Point>();

        private float _bestFitness = 0;
        private int _levelTries = 0;
        private const int _convergenceEnd = 100;

        public SolidColorBrush Color { get => _color; internal set => _color = value; }
        public int MaxTargetsSeen { get => _maxTargetsSeen; private set => _maxTargetsSeen = value; }
        public int Generation { get => _generation; private set => _generation = value; }
        public int IncreaseIterations { get => _increaseIterations; set => _increaseIterations = value; }
        public int IncreaseNumberBalls { get => _increaseNumberBalls; internal set => _increaseNumberBalls = value; }
        public bool DisasterMutate { get => _disasterMutate; internal set => _disasterMutate = value; }
        internal TargetingType Targeting { get => _levels[_currentLevel].Targeting; set => _levels[_currentLevel].Targeting = value; }
        public int NoToChooseForNextGeneration { get => _noToChoose; set => _noToChoose = value; }
        public float SpeedFitnessFactor { get => _speedFitnessFactor; set => _speedFitnessFactor = value; }
        public float LastPercentComplete { get => _lastPercentComplete; set => _lastPercentComplete = value; }

        
        protected abstract NeuMoverBase createMover(float scale, float centerX, float centerY, Point3D start, int id, int seed);

        protected abstract NeuMoverBase createMoverFromPreviousGen(float scale, float centerX, float centerY, Point3D start, float variance, int chance, NeuMoverBase previousGen);

        protected abstract void visualizeMovement(NeuMoverBase current, Point posStart);

        internal virtual void initUiElements()
        {
            initLevel();
            initBalls();

            foreach (var b in _balls)
                b.getUiElements(_newUiElements);

            //_fixedTargets = _levels[_currentLevel].TargetList;
            addRandomTarget(0);
            drawLastTarget();
        }


        public NeuralTrainer(int seed, NeuralSettings neuralSettings, double actualWidth, double actualHeight, SolidColorBrush[] colors, SolidColorBrush trainerColor)
        {
            _actualHeight = actualHeight;
            _actualWidth = actualWidth;

            _rnd = new Random(seed);
            _settings = neuralSettings;

            _colors = colors;
            _color = trainerColor;

            _layerConfig = new int[] { 8, 4, 2 };

            _levels = new List<NeuralTrainerLevel>();
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

        internal virtual void getUiElements(UIElementCollection uiElements)
        {
            foreach (var ball in _balls)
                ball.getUiElements(_newUiElements);

            foreach (var target in _targets)
                drawTarget(target);
        }

        internal void getMeshesToUpdate(ref List<P3dMesh> meshes)
        {
            foreach (var m in _newMeshes)
                meshes.Add(m);

            _newMeshes.Clear();
        }

        internal double getLevelScore()
        {
            //if(_currentLevel == 0)
            //{
            //    return _lastPercentComplete;
            //}

            //return _maxTargetsSeen;
            return _bestFitness;
        }

        private void initBalls()
        {
            var scale = (float)Math.Max(_actualWidth, _actualHeight);

            float centerX = 0.5f * (float)_actualHeight;
            float centerY = 0.5f * (float)_actualWidth;

            var start = _levels[_currentLevel].StartPoint.Value;

            _balls = new NeuMoverBase[_settings.NumberNets + _increaseNumberBalls];

            for (int id = 0; id < _balls.Length; id++)
            {
                var seed = _rnd.Next();
                _balls[id] = createMover(scale, centerX, centerY, start, id, seed);
                _balls[id].setColors(_color, _colors[_rnd.Next(_colors.Length)]);
                _balls[id].getMeshes(_newMeshes);
            }

            _generation++;
            _targets = new List<Point3D>();
        }


        internal virtual int initNextGeneration()
        {
            _generation++;
            _targets = new List<Point3D>();

            addRandomTarget(0);
            drawLastTarget();

            var scale = (float)Math.Max(_actualWidth, _actualHeight);

            float centerX = 0.5f * (float)_actualHeight;
            float centerY = 0.5f * (float)_actualWidth;

            var start = getStartPoint();

            int noPerPrevious = (_settings.NumberNets + _increaseNumberBalls) / _nextGen.Count;
            //_balls = new NeuBall[noPerPrevious * _nextGen.Count];


            var variance = 0.01f * _convergenceEnd / _generation;
            int chance = (int)(199f * ((float)_generation / (float)_convergenceEnd) + 1);

            if (_disasterMutate && _allOfPreviousGenerationDied)
            {
                chance = (int)(0.7 * chance);
                variance = 1.75f * variance;
                _allOfPreviousGenerationDied = false;
            }

            int count = 0;
            var nextGen = new List<NeuMoverBase>();
            foreach (var previousGen in _nextGen)
            {
                previousGen.resetPos(start);
                previousGen.Active = true;
                //_balls[count * noPerPrevious] = previousGen;

                var generationColor = previousGen.SecondaryColor;
                if (_generation % 10 == 0)
                    generationColor = _colors[_rnd.Next(_colors.Length)];

                var stroke = _colors[_rnd.Next(_colors.Length)];

                for (int id = count * noPerPrevious + 1; id < (count + 1) * noPerPrevious; id++)
                {
                    NeuMoverBase ball = createMoverFromPreviousGen(scale, centerX, centerY, start, variance, chance, previousGen);
                    ball.setColors(_color, generationColor);
                    ball.getMeshes(_newMeshes);

                    if (_settings.AnimateOnlyChampions)
                        ball.hide();

                    nextGen.Add(ball);
                }

                count++;
            }


            for (int i = 0; i < _nextGen.Count; i++)
            {
                NeuMoverBase ball = _nextGen[_nextGen.Count - i - 1];
                ball.Champion = true;
                ball.hide(false);
                ball.getUiElements(_newUiElements);
                nextGen.Add(ball);
            }

            nextGen[0].markWinner();
            _balls = nextGen.ToArray();
            _nextGen = null;

            return _maxTargetsSeen;
        }

        private Point3D getStartPoint()
        {
            switch (_levels[_currentLevel].Targeting)
            {
                case TargetingType.Fixed:
                    return _levels[_currentLevel].StartPoint.Value;
                case TargetingType.Circle:
                    return new Point3D(0.5 * _actualWidth, 0, 0.5 * _actualHeight);
                case TargetingType.Near:
                case TargetingType.Far:
                case TargetingType.Alternating:
                default:
                    return getRandomPoint();
            }
        }

        internal void AddLevel(NeuralTrainerLevel level, bool clear = false)
        {
            if (clear)
                _levels.Clear();

            _levels.Add(level);
        }

        internal void getNextIteration(UIElementCollection uiElements, ref string debug)
        {
            if (_balls.Length > 0)
            {
                var timeStart = DateTime.Now;
                int activeCount = doIteration();

                foreach (var e in _newUiElements)
                    uiElements.Add(e);

                _newUiElements.Clear();

                var iterationDuration = DateTime.Now - timeStart;
                debug += string.Format("_________________________________ \n");
                debug += string.Format("Level:\t\t{0} ({1} / {2}) \n", _currentLevel + 1, _levelTries + 1, _levels[_currentLevel].LevelTries);
                debug += string.Format("Generation:\t{0} / {1} \n", _generation, _levels[_currentLevel].GenerationsToComplete * _targetsMax);
                debug += string.Format("Iteration:\t\t{0} / {1} \n", _iteration, _maxIterations);
                debug += string.Format("Active:\t\t{0} / {1} \n", activeCount, _balls.Length);
                debug += string.Format("_________________________________ \n");
                debug += string.Format("Best Fitness:\t{0} \n", _bestFitness);
                debug += string.Format("Completed:\t{0} \n", Math.Round(100 * _lastPercentComplete, 2));
                debug += string.Format("Targets best:\t{0} \n", _targetsMax - 1);
                debug += string.Format("TargetCount:\t{0} \n", _targets.Count - 1);
                debug += string.Format("Time for it:\t{0} \n", iterationDuration.Milliseconds);
                debug += _debug;
                debug += string.Format("_________________________________ \n");
                debug += "\n";
            }
        }

        private int doIteration()
        {
            _iteration++;
            bool levelComplete = false;
            bool maxIterationsReached = _iteration > _maxIterations;

            int activeCount = 0;
            int bestNumTargets = 0;
            for (int id = 0; id < _balls.Length; id++)
            {

                NeuMoverBase current = _balls[id];
                if (current.Active)
                {
                    //if (!maxIterationsReached)
                    activeCount++;

                    var target = _targets[current.TargetCount];

                    var posStart = new Point(current.PosX, current.PosZ);
                    current.doTimeStep(_iteration, target, (float)_actualWidth, (float)_actualHeight);

                    visualizeMovement(current, posStart);

                    if (current.TargetReached)
                    {
                        if (current.TargetCount > _targets.Count - 1 && _targets.Count < _levels[_currentLevel].TargetList.Count)
                        {
                            current.setCurrentStartPos();
                            addRandomTarget(current.TargetCount);
                            drawLastTarget();
                        }

                        if (current.TargetCount > bestNumTargets)
                            bestNumTargets = current.TargetCount;

                        if (current.TargetCount == _currentLevelGoal)
                        {
                            current.Active = false;
                        }

                        if (current.TargetCount > _maxTargetsSeen)
                            _maxTargetsSeen = current.TargetCount;

                        if (maxIterationsReached)
                            current.Active = false;
                    }
                    else if (current.TargetCount < 10 && maxIterationsReached)
                        current.Active = false;
                }
                else if (current.TargetCount == _levels[_currentLevel].TargetList.Count)
                {
                    levelComplete = true;
                }
            }

            if (bestNumTargets >= _levels[_currentLevel].TargetList.Count)
                levelComplete = true;

            if (_currentLevelGoal < _levels[_currentLevel].TargetList.Count)
            {
                _currentLevelGoal++;
            }
            //int noToRestart = _balls.Length / 20;
            int noToRestart = 0;
            _allOfPreviousGenerationDied = activeCount < noToRestart + 1 && !(maxIterationsReached);
            if (activeCount < noToRestart + 1)
            {
                int nSuccess = 0;
                foreach (var b in _balls)
                {
                    if (b.TargetCount >= _levels[_currentLevel].TargetList.Count)
                        nSuccess++;
                }
                _lastPercentComplete = (float)nSuccess / (float)_settings.NumberNets;


                if (levelComplete)
                {
                    _allOfPreviousGenerationDied = false;

                    if (_lastPercentComplete > _levels[_currentLevel].WinPercentage && _currentLevel < _levels.Count - 1)
                        incrementLevel();
                }
                else if (_currentLevel >= 0 && _generation >= _levels[_currentLevel].GenerationsToComplete * (1 + bestNumTargets))
                {
                    if (_currentLevel < 1)
                        initBalls();

                    if (_currentLevel > 0)
                        decrementLevel();

                    initLevel();
                }

                restartIteration();

                if (_allOfPreviousGenerationDied)
                    _debug += "Catastrophic\n";
                else
                {
                    if (levelComplete)
                        _debug += "Level completed\n";
                    else
                        _debug += "Target iterations reached\n";
                }
            }

            return activeCount;
        }

        private void incrementLevel()
        {
            _currentLevel++;
            _levelTries = 0;
            initLevel();
        }

        private void decrementLevel()
        {
            if (_levelTries > _levels[_currentLevel].LevelTries)
            {
                //_currentLevel--;
                _currentLevel = 0;
                _levelTries = 0;
            }

            initLevel();
            _levelTries++;
        }

        private void initLevel()
        {
            _maxIterations = _levels[_currentLevel].MaxIterationsStart;
            _lastPercentComplete = 0;
            _currentLevelGoal = 1;
            _generation = 0;
        }

        internal void setLayerConfig(int[] layerConfig)
        {
            _layerConfig = layerConfig;
        }

        internal void updateSettings(double actualWidth, double actualHeight)
        {
            _actualHeight = actualHeight;
            _actualWidth = actualWidth;

            _maxIterations = _settings.NumberIterationsStart;

            _generation = 0;
            _targetsMax = 0;
            _balls = null;

            if (_levels.Count > 0)
            {
                _maxIterations = _levels[_currentLevel].MaxIterationsStart;

                switch (_settings.Targeting)
                {
                    case "Far":
                        _levels[_currentLevel].Targeting = TargetingType.Far;
                        break;
                    case "Circle":
                        _levels[_currentLevel].Targeting = TargetingType.Circle;
                        break;
                    case "Near":
                    default:
                        _levels[_currentLevel].Targeting = TargetingType.Near;
                        break;
                }
            }
        }

        private void restartIteration()
        {
            _debug = string.Empty;
            increaseMaxIterations();
            _iteration = 0;

            var sorted = new SortedDictionary<float, List<NeuMoverBase>>();
            for (int i = 1; i < _balls.Length; i++)
            {
                float fitness = -_balls[i].getFitness(_speedFitnessFactor);
                if (!sorted.ContainsKey(fitness))
                {
                    sorted[fitness] = new List<NeuMoverBase>();
                }

                sorted[fitness].Add(_balls[i]);
            }

            _bestFitness = float.NegativeInfinity;
            foreach (var entry in sorted)
            {
                _bestFitness = Math.Max(_bestFitness, -entry.Key);
            }

            _nextGen = new List<NeuMoverBase>();

            //if (pickNextGeneration(sorted, noToChoose, true) < noToChoose)
            var noToChooseOld = 20;
            if (_maxTargetsSeen > _increaseIterations && _noToChoose != noToChooseOld)
                noToChooseOld = _noToChoose;

            pickNextGeneration(sorted, noToChooseOld, false);
        }

        private void increaseMaxIterations()
        {
            //if (_levels[_currentLevel].Targeting == TargetingType.Fixed)
            //    _levels[_currentLevel].MaxIterationsEnd = (_fixedTargets.Count - 1) * 100;

            //_maxIterations += _maxIterations / 5;
            //_maxIterations++;
            if (_maxIterations < 50)
                _maxIterations += 5;

            var percIncrease = _disasterMutate ? 15 : 2;
            _maxIterations += percIncrease * _maxIterations / 100;

            if (_increaseIterations > 0 && _maxTargetsSeen > _increaseIterations)
                _maxIterations = Math.Max(_maxIterations, _settings.TurnsToTarget * (_maxTargetsSeen - 1));

            //if (_allOfPreviousGenerationDied)
            //{
            //    _maxIterations *= 2;
            //    _maxIterations /= 3;
            //}

            _maxIterations = Math.Min(_maxIterations, _levels[_currentLevel].MaxIterationsEnd);

            if (_levels[_currentLevel].Targeting == TargetingType.Circle)
                _maxIterations = _settings.NumberIterationsStart;

            _debug += "Last # iterations:\t" + _iteration + "\n";
        }

        private int pickNextGeneration(SortedDictionary<float, List<NeuMoverBase>> sorted, int noToChoose, bool onlyActive)
        {
            int count = 0;

            foreach (var entry in sorted)
            {
                foreach (var b in entry.Value)
                {
                    if (onlyActive && !b.Active)
                        continue;

                    if (!_nextGen.Contains(b))
                    {
                        _nextGen.Add(b);
                        if (!_settings.AnimateOnlyChampions)
                            b.markChampion();

                        b.hide(false);
                        if (++count > noToChoose - 1)
                            break;
                    }
                }

                if (++count > noToChoose - 1)
                    break;
            }

            return count;
        }

        private Point3D getRandomPoint()
        {
            return getRandomPoint(_levels[_currentLevel].Targeting);
        }

        public Point3D getRandomPoint(TargetingType targeting)
        {
            switch (targeting)
            {
                case TargetingType.Far:
                    return getRandomFarPoint();
                case TargetingType.Circle:
                    return getRandomeCirclePoint();
                case TargetingType.Alternating:
                    if (_generation % 2 == 0)
                        return getRandomFarPoint();
                    else
                        return getRandomNearPoint();
                default:
                case TargetingType.Near:
                    return getRandomNearPoint();
            }
        }

        private Point3D getRandomeCirclePoint()
        {
            var f = 0.1 + 0.9 * Math.Min((float)(_iteration + 1) / _levels[_currentLevel].MaxIterationsEnd, 0.4);
            var radius = (0.4 + 0.1 * _rnd.NextDouble()) * Math.Min(_actualHeight, _actualWidth);
            var phi = 360 * _rnd.NextDouble();
            var pX = 0.5 * _actualWidth + radius * Math.Cos(phi);
            var pY = 0.5 * _actualHeight + radius * Math.Sin(phi);

            return new Point3D(pX, 0, pY);
        }

        private Point3D getRandomNearPoint()
        {
            var f = 0.1 + 0.9 * Math.Min((float)(_iteration + 1) / _levels[_currentLevel].MaxIterationsEnd, 0.4);
            var pX = (0.5 + f * (_rnd.NextDouble() - 0.5)) * _actualWidth;
            var pY = (0.5 + f * (_rnd.NextDouble() - 0.5)) * _actualHeight;

            return new Point3D(pX, 0, pY);
        }

        private Point3D getRandomFarPoint()
        {
            var pX = (0.8 * _rnd.NextDouble() + 0.1) * _actualWidth;
            var pY = (0.8 * _rnd.NextDouble() + 0.1) * _actualHeight;
            return new Point3D(pX, 0, pY);
        }

        protected void drawLastTarget()
        {
            var target = _targets[_targets.Count - 1];

            drawTarget(target);
            if (_targets.Count > _targetsMax)
                _targetsMax = _targets.Count;
        }

        protected abstract void drawTarget(Point3D target);

        private void addRandomTarget(int nTarget)
        {
            Point3D target;
            if(_levels[_currentLevel].Targeting == TargetingType.Fixed)
            {
                var targetList = _levels[_currentLevel].TargetList;
                target = targetList[nTarget % targetList.Count];
            }
            else
                target = getRandomPoint();

            _targets.Add(target);
            _maxTargetsSeen = _targets.Count;
        }
    }
}