using Core.Utils.Vector;

namespace Hopper.ViewModel
{
    public enum AnimationCode
    {
        Destroy, None, Jump
    }

    public interface IAnimationGuide
    {
        AnimationCode AnimationCode { get; }
    }
}