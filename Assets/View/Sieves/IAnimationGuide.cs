using Core.Utils.Vector;

namespace Hopper.ViewModel
{
    public enum AnimationCode
    {
        Destroy, None
    }

    public interface IAnimationGuide
    {
        AnimationCode AnimationCode { get; }
    }
}