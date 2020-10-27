using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Hopper.ViewModel;
using UnityEngine;
using Core.Utils.Vector;

namespace Hopper.View
{
    public class SceneEnt : ISceneEnt, ICamera
    {
        // TODO: do smth with this
        public IReadOnlyList<ISieve> Sieves => m_sieves.AsReadOnly();
        public GameObject GameObject { protected get; set; }

        private List<IViewSieve> m_sieves;
        public void SetSieves(IList<IViewSieve> sieves)
        {
            m_sieves = sieves.ToList();
            m_sieves.Sort((a, b) => a.Weight - b.Weight);
        }

        private Core.Utils.Vector.Vector2 m_prevPos;

        public SceneEnt(GameObject gameObject)
        {
            GameObject = gameObject;
            m_sieves = new List<IViewSieve>();
        }

        public SceneEnt()
        {
            m_sieves = new List<IViewSieve>();
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

        public virtual void ChangePos(Core.Utils.Vector.Vector2 pos)
        {
            if (m_prevPos == null)
            {
                m_prevPos = pos;
            }
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

        private Core.Utils.Vector.Vector2 m_prevFinalState;
        private bool m_ignoreUpdates;

        // TODO: maybe have a transition change separate function
        // feed the data about the state and the sieve there
        // update with just the animation info
        public void Update(
            Core.History.EntityState finalState,
            ISieve sieve,
            ViewModel.AnimationInfo animationInfo)
        {
            if (m_ignoreUpdates)
            {
                return;
            }
            if (animationInfo.isFirstTimeInPhase)
            {
                m_prevPos = animationInfo.currentPhase > 0 ? m_prevFinalState : m_prevPos;
                m_prevFinalState = finalState.pos;
                if (sieve != null)
                {
                    if (((IViewSieve)sieve).AnimationCode == AnimationCode.Destroy)
                    {
                        Destroy();
                    }
                }
            }
            ChangePos((finalState.pos - m_prevPos) * animationInfo.proportionIntoPhase + m_prevPos);
            ChangeOrientation(finalState.orientation);
        }
    }
}