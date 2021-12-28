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
        
        public P3bSetting<bool> AnimateOnlyChampions;
        public P3bSetting<bool> DrawLines;

        public P3bSetting<bool> Float;
        public P3bSetting<bool> PauseOnGeneration;

        public P3bSettingChoices<string, List<string>> Targeting;
        public P3bSetting<bool> RandomTargets;
        public P3bSettingMinMax<int> TurnsToTarget;
        public P3bSettingMinMax<int> GoalTargetIterations;

        public P3bSettingMinMax<int> MaxHits;
        public P3bSettingMinMax<double> AgentScale;

        public NeuralSettings(string name) : base(name)
        {
            RenderAnimated = new P3bSetting<bool>("Render Animated", true);

            Render3D = new P3bSetting<bool>("Render Animated", true);
            NumberNets = new P3bSettingMinMax<int>("# agents", 250, 1, 1, 1000);
            NumberIterationsStart = new P3bSettingMinMax<int>("# iterations start", 250, 1, 25, 1000);

            AnimateOnlyChampions = new P3bSetting<bool>("Show only champions", true);
            DrawLines = new P3bSetting<bool>("Spur lines", true);

            Float = new P3bSetting<bool>("Lift only", false);
            PauseOnGeneration = new P3bSetting<bool>("Pause before generation", false);

            var targetingValues = Enum.GetNames(typeof(TargetingType));
            var targetingList = new List<string>(targetingValues);
            Targeting = new P3bSettingChoices<string, List<string>>("Targeting", targetingList, TargetingType.Near.ToString());

            RandomTargets = new P3bSetting<bool>("Random targets", true);
            TurnsToTarget = new P3bSettingMinMax<int>("Turns to target", 200, 1, 100, 1000);
            GoalTargetIterations = new P3bSettingMinMax<int>("# iterations target", 50, 1, 1, 1000);

            MaxHits = new P3bSettingMinMax<int>("max # hits", 1, 1, 1, 10);
            AgentScale = new P3bSettingMinMax<double>("Agent scale", 0.1, 0.1, 0.1, 1);

            AddHidden(RenderAnimated);

            AddHidden(Render3D);
            
            Add(NumberNets);
            Add(NumberIterationsStart);
            Add(AgentScale);
            Add(AnimateOnlyChampions);
            Add(DrawLines); 
            Add(Float);
            Add(Targeting);
            Add(PauseOnGeneration);
            Add(TurnsToTarget);
            Add(GoalTargetIterations);

            Add(MaxHits);
        }
    }
}