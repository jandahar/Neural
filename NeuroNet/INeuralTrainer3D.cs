using Power3D;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace NeuroNet
{
    internal interface INeuralTrainer3D
    {
        SolidColorBrush Color { get; set; }

        void getMeshesToUpdate(ref List<P3dMesh> meshes);
        void initUiElements();
        NeuralNet getActiveNet();
        bool hasNextGen();
        double getLevelScore();
        int initNextGeneration();
        void getUiElements(UIElementCollection uiElements);
        void addMeshes();
        void getNextIteration(UIElementCollection uiElements, ref string debug);
        void setLayerConfig(int[] vs);
        void updateSettings(double actualWidth, double actualHeight);
        void AddLevel(NeuralTrainerLevel neuralTrainerLevel);
        void addModels(List<P3DModelVisual3D> models);
    }
}