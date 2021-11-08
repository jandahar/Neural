using Power3DBuilder.Models;
using System;

namespace NeuroNet
{
    internal class NeuralSettings : P3bSettingsList
    {
        public P3bSetting<bool> RenderAnimated;

        public P3bSettingMinMax<int> NumberNets;
        public P3bSettingMinMax<int> NumberIterationsStart;

        public NeuralSettings() : base("Neural")
        {
            RenderAnimated = new P3bSetting<bool>("Render Animated", true);

            NumberNets = new P3bSettingMinMax<int>("# agents", 200, 1, 1, 500);
            NumberIterationsStart = new P3bSettingMinMax<int>("# iterations start", 200, 1, 1, 1000);

            AddHidden(RenderAnimated);

            Add(NumberNets);
            Add(NumberIterationsStart);
        }
    }
}