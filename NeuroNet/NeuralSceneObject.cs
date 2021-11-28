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

            bool cleared = false;
            foreach (var trainer in _trainers)
            {
                if (_pauseOnNextIteration > 0 && trainer.hasNextGen())
                {
                    _pauseOnNextIteration--;

                    if (_pauseOnNextIteration == 0)
                    {
                        uiElements.Clear();
                        if(_history != null)
                            _history.getUiElements(uiElements);

                        cleared = true;
                        break;
                    }
                }
            }

            foreach (var trainer in _trainers)
            {
                if (trainer.hasNextGen())
                {
                    if (_history == null)
                        _history = new NeuHistoryPlot(new Vector(0.11 * _visualGraph.ActualWidth, 0.01 * _visualGraph.ActualHeight), new Vector(0.7 * _visualGraph.ActualWidth, 0.1 * _visualGraph.ActualHeight));

                    _history.addDataPoint(uiElements, trainer.Color, trainer.MaxTargetsSeen);

                    int noTargets = trainer.initNextGeneration(uiElements);

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
                _trainers = new List<NeuralTrainer>();
                _trainers.Add(new NeuralTrainer(0, _settings, _visualGraph.ActualWidth, _visualGraph.ActualHeight, _colors, _colors[0]));
                _trainers.Add(new NeuralTrainer(1, _settings, _visualGraph.ActualWidth, _visualGraph.ActualHeight, _colors, _colors[1]));
                _trainers.Add(new NeuralTrainer(2, _settings, _visualGraph.ActualWidth, _visualGraph.ActualHeight, _colors, _colors[2]));
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

            //_trainers[1].setLayerConfig(new int[] { 8, 8, 8, 8, 8, 4, 2 });
            //_trainers[2].setLayerConfig(new int[] { 8, 128, 2 });

            var centerX = 0.5 * _visualGraph.ActualWidth;
            var centerY = 0.5 * _visualGraph.ActualHeight;

            List<Point> targets0 = new List<Point>();
            Point center = new Point(centerX, centerY);
            targets0.Add(center);
            for (int i = 0; i < 20; i++)
            {
                float px;
                float py;
                _trainers[0].getRandomPoint(out px, out py, i % 2 == 0 ? NeuralTrainer.TargetingType.Near : NeuralTrainer.TargetingType.Far);
                targets0.Add(new Point(px, py));
            }
            var centerPoints = makeCircularTargets(center, 3, 0.15 * _visualGraph.ActualWidth, 1);

            _trainers[0].FixedTargets = makeCircularTargets(centerPoints[1], 4, 0.05 * _visualGraph.ActualWidth, 5);
            _trainers[1].FixedTargets = makeCircularTargets(centerPoints[3], 2, -0.05 * _visualGraph.ActualWidth, 5);
            _trainers[2].FixedTargets = makeCircularTargets(centerPoints[5], 4, -0.05 * _visualGraph.ActualWidth, 5);

            //_trainers[1].NoToChooseForNextGeneration = 5;
            //_trainers[2].SpeedFitnessFactor = 10;

            _trainers[0].IncreaseIterations = 2;
            _trainers[1].IncreaseIterations = 2;
            _trainers[2].IncreaseIterations = 2;
            _trainers[0].Targeting = NeuralTrainer.TargetingType.Fixed;
            _trainers[1].Targeting = NeuralTrainer.TargetingType.Fixed;
            _trainers[2].Targeting = NeuralTrainer.TargetingType.Fixed;
        }

        private static List<Point> makeCircularTargets(Point center, int divisor, double baseRadius, int noCircles = 3)
        {
            double absRadius = Math.Abs(baseRadius);
            int signRadius = Math.Sign(baseRadius);
            var targets2 = new List<Point>();
            targets2.Add(center);
            for (int j = 0; j < noCircles; j++)
            {
                var radius = (j + 1) * absRadius;
                var circumference = 2 * Math.PI * radius;
                var nSegments = (int)(circumference / (10 * NeuMoverBase.Radius));
                nSegments -= (1 - nSegments % 2);
                int step = nSegments / divisor + 1;
                var deltaPhi = step * (2 * Math.PI / nSegments);

                for (int i = 0; i < nSegments; i++)
                {
                    var vecP = new Vector(signRadius * radius * Math.Cos(i * deltaPhi), radius * Math.Sin(i * deltaPhi));
                    targets2.Add(center + vecP);
                    targets2.Add(center);
                }
            }

            return targets2;
        }
    }
}