using Hopper.Controller;
using UnityEngine;
using Hopper.Utils.Vector;
using System.Collections.Generic;
using System.Linq;

namespace Hopper.View
{
    public class Model<T> : IModel<T> where T : SceneEnt, new()
    {
        private GameObject m_obj;
        private List<IViewSieve> m_sieves;
        public IReadOnlyList<ISieve> Sieves => m_sieves;


        public Model(GameObject prefab, params IViewSieve[] sieves)
        {
            m_obj = prefab;
            m_sieves = sieves.ToList();
            m_sieves.Sort((a, b) => a.Weight - b.Weight);
        }

        public T Instantiate(IntVector2 pos, IntVector2 orientation)
        {
            var obj = GameObject.Instantiate(m_obj);
            var scent = new T();
            scent.GameObject = obj;
            scent.SetInitialPosition(pos);
            scent.SetInitialOrientation(orientation);
            return scent;
        }
    }
}