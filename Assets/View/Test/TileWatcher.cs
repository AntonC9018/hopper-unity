using System.Collections.Generic;
using Hopper.Core;
using Hopper.ViewModel;
using Hopper.Utils.Vector;

namespace Hopper.View
{
    // TODO: move this static class to wherever the generator output is realized
    public static class TileStuff
    {
        public static readonly WorldEventPath<IntVector2> CreatedEventPath = new WorldEventPath<IntVector2>();
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
            TileStuff.CreatedEventPath.Subscribe(world, AddTile);
        }

        private void AddTile(IntVector2 pos)
        {
            var scent = m_tilePrefab.Instantiate(pos, IntVector2.Right);
            m_scents.Add(scent);
        }
    }
}