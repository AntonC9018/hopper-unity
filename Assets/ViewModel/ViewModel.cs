using System.Collections.Generic;
using System.Linq;
using Core;
using Core.Utils;
using Core.History;
using LogicEnt = Core.Entity;  // LogicEnt = Logent = Logical Entity

namespace Hopper.ViewModel
{
    public class View_Model
    {
        private Dictionary<int, IPrefab<ISceneEnt>> m_factoryIdPrefabMap;
        private Dictionary<LogicEnt, ISceneEnt> m_activeScentsMap;
        private List<LogicEnt> m_players;
        private IPrefab<ISceneEnt> m_defaultPrefab;
        private IAnimator m_animator; // not really an animator
        private CameraState m_cameraState;

        public View_Model(IAnimator animator)
        {
            m_factoryIdPrefabMap = new Dictionary<int, IPrefab<ISceneEnt>>();
            m_activeScentsMap = new Dictionary<LogicEnt, ISceneEnt>();
            m_players = new List<LogicEnt>();

            // The contract between these two `managers` (their duties) should
            // be outlined more explicitly (currently heck knows where's the line between them)
            m_animator = animator;
        }

        public void WatchWorld(World world, params IWatcher[] customWatchers)
        {
            world.SpawnEntityEvent += AddScentForLogent;
            world.State.EndOfLoopEvent += Update;
            foreach (var customWatcher in customWatchers)
            {
                // TODO: These events should prefferably be directly associated with the world
                // this would remove constant checks to see if the world is right
                // which are a hassle.
                customWatcher.Watch(world, this);
            }
        }

        // factory is the entity kind
        public void SetPrefabForFactory(int factoryId, IPrefab<ISceneEnt> prefab)
        {
            m_factoryIdPrefabMap[factoryId] = prefab;
        }

        public void SetDefaultPrefab(IPrefab<ISceneEnt> prefab)
        {
            m_defaultPrefab = prefab;
        }

        private void AddScentForLogent(LogicEnt logent)
        {
            // TODO: spawn it on the screen at the exact timeframe (or phase) 
            // the easiest way to implement this would be through the sieves
            // which would unhide the entity, but then we need an extra phase for that
            // So this one's an open question
            IPrefab<ISceneEnt> prefab = GetPrefabForLogent(logent);
            ISceneEnt scent = prefab.Instantiate(logent.Pos, logent.Orientation);

            m_activeScentsMap.Add(logent, scent);

            if (logent.IsPlayer)
            {
                m_players.Add(logent);
                // This is also probably pretty meh, since this should probably only 
                // be called at the frame before the first frame that this ViewModel
                // exists. There should also be the ability to bring it into that state without unsubbing from
                // the world.
                if (m_players.Count == 1)
                {
                    m_cameraState = new CameraState(m_players);
                    m_animator.SetCamera(m_cameraState);
                }
            }
        }

        private IPrefab<ISceneEnt> GetPrefabForLogent(LogicEnt logent)
        {
            var factoryId = logent.GetFactoryId();

            // TODO: this should be a separate method
            IPrefab<ISceneEnt> prefab = m_factoryIdPrefabMap.ContainsKey(factoryId)
                ? m_factoryIdPrefabMap[factoryId]
                : m_defaultPrefab;
            return prefab;
        }

        private void Update()
        {
            var historyData = new List<HistoryData>(m_activeScentsMap.Count);
            m_cameraState.NextLoop();

            foreach (var logent in m_activeScentsMap.Keys)
            {
                ISceneEnt scent = m_activeScentsMap[logent];
                EntityStatesAndSieves newData = GetPhaseStates(logent);

                var dataPoint = new HistoryData
                {
                    sceneEnt = scent,
                    entityStatesAndSieves = newData
                };

                historyData.Add(dataPoint);

                if (logent.IsPlayer)
                {
                    m_cameraState.AccumulateStates(newData.states);
                }
            }

            RemoveDeadLogents();
            m_cameraState.AverageOutStates();
            m_animator.Animate(historyData);
        }

        private void RemoveDeadLogents()
        {
            var logentsToRemove = new List<LogicEnt>();
            foreach (var logent in m_activeScentsMap.Keys)
            {
                if (logent.IsDead)
                {
                    logentsToRemove.Add(logent);
                }
            }
            foreach (var logent in logentsToRemove)
            {
                m_activeScentsMap.Remove(logent);
            }
        }

        private EntityStatesAndSieves GetPhaseStates(LogicEnt logent)
        {
            // construct the list of updates by phases
            // this is sort of necessary, but i sort of don't like it
            var updatesByPhases = SplitUpdatesByPhases(
                logent.History.Updates, logent.World.State.UpdateCountPhaseLimit);

            return GetResultantStatesAndSieves(
                GetPrefabForLogent(logent).Sieves, updatesByPhases);
        }

        private static EntityStatesAndSieves GetResultantStatesAndSieves(
            IReadOnlyList<ISieve> sieves,
            List<UpdateInfo<EntityState>>[] updatesByPhases)
        {
            var result = new EntityStatesAndSieves
            {
                states = new EntityState[World.NumPhases],
                sieves = new ISieve[World.NumPhases]
            };

            // The first update always indicates the initial state. See History.
            result.states[0] = updatesByPhases[0][0].stateAfter;

            for (int phase = 0; phase < World.NumPhases; phase++)
            {
                if (updatesByPhases[phase].Count > 0)
                {
                    result.states[phase] = updatesByPhases[phase].Last().stateAfter;
                }
                else
                {
                    result.states[phase] = result.states[phase - 1];
                }

                result.sieves[phase] = SelectSieve(sieves, updatesByPhases[phase]);
            }

            return result;
        }

        private static ISieve SelectSieve(
            IReadOnlyList<ISieve> sieves,
            List<UpdateInfo<EntityState>> updates)
        {
            // update all sieves
            foreach (var update in updates)
            {
                foreach (var sieve in sieves)
                {
                    sieve.Sieve(update.updateCode);
                }
            }

            // take the first full sieve
            var result = sieves.Find(sieve => sieve.IsFull);

            // reset sieves
            foreach (var sieve in sieves)
                sieve.Reset();

            return result;
        }

        private static List<UpdateInfo<EntityState>>[] SplitUpdatesByPhases(
            IReadOnlyList<UpdateInfo<EntityState>> updates,
            IReadOnlyList<int> timestampPhaseLimits)
        {
            List<UpdateInfo<EntityState>>[] result =
                new List<UpdateInfo<EntityState>>[World.NumPhases];

            {
                int i = 0;
                for (int phase = 0; phase < World.NumPhases; phase++)
                {
                    result[phase] = new List<UpdateInfo<EntityState>>();
                    // go through updates of the current phase
                    while (i < updates.Count && updates[i].timeframe < timestampPhaseLimits[phase])
                    {
                        result[phase].Add(updates[i]);
                        i++;
                    }
                }
            }

            return result;
        }
    }
}