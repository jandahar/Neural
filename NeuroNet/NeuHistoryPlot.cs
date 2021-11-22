using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NeuroNet
{
    internal class NeuHistoryPlot
    {
        private Vector _offset;
        private Vector _size;
        private DateTime _start;
        private double _maxX = 1;
        private double _maxY = 1;

        private Dictionary<Brush, List<Point>> _lastPoints = new Dictionary<Brush, List<Point>>();
        private Dictionary<Brush, List<Line>> _lines = new Dictionary<Brush, List<Line>>();
        public NeuHistoryPlot(Vector offset, Vector size)
        {
            _offset = offset;
            _size = size;
            _start = DateTime.Now;
        }

        internal void addDataPoint(UIElementCollection uiElements, Brush color, int maxTargetsHit)
        {
            var now = DateTime.Now;
            var tSeconds = (int)(now - _start).TotalSeconds;

            bool redoTransformation = false;
            if (tSeconds > _maxX)
            {
                _maxX = tSeconds;
                redoTransformation = true;
            }

            if(maxTargetsHit > _maxY)
            {
                _maxY = maxTargetsHit;
                redoTransformation = true;
            }

            if (!_lastPoints.ContainsKey(color))
            {
                _lastPoints[color] = new List<Point>();
                _lastPoints[color].Add(new Point(tSeconds, maxTargetsHit));
                _lines[color] = new List<Line>();
            }
            else
            {
                double xs1, ys1, xs2, ys2;
                if (redoTransformation)
                {
                    foreach (var col in _lastPoints.Keys)
                    {
                        for (int i = 0; i < _lastPoints[col].Count - 1; i++)
                        {
                            var p1 = _lastPoints[col][i];
                            var p2 = _lastPoints[col][i + 1];

                            getLineCoords(p2.X, p2.Y, p1, out xs1, out ys1, out xs2, out ys2);
                            _lines[col][i].X1 = xs1;
                            _lines[col][i].Y1 = ys1;
                            _lines[col][i].X2 = xs2;
                            _lines[col][i].Y2 = ys2;
                        }
                    }
                }

                var lastPoint = _lastPoints[color][_lastPoints[color].Count - 1];

                getLineCoords(tSeconds, maxTargetsHit, lastPoint, out xs1, out ys1, out xs2, out ys2);

                Line line = new Line
                {
                    X1 = xs1,
                    Y1 = ys1,
                    X2 = xs2,
                    Y2 = ys2,
                    Stroke = color,
                    StrokeThickness = 2,
                    StrokeEndLineCap = PenLineCap.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    //RenderTransform = _transform,
                };

                _lastPoints[color].Add(new Point(tSeconds, maxTargetsHit));
                uiElements.Add(line);
                _lines[color].Add(line);
            }
        }

        private void getLineCoords(double generation, double maxTargetsHit, Point lastPoint, out double xs1, out double ys1, out double xs2, out double ys2)
        {
            var x1 = lastPoint.X;
            var y1 = lastPoint.Y;
            var x2 = generation;
            var y2 = maxTargetsHit;

            var height = Math.Max(_maxY, 3);
            var width = Math.Max(_maxX, 10);

            var scaleX = _size.X / width;
            var scaleY = _size.Y / height;

            xs1 = _offset.X + scaleX * x1;
            ys1 = _offset.Y + _size.Y - scaleY * y1;
            xs2 = _offset.X + scaleX * x2;
            ys2 = _offset.Y + _size.Y - scaleY * y2;
        }

        internal void getUiElements(UIElementCollection uiElements)
        {
            if(_lines != null)
                foreach (var l in _lines)
                    foreach(var ll in _lines[l.Key])
                        uiElements.Add(ll);
        }
    }
}