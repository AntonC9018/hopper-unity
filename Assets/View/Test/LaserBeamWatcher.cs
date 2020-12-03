using System.Collections.Generic;
using Core;
using Hopper.ViewModel;
using Test;
using UnityEngine;

namespace Hopper.View
{
    public class LaserScentInfo
    {
        public LaserInfo laser_info;
        public List<RegularRotationSceneEnt> body;
        public RegularRotationSceneEnt head;
    }

    public class LaserBeamWatcher : IWatcher
    {
        private Prefab<RegularRotationSceneEnt> m_beamHeadPrefab;
        private Prefab<RegularRotationSceneEnt> m_beamBodyPrefab;
        private List<LaserScentInfo> m_activeBeams;

        public LaserBeamWatcher(GameObject headPrefab, GameObject bodyPrefab)
        {
            m_beamHeadPrefab = new Prefab<RegularRotationSceneEnt>(headPrefab);
            m_beamBodyPrefab = new Prefab<RegularRotationSceneEnt>(bodyPrefab);
            m_activeBeams = new List<LaserScentInfo>();
        }

        public void Watch(World world, View_Model vm)
        {
            Test.Laser.EventPath.Subscribe(world, AddBeam);
            world.State.StartOfLoopEvent += UpdateBeams;
        }

        public void AddBeam(LaserInfo laser_info)
        {
            var beam_info = new LaserScentInfo();
            beam_info.laser_info = laser_info;
            beam_info.head = m_beamHeadPrefab.Instantiate(laser_info.pos_start, -laser_info.direction);

            int numBodyElements = (laser_info.pos_end - laser_info.pos_start).Abs().ComponentSum();
            beam_info.body = new List<RegularRotationSceneEnt>(numBodyElements);

            for (int i = 0; i < numBodyElements; i++)
            {
                beam_info.body.Add(m_beamBodyPrefab.Instantiate(
                    laser_info.pos_start + laser_info.direction * (i + 1),
                    -laser_info.direction));
            }

            m_activeBeams.Add(beam_info);
        }

        private void UpdateBeams()
        {
            foreach (var info in m_activeBeams)
            {
                foreach (var bodyEl in info.body)
                {
                    bodyEl.Destroy();
                }
                info.head.Destroy();
            }
            m_activeBeams.Clear();
        }
    }
}