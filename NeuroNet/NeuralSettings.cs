using Power3DBuilder.Models;
using System;

namespace NeuroNet
{
    internal class NeuralSettings : P3bSettingsList
    {
        public P3bSetting<bool> RenderAnimated;

        public NeuralSettings() : base("Neural")
        {
            RenderAnimated = new P3bSetting<bool>("Render Animated", true);

            AddHidden(RenderAnimated);
        }
    }
}