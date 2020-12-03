using System.Collections.Generic;
using Core;
using Core.Utils.Vector;
using Hopper.ViewModel;
using Test;
using UnityEngine;

namespace Hopper.View
{
    public class ExplosionWatcher : IWatcher
    {
        private Prefab<ExplosionScent> m_explosionPrefab;
        private List<ExplosionScent> m_goingExplosions;

        public ExplosionWatcher(GameObject prefab)
        {
            m_explosionPrefab = new Prefab<ExplosionScent>(prefab);
            m_goingExplosions = new List<ExplosionScent>();
        }

        public void Watch(World world, View_Model vm)
        {
            Explosion.EventPath.Subscribe(world, AddExplosion);
            world.State.EndOfLoopEvent += UpdateGoingExplosions;
        }

        private void AddExplosion(IntVector2 pos)
        {
            var scent = m_explosionPrefab.Instantiate(pos, IntVector2.Right);
            m_goingExplosions.Add(scent);
        }

        private void UpdateGoingExplosions()
        {
            for (int i = m_goingExplosions.Count - 1; i >= 0; i--)
            {
                m_goingExplosions[i].Update();
                if (m_goingExplosions[i].ShouldRemove())
                {
                    m_goingExplosions[i].Destroy();
                    m_goingExplosions.RemoveAt(i);
                }
            }
        }
    }
}