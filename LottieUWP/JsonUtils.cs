using Newtonsoft.Json.Linq;

namespace LottieUWP
{
    internal static class JsonUtils
    {
        internal static PointF PointFromJsonObject(JObject values, float scale)
        {
            return new PointF(ValueFromObject(values["x"]) * scale, ValueFromObject(values["y"]) * scale);
        }

        internal static PointF PointFromJsonArray(JObject values, float scale)
        {
            if (values.Count < 2)
            {
                throw new System.ArgumentException("Unable to parse point for " + values);
            }
            var value0 = values[0] != null ? values[0].Value<float>() : 1;
            var value1 = values[1] != null ? values[1].Value<float>() : 1;
            return new PointF(value0 * scale, value1 * scale);
        }

        internal static float ValueFromObject(JToken @object)
        {
            if (@object.Type == JTokenType.Integer || @object.Type == JTokenType.Float)
            {
                return @object.Value<float>();
            }
            if (@object.Type == JTokenType.Array)
            {
                return @object[0].Value<float>();
            }
            return 0;
        }
    }
}