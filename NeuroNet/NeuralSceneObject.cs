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
        private NeuralTrainer _trainer;
        private NeuralNetDisplay _netDisplay;
        private NeuralSettings _settings;
        private Canvas _visualGraph;
        private Random _rnd;
        private Brush[] _colors;
        private NeuBall[] _balls;

        private int _generation = 0;
        private int _targets = 0;
        private int _targetsMax = 0;

        private int _maxIterationsEnd = 2500;
        private int _maxIterations = 25;
        private int _iteration = 0;
        private int _pauseOnNextIteration = 0;
        private List<NeuBall> _nextGen;

        List<Point> _targetList = new List<Point>();

        public NeuralSceneObject(NeuralSettings neuralSettings, Canvas visualGraph)
        {
            _trainer = new NeuralTrainer(neuralSettings);

            _settings = neuralSettings;
            _visualGraph = visualGraph;

            _rnd = new Random((int)DateTime.Now.Ticks);

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
            initBalls(uiElements);
            initNetDisplay(uiElements);
            drawTarget(uiElements);
        }

        private void drawTarget(UIElementCollection uiElements)
        {
            var radius = 30;
            var ellipse = new Ellipse
            {
                Stroke = Brushes.LightBlue,
                StrokeThickness = 2,

                Width = 2 * radius,
                Height = 2 * radius,
            };

            var target = _targetList[_targetList.Count - 1];
            ellipse.RenderTransform = new TranslateTransform(target.X - radius, target.Y - radius);

            uiElements.Add(ellipse);
            _targets++;
            if (_targets > _targetsMax)
                _targetsMax = _targets;
        }

        private void initNetDisplay(UIElementCollection uiElements)
        {
            if (_balls.Length > 0)
            {
                var layers = _balls[0].Net.Layers;
                var width = 0.1 * _visualGraph.ActualWidth;
                var height = 0.1 * _visualGraph.ActualHeight;

                _netDisplay = new NeuralNetDisplay(layers, width, height);
                _netDisplay.getDrawing(uiElements);
            }
        }



        private void initBalls(UIElementCollection uiElements)
        {
            initBalls();

            for (int id = 0; id < _balls.Length; id++)
            {
                uiElements.Add(_balls[id].Ellipse);
            }
        }

        private void initBalls()
        {
            var scale = (float)Math.Max(_visualGraph.ActualWidth, _visualGraph.ActualHeight);

            float centerX = 0.5f * (float)_visualGraph.ActualHeight;
            float centerY = 0.5f * (float)_visualGraph.ActualWidth;

            float startX;
            float startY;
            getRandomPoint(out startX, out startY);

            _balls = new NeuBall[_settings.NumberNets];

            for (int id = 0; id < _balls.Length; id++)
            {
                var seed = (int)DateTime.Now.Ticks % (id + 1000);
                _balls[id] = new NeuBall(_settings, seed, startX, startY, centerX, centerY, scale);
                _balls[id].setColors(_colors[_rnd.Next(_colors.Length)], _colors[_rnd.Next(_colors.Length)]);
            }

            _generation++;
            _targets = 0;
        }

        private void initBalls(UIElementCollection uiElements, NeuBall[] bestOfPrevious)
        {
            var scale = (float)Math.Max(_visualGraph.ActualWidth, _visualGraph.ActualHeight);

            float centerX = 0.5f * (float)_visualGraph.ActualHeight;
            float centerY = 0.5f * (float)_visualGraph.ActualWidth;

            float startX;
            float startY;
            getRandomPoint(out startX, out startY);

            int noPerPrevious = _balls.Length / bestOfPrevious.Length;
            _balls = new NeuBall[noPerPrevious * bestOfPrevious.Length];

            var variance = 0.02f * _maxIterationsEnd / _maxIterations;
            int chance = (int)(99f * ((float)_generation / (float)_maxIterationsEnd) + 1);

            int count = 0;
            foreach (var previousGen in bestOfPrevious)
            {
                previousGen.resetPos(startX, startY);
                previousGen.Active = true;
                _balls[count * noPerPrevious] = previousGen;

                var fill = previousGen.MainColor;
                if(_generation % 10 == 0)
                    fill = _colors[_rnd.Next(_colors.Length)];

                var stroke = _colors[_rnd.Next(_colors.Length)];

                for (int id = count * noPerPrevious + 1; id < (count + 1) * noPerPrevious; id++)
                {
                    _balls[id] = new NeuBall(_settings, startX, startY, centerX, centerY, scale, previousGen, chance, variance);
                    _balls[id].setColors(stroke, fill);
                    uiElements.Add(_balls[id].Ellipse);
                }

                count++;
            }

            _balls[0].highlight();

            for (int i = 0; i < bestOfPrevious.Length; i++)
                uiElements.Add(bestOfPrevious[bestOfPrevious.Length - i - 1].Ellipse);
        }

        public bool getUIElementsToAdd(ref UIElementCollection uiElements, ref string debug)
        {
            //if (_settings.Render3D)
            //    return false;

            if (_pauseOnNextIteration > 0)
            {
                _pauseOnNextIteration--;

                if (_pauseOnNextIteration == 0 && _nextGen.Count > 0)
                {
                    uiElements.Clear();
                    initBalls(uiElements, _nextGen.ToArray());
                    initNetDisplay(uiElements);
                    drawTarget(uiElements);
                }
                else
                {
                    return true;
                }
            }

            _iteration++;
            debug += string.Format("Generation {0}\nIteration: {1} / {2}\nTargets {3}\nTargets best {4} \nTargetCount {5}", _generation, _iteration, _maxIterations, _targets, _targetsMax, _targetList.Count);

            if (_balls.Length > 0)
            {
                int activeCount = 0;
                for (int id = 0; id < _balls.Length; id++)
                {

                    NeuBall current = _balls[id];
                    if (current.Active)
                    {
                        activeCount++;

                        //float distY = (float)((_targetY - _balls[id].PosY));
                        //float distX = (float)((_targetX - _balls[id].PosX));
                        //float distSquared = distX * distX + distY * distY;

                        var targetX = (float)_targetList[current.TargetCount].X;
                        var targetY = (float)_targetList[current.TargetCount].Y;
                        current.doTimeStep(_iteration, targetX, targetY, (float)_visualGraph.ActualWidth, (float)_visualGraph.ActualHeight);
                        //_balls[id].Fitness += distSquared * weight;

                        if (current.TargetReached)
                        {
                            //setRandomTarget();
                            if (current.TargetCount > _targetList.Count - 1)
                            {
                                addRandomTarget();
                                drawTarget(uiElements);
                            }
                        }
                    }
                }

                int noToChoose = _balls.Length / 10;
                if (_iteration > _maxIterations || activeCount < noToChoose)
                {
                    restartIteration();
                }

                var net = _balls[0].Net;

                var neurons = net.Neurons;

                _netDisplay.drawNeurons(net, neurons);
            }

            return true;
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
                if (!sorted.ContainsKey(-_balls[i].Fitness))
                    sorted.Add(-_balls[i].Fitness, _balls[i]);
            }

            int noToChoose = _balls.Length / 10;
            _nextGen = new List<NeuBall>();

            if (pickNextGeneration(sorted, noToChoose, true) < noToChoose)
                pickNextGeneration(sorted, noToChoose, false);

            if (_settings.PauseOnGeneration)
                _pauseOnNextIteration = 10;
            else
                _pauseOnNextIteration = 1;

            _generation++;
            _targets = 0;
            _targetList = new List<Point>();
            addRandomTarget();
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

        private void addRandomTarget()
        {
            float px;
            float py;
            getRandomPoint(out px, out py);
            _targetList.Add(new Point(px, py));
        }

        private void getRandomPoint(out float pX, out float pY)
        {
            if (_settings.RandomTargets && _generation % 2 == 0)
            {
                pX = (float)((0.8 * _rnd.NextDouble() + 0.1) * _visualGraph.ActualWidth);
                pY = (float)((0.8 * _rnd.NextDouble() + 0.1) * _visualGraph.ActualHeight);
            }
            else
            {
                var f = Math.Min((float)(_iteration + 1) / 500, 0.4);
                pX = (float)((0.5 + f * (_rnd.NextDouble() - 0.5)) * _visualGraph.ActualWidth);
                pY = (float)((0.5 + f * (_rnd.NextDouble() - 0.5)) * _visualGraph.ActualHeight);
            }
        }

        public void updateSettings()
        {
            _maxIterations = _settings.NumberIterationsStart;
            _generation = 0;
            _targetsMax = 0;
            _balls = null;
            addRandomTarget();

            //    _nets = new NeuralNet[_settings.NumberNets];
            //    for (int i = 0; i < _settings.NumberNets; i++)
            //        _nets[i] = new NeuralNet(i, _settings);
        }
    }
}