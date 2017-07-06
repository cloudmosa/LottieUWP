using Newtonsoft.Json.Linq;

namespace LottieUWP
{
    internal interface IAnimatableValue<out TO>
    {
        IBaseKeyframeAnimation<TO> CreateAnimation();
        bool HasAnimation();
    }

    internal interface IAnimatableValueFactory<out TV>
    {
        TV ValueFromObject(JToken @object, float scale);
    }
}