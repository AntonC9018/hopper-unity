using System.Collections.Generic;
using System.Linq;
using Core;
using Core.Utils;
using Core.History;
using Core.Utils.Vector;
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
        private Dictionary<LogicEnt, EntityState> m_prevStates;

        public View_Model(IAnimator animator)
        {
            m_factoryIdPrefabMap = new Dictionary<int, IPrefab<ISceneEnt>>();
            m_activeScentsMap = new Dictionary<LogicEnt, ISceneEnt>();
            m_players = new List<LogicEnt>();

            // This one is absolutely hideous
            m_prevStates = new Dictionary<LogicEnt, EntityState>();

            // The contract between these two `managers` (their duties) should
            // be outlined more explicitly (currently heck knows where's the line between them)
            m_animator = animator;
        }

        public void WatchWorld(World world, params IWatcher[] customWatchers)
        {
            world.SpawnEntityEvent += AddScentForLogent;
            world.m_state.EndOfLoopEvent += Update;
            foreach (var customWatcher in customWatchers)
            {
                // TODO: These events should prefferably be directly associated with the world
                // this would remove constant checks to see if the world is right
                // which are a hassle.
                customWatcher.Watch(world, this);
            }
        }

        // factory is, in essence, the entity type
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
            var factoryId = logent.GetFactoryId();

            // TODO: this should be a separate method
            IPrefab<ISceneEnt> prefab = m_factoryIdPrefabMap.ContainsKey(factoryId)
                ? m_factoryIdPrefabMap[factoryId]
                : m_defaultPrefab;
            ISceneEnt scent = prefab.Instantiate(logent.Pos, logent.Orientation);

            m_activeScentsMap.Add(logent, scent);
            m_prevStates.Add(logent, new EntityState(logent));

            if (logent.IsPlayer)
            {
                m_players.Add(logent);
                // This is also probably pretty meh, since this should probably only 
                // be called at the frame before the first frame that this ViewModel
                // exists. There should also be the ability to bring it into that state without unsubbing from
                // the world.
                if (m_players.Count == 1)
                {
                    m_animator.SetupCamera(GetCenterBetweenPlayers());
                }
            }
        }

        private void Update()
        {
            // TODO: this is too low level, refactor
            var logentsToRemove = new List<LogicEnt>();

            // TODO: rethink what is done where
            var cameraData = CreateCameraDataArray();
            var historyData = new List<HistoryData>();

            foreach (var logent in m_activeScentsMap.Keys)
            {
                ISceneEnt scent = m_activeScentsMap[logent];
                EntityStatesAndSieves newData = GetPhaseStates(logent, scent);
                var dataPoint = new HistoryData
                {
                    sceneEnt = scent,
                    entityStatesAndSieves = newData
                };

                historyData.Add(dataPoint);

                if (logent.IsPlayer)
                {
                    // TODO: too low level, too much code
                    // this needs to be a separate class, ideally
                    AccumulateStates(cameraData, newData.states);
                }
                if (logent.IsDead)
                {
                    logentsToRemove.Add(logent);
                }
            }

            foreach (var logent in logentsToRemove)
            {
                m_activeScentsMap.Remove(logent);
                m_prevStates.Remove(logent);
            }

            FindMean(cameraData, m_players.Count);
            m_animator.Animate(historyData, cameraData);
        }

        private Vector2 GetCenterBetweenPlayers()
        {
            var sum = new Vector2(0, 0);
            foreach (var player in m_players)
            {
                sum += player.Pos;
            }
            return sum;
        }

        private Vector2[] CreateCameraDataArray()
        {
            var arr = new Vector2[World.NumPhases];
            for (int i = 0; i < World.NumPhases; i++)
            {
                arr[i] = new Vector2(0, 0);
            }
            return arr;
        }

        private void AccumulateStates(Vector2[] accumulator, EntityState[] states)
        {
            for (int i = 0; i < accumulator.Length; i++)
            {
                accumulator[i] = accumulator[i] + states[i].pos;
            }
        }

        private void FindMean(Vector2[] accumulator, int count)
        {
            for (int i = 0; i < accumulator.Length; i++)
            {
                accumulator[i] /= count;
            }
        }

        private EntityStatesAndSieves GetPhaseStates(
            LogicEnt logent, ISceneEnt scent)
        {
            return GetPhaseStates(
                scent.Sieves,
                logent.History.Updates,
                logent.World.m_state.TimeStampPhaseLimit,
                logent);
        }

        // It is assumed that sieves are sorted by weights
        // TODO: change input params
        private EntityStatesAndSieves GetPhaseStates(
            IReadOnlyList<ISieve> sieves,
            IReadOnlyList<UpdateInfo<EntityState>> updates,
            IList<int> timestampPhaseLimits,
            LogicEnt logent)
        {
            var result = new EntityStatesAndSieves();
            result.states = new EntityState[World.NumPhases];
            result.sieves = new ISieve[World.NumPhases];

            // construct the list of updates by phases
            // this is sort of necessary, but i sort of don't like it
            // TODO: Refactor in a separate method
            List<UpdateInfo<EntityState>>[] updatesByPhases =
                new List<UpdateInfo<EntityState>>[World.NumPhases];

            {
                int i = 0;
                for (int phase = 0; phase < World.NumPhases; phase++)
                {
                    updatesByPhases[phase] = new List<UpdateInfo<EntityState>>();
                    // go through updates of the current phase
                    while (i < updates.Count && updates[i].timeframe < timestampPhaseLimits[phase])
                    {
                        updatesByPhases[phase].Add(updates[i]);
                        i++;
                    }
                }
            }

            // TODO: Refactor in a method
            for (int phase = 0; phase < World.NumPhases; phase++)
            {
                if (updatesByPhases[phase].Count > 0)
                {
                    result.states[phase] = updatesByPhases[phase].Last().stateAfter;
                    m_prevStates[logent] = result.states[phase];
                }
                else
                {
                    result.states[phase] = m_prevStates[logent];
                }

                // update all sieves
                foreach (var update in updatesByPhases[phase])
                {
                    foreach (var sieve in sieves)
                    {
                        sieve.Sieve(update.updateCode);
                    }
                }

                // take the first full sieve
                result.sieves[phase] = sieves.Find(sieve => sieve.IsFull);

                // reset sieves
                foreach (var sieve in sieves)
                    sieve.Reset();
            }

            return result;
        }
    }
}