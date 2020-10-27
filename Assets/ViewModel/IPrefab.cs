using Core.Utils.Vector;

namespace Hopper.ViewModel
{
    public interface IPrefab<out T> where T : ISceneEnt
    {
        T Instantiate(IntVector2 pos, IntVector2 orientation);
    }
}