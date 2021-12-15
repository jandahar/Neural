using Power3DBuilder;
using Power3DBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NeuroNet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private P3bGuiControl _guiControl;
        private NeuralSceneObject _sceneObject;
        private NeuralSceneObject3D _sceneObject3D;

        public MainWindow()
        {
            InitializeComponent();

            _guiControl = new P3bGuiControl(VisualGraph, RenderSpace, Dispatcher, DebugWindow, StackPanelTop, StackPanelRight);
            _guiControl.initializeGui();

            var settings = new NeuralSettings("Neural 2D");
            _sceneObject = new NeuralSceneObject(settings, VisualGraph);

            _guiControl.addSetting(settings, (IP3bSetting s) =>
            {
                return _sceneObject;
            });

            _guiControl.RenderControl.VControl.CameraDistanceMin = 200;
            _guiControl.RenderControl.VControl.CameraDistanceMax = 1000;
            _guiControl.RenderControl.VControl.CameraDistance = 500;
            _guiControl.RenderControl.VControl.WheelSensivity = 0.5;

             var settings3D = new NeuralSettings("Neural 3D");
            settings3D.DrawLines.Value = false;
            settings3D.AnimateOnlyChampions.Value = false;
            settings3D.NumberNets.Value = 1000;

            _sceneObject3D = new NeuralSceneObject3D(settings3D, VisualGraph, _guiControl.RenderControl);

            _guiControl.addSetting(settings3D, (IP3bSetting s) =>
            {
                return _sceneObject3D;
            });
        }
    }
}
