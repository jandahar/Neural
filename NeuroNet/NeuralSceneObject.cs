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
        private NeuralSettings _settings;
        private Canvas _visualGraph;
        private Random _rnd;
        private Brush[] _colors;
        private Point[][] _positions;
        private Ellipse[][] _neurons;
        private NeuBall[] _balls;

        private int _generation = 0;
        private int _targets = 0;
        private int _targetsMax = 0;

        private int _maxIterationsEnd = 1000;
        private int _maxIterations = 25;
        private int _iteration = 0;
        private float _targetX;
        private float _targetY;
        private int _pauseOnNextIteration = 0;
        private List<NeuBall> _nextGen;

        List<Point> _targetList = new List<Point>();

        public NeuralSceneObject(NeuralSettings neuralSettings, Canvas visualGraph)
        {
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
            //var line = new Line
            //{
            //    Stroke = Brushes.LightBlue,
            //    StrokeThickness = 2,
            //    X1 = 0,
            //    X2 = _visualGraph.ActualWidth,
            //    Y1 = _targetY,
            //    Y2 = _targetY,
            //};
            //uiElements.Add(line);

            //line = new Line
            //{
            //    Stroke = Brushes.LightBlue,
            //    StrokeThickness = 2,
            //    X1 = _targetX,
            //    X2 = _targetX,
            //    Y1 = 0,
            //    Y2 = _visualGraph.ActualHeight,
            //};
            //uiElements.Add(line);

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

                var color = Brushes.Yellow;
                var nodeDiameter = 20;
                //var width = 0.8 * _visualGraph.ActualWidth;
                //var height = 0.8 * _visualGraph.ActualHeight;
                //var offX = 0.1 * _visualGraph.ActualWidth;
                //var offY = 0.1 * _visualGraph.ActualHeight;

                var width = 0.1 * _visualGraph.ActualWidth;
                var height = 0.1 * _visualGraph.ActualHeight;
                var offX = 0.0;//.1 * _visualGraph.ActualWidth;
                var offY = 0.0;//0.1 * _visualGraph.ActualHeight;
                var offYMiddle = offY + 0.5 * height - nodeDiameter;

                var bounding = new Rectangle
                {
                    Stroke = color,
                    StrokeThickness = 3,
                    Width = width,
                    Height = height
                };
                bounding.RenderTransform = new TranslateTransform(offX, offY);
                uiElements.Add(bounding);

                var spacingX = (width - 2 * nodeDiameter) / (layers.Length - 1);

                _positions = new Point[layers.Length][];
                _neurons = new Ellipse[layers.Length][];

                for (int i = 0; i < layers.Length; i++)
                {
                    _positions[i] = new Point[layers[i]];
                    _neurons[i] = new Ellipse[layers[i]];

                    var posX = offX + i * spacingX;

                    var spacingY = (height - 2 * nodeDiameter) / (layers[i] - 1);
                    var realOffY = offY;
                    if (layers[i] == 1)
                    {
                        spacingY = 0;
                        realOffY = offYMiddle;
                    }

                    for (int j = 0; j < layers[i]; j++)
                    {
                        var posY = realOffY + j * spacingY;
                        Ellipse e = new Ellipse
                        {
                            Stroke = color,
                            Fill = color,
                            StrokeThickness = 5,
                        };
                        e.Width = nodeDiameter;
                        e.Height = nodeDiameter;

                        _positions[i][j] = new Point(posX, posY);
                        _neurons[i][j] = e;
                        e.RenderTransform = new TranslateTransform(posX + nodeDiameter / 2, posY + nodeDiameter / 2);
                        uiElements.Add(e);
                    }
                }
            }
        }

        private void initBalls(UIElementCollection uiElements)
        {
            var scale = (float)Math.Max(_visualGraph.ActualWidth, _visualGraph.ActualHeight);

            float centerX = 0.5f * (float)_visualGraph.ActualHeight;
            float centerY = 0.5f * (float)_visualGraph.ActualWidth;

            _targetY = centerX;
            _targetX = centerY;

            float startX;
            float startY;
            getRandomPoint(out startX, out startY);

            _balls = new NeuBall[_settings.NumberNets];

            for (int id = 0; id < _balls.Length; id++)
            {
                var seed = (int)DateTime.Now.Ticks % (id + 1000);
                _balls[id] = new NeuBall(_settings, seed, startX, startY, centerX, centerY, scale);
                _balls[id].setColors(_colors[_rnd.Next(_colors.Length)], _colors[_rnd.Next(_colors.Length)]);
                uiElements.Add(_balls[id].Ellipse);

                //var input = new float[net.Layers[0]];
                //for (int i = 0; i < input.Length; i++)
                //    input[i] = net.getRandomInit();
            }

            _generation++;
            _targets = 0;
        }


        private void initBalls(UIElementCollection uiElements, NeuBall previousGen)
        {
            var scale = (float)Math.Max(_visualGraph.ActualWidth, _visualGraph.ActualHeight);
            //float startX = (float)((0.8 * _rnd.NextDouble() + 0.1) * _visualGraph.ActualWidth);
            //float startY = (float)((0.8 * _rnd.NextDouble() + 0.1) * _visualGraph.ActualHeight);

            //float startX = (float)(0.5 * _visualGraph.ActualWidth);
            //float startY = (float)(0.9 * _visualGraph.ActualHeight);
            float centerX = 0.5f * (float)_visualGraph.ActualHeight;
            float centerY = 0.5f * (float)_visualGraph.ActualWidth;

            float startX;
            float startY;
            getRandomPoint(out startX, out startY);
            setRandomTarget();

            _balls = new NeuBall[_settings.NumberNets];
            previousGen.resetPos(startX, startY);
            previousGen.Ellipse.Stroke = Brushes.Red;
            previousGen.Active = true;
            _balls[0] = previousGen;

            var variance = 0.02f * _maxIterationsEnd / _maxIterations;
            for (int id = 1; id < _balls.Length; id++)
            {
                _balls[id] = new NeuBall(_settings, startX, startY, centerX, centerY, scale, previousGen, 1, variance);
                uiElements.Add(_balls[id].Ellipse);
            }

            uiElements.Add(previousGen.Ellipse);

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
            setRandomTarget();

            int noPerPrevious = _balls.Length / bestOfPrevious.Length;
            _balls = new NeuBall[noPerPrevious * bestOfPrevious.Length];

            var variance = 0.02f * _maxIterationsEnd / _maxIterations;

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
                    _balls[id] = new NeuBall(_settings, startX, startY, centerX, centerY, scale, previousGen, 1, variance);
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
                for (int id = 0; id < _balls.Length; id++)
                {

                    NeuBall current = _balls[id];
                    if (current.Active)
                    {
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
                            if(current.TargetCount > _targetList.Count - 1)
                            {
                                addRandomTarget();
                                drawTarget(uiElements);
                            }
                        }
                    }
                }

                if (_iteration > _maxIterations)
                {
                    restartIteration(uiElements);
                }

                var net = _balls[0].Net;

                var neurons = net.Neurons;

                for (int i = 0; i < neurons.Length; i++)
                {
                    for (int j = 0; j < net.Layers[i]; j++)
                    {
                        if (neurons[i][j] > 0)
                            _neurons[i][j].Stroke = Brushes.Green;
                        else
                            _neurons[i][j].Stroke = Brushes.Red;
                    }
                }
            }

            return true;
        }

        private void restartIteration(UIElementCollection uiElements)
        {
            //_maxIterations += _maxIterations / 5;
            _maxIterations++;
            if (_maxIterations > 100)
                _maxIterations++;

            _maxIterations = Math.Min(_maxIterations, _maxIterationsEnd);

            _iteration = 0;

            NeuBall best = _balls[0];

            var sorted = new SortedDictionary<float, NeuBall>();
            for (int i = 1; i < _balls.Length; i++)
            {
                if (!sorted.ContainsKey(-_balls[i].Fitness))
                    sorted.Add(-_balls[i].Fitness, _balls[i]);
            }

            int noToChoose = _balls.Length / 10;
            _nextGen = new List<NeuBall>();

            var count = 0;
            foreach (var entry in sorted)
            {
                _nextGen.Add(entry.Value);
                entry.Value.Ellipse.Stroke = Brushes.Blue;
                entry.Value.Ellipse.Fill = Brushes.Red;
                if (++count > noToChoose - 1)
                    break;
            }

            //initBalls(uiElements, best);

            if (_settings.PauseOnGeneration)
                _pauseOnNextIteration = 10;
            else
                _pauseOnNextIteration = 1;

            _generation++;
            _targets = 0;
            _targetList = new List<Point>();
            addRandomTarget();
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
            if (_settings.RandomTargets)
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

        private void setRandomTarget()
        {
            if (_settings.RandomTargets)
            {
                _targetX = (float)((0.8 * _rnd.NextDouble() + 0.1) * _visualGraph.ActualWidth);
                _targetY = (float)((0.8 * _rnd.NextDouble() + 0.1) * _visualGraph.ActualHeight);
            }
            else
            {
                var f = Math.Min((float)_iteration / 100, 0.4);
                _targetX = (float)((0.5 + f * (_rnd.NextDouble() - 0.5)) * _visualGraph.ActualWidth);
                _targetY = (float)((0.5 + f * (_rnd.NextDouble() - 0.5)) * _visualGraph.ActualHeight);
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