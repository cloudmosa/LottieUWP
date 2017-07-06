﻿using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace LottieUWP
{
    public interface IKeyframe<out T>
    {
        T StartValue { get; }
        T EndValue { get; }
        float? StartFrame { get; }
        float? EndFrame { get; set; }
        float StartProgress { get; }
        bool ContainsProgress(float progress);
        bool Static { get; }
        float EndProgress { get; }
        IInterpolator Interpolator { get; }
    }

    public class Keyframe<T> : IKeyframe<T>
    {
        /// <summary>
        /// Some animations get exported with insane cp values in the tens of thousands.
        /// PathInterpolator fails to create the interpolator in those cases and hangs.
        /// Clamping the cp helps prevent that.
        /// </summary>
        private const float MaxCpValue = 100;
        private static readonly IInterpolator LinearInterpolator = new LinearInterpolator();

        /// <summary>
        /// The json doesn't include end frames. The data can be taken from the start frame of the next
        /// keyframe though.
        /// </summary>
        internal static void SetEndFrames<TU, TV>(IList<TU> keyframes) where TU : IKeyframe<TV>
        {
            var size = keyframes.Count;
            for (var i = 0; i < size - 1; i++)
            {
                // In the json, the value only contain their starting frame.
                keyframes[i].EndFrame = keyframes[i + 1].StartFrame;
            }
            var lastKeyframe = keyframes[size - 1];
            if (lastKeyframe.StartValue == null)
            {
                // The only purpose the last keyframe has is to provide the end frame of the previous
                // keyframe.
                keyframes.Remove(lastKeyframe);
            }
        }

        private readonly LottieComposition _composition;
        public T StartValue { get; }
        public T EndValue { get; }
        public IInterpolator Interpolator { get; }
        public float? StartFrame { get; }
        public float? EndFrame { get; set; }

        private float _startProgress = float.MinValue; 
        private float _endProgress = float.MinValue;

        public Keyframe(LottieComposition composition, T startValue, T endValue, IInterpolator interpolator, float? startFrame, float? endFrame)
        {
            _composition = composition;
            StartValue = startValue;
            EndValue = endValue;
            Interpolator = interpolator;
            StartFrame = startFrame;
            EndFrame = endFrame;
        }

        public virtual float StartProgress
        {
            get
            {
                if (_startProgress == float.MinValue)
                {
                    _startProgress = (StartFrame.Value - _composition.StartFrame) / _composition.DurationFrames;
                }
                return _startProgress;
            }
        }

        public virtual float EndProgress
        {
            get
            {
                if (_endProgress == float.MinValue)
                {
                    if (EndFrame == null)
                    {
                        _endProgress = 1f;
                    }
                    else
                    {
                        var startProgress = StartProgress;
                        var durationFrames = EndFrame.Value - StartFrame.Value;
                        var durationProgress = durationFrames / _composition.DurationFrames;
                        _endProgress = startProgress + durationProgress;
                    }
                }
                return _endProgress;
            }
        }

        public virtual bool Static => Interpolator == null;

        public virtual bool ContainsProgress(float progress)
        {
            return progress >= StartProgress && progress <= EndProgress;
        }

        public override string ToString()
        {
            return "Keyframe{" + "startValue=" + StartValue + ", endValue=" + EndValue + ", startFrame=" + StartFrame + ", endFrame=" + EndFrame + ", interpolator=" + Interpolator + '}';
        }

        internal static class KeyFrameFactory
        {
            internal static Keyframe<T> NewInstance(JObject json, LottieComposition composition, float scale, IAnimatableValueFactory<T> valueFactory)
            {
                PointF cp1 = null;
                PointF cp2 = null;
                float startFrame = 0;
                var startValue = default(T);
                var endValue = default(T);
                IInterpolator interpolator = null;

                if (json["t"] != null && (json["t"].Type == JTokenType.Float || json["t"].Type == JTokenType.Integer)) {
                    startFrame = json["t"].Value<float>();
                    if (json.TryGetValue("s", out startValueJson)) {
                        startValue = valueFactory.ValueFromObject(startValueJson, scale);
                    }

                    JToken endValueJson;
                    if (json.TryGetValue("e", out endValueJson)) {
                        endValue = valueFactory.ValueFromObject(endValueJson, scale);
                    }

                    var cp1Json = json["o"];
                    var cp2Json = json["i"];
                    if (cp1Json != null && cp1Json.Type == JTokenType.Object &&
                        cp2Json != null && cp2Json.Type == JTokenType.Object) {
                        cp1 = JsonUtils.PointFromJsonObject((JObject)cp1Json, scale);
                        cp2 = JsonUtils.PointFromJsonObject((JObject)cp2Json, scale);
                    }

                    var holdValue = json["h"] != null ? json["h"].Value<int>() : 0;
                    var hold = holdValue == 1;

                    if (hold)
                    {
                        endValue = startValue;
                        // TODO: create a HoldInterpolator so progress changes don't invalidate.
                        interpolator = LinearInterpolator;
                    }
                    else if (cp1 != null)
                    {
                        cp1 = new PointF(MiscUtils.Clamp(cp1.X, -scale, scale),
                            MiscUtils.Clamp(cp1.Y, -MaxCpValue, MaxCpValue));
                        cp2 = new PointF(MiscUtils.Clamp(cp2.X, -scale, scale),
                            MiscUtils.Clamp(cp2.Y, -MaxCpValue, MaxCpValue));
                        interpolator = new PathInterpolator(cp1.X / scale, cp1.Y / scale, cp2.X / scale, cp2.Y / scale);
                    }
                    else
                    {
                        interpolator = LinearInterpolator;
                    }
                }
                else
                {
                    startValue = valueFactory.ValueFromObject(json, scale);
                    endValue = startValue;
                }
                return new Keyframe<T>(composition, startValue, endValue, interpolator, startFrame, null);
            }

            internal static IList<IKeyframe<T>> ParseKeyframes(JArray json, LottieComposition composition, float scale, IAnimatableValueFactory<T> valueFactory)
            {
                var length = json.Count;
                if (length == 0)
                {
                    return new List<IKeyframe<T>>();
                }
                IList<IKeyframe<T>> keyframes = new List<IKeyframe<T>>();
                for (uint i = 0; i < length; i++)
                {
                    keyframes.Add(NewInstance((JObject)json[i], composition, scale, valueFactory));
                }

                SetEndFrames<IKeyframe<T>, T>(keyframes);
                return keyframes;
            }
        }
    }
}