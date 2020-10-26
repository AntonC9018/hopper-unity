using System.Collections.Generic;
using Core;
using Core.History;
using Core.Utils;
using Utils.Vector;
using Logent = Core.Entity;           // logent = logical entity

namespace Hopper.ViewModel
{
    public class View_Model
    {
        private Dictionary<int, IPrefab<IScent>> m_factoryIdPrefabMap;
        private Dictionary<Logent, IScent> m_activeScentsMap;
        private List<Logent> m_players;
        private IPrefab<IScent> m_defaultPrefab;
        private ICamera m_camera;

        public View_Model(ICamera camera)
        {
            m_factoryIdPrefabMap = new Dictionary<int, IPrefab<IScent>>();
            m_activeScentsMap = new Dictionary<Logent, IScent>();
            m_players = new List<Logent>();
            m_camera = camera;
        }

        public void WatchWorld(World world, params IWatcher[] customWatchers)
        {
            world.SpawnEntityEvent += AddScentForLogent;
            world.m_state.EndOfLoopEvent += Update;
            foreach (var customWatcher in customWatchers)
            {
                customWatcher.Watch(world, this);
            }
        }

        // factory is, in essence, the entity type
        public void SetPrefabForFactory(int factoryId, IPrefab<IScent> prefab)
        {
            m_factoryIdPrefabMap[factoryId] = prefab;
        }

        public void SetDefaultPrefab(IPrefab<IScent> prefab)
        {
            m_defaultPrefab = prefab;
        }

        public void AddScentForLogent(Logent logent)
        {
            var factoryId = logent.GetFactoryId();
            IPrefab<IScent> prefab = m_factoryIdPrefabMap.ContainsKey(factoryId)
                ? m_factoryIdPrefabMap[factoryId]
                : m_defaultPrefab;
            IScent scent = prefab.Instantiate(logent.Pos, logent.Orientation);
            m_activeScentsMap.Add(logent, scent);
            if (logent.IsPlayer)
            {
                m_players.Add(logent);
            }
        }

        public void Update()
        {
            var logentsToRemove = new List<Logent>();
            foreach (var logent in m_activeScentsMap.Keys)
            {
                EntityState newState = GetLastStateOf(logent);
                IScent scent = m_activeScentsMap[logent];
                if (logent.IsDead)
                {
                    scent.Destroy();
                    logentsToRemove.Add(logent);
                }
                else
                {
                    scent.ChangePos(newState.pos);
                    scent.ChangeOrientation(newState.orientation);
                }
            }
            foreach (var logent in logentsToRemove)
            {
                m_activeScentsMap.Remove(logent);
            }
            var cameraPos = GetCenterBetweenPlayers();
            m_camera.ChangePos(cameraPos);
        }

        private IntVector2 GetCenterBetweenPlayers()
        {
            var sum = new IntVector2(0, 0);
            foreach (var player in m_players)
            {
                sum += player.Pos;
            }
            return sum;
        }

        private EntityState GetLastStateOf(Logent logent)
        {
            var history = logent.History;

            var latestChangePosUpdate = history.Updates.FindLast(
                e => e.updateCode == UpdateCode.move_do || e.updateCode == UpdateCode.displaced_do
            );
            if (latestChangePosUpdate != null)
            {
                return latestChangePosUpdate.stateAfter;
            }

            return new EntityState(logent);
        }
    }
}