using Hopper.ViewModel;
using UnityEngine;
using Utils.Vector;

namespace Hopper.View
{
    public class Scent : IScent, ICamera
    {
        protected GameObject m_obj;

        public Scent(GameObject gameObject)
        {
            m_obj = gameObject;
        }

        public virtual void ChangeOrientation(IntVector2 orientation)
        {
            if (orientation.x != 0)
            {
                m_obj.transform.localScale = new Vector3(
                    System.Math.Abs(m_obj.transform.localScale.x) * orientation.x,
                    m_obj.transform.localScale.y,
                    m_obj.transform.localScale.z
                );
            }
        }

        public virtual void ChangePos(IntVector2 pos)
        {
            m_obj.transform.position = new Vector3(
                pos.x * Reference.Width,
                -pos.y * Reference.Width,
                m_obj.transform.position.z
            );
        }

        public virtual void Destroy()
        {
            GameObject.Destroy(m_obj);
        }
    }
}