using Utils.Vector;

namespace Hopper.ViewModel
{
    public interface IPrefab<out T> where T : IScent
    {
        T Instantiate(IntVector2 pos, IntVector2 orientation);
    }
}