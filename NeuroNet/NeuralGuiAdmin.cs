using Power3DBuilder;
using Power3DBuilder.Models;

namespace NeuroNet
{
    internal class NeuralGuiAdmin
    {
        private NeuralSettings _settings;
        private NeuralSceneObject _sceneObject;
        private P3bGuiControl _guiControl;

        public NeuralGuiAdmin(P3bGuiControl guiControl, System.Windows.Controls.Canvas visualGraph)
        {
            _settings = new NeuralSettings();

            _sceneObject = new NeuralSceneObject(_settings, visualGraph);

            _guiControl = guiControl;
            _guiControl.addSetting(_settings, (IP3bSetting s) =>
            {
                return _sceneObject;
            });
        }
    }
}