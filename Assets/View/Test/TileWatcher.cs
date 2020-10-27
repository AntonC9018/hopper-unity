using System.Collections.Generic;
using System.Linq;
using Core;
using Hopper.ViewModel;
using Test;
using UnityEngine;
using Utils.Vector;

namespace Hopper.View
{
    // TODO: move this static class to wherever the generator output is realized
    public static class TileStuff
    {
        public static event System.Action<IntVector2> CreatedEvent;

        public static void FireCreatedEvent(IntVector2 pos)
        {
            CreatedEvent?.Invoke(pos);
        }
    }

    public class TileWatcher : IWatcher
    {
        private Prefab<SceneEnt> m_tilePrefab;
        private List<ISceneEnt> m_scents;

        public TileWatcher(Prefab<SceneEnt> prefab)
        {
            m_tilePrefab = prefab;
            m_scents = new List<ISceneEnt>();
        }

        public void Watch(World world, View_Model vm)
        {
            TileStuff.CreatedEvent += AddTile;
        }

        private void AddTile(IntVector2 pos)
        {
            var scent = m_tilePrefab.Instantiate(pos, IntVector2.Right);
            m_scents.Add(scent);
        }
    }
}