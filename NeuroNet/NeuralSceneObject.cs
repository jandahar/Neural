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
                Brushes.Blue,
                Brushes.DarkRed,
                Brushes.Yellow,
                Brushes.LightBlue,
                Brushes.DarkBlue,
                Brushes.BlueViolet,
                Brushes.AliceBlue,
                Brushes.DarkGreen,
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
                    int noTargets = trainer.initNextGeneration(uiElements);

                    if (_history == null)
                        _history = new NeuHistoryPlot(new Vector(0.11 * _visualGraph.ActualWidth, 0.01 * _visualGraph.ActualHeight), new Vector(0.7 * _visualGraph.ActualWidth, 0.1 * _visualGraph.ActualHeight));

                    _history.addDataPoint(uiElements, trainer.Color, trainer.MaxTargetsSeen);

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

                _trainers[0].IncreaseIterations = 3;
                _trainers[1].IncreaseIterations = 4;
                _trainers[2].IncreaseIterations = 5;
                //_trainers[1].setLayerConfig(new int[] { 8, 8, 8, 4, 2 });
                //_trainers[2].setLayerConfig(new int[] { 8, 33, 2 });
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

                _trainerNeedsInit = true;

                _settings.GoalTargetIterations.Changed = false;
                _settings.NumberIterationsStart.Changed = false;
                _settings.NumberNets.Changed = false;
                _settings.TurnsToTarget.Changed = false;
            }
        }
    }
}