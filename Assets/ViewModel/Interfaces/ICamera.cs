using Hopper.Utils.Vector;

namespace Hopper.ViewModel
{
    public interface ICamera
    {
        void SetInitialPosition(Vector2 pos);
        void ChangePos(Vector2 pos);
    }
}