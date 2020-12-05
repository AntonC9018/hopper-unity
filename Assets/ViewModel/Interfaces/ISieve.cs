using Hopper.Core.History;

namespace Hopper.ViewModel
{
    public interface ISieve
    {
        int Weight { get; }
        bool IsFull { get; }
        void Sieve(UpdateCode code);
        void Reset();
    }
}