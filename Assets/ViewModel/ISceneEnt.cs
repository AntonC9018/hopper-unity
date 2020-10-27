using System.Collections.Generic;
using Core.Utils.Vector;

namespace Hopper.ViewModel
{
    public interface ISceneEnt
    {
        IReadOnlyList<ISieve> Sieves { get; }
        // void Destroy();
        void Update(Core.History.EntityState finalState, ISieve sieve, AnimationInfo animationInfo);
        void ChangePos(Vector2 pos);
        void ChangeOrientation(IntVector2 orientation);
    }
}