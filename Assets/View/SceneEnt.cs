using Hopper.ViewModel;
using UnityEngine;
using Hopper.Core.Utils.Vector;
using HopperVector2 = Hopper.Core.Utils.Vector.Vector2;

namespace Hopper.View
{
    public class SceneEnt : ISceneEnt, ICamera
    {
        public GameObject GameObject { protected get; set; }
        private HopperVector2 m_prevPos;
        private bool m_ignoreUpdates;
        private HopperVector2 m_currentFinalState;

        public SceneEnt(GameObject gameObject)
        {
            GameObject = gameObject;
        }

        public SceneEnt()
        {
        }

        public void SetInitialOrientation(IntVector2 pos)
        {
            ChangeOrientation(pos);
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

        public void SetInitialPosition(HopperVector2 pos)
        {
            m_prevPos = pos;
            ChangePos(pos);
        }

        public virtual void ChangePos(HopperVector2 pos)
        {
            GameObject.transform.position = new Vector3(
                pos.x * Reference.Width,
                -pos.y * Reference.Width,
                GameObject.transform.position.z
            );
        }

        public virtual void Destroy()
        {
            m_ignoreUpdates = true;
            GameObject.Destroy(GameObject);
        }

        public void EnterPhase(
            Hopper.Core.History.EntityState finalState, ISieve sieve, ViewModel.AnimationInfo animationInfo)
        {
            if (m_ignoreUpdates)
            {
                return;
            }

            if (sieve != null)
            {
                var viewSieve = (IViewSieve)sieve;
                StartAnimation(viewSieve.AnimationCode);
            }

            m_prevPos = animationInfo.currentPhase > 0 ? m_currentFinalState : m_prevPos;
            m_currentFinalState = finalState.pos;
            ChangeOrientation(finalState.orientation);
        }

        private void StartAnimation(AnimationCode animationCode)
        {
            if (animationCode == AnimationCode.Destroy)
            {
                Destroy();
            }
            // TODO: this should be scalable, obviously
            else if (animationCode == AnimationCode.Jump)
            {
                GameObject.GetComponent<Animator>().Play("Candace_Jump");
            }
        }

        public void Update(ViewModel.AnimationInfo animationInfo)
        {
            if (m_ignoreUpdates)
            {
                return;
            }
            ChangePos(HopperVector2.Lerp(m_prevPos, m_currentFinalState, animationInfo.proportionIntoPhase));
        }
    }
}