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
        private NeuralNetDisplay _netDisplay;
        private NeuralSettings _settings;
        private Canvas _visualGraph;
        private Brush[] _colors;
        private int _pauseOnNextIteration = 1;
        private bool _trainerNeedsInit = true;

        public NeuralSceneObject(NeuralSettings neuralSettings, Canvas visualGraph)
        {
            _settings = neuralSettings;
            _visualGraph = visualGraph;

            _colors = new Brush[]
            {
                Brushes.Blue,
                Brushes.LightBlue,
                Brushes.DarkBlue,
                Brushes.BlueViolet,
                Brushes.AliceBlue,
                Brushes.DarkRed,
                Brushes.DarkGreen,
                Brushes.Yellow
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
            var net = _trainers[0].getActiveNet();
            if (net != null)
            {
                var layers = net.Layers;
                var width = 0.1 * _visualGraph.ActualWidth;
                var height = 0.1 * _visualGraph.ActualHeight;

                _netDisplay = new NeuralNetDisplay(layers, width, height);
                _netDisplay.getDrawing(uiElements);
            }
        }

        public bool getUIElementsToAdd(ref UIElementCollection uiElements, ref string debug)
        {
            //if (_settings.Render3D)
            //    return false;

            foreach (var trainer in _trainers)
            {
                if (_pauseOnNextIteration > 0 && trainer.hasNextGen())
                {
                    _pauseOnNextIteration--;

                    if (_pauseOnNextIteration == 0)
                    {
                        uiElements.Clear();
                        trainer.initNextGeneration(uiElements);
                        initNetDisplay(uiElements);

                        if (_settings.PauseOnGeneration)
                            _pauseOnNextIteration = 10;
                        else
                            _pauseOnNextIteration = 1;
                    }
                    else
                    {
                        return true;
                    }
                }

                trainer.getNextIteration(uiElements, ref debug);
            }

            _netDisplay.drawNeurons(_trainers[0].getActiveNet());

            return true;
        }

        public void updateSettings()
        {
            if (_trainers == null)
            {
                _trainers = new List<NeuralTrainer>();
                _trainers.Add(new NeuralTrainer(_settings, _visualGraph.ActualWidth, _visualGraph.ActualHeight, _colors));
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