using Power3D;
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
        private NeuBall _ball;
        private Ellipse _ballEllipse;

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
            if (_nets.Length > 0)
            {
                _ball = new NeuBall(_visualGraph.ActualWidth / 2, _visualGraph.ActualHeight / 2);

                _ballEllipse = new Ellipse
                {
                    Stroke = Brushes.Blue,
                    Fill = Brushes.Blue,
                    StrokeThickness = 5,
                    Width = 20,
                    Height = 20,
                };

                _ballEllipse.RenderTransform = new TranslateTransform(_ball.PosX, _ball.PosY);
                uiElements.Add(_ballEllipse);

                var net = _nets[0];

                var layers = net.Layers;

                var input = new float[net.Layers[0]];
                for (int i = 0; i < input.Length; i++)
                    input[i] = net.getRandomInit();

                var output = net.FeedForward(input);

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

                for(int i = 0; i < layers.Length; i++)
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

                    for(int j = 0; j < layers[i]; j++)
                    {
                        if (i == layers.Length - 1 && output[j] < 0)
                        {
                            color = Brushes.Red;
                        }

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
                        e.RenderTransform = new TranslateTransform(posX + nodeDiameter/2, posY + nodeDiameter/2);
                        uiElements.Add(e);
                    }
                }
            }
        }

        public bool getUIElementsToAdd(ref UIElementCollection uIElements, ref string debug)
        {
            _ball.doTimeStep();
            _ballEllipse.RenderTransform = new TranslateTransform(_ball.PosX, _ball.PosY);
            if (_nets.Length > 0)
            {
                var net = _nets[0];

                var input = new float[net.Layers[0]];
                input[0] = (float)_ball.PosY;
                input[1] = (float)_ball.VelY;

                var output = net.FeedForward(input);
                _ball.setAccelY(output[0]);

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
            _nets = new NeuralNet[_settings.NumberNets];
            _nets[0] = new NeuralNet(_settings);
        }
    }
}