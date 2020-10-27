using Hopper.ViewModel;
using UnityEngine;
using Core.Utils.Vector;

namespace Hopper.View
{
    public class Prefab<T> : IPrefab<T> where T : SceneEnt, new()
    {
        private GameObject m_obj;
        private IViewSieve[] m_sieves;

        public Prefab(GameObject prefab, params IViewSieve[] sieves)
        {
            m_obj = prefab;
            m_sieves = sieves;
        }

        public T Instantiate(IntVector2 pos, IntVector2 orientation)
        {
            var obj = GameObject.Instantiate(m_obj);
            var scent = new T();
            scent.GameObject = obj;
            scent.SetSieves(m_sieves);
            scent.ChangePos(pos);
            scent.ChangeOrientation(orientation);
            return scent;
        }
    }
}