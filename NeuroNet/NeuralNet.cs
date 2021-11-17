using System;
using System.Collections.Generic;

namespace NeuroNet
{
    internal class NeuralNet
    {
        //private int[] _layerConfig = new int[] { 6, 24, 32, 16, 8, 4, 2 };
        private int[] _layerConfig = new int[] { 6, 4, 6, 2 };

        private Random _rnd;

        private int[] _layers;
        private float[][] _neurons;
        private float[][] _biases;
        private float[][][] _weights;
        //private int[] _activations;
        //public float _fitness = 0;

        public int[] Layers { private set => _layers = value; get => _layers; }
        public float[][] Neurons { private set => _neurons = value; get => _neurons; }

        public NeuralNet(int seed, int[] layerConfig)
        {
            _layerConfig = layerConfig;
            _rnd = new Random(seed);

            initLayers(_layerConfig);
            initialize();
        }

        private void initialize()
        {
            initNeurons();
            initBiases();
            initWeights();
        }

        private void initLayers(int[] layers)
        {
            _layers = new int[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                _layers[i] = layers[i];
            }
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

        internal NeuralNet clone()
        {
            var result = new NeuralNet(_rnd.Next(), _layerConfig);

            List<float[]> neuronsList = new List<float[]>();
            for (int i = 0; i < _layers.Length; i++)
            {
                neuronsList.Add(new float[_layers[i]]);

            }
            result._neurons = neuronsList.ToArray();


            List<float[]> biasList = new List<float[]>();
            for (int i = 0; i < _layers.Length; i++)
            {
                float[] bias = new float[_layers[i]];
                for (int j = 0; j < _layers[i]; j++)
                {
                    bias[j] = _biases[i][j];
                }
                biasList.Add(bias);
            }
            result._biases = biasList.ToArray();

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
                        neuronWeights[k] = _weights[i - 1][j][k];
                    }
                    layerWeightsList.Add(neuronWeights);
                }
                weightsList.Add(layerWeightsList.ToArray());
            }
            result._weights = weightsList.ToArray();

            return result;
        }

        public static float activate(float value)
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


        //public int CompareTo(NeuralNet other)
        //{
        //    if (other == null)
        //        return 1;
        //    if (_fitness > other._fitness)
        //        return 1;
        //    else if (_fitness < other._fitness)
        //        return -1;
        //    else
        //        return 0;
        //}
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

        //public override string ToString()
        //{
        //    return string.Format("{0}: {1}", _id.ToString(), _fitness.ToString());
        //}
    }
}