using Hopper.ViewModel;
using UnityEngine;
using Utils.Vector;

namespace Hopper.View
{
    public class Prefab<T> : IPrefab<T> where T : Scent, new()
    {
        private GameObject m_obj;

        public Prefab(GameObject prefab)
        {
            m_obj = prefab;
        }

        public T Instantiate(IntVector2 pos, IntVector2 orientation)
        {
            var obj = GameObject.Instantiate(m_obj);
            var scent = new T();
            scent.GameObject = obj;
            scent.ChangePos(pos);
            scent.ChangeOrientation(orientation);
            return scent;
        }
    }
}