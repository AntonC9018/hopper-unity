using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Hopper.ViewModel;
using UnityEngine;
using Utils.Vector;

namespace Hopper.View
{
    public class SceneEnt : ISceneEnt, ICamera
    {
        // TODO: do smth with this
        public ReadOnlyCollection<ISieve> Sieves => m_sieves.AsReadOnly();
        public GameObject GameObject { protected get; set; }

        private List<ISieve> m_sieves;
        public void SetSieves(IList<ISieve> sieves)
        {
            m_sieves = sieves.ToList();
            m_sieves.Sort((a, b) => a.Weight - b.Weight);
        }


        public SceneEnt(GameObject gameObject)
        {
            GameObject = gameObject;
            m_sieves = new List<ISieve>();
        }

        public SceneEnt()
        {
            m_sieves = new List<ISieve>();
        }

        public virtual void ChangeOrientation(IntVector2 orientation)
        {
            if (orientation.x != 0)
            {
                GameObject.transform.localScale = new Vector3(
                    System.Math.Abs(GameObject.transform.localScale.x) * orientation.x,
                    GameObject.transform.localScale.y,
                    GameObject.transform.localScale.z
                );
            }
        }

        public virtual void ChangePos(IntVector2 pos)
        {
            GameObject.transform.position = new Vector3(
                pos.x * Reference.Width,
                -pos.y * Reference.Width,
                GameObject.transform.position.z
            );
        }

        public virtual void Destroy()
        {
            GameObject.Destroy(GameObject);
        }
    }
}