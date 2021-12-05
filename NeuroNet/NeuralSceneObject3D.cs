using Power3D;
using Power3DBuilder.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace NeuroNet
{
    internal class NeuralSceneObject3D : IP3bSceneObject
    {
        private const double boundingBoxHalfLength = 5;
        private NeuralSettings3D _settings;
        private Viewport3D _viewport3D;
        private Random _rnd;
        private Color[] _colors;
        private List<P3DModelVisual3D> _models = new List<P3DModelVisual3D>();
        private SortedDictionary<int, Point3D> _positions = new SortedDictionary<int, Point3D>();
        private SortedDictionary<int, Vector3D> _velocities = new SortedDictionary<int, Vector3D>();
        private int _count;

        public NeuralSceneObject3D(NeuralSettings3D settings3D, Viewport3D renderSpace)
        {
            _settings = settings3D;
            _viewport3D = renderSpace;

            _rnd = new Random();
            _colors = new Color[]
            {
                Colors.DarkGreen,
                Colors.DarkRed,
                Colors.Yellow,
                Colors.LightBlue,
                Colors.DarkBlue,
                Colors.BlueViolet,
                Colors.AliceBlue,
                Colors.Blue,
            };
        }

        public void addModels(List<P3DModelVisual3D> models)
        {
            foreach (var m in models)
            {
                _models.Add(m);
                m.ID = _models.Count;
                _positions[m.ID] = new Point3D();
                _velocities[m.ID] = new Vector3D(10 * getRandomAcceleration(), 10 * getRandomAcceleration(), 0);
            }
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
            _count++;
            if (_count % 5 == 0 || _models.Count == 0)
            {
                P3dMesh mesh = P3dIcoSphere.getIcoMesh(0.1);
                mesh.BaseColor = _colors[_models.Count % _colors.Length];
                meshes.Add(mesh);
            }

            foreach(var m in _models)
            {
                //var a = new Vector3D(getRandomAcceleration(), getRandomAcceleration(), getRandomAcceleration() - 0.25);
                var a = new Vector3D(0, 0, -0.01);
                _velocities[m.ID] += a;
                _positions[m.ID] += _velocities[m.ID];
                TranslateTransform3D trans = new TranslateTransform3D((Vector3D)_positions[m.ID]);
                m.Transform = trans;

                var damp = 0.9;
                if (_positions[m.ID].X > boundingBoxHalfLength || _positions[m.ID].X < -boundingBoxHalfLength)
                {
                    _positions[m.ID] = new Point3D(Math.Sign(_positions[m.ID].X) * boundingBoxHalfLength, _positions[m.ID].Y, _positions[m.ID].Z);
                    _velocities[m.ID] = new Vector3D(-damp *_velocities[m.ID].X, damp * _velocities[m.ID].Y, damp * _velocities[m.ID].Z);
                }

                if (_positions[m.ID].Y > boundingBoxHalfLength || _positions[m.ID].Y < -boundingBoxHalfLength)
                {
                    _positions[m.ID] = new Point3D(_positions[m.ID].X, Math.Sign(_positions[m.ID].Y) * boundingBoxHalfLength, _positions[m.ID].Z);
                    _velocities[m.ID] = new Vector3D(damp * _velocities[m.ID].X, -damp * _velocities[m.ID].Y, damp * _velocities[m.ID].Z);
                }

                if (_positions[m.ID].Z > boundingBoxHalfLength || _positions[m.ID].Z < -boundingBoxHalfLength)
                {
                    _positions[m.ID] = new Point3D(_positions[m.ID].X, _positions[m.ID].Y, Math.Sign(_positions[m.ID].Z) * boundingBoxHalfLength);
                    _velocities[m.ID] = new Vector3D(damp * _velocities[m.ID].X, damp * _velocities[m.ID].Y, -damp * _velocities[m.ID].Z);
                }
            }

            debug = string.Format("Spheres added: {0}\n", _models.Count);

            return true;
        }

        private double getRandomAcceleration()
        {
            return 0.1 * (_rnd.NextDouble() - 0.5);
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