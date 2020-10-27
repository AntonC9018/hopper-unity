using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Core;
using Core.History;
using Core.Utils;
using Utils;
using Utils.Vector;
using LogicEnt = Core.Entity;           // logent = logical entity

namespace Hopper.ViewModel
{
    public class View_Model
    {
        private Dictionary<int, IPrefab<ISceneEnt>> m_factoryIdPrefabMap;
        private Dictionary<LogicEnt, ISceneEnt> m_activeScentsMap;
        private List<LogicEnt> m_players;
        private IPrefab<ISceneEnt> m_defaultPrefab;
        private ICamera m_camera;

        public View_Model(ICamera camera)
        {
            m_factoryIdPrefabMap = new Dictionary<int, IPrefab<ISceneEnt>>();
            m_activeScentsMap = new Dictionary<LogicEnt, ISceneEnt>();
            m_players = new List<LogicEnt>();
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
        public void SetPrefabForFactory(int factoryId, IPrefab<ISceneEnt> prefab)
        {
            m_factoryIdPrefabMap[factoryId] = prefab;
        }

        public void SetDefaultPrefab(IPrefab<ISceneEnt> prefab)
        {
            m_defaultPrefab = prefab;
        }

        public void AddScentForLogent(LogicEnt logent)
        {
            var factoryId = logent.GetFactoryId();
            IPrefab<ISceneEnt> prefab = m_factoryIdPrefabMap.ContainsKey(factoryId)
                ? m_factoryIdPrefabMap[factoryId]
                : m_defaultPrefab;
            ISceneEnt scent = prefab.Instantiate(logent.Pos, logent.Orientation);
            m_activeScentsMap.Add(logent, scent);
            if (logent.IsPlayer)
            {
                m_players.Add(logent);
            }
        }

        public void Update()
        {
            var logentsToRemove = new List<LogicEnt>();
            foreach (var logent in m_activeScentsMap.Keys)
            {
                // TODO: get states for all of the phases
                ISceneEnt scent = m_activeScentsMap[logent];
                EntityStatesAndSieves newData = GetPhaseStates(logent, scent);
                var lastState = newData.states.FindLast(state => state != null);
                if (lastState == null)
                {
                    continue;
                }
                if (logent.IsDead)
                {
                    // TODO: destroy automatically on detecting the death event
                    scent.Destroy();
                    logentsToRemove.Add(logent);
                }
                else
                {
                    scent.ChangePos(lastState.pos);
                    scent.ChangeOrientation(lastState.orientation);
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

        private class EntityStatesAndSieves
        {
            public EntityState[] states;
            public ISieve[] sieves;
        }

        private EntityStatesAndSieves GetPhaseStates(
            LogicEnt logent, ISceneEnt scent)
        {
            return GetPhaseStates(scent.Sieves, logent.History.Updates, logent.World.m_state.TimeStampPhaseLimit);
        }

        // sieves must be sorted by weight
        private EntityStatesAndSieves GetPhaseStates(
            IList<ISieve> sieves,
            IList<UpdateInfo<EntityState>> updates,
            IList<int> timestampPhaseLimits)
        {
            var result = new EntityStatesAndSieves();
            result.states = new EntityState[World.NumPhases];
            result.sieves = new ISieve[World.NumPhases];

            if (updates.Count == 0)
            {
                return result;
            }

            // construct the list of updates by phases
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
                        System.Console.WriteLine($"Adding {updates[i].updateCode}");
                        i++;
                    }
                }
            }

            for (int phase = 0; phase < World.NumPhases; phase++)
            {
                // take the last state
                result.states[phase] = updatesByPhases[phase].Count > 0
                    ? updatesByPhases[phase].Last().stateAfter
                    : result.states[phase - 1];

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