using Utils.Vector;

namespace Hopper.ViewModel
{
    public interface IScent
    {
        void Destroy();
        void ChangePos(IntVector2 pos);
        void ChangeOrientation(IntVector2 orientation);
    }
}