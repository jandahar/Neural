using Power3D;
using Power3DBuilder.Models;
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

        public NeuralSceneObject(NeuralSettings neuralSettings, Canvas visualGraph)
        {
            _settings = neuralSettings;
            _visualGraph = visualGraph;
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
        }

        public bool getUIElementsToAdd(ref UIElementCollection uIElement, ref string debug)
        {
            Line line = new Line
            {
                Stroke = Brushes.Yellow,
                StrokeThickness = 5,
                StrokeEndLineCap = PenLineCap.Square,
                StrokeStartLineCap = PenLineCap.Square,
                //StrokeEndLineCap = PenLineCap.Round,
                //StrokeStartLineCap = PenLineCap.Round,

                X1 = 0,
                Y1 = 0,

                X2 = _visualGraph.ActualWidth,
                Y2 = _visualGraph.ActualHeight,
            };
            uIElement.Add(line);
            return false;
        }

        public void updateSettings()
        {
        }
    }
}