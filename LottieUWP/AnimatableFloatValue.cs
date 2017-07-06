﻿using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace LottieUWP
{
    internal class AnimatableFloatValue : BaseAnimatableValue<float?, float?>
    {
        private AnimatableFloatValue() : base(0f)
        {
        }

        private AnimatableFloatValue(IList<IKeyframe<float?>> keyframes, float? initialValue) : base(keyframes, initialValue)
        {
        }

        protected override float? ConvertType(float? value)
        {
            return value;
        }

        public override IBaseKeyframeAnimation<float?> CreateAnimation()
        {
            if (!HasAnimation())
            {
                return new StaticKeyframeAnimation<float?>(_initialValue);
            }

            return new FloatKeyframeAnimation(Keyframes);
        }

        public override float? InitialValue => _initialValue;

        private class ValueFactory : IAnimatableValueFactory<float?>
        {
            internal static readonly ValueFactory Instance = new ValueFactory();

            public virtual float? ValueFromObject(JToken @object, float scale)
            {
                return JsonUtils.ValueFromObject(@object) * scale;
            }
        }

        internal static class Factory
        {
            internal static AnimatableFloatValue NewInstance()
            {
                return new AnimatableFloatValue();
            }

            internal static AnimatableFloatValue NewInstance(JObject json, LottieComposition composition, bool isDp = true)
            {
                var scale = isDp ? composition.DpScale : 1f;
                if (json != null && json["x"] != null)
                {
                    composition.AddWarning("Lottie doesn't support expressions.");
                }
                var result = AnimatableValueParser<float?>.NewInstance(json, scale, composition, ValueFactory.Instance).ParseJson();
                return new AnimatableFloatValue(result.Keyframes, result.InitialValue);
            }
        }
    }
}