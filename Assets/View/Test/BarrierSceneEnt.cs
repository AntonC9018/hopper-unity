using System;
using Core.Utils.Vector;
using UnityEngine;

namespace Hopper.View
{
    public class BarrierSceneEnt : SceneEnt
    {
        public override void ChangeOrientation(IntVector2 orientation)
        {
            double angle = orientation.AngleTo(IntVector2.UnitX);
            GameObject.transform.rotation = Quaternion.AngleAxis(Mathf.Rad2Deg * (float)angle, Vector3.forward);
        }
    }
}