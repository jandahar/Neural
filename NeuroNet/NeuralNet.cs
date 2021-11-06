using System;
using System.Collections.Generic;

namespace NeuroNet
{
    internal class NeuralNet
    {
        private NeuralSettings _settings;

        private Random _rnd;

        private int[] _layers;
        private float[][] _neurons;
        private float[][] _biases;
        private float[][][] _weights;
        private int[] _activations;
        public float _fitness = 0;

        public int[] Layers { private set => _layers = value; get => _layers; }
        public float[][] Neurons { private set => _neurons = value; get => _neurons; }

        public NeuralNet(NeuralSettings settings)
        {
            _settings = settings;
            _rnd = new Random();
            initialize(new int[]{ 2, 2, 1});
        }

        private void initialize(int[] layers)
        {
            _layers = new int[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                _layers[i] = layers[i];
            }
            initNeurons();
            initBiases();
            initWeights();
        }


        private void initNeurons()
        {
            List<float[]> neuronsList = new List<float[]>();
            for (int i = 0; i < _layers.Length; i++)
            {
                neuronsList.Add(new float[_layers[i]]);
            }
            _neurons = neuronsList.ToArray();
        }
        
        private void initBiases()
        {
            List<float[]> biasList = new List<float[]>();
            for (int i = 0; i < _layers.Length; i++)
            {
                float[] bias = new float[_layers[i]];
                for (int j = 0; j < _layers[i]; j++)
                {
                    bias[j] = getRandomInit();
                }
                biasList.Add(bias);
            }
            _biases = biasList.ToArray();
        }

        private void initWeights()
        {
            List<float[][]> weightsList = new List<float[][]>();
            for (int i = 1; i < _layers.Length; i++)
            {
                List<float[]> layerWeightsList = new List<float[]>();
                int neuronsInPreviousLayer = _layers[i - 1];
                for (int j = 0; j < _neurons[i].Length; j++)
                {
                    float[] neuronWeights = new float[neuronsInPreviousLayer];
                    for (int k = 0; k < neuronsInPreviousLayer; k++)
                    {
                        neuronWeights[k] = getRandomInit();
                    }
                    layerWeightsList.Add(neuronWeights);
                }
                weightsList.Add(layerWeightsList.ToArray());
            }
            _weights = weightsList.ToArray();
        }
        public float activate(float value)
        {
            return (float)Math.Tanh(value);
        }

        //feed forward, inputs >==> outputs.
        public float[] FeedForward(float[] inputs)
        {
            for (int i = 0; i < inputs.Length; i++)
            {
                _neurons[0][i] = inputs[i];
            }
            for (int i = 1; i < _layers.Length; i++)
            {
                int layer = i - 1;
                for (int j = 0; j < _neurons[i].Length; j++)
                {
                    float value = 0f;
                    for (int k = 0; k < _neurons[i - 1].Length; k++)
                    {
                        value += _weights[i - 1][j][k] * _neurons[i - 1][k];
                    }
                    _neurons[i][j] = activate(value + _biases[i][j]);
                }
            }
            return _neurons[_neurons.Length - 1];
        }


        public int CompareTo(NeuralNet other)
        {
            if (other == null)
                return 1;
            if (_fitness > other._fitness)
                return 1;
            else if (_fitness < other._fitness)
                return -1;
            else
                return 0;
        }
        public void Mutate(int chance, float val)
        {
            for (int i = 0; i < _biases.Length; i++)
            {
                for (int j = 0; j < _biases[i].Length; j++)
                {
                    _biases[i][j] = (getRandomNudge(0, chance) <= 5) ? _biases[i][j] += getRandomNudge(-val, val) : _biases[i][j];
                }
            }

            for (int i = 0; i < _weights.Length; i++)
            {
                for (int j = 0; j < _weights[i].Length; j++)
                {
                    for (int k = 0; k < _weights[i][j].Length; k++)
                    {
                        _weights[i][j][k] = (getRandomNudge(0, chance) <= 5) ? _weights[i][j][k] += getRandomNudge(-val, val) : _weights[i][j][k];

                    }
                }
            }
        }

        public float getRandomInit()
        {
            return (float)(_rnd.NextDouble() - 0.5);
        }

        private float getRandomNudge(double min, double max)
        {
            return (float)((max - min) * _rnd.NextDouble() + min);
        }
    }
}