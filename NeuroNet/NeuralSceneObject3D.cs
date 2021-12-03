using Power3D;
using Power3DBuilder.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace NeuroNet
{
    internal class NeuralSceneObject3D : IP3bSceneObject
    {
        private NeuralSettings3D _settings;
        private Viewport3D _viewport3D;
        private List<P3DModelVisual3D> _models = new List<P3DModelVisual3D>();
        private double _posX = 0;

        public NeuralSceneObject3D(NeuralSettings3D settings3D, Viewport3D renderSpace)
        {
            _settings = settings3D;
            _viewport3D = renderSpace;
        }

        public void addModels(List<P3DModelVisual3D> models)
        {
            foreach(var m in models) _models.Add(m);
        }

        public void get2dDrawing(double width, double height, Transform3D cameraProjection, ref List<FrameworkElement> shapes, ref string debug)
        {
        }

        public string getDebugInfo(CallBackType info)
        {
            return "No debug info yet";
        }

        public void getMeshes(ref P3dColoredMeshCollection meshes, ref string debug)
        {
        }

        public bool getMeshesToUpdate(ref List<P3dMesh> meshes, ref string debug)
        {
            if(_models.Count == 0)
            {
                meshes.Add(P3dIcoSphere.getIcoMesh());
            }

            foreach(var m in _models)
            {
                _posX += 0.1;
                TranslateTransform3D trans = new TranslateTransform3D(_posX, 0, 0);
                m.Transform = trans;
            }

            return true;
        }

        public IP3bSetting getSettings()
        {
            return _settings;
        }

        public void getUIElements(ref UIElementCollection uiElements, ref string debug)
        {
        }

        public bool getUIElementsToAdd(ref UIElementCollection uIElements, ref string debug)
        {
            return true;
        }

        public void updateSettings()
        {
        }
    }
}