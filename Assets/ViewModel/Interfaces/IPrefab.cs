using System.Collections.Generic;
using Hopper.Core.History;
using Hopper.Utils.Vector;

namespace Hopper.ViewModel
{
    public interface IPrefab<out T> where T : ISceneEnt
    {
        T Instantiate(IntVector2 pos, IntVector2 orientation);
        IReadOnlyList<ISieve> Sieves { get; }
    }
}