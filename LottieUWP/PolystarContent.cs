﻿using System;
using System.Collections.Generic;

namespace LottieUWP
{
    internal class PolystarContent : IPathContent
    {
        /// <summary>
        /// This was empirically derived by creating polystars, converting them to
        /// curves, and calculating a scale factor.
        /// It works best for polygons and stars with 3 points and needs more
        /// work otherwise.
        /// </summary>
        private const float PolystarMagicNumber = .47829f;
        private const float PolygonMagicNumber = .25f;
        private readonly Path _path = new Path();

        private readonly LottieDrawable _lottieDrawable;
        private readonly PolystarShape.Type _type;
        private readonly IBaseKeyframeAnimation<float?> _pointsAnimation;
        private readonly IBaseKeyframeAnimation<PointF> _positionAnimation;
        private readonly IBaseKeyframeAnimation<float?> _rotationAnimation;
        private readonly IBaseKeyframeAnimation<float?> _innerRadiusAnimation;
        private readonly IBaseKeyframeAnimation<float?> _outerRadiusAnimation;
        private readonly IBaseKeyframeAnimation<float?> _innerRoundednessAnimation;
        private readonly IBaseKeyframeAnimation<float?> _outerRoundednessAnimation;

        private TrimPathContent _trimPath;
        private bool _isPathValid;

        internal PolystarContent(LottieDrawable lottieDrawable, BaseLayer layer, PolystarShape polystarShape)
        {
            _lottieDrawable = lottieDrawable;

            Name = polystarShape.Name;
            _type = polystarShape.GetType();
            _pointsAnimation = polystarShape.Points.CreateAnimation();
            _positionAnimation = polystarShape.Position.CreateAnimation();
            _rotationAnimation = polystarShape.Rotation.CreateAnimation();
            _outerRadiusAnimation = polystarShape.OuterRadius.CreateAnimation();
            _outerRoundednessAnimation = polystarShape.OuterRoundedness.CreateAnimation();
            if (_type == PolystarShape.Type.Star)
            {
                _innerRadiusAnimation = polystarShape.InnerRadius.CreateAnimation();
                _innerRoundednessAnimation = polystarShape.InnerRoundedness.CreateAnimation();
            }
            else
            {
                _innerRadiusAnimation = null;
                _innerRoundednessAnimation = null;
            }

            layer.AddAnimation(_pointsAnimation);
            layer.AddAnimation(_positionAnimation);
            layer.AddAnimation(_rotationAnimation);
            layer.AddAnimation(_outerRadiusAnimation);
            layer.AddAnimation(_outerRoundednessAnimation);
            if (_type == PolystarShape.Type.Star)
            {
                layer.AddAnimation(_innerRadiusAnimation);
                layer.AddAnimation(_innerRoundednessAnimation);
            }

            _pointsAnimation.ValueChanged += OnValueChanged;
            _positionAnimation.ValueChanged += OnValueChanged;
            _rotationAnimation.ValueChanged += OnValueChanged;
            _outerRadiusAnimation.ValueChanged += OnValueChanged;
            _outerRoundednessAnimation.ValueChanged += OnValueChanged;
            if (_type == PolystarShape.Type.Star)
            {
                _outerRadiusAnimation.ValueChanged += OnValueChanged;
                _outerRoundednessAnimation.ValueChanged += OnValueChanged;
            }
        }

        private void OnValueChanged(object sender, EventArgs eventArgs)
        {
            Invalidate();
        }

        private void Invalidate()
        {
            _isPathValid = false;
            _lottieDrawable.InvalidateSelf();
        }

        public void SetContents(IList<IContent> contentsBefore, IList<IContent> contentsAfter)
        {
            for (var i = 0; i < contentsBefore.Count; i++)
            {
                if (contentsBefore[i] is TrimPathContent trimPathContent && trimPathContent.Type == ShapeTrimPath.Type.Simultaneously)
                {
                    _trimPath = trimPathContent;
                    _trimPath.ValueChanged += OnValueChanged;
                }
            }
        }

        public Path Path
        {
            get
            {
                if (_isPathValid)
                {
                    return _path;
                }

                _path.Reset();

                switch (_type)
                {
                    case PolystarShape.Type.Star:
                        CreateStarPath();
                        break;
                    case PolystarShape.Type.Polygon:
                        CreatePolygonPath();
                        break;
                }

                _path.Close();

                Utils.ApplyTrimPathIfNeeded(_path, _trimPath);

                _isPathValid = true;
                return _path;
            }
        }

        public string Name { get; }

        private void CreateStarPath()
        {
            var points = _pointsAnimation.Value.Value;
            double currentAngle = _rotationAnimation?.Value ?? 0f;
            // Start at +y instead of +x
            currentAngle -= 90;
            // convert to radians
            currentAngle = MathExt.ToRadians(currentAngle);
            // adjust current angle for partial points
            var anglePerPoint = (float)(2 * Math.PI / points);
            var halfAnglePerPoint = anglePerPoint / 2.0f;
            var partialPointAmount = points - (int)points;
            if (partialPointAmount != 0)
            {
                currentAngle += halfAnglePerPoint * (1f - partialPointAmount);
            }

            var outerRadius = _outerRadiusAnimation.Value.Value;

            var innerRadius = _innerRadiusAnimation.Value.Value;

            var innerRoundedness = 0f;
            if (_innerRoundednessAnimation != null)
            {
                innerRoundedness = _innerRoundednessAnimation.Value.Value / 100f;
            }
            var outerRoundedness = 0f;
            if (_outerRoundednessAnimation != null)
            {
                outerRoundedness = _outerRoundednessAnimation.Value.Value / 100f;
            }

            float x;
            float y;
            float partialPointRadius = 0;
            if (partialPointAmount != 0)
            {
                partialPointRadius = innerRadius + partialPointAmount * (outerRadius - innerRadius);
                x = (float)(partialPointRadius * Math.Cos(currentAngle));
                y = (float)(partialPointRadius * Math.Sin(currentAngle));
                _path.MoveTo(x, y);
                currentAngle += anglePerPoint * partialPointAmount / 2f;
            }
            else
            {
                x = (float)(outerRadius * Math.Cos(currentAngle));
                y = (float)(outerRadius * Math.Sin(currentAngle));
                _path.MoveTo(x, y);
                currentAngle += halfAnglePerPoint;
            }

            // True means the line will go to outer radius. False means inner radius.
            var longSegment = false;
            var numPoints = (int)Math.Ceiling(points) * 2;
            for (var i = 0; i < numPoints; i++)
            {
                var radius = longSegment ? outerRadius : innerRadius;
                var dTheta = halfAnglePerPoint;
                if (partialPointRadius != 0 && i == numPoints - 2)
                {
                    dTheta = anglePerPoint * partialPointAmount / 2f;
                }
                if (partialPointRadius != 0 && i == numPoints - 1)
                {
                    radius = partialPointRadius;
                }
                var previousX = x;
                var previousY = y;
                x = (float)(radius * Math.Cos(currentAngle));
                y = (float)(radius * Math.Sin(currentAngle));

                if (innerRoundedness == 0 && outerRoundedness == 0)
                {
                    _path.LineTo(x, y);
                }
                else
                {
                    var cp1Theta = (float)(Math.Atan2(previousY, previousX) - Math.PI / 2f);
                    var cp1Dx = (float)Math.Cos(cp1Theta);
                    var cp1Dy = (float)Math.Sin(cp1Theta);

                    var cp2Theta = (float)(Math.Atan2(y, x) - Math.PI / 2f);
                    var cp2Dx = (float)Math.Cos(cp2Theta);
                    var cp2Dy = (float)Math.Sin(cp2Theta);

                    var cp1Roundedness = longSegment ? innerRoundedness : outerRoundedness;
                    var cp2Roundedness = longSegment ? outerRoundedness : innerRoundedness;
                    var cp1Radius = longSegment ? innerRadius : outerRadius;
                    var cp2Radius = longSegment ? outerRadius : innerRadius;

                    var cp1X = cp1Radius * cp1Roundedness * PolystarMagicNumber * cp1Dx;
                    var cp1Y = cp1Radius * cp1Roundedness * PolystarMagicNumber * cp1Dy;
                    var cp2X = cp2Radius * cp2Roundedness * PolystarMagicNumber * cp2Dx;
                    var cp2Y = cp2Radius * cp2Roundedness * PolystarMagicNumber * cp2Dy;
                    if (partialPointAmount != 0)
                    {
                        if (i == 0)
                        {
                            cp1X *= partialPointAmount;
                            cp1Y *= partialPointAmount;
                        }
                        else if (i == numPoints - 1)
                        {
                            cp2X *= partialPointAmount;
                            cp2Y *= partialPointAmount;
                        }
                    }

                    _path.CubicTo(previousX - cp1X, previousY - cp1Y, x + cp2X, y + cp2Y, x, y);
                }

                currentAngle += dTheta;
                longSegment = !longSegment;
            }


            var position = _positionAnimation.Value;
            if (position != null)
            {
                _path.Offset(position.X, position.Y);
            }
            _path.Close();
        }

        private void CreatePolygonPath()
        {
            var points = (float)Math.Floor(_pointsAnimation.Value.Value);
            double currentAngle = _rotationAnimation?.Value ?? 0f;
            // Start at +y instead of +x
            currentAngle -= 90;
            // convert to radians
            currentAngle = MathExt.ToRadians(currentAngle);
            // adjust current angle for partial points
            var anglePerPoint = (float)(2 * Math.PI / points);

            var roundedness = _outerRoundednessAnimation.Value.Value / 100f;
            var radius = _outerRadiusAnimation.Value.Value;
            float x;
            float y;
            x = (float)(radius * Math.Cos(currentAngle));
            y = (float)(radius * Math.Sin(currentAngle));
            _path.MoveTo(x, y);
            currentAngle += anglePerPoint;

            var numPoints = (int)Math.Ceiling(points);
            for (var i = 0; i < numPoints; i++)
            {
                var previousX = x;
                var previousY = y;
                x = (float)(radius * Math.Cos(currentAngle));
                y = (float)(radius * Math.Sin(currentAngle));

                if (roundedness != 0)
                {
                    var cp1Theta = (float)(Math.Atan2(previousY, previousX) - Math.PI / 2f);
                    var cp1Dx = (float)Math.Cos(cp1Theta);
                    var cp1Dy = (float)Math.Sin(cp1Theta);

                    var cp2Theta = (float)(Math.Atan2(y, x) - Math.PI / 2f);
                    var cp2Dx = (float)Math.Cos(cp2Theta);
                    var cp2Dy = (float)Math.Sin(cp2Theta);

                    var cp1X = radius * roundedness * PolygonMagicNumber * cp1Dx;
                    var cp1Y = radius * roundedness * PolygonMagicNumber * cp1Dy;
                    var cp2X = radius * roundedness * PolygonMagicNumber * cp2Dx;
                    var cp2Y = radius * roundedness * PolygonMagicNumber * cp2Dy;
                    _path.CubicTo(previousX - cp1X, previousY - cp1Y, x + cp2X, y + cp2Y, x, y);
                }
                else
                {
                    _path.LineTo(x, y);
                }

                currentAngle += anglePerPoint;
            }

            var position = _positionAnimation.Value;
            if (position != null)
            {
                _path.Offset(position.X, position.Y);
            }
            _path.Close();
        }
    }
}