using Utils.Vector;

namespace Hopper.ViewModel
{
    public interface IPrefab
    {
        IScent Instantiate(IntVector2 pos, IntVector2 orientation);
    }
}