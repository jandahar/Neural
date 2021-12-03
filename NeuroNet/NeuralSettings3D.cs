using Power3DBuilder.Models;

namespace NeuroNet
{
    internal class NeuralSettings3D : P3bSettingsList
    {
        public P3bSetting<bool> RenderAnimated;

        public NeuralSettings3D() : base("Neural 3D")
        {
            RenderAnimated = new P3bSetting<bool>("Render Animated", true);

            AddHidden(RenderAnimated);
        }
    }
}