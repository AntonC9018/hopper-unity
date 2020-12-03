using System.Collections.Generic;
using Core.Utils.Vector;

namespace Hopper.ViewModel
{
    public interface ISceneEnt
    {
        void EnterPhase(Core.History.EntityState finalState, ISieve sieve, AnimationInfo animationInfo);
        void Update(AnimationInfo animationInfo);
        void ChangePos(Vector2 pos);
        void ChangeOrientation(IntVector2 orientation);
    }
}