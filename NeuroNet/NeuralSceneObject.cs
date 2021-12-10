using Power3D;
using Power3DBuilder.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace NeuroNet
{
    internal class NeuralSceneObject : IP3bSceneObject
    {
        private List<NeuralTrainer> _trainers = null;
        private List<NeuralNetDisplay> _netDisplays;
        private NeuralSettings _settings;
        private Canvas _visualGraph;
        private Brush[] _colors;
        private int _pauseOnNextIteration = 1;
        private bool _trainerNeedsInit = true;
        private NeuHistoryPlot _history;

        public NeuralSceneObject(NeuralSettings neuralSettings, Canvas visualGraph)
        {
            _settings = neuralSettings;
            _visualGraph = visualGraph;

            _colors = new Brush[]
            {
                Brushes.DarkGreen,
                Brushes.DarkRed,
                Brushes.Yellow,
                Brushes.LightBlue,
                Brushes.DarkBlue,
                Brushes.BlueViolet,
                Brushes.AliceBlue,
                Brushes.Blue,
            };
        }

        public void get2dDrawing(double width, double height, Transform3D cameraProjection, ref List<FrameworkElement> shapes, ref string debug)
        {
        }

        public string getDebugInfo(CallBackType info)
        {
            return "No debug info";
        }

        public void getMeshes(ref P3dColoredMeshCollection meshes, ref string debug)
        {
        }

        public bool getMeshesToUpdate(ref List<P3dMesh> meshes, ref string debug)
        {
            return false;
        }

        public IP3bSetting getSettings()
        {
            return _settings;
        }

        public void getUIElements(ref UIElementCollection uiElements, ref string debug)
        {
            if (_trainerNeedsInit)
            {
                updateSettings();

                foreach (var trainer in _trainers)
                {
                    trainer.init(uiElements);
                }

                initNetDisplay(uiElements);
                _trainerNeedsInit = false;
            }
        }

        private void initNetDisplay(UIElementCollection uiElements)
        {
            var width = 0.1 * _visualGraph.ActualWidth;
            var height = 0.1 * _visualGraph.ActualHeight;
            _netDisplays = new List<NeuralNetDisplay>();
            for (int i = 0; i < _trainers.Count; i++)
            {
                var net = _trainers[i].getActiveNet();
                var offset = i * (height + 10);
                if (net != null)
                {
                    var layers = net.Layers;

                    NeuralNetDisplay netDisplay = new NeuralNetDisplay(layers, width, height, offset, _trainers[i].Color);
                    netDisplay.getDrawing(uiElements);
                    _netDisplays.Add(netDisplay);
                }
            }
        }

        public bool getUIElementsToAdd(ref UIElementCollection uiElements, ref string debug)
        {
            //if (_settings.Render3D)
            //    return false;

            bool cleared = uiElements.Count < 2;
            if (!cleared)
            {
                foreach (var trainer in _trainers)
                {
                    if (_pauseOnNextIteration > 0 && trainer.hasNextGen())
                    {
                        _pauseOnNextIteration--;

                        if (_pauseOnNextIteration == 0)
                        {
                            uiElements.Clear();
                            cleared = true;
                            break;
                        }
                    }
                }
            }

            if (cleared)
            {
                if (_history != null)
                    _history.getUiElements(uiElements);
            }

            foreach (var trainer in _trainers)
            {
                if (trainer.hasNextGen())
                {
                    if (_history == null)
                        _history = new NeuHistoryPlot(new Vector(0.11 * _visualGraph.ActualWidth, 0.01 * _visualGraph.ActualHeight), new Vector(0.7 * _visualGraph.ActualWidth, 0.1 * _visualGraph.ActualHeight));

                    _history.addDataPoint(uiElements, trainer.Color, trainer.getLevelScore());
                    
                    trainer.initNextGeneration(uiElements);

                    initNetDisplay(uiElements);

                    if (_settings.PauseOnGeneration)
                        _pauseOnNextIteration = 10;
                    else
                        _pauseOnNextIteration = 1;
                }
                else if(cleared)
                {
                    trainer.getUiElements(uiElements);
                }
            }

            foreach (var trainer in _trainers)
            {
                trainer.getNextIteration(uiElements, ref debug);
            }

            for(int i = 0; i < _trainers.Count; i++)
                _netDisplays[i].drawNeurons(_trainers[i].getActiveNet());

            return true;
        }

        public void updateSettings()
        {
            if (_trainers == null)
            {
                var rnd = new Random();
                _trainers = new List<NeuralTrainer>();
                _trainers.Add(new NeuralTrainer(rnd.Next(), _settings, _visualGraph.ActualWidth, _visualGraph.ActualHeight, _colors, _colors[0]));
                _trainers.Add(new NeuralTrainer(rnd.Next(), _settings, _visualGraph.ActualWidth, _visualGraph.ActualHeight, _colors, _colors[1]));
                _trainers.Add(new NeuralTrainer(rnd.Next(), _settings, _visualGraph.ActualWidth, _visualGraph.ActualHeight, _colors, _colors[2]));
            }

            if (_settings.GoalTargetIterations.Changed ||
                _settings.NumberIterationsStart.Changed ||
                _settings.NumberNets.Changed ||
                _settings.TurnsToTarget.Changed)
            {
                foreach (var trainer in _trainers)
                {
                    trainer.updateSettings(_visualGraph.ActualWidth, _visualGraph.ActualHeight);
                }

                if (_history != null)
                    _history.reset();

                _trainerNeedsInit = true;

                _settings.GoalTargetIterations.Changed = false;
                _settings.NumberIterationsStart.Changed = false;
                _settings.NumberNets.Changed = false;
                _settings.TurnsToTarget.Changed = false;
            }


            _trainers[0].DisasterMutate = true;
            _trainers[1].DisasterMutate = true;
            _trainers[2].DisasterMutate = true;

            //_trainers[1].IncreaseNumberBalls = -100;
            //_trainers[2].IncreaseNumberBalls = 200;

            _trainers[1].setLayerConfig(new int[] { 8, 8, 8, 8, 8, 4, 2 });
            _trainers[2].setLayerConfig(new int[] { 8, 128, 2 });

            var centerX = 0.5 * _visualGraph.ActualWidth;
            var centerY = 0.5 * _visualGraph.ActualHeight;

            Point center = new Point(centerX, centerY);
            var centerPoints = makeCircularTargets(center, 1, 0.15 * _visualGraph.ActualWidth, 1, _trainers.Count);

            setupLevels(_trainers[0], centerPoints[0]);
            setupLevels(_trainers[1], centerPoints[1]);
            setupLevels(_trainers[2], centerPoints[2]);

            //_trainers[1].NoToChooseForNextGeneration = 5;
            //_trainers[2].SpeedFitnessFactor = 10;

            _trainers[0].IncreaseIterations = 1;
            _trainers[1].IncreaseIterations = 1;
            _trainers[2].IncreaseIterations = 1;
            _trainers[0].SpeedFitnessFactor = 5;
            _trainers[1].SpeedFitnessFactor = 5;
            _trainers[2].SpeedFitnessFactor = 5;
            _trainers[0].Targeting = TargetingType.Fixed;
            _trainers[1].Targeting = TargetingType.Fixed;
            _trainers[2].Targeting = TargetingType.Fixed;
        }

        private static void setupLevels(NeuralTrainer neuralTrainer, Point centerPoint)
        {
            //var levelStart = new NeuralTrainerLevel();
            //levelStart.MaxIterationsStart = 50;
            //levelStart.MaxIterationsEnd = 52;
            //levelStart.GenerationsToComplete = 15;
            //levelStart.TargetList = new List<Point> { centerPoint };
            //neuralTrainer.AddLevel(levelStart, true);

            var level = new NeuralTrainerLevel();
            level.MaxIterationsStart = 25;
            level.GenerationsToComplete = 20;
            level.Targeting = TargetingType.Fixed;
            level.TargetList = makeCircularTargets(centerPoint, 2, 5 * NeuMoverBase.Radius, 1, 3);
            level.StartPoint = centerPoint;
            level.LevelTries = 1;
            const int iterationPerTarget = 150;
            level.MaxIterationsEnd = iterationPerTarget * level.TargetList.Count;
            neuralTrainer.AddLevel(level);

            level = new NeuralTrainerLevel();
            level.MaxIterationsStart = 200;
            level.GenerationsToComplete = 15;
            level.Targeting = TargetingType.Fixed;
            level.TargetList = makeCircularTargets(centerPoint, 4, -8 * NeuMoverBase.Radius, 1);
            level.StartPoint = centerPoint;
            level.LevelTries = 2;
            level.MaxIterationsEnd = iterationPerTarget * level.TargetList.Count;
            neuralTrainer.AddLevel(level);

            level = new NeuralTrainerLevel();
            level.MaxIterationsStart = 250;
            level.GenerationsToComplete = 20;
            level.Targeting = TargetingType.Fixed;
            level.TargetList = makeCircularTargets(centerPoint, 4, 12 * NeuMoverBase.Radius, 1);
            level.StartPoint = centerPoint;
            level.MaxIterationsEnd = iterationPerTarget * level.TargetList.Count;
            level.LevelTries = 3;
            neuralTrainer.AddLevel(level);

            level = new NeuralTrainerLevel();
            level.MaxIterationsStart = 250;
            level.GenerationsToComplete = 20;
            level.Targeting = TargetingType.Fixed;
            level.TargetList = makeCircularTargets(centerPoint, 4, 18 * NeuMoverBase.Radius, 1);
            level.StartPoint = centerPoint;
            level.MaxIterationsEnd = iterationPerTarget * level.TargetList.Count;
            level.LevelTries = 5;
            neuralTrainer.AddLevel(level);

            level = new NeuralTrainerLevel();
            level.MaxIterationsStart = 250;
            level.GenerationsToComplete = 300;
            level.Targeting = TargetingType.Fixed;
            level.TargetList = makeCircularTargets(centerPoint, 2, 6 * NeuMoverBase.Radius, 4);
            level.StartPoint = centerPoint;
            level.MaxIterationsEnd = iterationPerTarget * level.TargetList.Count;
            neuralTrainer.AddLevel(level);
        }

        private static List<Point> makeCircularTargets(Point center, int divisor, double baseRadius, int noCircles = 3, int nSegments = -1)
        {
            double absRadius = Math.Abs(baseRadius);
            int signRadius = Math.Sign(baseRadius);
            var targets2 = new List<Point>();
            //targets2.Add(center);

            bool determineSegments = nSegments < 0;
            for (int j = 0; j < noCircles; j++)
            {
                var radius = (j + 1) * absRadius;
                var circumference = 2 * Math.PI * radius;

                if(determineSegments)
                {
                    nSegments = (int)(circumference / (10 * NeuMoverBase.Radius));
                    nSegments -= (1 - nSegments % 2);
                }

                int step = nSegments / divisor + 1;
                var deltaPhi = step * (2 * Math.PI / nSegments);

                for (int i = 0; i < nSegments + 1; i++)
                {
                    var vecP = new Vector(signRadius * radius * Math.Cos(i * deltaPhi), radius * Math.Sin(i * deltaPhi));
                    targets2.Add(center + vecP);
                    //targets2.Add(center);
                }
            }

            return targets2;
        }

        public void addModels(List<P3DModelVisual3D> models)
        {
        }
    }
}