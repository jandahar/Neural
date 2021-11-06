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
                var net = _nets[0];

                var layers = net.Layers;

                var width = 0.8 * _visualGraph.ActualWidth;
                var height = 0.8 * _visualGraph.ActualHeight;
                var offX = 0.1 * _visualGraph.ActualWidth;
                var offY = 0.1 * _visualGraph.ActualHeight;
                var spacingX = width / (layers.Length - 1);

                for(int i = 0; i < layers.Length; i++)
                {
                    var posX = offX + i * spacingX;

                    var spacingY = height / (layers[i] - 1);
                    if (layers[i] == 1)
                    {
                        spacingY = 0;
                        offY += 0.5 * height;
                    }

                    for(int j = 0; j < layers[i]; j++)
                    {
                        var posY = offY + j * spacingY;
                        Ellipse e = new Ellipse
                        {
                            Stroke = Brushes.Yellow,
                            StrokeThickness = 5,
                        };
                        e.Width = 20;
                        e.Height = 20;

                        e.RenderTransform = new TranslateTransform(posX, posY);
                        uiElements.Add(e);
                    }
                }
            }
        }

        public bool getUIElementsToAdd(ref UIElementCollection uIElements, ref string debug)
        {
            if (_nets.Length > 0)
            {
                //Line line = new Line
                //{
                //    Stroke = Brushes.Yellow,
                //    StrokeThickness = 5,
                //    StrokeEndLineCap = PenLineCap.Square,
                //    StrokeStartLineCap = PenLineCap.Square,
                //    //StrokeEndLineCap = PenLineCap.Round,
                //    //StrokeStartLineCap = PenLineCap.Round,

                //    X1 = 0,
                //    Y1 = 0,

                //    X2 = _visualGraph.ActualWidth,
                //    Y2 = _visualGraph.ActualHeight,
                //};
                //uIElements.Add(line);
            }

            return false;
        }

        public void updateSettings()
        {
        }
    }
}