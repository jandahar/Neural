using Power3DBuilder;
using Power3DBuilder.Models;

namespace NeuroNet
{
    internal class NeuralGuiAdmin
    {
        private NeuralSettings _settings;

        private NeuralNet[] _nets;
        private NeuralSceneObject _sceneObject;
        private P3bGuiControl _guiControl;

        public NeuralGuiAdmin(P3bGuiControl guiControl, System.Windows.Controls.Canvas visualGraph)
        {
            _settings = new NeuralSettings();

            _nets = new NeuralNet[_settings.NumberNets];

            _sceneObject = new NeuralSceneObject(_settings, visualGraph);

            _guiControl = guiControl;
            _guiControl.addSetting(_settings, (IP3bSetting s) =>
            {
                //for (int i = 0; i < _settings.NumberNets; i++)
                //    _nets[i] = new NeuralNet(i, _settings);
                //_sceneObject.setNets(_nets);

                return _sceneObject;
            });
        }
    }
}