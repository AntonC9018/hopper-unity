using Hopper.ViewModel;
using UnityEngine;
using Utils.Vector;

namespace Hopper.View
{
    public class Scent : IScent, ICamera
    {
        public GameObject GameObject { protected get; set; }

        public Scent(GameObject gameObject)
        {
            GameObject = gameObject;
        }

        public Scent()
        {
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