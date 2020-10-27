using System.Collections.Generic;
using System.Collections.ObjectModel;
using Utils.Vector;

namespace Hopper.ViewModel
{
    public interface ISceneEnt
    {
        ReadOnlyCollection<ISieve> Sieves { get; }
        void Destroy();
        void ChangePos(IntVector2 pos);
        void ChangeOrientation(IntVector2 orientation);
    }
}