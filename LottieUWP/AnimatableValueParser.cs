﻿using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace LottieUWP
{
    internal class AnimatableValueParser<T>
    {
        private readonly JObject _json;
        private readonly float _scale;
        private readonly LottieComposition _composition;
        private readonly IAnimatableValueFactory<T> _valueFactory;

        private AnimatableValueParser(JObject json, float scale, LottieComposition composition, IAnimatableValueFactory<T> valueFactory)
        {
            _json = json;
            _scale = scale;
            _composition = composition;
            _valueFactory = valueFactory;
        }

        internal static AnimatableValueParser<T> NewInstance(JObject json, float scale, LottieComposition composition, IAnimatableValueFactory<T> valueFactory)
        {
            return new AnimatableValueParser<T>(json, scale, composition, valueFactory);
        }

        internal virtual Result ParseJson()
        {
            var keyframes = ParseKeyframes();
            var initialValue = ParseInitialValue(keyframes);
            return new Result(keyframes, initialValue);
        }

        private IList<IKeyframe<T>> ParseKeyframes()
        {
            if (_json != null)
            {
                var k = _json["k"];
                if (HasKeyframes(k))
                {
                    return Keyframe<T>.KeyFrameFactory.ParseKeyframes((JArray)k, _composition, _scale, _valueFactory);
                }
                return new List<IKeyframe<T>>();
            }
            return new List<IKeyframe<T>>();
        }

        private T ParseInitialValue(IList<IKeyframe<T>> keyframes)
        {
            if (_json != null)
            {
                if (keyframes.Count > 0)
                {
                    return keyframes[0].StartValue;
                }
                return _valueFactory.ValueFromObject(_json["k"], _scale);
            }
            return default(T);
        }

        private static bool HasKeyframes(JToken json)
        {
            if (json.Type != JTokenType.Array)
            {
                return false;
            }

            var firstObject = ((JArray)json)[0];
            return firstObject.Type == JTokenType.Object && ((JObject)firstObject)["t"] != null;
        }

        internal class Result
        {
            internal readonly IList<IKeyframe<T>> Keyframes;
            internal readonly T InitialValue;

            internal Result(IList<IKeyframe<T>> keyframes, T initialValue)
            {
                Keyframes = keyframes;
                InitialValue = initialValue;
            }
        }
    }
}