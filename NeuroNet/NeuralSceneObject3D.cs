using Power3D;
using Power3DBuilder;
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
        private List<INeuralTrainer3D> _trainers = null;
        private List<NeuralNetDisplay> _netDisplays;
        private NeuralSettings _settings;
        private Canvas _visualGraph;
        private P3bRenderControl _renderControl;
        private SolidColorBrush[] _colors;
        private int _pauseOnNextIteration = 1;
        private bool _trainerNeedsInit = true;
        private NeuHistoryPlot _history;
        private bool _rendererNeedsClearing = false;

        public NeuralSceneObject3D(NeuralSettings neuralSettings, Canvas visualGraph, Power3DBuilder.P3bRenderControl renderControl)
        {
            _settings = neuralSettings;
            _visualGraph = visualGraph;
            _renderControl = renderControl;

            _colors = new SolidColorBrush[]
            {
                Brushes.DarkGreen,
                Brushes.DarkRed,
                Brushes.Yellow,
                Brushes.LightBlue,
                Brushes.DarkBlue,
                Brushes.BlueViolet,
                Brushes.AliceBlue,
                Brushes.Blue,
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
            if (_rendererNeedsClearing)
            {
                _renderControl.resetView();
                _rendererNeedsClearing = false;
            }

            foreach (var trainer in _trainers)
                trainer.getMeshesToUpdate(ref meshes);

            return true;
        }

        public IP3bSetting getSettings()
        {
            return _settings;
        }

        public void getUIElements(ref UIElementCollection uiElements, ref string debug)
        {
            if (_trainerNeedsInit)
            {
                updateSettings();

                foreach (var trainer in _trainers)
                {
                    trainer.initUiElements();
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

            bool cleared = uiElements.Count < 2;
            if (!cleared)
            {
                foreach (var trainer in _trainers)
                {
                    if (_pauseOnNextIteration > 0 && trainer.hasNextGen())
                    {
                        _pauseOnNextIteration--;

                        if (_pauseOnNextIteration == 0)
                        {
                            var vp = uiElements[0];
                            uiElements.Clear();
                            uiElements.Add(vp);
                            _rendererNeedsClearing = true;
                            cleared = true;
                            break;
                        }
                    }
                }
            }

            if (cleared)
            {
                if (_history != null)
                    _history.getUiElements(uiElements);
            }

            foreach (var trainer in _trainers)
            {
                if (trainer.hasNextGen())
                {
                    if (_history == null)
                        _history = new NeuHistoryPlot(new Vector(0.11 * _visualGraph.ActualWidth, 0.01 * _visualGraph.ActualHeight), new Vector(0.7 * _visualGraph.ActualWidth, 0.1 * _visualGraph.ActualHeight));

                    _history.addDataPoint(uiElements, trainer.Color, trainer.getLevelScore());

                    trainer.initNextGeneration();

                    initNetDisplay(uiElements);

                    if (_settings.PauseOnGeneration)
                        _pauseOnNextIteration = 10;
                    else
                        _pauseOnNextIteration = 1;
                }
                else if (cleared)
                {
                    trainer.getUiElements(uiElements);
                    trainer.addMeshes();
                }
            }

            foreach (var trainer in _trainers)
            {
                trainer.getNextIteration(uiElements, ref debug);
            }

            for (int i = 0; i < _trainers.Count; i++)
                _netDisplays[i].drawNeurons(_trainers[i].getActiveNet());

            return true;
        }

        public void updateSettings()
        {
            if (_trainers == null)
            {
                var rnd = new Random();
                _trainers = new List<INeuralTrainer3D>();
                _trainers.Add(makeTrainer3D(rnd.Next(), -1, _colors[0]));
                _trainers.Add(makeTrainer3D(rnd.Next(), 0, _colors[1]));
                _trainers.Add(makeTrainer3D(rnd.Next(), 1, _colors[2]));
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

                if (_history != null)
                    _history.reset();

                _trainerNeedsInit = true;

                _settings.GoalTargetIterations.Changed = false;
                _settings.NumberIterationsStart.Changed = false;
                _settings.NumberNets.Changed = false;
                _settings.TurnsToTarget.Changed = false;
            }


            //_trainers[0].DisasterMutate = true;
            //_trainers[1].DisasterMutate = true;
            //_trainers[2].DisasterMutate = true;

            //_trainers[1].IncreaseNumberBalls = -100;
            //_trainers[2].IncreaseNumberBalls = 200;

            _trainers[1].setLayerConfig(new int[] { 11, 11, 11, 11, 6, 3 });
            _trainers[2].setLayerConfig(new int[] { 11, 128, 3 });

            var centerX = 0;
            var centerY = 0;
            //_trainers[1].NoToChooseForNextGeneration = 5;
            //_trainers[2].SpeedFitnessFactor = 10;

            var center = new Point3D(centerX, 0.0, centerY);
            var cps = makeCircularTargets(center, 1, 0.15, 1, _trainers.Count);

            setupLevels(_trainers[0], new Point3D(cps[0].X, -250.0, cps[0].Z));
            setupLevels(_trainers[1], new Point3D(cps[1].X, 0.0, cps[1].Z));
            setupLevels(_trainers[2], new Point3D(cps[2].X, 250.0, cps[2].Z));
        }

        private NeuralTrainer3D makeTrainer3D(int rnd, int pos, SolidColorBrush color)
        {
            var neuralTrainer3D = new NeuralTrainer3D(rnd, _settings, _visualGraph.ActualWidth, _visualGraph.ActualHeight, _colors, color);

            neuralTrainer3D.DisasterMutate = true;
            neuralTrainer3D.IncreaseIterations = 1;
            neuralTrainer3D.SpeedFitnessFactor = 5;
            //neuralTrainer3D.Targeting = TargetingType.Fixed;

            return neuralTrainer3D;
        }

        private static void setupLevels(INeuralTrainer3D neuralTrainer, Point3D centerPoint)
        {
            //var levelStart = new NeuralTrainerLevel();
            //levelStart.MaxIterationsStart = 50;
            //levelStart.MaxIterationsEnd = 52;
            //levelStart.GenerationsToComplete = 15;
            //levelStart.TargetList = new List<Point> { centerPoint };
            //neuralTrainer.AddLevel(levelStart, true);


            var tetraUp = makeTetrahedronTargets(centerPoint, 5 * NeuMoverBase.Radius);
            var tetraDown = makeTetrahedronTargets(centerPoint, 5 * NeuMoverBase.Radius, true);
            var tetraOcta = new List<Point3D>();

            for (int i = 0; i < tetraUp.Count; i++)
            {
                tetraOcta.Add(tetraUp[i]);
                tetraOcta.Add(tetraDown[i]);
            }

            neuralTrainer.AddLevel(createLevel(centerPoint, tetraUp));
            neuralTrainer.AddLevel(createLevel(centerPoint, tetraDown));
            neuralTrainer.AddLevel(createLevel(centerPoint, tetraOcta));

            neuralTrainer.AddLevel(createLevel(centerPoint, makeCircularTargets(centerPoint, 4, -8 * NeuMoverBase.Radius, 1)));
            neuralTrainer.AddLevel(createLevel(centerPoint, makeCircularTargets(centerPoint, 4, 12 * NeuMoverBase.Radius, 1)));
            neuralTrainer.AddLevel(createLevel(centerPoint, makeCircularTargets(centerPoint, 4, 18 * NeuMoverBase.Radius, 1)));
            neuralTrainer.AddLevel(createLevel(centerPoint, makeCircularTargets(centerPoint, 2, +6 * NeuMoverBase.Radius, 4)));
        }

        private static NeuralTrainerLevel createLevel(Point3D centerPoint, List<Point3D> tetraUp)
        {
            const int iterationPerTarget = 150;

            var level = new NeuralTrainerLevel();
            level.MaxIterationsStart = 125;
            level.GenerationsToComplete = 20;
            level.Targeting = TargetingType.Fixed;
            level.TargetList = tetraUp;
            level.StartPoint = centerPoint;
            level.LevelTries = 1;
            level.MaxIterationsEnd = iterationPerTarget * (level.TargetList.Count + 1);
            return level;
        }

        private static List<Point3D> makeTetrahedronTargets(Point3D center, double radius, bool invertZ = false)
        {
            var tetraData = P3dPlatonicSolids.getTetrahedronPoints(radius);
            var targets = new List<Point3D>();

            var signZ = invertZ ? -1 : 1;
            foreach (var t in tetraData.Item1)
                targets.Add(new Point3D(t.X + center.X, t.Y + center.Y, signZ * t.Z + center.Z));

            return targets;
        }


        private static List<Point> makeCircularTargets(Point center, int divisor, double baseRadius, int noCircles = 3, int nSegments = -1)
        {
            double absRadius = Math.Abs(baseRadius);
            int signRadius = Math.Sign(baseRadius);
            var targets = new List<Point>();
            //targets2.Add(center);

            bool determineSegments = nSegments < 0;
            for (int j = 0; j < noCircles; j++)
            {
                var radius = (j + 1) * absRadius;
                var circumference = 2 * Math.PI * radius;

                if (determineSegments)
                {
                    nSegments = (int)(circumference / (10 * NeuMoverBase.Radius));
                    nSegments -= (1 - nSegments % 2);
                }

                int step = nSegments / divisor + 1;
                var deltaPhi = step * (2 * Math.PI / nSegments);

                for (int i = 0; i < nSegments + 1; i++)
                {
                    var vecP = new Vector(signRadius * radius * Math.Cos(i * deltaPhi), radius * Math.Sin(i * deltaPhi));
                    targets.Add(center + vecP);
                    //targets2.Add(center);
                }
            }

            return targets;
        }

        private static List<Point3D> makeCircularTargets(Point3D center, int divisor, double baseRadius, int noCircles = 3, int nSegments = -1)
        {
            var targets2D = makeCircularTargets(new Point(center.X, center.Z), divisor, baseRadius, noCircles, nSegments);
            var result = new List<Point3D>();
            foreach (var t in targets2D)
                result.Add(new Point3D(t.X, center.Y, t.Y));

            return result;
        }

        public void addModels(List<P3DModelVisual3D> models)
        {
            foreach (var t in _trainers)
                t.addModels(models);
            //foreach (var m in models)
            //{
            //    _models[m.ID].Ellipse = m;
            //    _positions[m.ID] = new Point3D();
            //    _velocities[m.ID] = new Vector3D(10 * getRandomAcceleration(), 10 * getRandomAcceleration(), 0);
            //}
        }

        //    private const double boundingBoxHalfLength = 5;
        //    private NeuralSettings3D _settings;
        //    private Viewport3D _viewport3D;
        //    private Random _rnd;
        //    private Color[] _colors;
        //private SortedDictionary<int, NeuBall3D> _models = new SortedDictionary<int, NeuBall3D>();
        //    private SortedDictionary<int, Point3D> _positions = new SortedDictionary<int, Point3D>();
        //    private SortedDictionary<int, Vector3D> _velocities = new SortedDictionary<int, Vector3D>();
        //    private int _count;

        //    public NeuralSceneObject3D(NeuralSettings3D settings3D, Viewport3D renderSpace)
        //    {
        //        _settings = settings3D;
        //        _viewport3D = renderSpace;

        //        _rnd = new Random();
        //        _colors = new Color[]
        //        {
        //            Colors.DarkGreen,
        //            Colors.DarkRed,
        //            Colors.Yellow,
        //            Colors.LightBlue,
        //            Colors.DarkBlue,
        //            Colors.BlueViolet,
        //            Colors.AliceBlue,
        //            Colors.Blue,
        //        };
        //    }

        //public void addModels(List<P3DModelVisual3D> models)
        //{
        //    foreach (var m in models)
        //    {
        //        _models[m.ID].Ellipse = m;
        //        _positions[m.ID] = new Point3D();
        //        _velocities[m.ID] = new Vector3D(10 * getRandomAcceleration(), 10 * getRandomAcceleration(), 0);
        //    }
        //}

        //    public void get2dDrawing(double width, double height, Transform3D cameraProjection, ref List<FrameworkElement> shapes, ref string debug)
        //    {
        //    }

        //    public string getDebugInfo(CallBackType info)
        //    {
        //        return "No debug info yet";
        //    }

        //    public void getMeshes(ref P3dColoredMeshCollection meshes, ref string debug)
        //    {
        //    }

        //    public bool getMeshesToUpdate(ref List<P3dMesh> meshes, ref string debug)
        //    {
        //        _count++;
        //        if (_count % 5 == 0 || _models.Count == 0)
        //        {
        //            P3dMesh mesh = P3dIcoSphere.getIcoMesh(0.1);
        //            mesh.ID = _models.Count;
        //            mesh.BaseColor = _colors[_models.Count % _colors.Length];
        //            meshes.Add(mesh);
        //            _models[mesh.ID] = new NeuBall3D(_settings, new Point(), 0, 0, 1);
        //        }

        //        foreach(var m in _models.Values)
        //        {
        //            //var a = new Vector3D(getRandomAcceleration(), getRandomAcceleration(), getRandomAcceleration() - 0.25);
        //            var a = new Vector3D(0, 0, -0.01);
        //            _velocities[m.ID] += a;
        //            _positions[m.ID] += _velocities[m.ID];
        //            TranslateTransform3D trans = new TranslateTransform3D((Vector3D)_positions[m.ID]);
        //            m.Ellipse.Transform = trans;

        //            var damp = 0.9;
        //            if (_positions[m.ID].X > boundingBoxHalfLength || _positions[m.ID].X < -boundingBoxHalfLength)
        //            {
        //                _positions[m.ID] = new Point3D(Math.Sign(_positions[m.ID].X) * boundingBoxHalfLength, _positions[m.ID].Y, _positions[m.ID].Z);
        //                _velocities[m.ID] = new Vector3D(-damp *_velocities[m.ID].X, damp * _velocities[m.ID].Y, damp * _velocities[m.ID].Z);
        //            }

        //            if (_positions[m.ID].Y > boundingBoxHalfLength || _positions[m.ID].Y < -boundingBoxHalfLength)
        //            {
        //                _positions[m.ID] = new Point3D(_positions[m.ID].X, Math.Sign(_positions[m.ID].Y) * boundingBoxHalfLength, _positions[m.ID].Z);
        //                _velocities[m.ID] = new Vector3D(damp * _velocities[m.ID].X, -damp * _velocities[m.ID].Y, damp * _velocities[m.ID].Z);
        //            }

        //            if (_positions[m.ID].Z > boundingBoxHalfLength || _positions[m.ID].Z < -boundingBoxHalfLength)
        //            {
        //                _positions[m.ID] = new Point3D(_positions[m.ID].X, _positions[m.ID].Y, Math.Sign(_positions[m.ID].Z) * boundingBoxHalfLength);
        //                _velocities[m.ID] = new Vector3D(damp * _velocities[m.ID].X, damp * _velocities[m.ID].Y, -damp * _velocities[m.ID].Z);
        //            }
        //        }

        //        debug = string.Format("Spheres added: {0}\n", _models.Count);

        //        return true;
        //    }

        //    private double getRandomAcceleration()
        //    {
        //        return 0.1 * (_rnd.NextDouble() - 0.5);
        //    }

        //    public IP3bSetting getSettings()
        //    {
        //        return _settings;
        //    }

        //    public void getUIElements(ref UIElementCollection uiElements, ref string debug)
        //    {
        //    }

        //    public bool getUIElementsToAdd(ref UIElementCollection uIElements, ref string debug)
        //    {
        //        return true;
        //    }

        //    public void updateSettings()
        //    {
        //    }
    }
}