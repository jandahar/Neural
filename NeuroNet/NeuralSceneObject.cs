﻿using Power3D;
using Power3DBuilder.Models;
using System;
using System.Collections.Generic;
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
        private NeuralNet[] _nets;
        private Point[][] _positions;
        private Ellipse[][] _neurons;
        private NeuBall[] _balls;

        private int _maxIterationsStart = 25;
        private int _maxIterationsEnd = 1000;
        private int _maxIterations = 25;
        private int _iteration = 0;
        private double _targetX;
        private double _targetY;

        public NeuralSceneObject(NeuralSettings neuralSettings, Canvas visualGraph)
        {
            _settings = neuralSettings;
            _visualGraph = visualGraph;
        }

        public void get2dDrawing(double width, double height, Transform3D cameraProjection, ref List<FrameworkElement> shapes, ref string debug)
        {
        }

        internal void setNets(NeuralNet[] nets)
        {
            _nets = nets;
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
            drawGoalLines(uiElements);
        }

        private void drawGoalLines(UIElementCollection uiElements)
        {
            var line = new Line
            {
                Stroke = Brushes.LightBlue,
                StrokeThickness = 2,
                X1 = 0,
                X2 = _visualGraph.ActualWidth,
                Y1 = _targetY,
                Y2 = _targetY,
            };
            uiElements.Add(line);
            
            line = new Line
            {
                Stroke = Brushes.LightBlue,
                StrokeThickness = 2,
                X1 = _targetX,
                X2 = _targetX,
                Y1 = 0,
                Y2 = _visualGraph.ActualHeight,
            };
            uiElements.Add(line);
        }

        private void initNetDisplay(UIElementCollection uiElements)
        {
            if (_nets.Length > 0)
            {
                var layers = _nets[0].Layers;

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
            _targetY = 0.5 * _visualGraph.ActualHeight;
            _targetX = 0.25 * _visualGraph.ActualWidth;

            float startX = (float)(0.5f * _visualGraph.ActualWidth);
            float startY = (float)(0.9f * _visualGraph.ActualHeight);

            _balls = new NeuBall[_nets.Length];

            for (int id = 0; id < _nets.Length; id++)
            {
                _nets[id] = new NeuralNet(id, _settings);
                var net = _nets[id];
                _balls[id] = new NeuBall(startX, startY, net);

                uiElements.Add(_balls[id].Ellipse);

                var input = new float[net.Layers[0]];
                for (int i = 0; i < input.Length; i++)
                    input[i] = net.getRandomInit();
            }
        }

        private void initBalls(UIElementCollection uiElements, NeuBall previousGen, NeuBall previousBestDist)
        {
            float startX = (float)(0.5f * _visualGraph.ActualWidth);
            float startY = (float)(0.9f * _visualGraph.ActualHeight);

            _balls = new NeuBall[_nets.Length];
            previousGen.resetPos(startX, startY);
            previousGen.Ellipse.Stroke = Brushes.Red;
            previousGen.Active = true;
            _balls[0] = previousGen;

            previousBestDist.resetPos(startX, startY);
            previousBestDist.Ellipse.Stroke = Brushes.Green;
            previousBestDist.Active = true;
            _balls[1] = previousBestDist;

            var idHalf = _balls.Length / 2;
            var variance = 0.02f * _maxIterationsEnd / _maxIterations;
            for (int id = 2; id < idHalf; id++)
            {
                _balls[id] = new NeuBall(startX, startY, previousGen, 1, variance);
                uiElements.Add(_balls[id].Ellipse);
            }

            for (int id = idHalf; id < _balls.Length; id++)
            {
                _balls[id] = new NeuBall(startX, startY, previousBestDist, 1, variance);
                uiElements.Add(_balls[id].Ellipse);
            }

            uiElements.Add(previousGen.Ellipse);
            if(previousGen != previousBestDist)
                uiElements.Add(previousBestDist.Ellipse);
        }

        public bool getUIElementsToAdd(ref UIElementCollection uiElements, ref string debug)
        {
            _iteration++;
            debug += string.Format("\nIteration: {0} / {1}\n", _iteration, _maxIterations);

            if (_nets.Length > 0)
            {
                var weight = (float)_iteration / (float)_maxIterations;
                int countActive = 0;
                for (int id = 0; id < _balls.Length; id++)
                {

                    if (_balls[id].Active)
                    {
                        float distY = (float)((_targetY - _balls[id].PosY) / _visualGraph.ActualHeight);
                        float distX = (float)((_targetX - _balls[id].PosX) / _visualGraph.ActualWidth);
                        float distSquared = distX * distX + distY * distY;

                        _balls[id].doTimeStep(distX, distY);
                        //if (_balls[id].PosX < 0 || _balls[id].PosX > _visualGraph.ActualWidth ||
                        //    _balls[id].PosY < 0 || _balls[id].PosY > _visualGraph.ActualHeight)
                        //{
                        //    _balls[id].Active = false;
                        //    _balls[id].hide();

                        //    _balls[id].Fitness += 1e6f;
                        //    _balls[id].Fitness *= 1e6f;
                        //}
                        //else
                        {
                            countActive++;
                            _balls[id].Fitness += distSquared * weight;
                        }
                    }

                    if(countActive == 0 || _iteration > _maxIterations)
                    {
                        _maxIterations += _maxIterations / 10;
                        _maxIterations = Math.Min(_maxIterations, _maxIterationsEnd);

                        _iteration = 0;
                        uiElements.Clear();

                        NeuBall best = _balls[0];

                        float bestDistY = (float)((_targetY - _balls[0].PosY) / _visualGraph.ActualHeight);
                        float bestDistX = (float)((_targetX - _balls[0].PosX) / _visualGraph.ActualWidth);
                        float bestDistSquared = bestDistX * bestDistX + bestDistY * bestDistY;
                        var bestDist = best;
                        for (int i = 1; i < _balls.Length; i++)
                        {
                            float distY = (float)((_targetY - _balls[i].PosY) / _visualGraph.ActualHeight);
                            float distX = (float)((_targetX - _balls[i].PosX) / _visualGraph.ActualWidth);
                            float distSquared = distX * distX + distY * distY;

                            if (_balls[i].Fitness < best.Fitness)
                                best = _balls[i];

                            if(distSquared < bestDistSquared && _balls[i] != best)
                            {
                                bestDist = _balls[i];
                                bestDistSquared = distSquared;
                            }
                        }
                        initBalls(uiElements, best, bestDist);
                        initNetDisplay(uiElements);
                        drawGoalLines(uiElements);
                    }
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

        public void updateSettings()
        {
            _maxIterations = _maxIterationsStart;
            _nets = new NeuralNet[_settings.NumberNets];
            for (int i = 0; i < _settings.NumberNets; i++)
                _nets[i] = new NeuralNet(i, _settings);
        }
    }
}