using Power3DBuilder.Models;
using System;
using System.Collections.Generic;

namespace NeuroNet
{
    internal class NeuralSettings : P3bSettingsList
    {
        public P3bSetting<bool> RenderAnimated;

        public P3bSetting<bool> Render3D;
        public P3bSettingMinMax<int> NumberNets;
        public P3bSettingMinMax<int> NumberIterationsStart;

        public P3bSetting<bool> Float;
        public P3bSetting<bool> PauseOnGeneration;

        public P3bSettingChoices<string, List<string>> Targeting;
        public P3bSetting<bool> RandomTargets;
        public P3bSettingMinMax<int> TurnsToTarget;
        public P3bSettingMinMax<int> GoalTargetIterations;

        public P3bSettingMinMax<int> MaxHits;

        public NeuralSettings() : base("Neural")
        {
            RenderAnimated = new P3bSetting<bool>("Render Animated", true);

            Render3D = new P3bSetting<bool>("Render Animated", true);
            NumberNets = new P3bSettingMinMax<int>("# agents", 250, 1, 1, 1000);
            NumberIterationsStart = new P3bSettingMinMax<int>("# iterations start", 50, 1, 25, 1000);

            Float = new P3bSetting<bool>("Lift only", false);
            PauseOnGeneration = new P3bSetting<bool>("Pause before generation", false);

            Targeting = new P3bSettingChoices<string, List<string>>("Targeting", new List<string> { "Default" }, "Default");
            RandomTargets = new P3bSetting<bool>("Random targets", true);
            TurnsToTarget = new P3bSettingMinMax<int>("Turns to target", 200, 1, 100, 1000);
            GoalTargetIterations = new P3bSettingMinMax<int>("# iterations target", 50, 1, 1, 1000);

            MaxHits = new P3bSettingMinMax<int>("max # hits", 1, 1, 1, 10);

            AddHidden(RenderAnimated);

            AddHidden(Render3D);
            
            Add(NumberNets);
            Add(NumberIterationsStart);
            Add(Float);
            Add(RandomTargets);
            Add(PauseOnGeneration);
            Add(TurnsToTarget);
            Add(GoalTargetIterations);

            Add(MaxHits);
        }
    }
}