using System.Collections.Generic;
using Hopper.Utils.Vector;

namespace Hopper.Controller
{
    public class HistoryData
    {
        public EntityStatesAndSieves entityStatesAndSieves;
        public ISceneEntity sceneEnt;
    }

    public interface IAnimator
    {
        void Animate(IEnumerable<HistoryData> historyData);
        void SetCamera(CameraState cameraInitialPosition);
    }
}
