﻿using Power3DBuilder.Models;
using System;

namespace NeuroNet
{
    internal class NeuralSettings : P3bSettingsList
    {
        public P3bSetting<bool> RenderAnimated;

        public P3bSettingMinMax<int> NumberNets;

        public NeuralSettings() : base("Neural")
        {
            RenderAnimated = new P3bSetting<bool>("Render Animated", true);

            NumberNets = new P3bSettingMinMax<int>("# Nets", 50, 1, 1, 100);

            AddHidden(RenderAnimated);

            Add(NumberNets);
        }
    }
}