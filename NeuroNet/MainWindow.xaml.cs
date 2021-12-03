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

            var settings = new NeuralSettings();
            _sceneObject = new NeuralSceneObject(settings, VisualGraph);

            _guiControl.addSetting(settings, (IP3bSetting s) =>
            {
                return _sceneObject;
            });


            var settings3D = new NeuralSettings3D();
            _sceneObject3D = new NeuralSceneObject3D(settings3D, RenderSpace);

            _guiControl.addSetting(settings3D, (IP3bSetting s) =>
            {
                return _sceneObject3D;
            });
        }
    }
}
