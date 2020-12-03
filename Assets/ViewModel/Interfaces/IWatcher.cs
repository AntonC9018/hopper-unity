using Core;

namespace Hopper.ViewModel
{
    public interface IWatcher
    {
        void Watch(World world, View_Model vm);
    }
}