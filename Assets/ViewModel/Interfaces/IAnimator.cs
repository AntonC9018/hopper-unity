using System.Collections.Generic;
using Hopper.Utils.Vector;

namespace Hopper.ViewModel
{
    public class HistoryData
    {
        public EntityStatesAndSieves entityStatesAndSieves;
        public ISceneEnt sceneEnt;
    }

    public interface IAnimator
    {
        void Animate(IEnumerable<HistoryData> historyData);
        void SetCamera(CameraState cameraInitialPosition);
    }
}
