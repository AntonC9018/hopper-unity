using System.Collections.Generic;
using System.Linq;
using Core;
using Core.Utils.Vector;

namespace Hopper.ViewModel
{

    public delegate void TimerEventHandler(AnimationInfo info);

    public class AnimationInfo
    {
        public bool isFirstTimeInPhase;
        public int currentPhase;
        public float proportionIntoPhase;
        public int tickCount;
    }

    public class ViewAnimator : IAnimator
    {
        private ICamera m_camera;
        private ITimer m_timer;
        private IEnumerable<HistoryData> m_currentData;

        // TODO: this obviously needs an abstraction
        private Vector2 m_prevCamPos;
        private Vector2[] m_currentCameraData;

        // TODO: think about the overlaying phases     
        // TODO: think about skipping phases without any updates or slurring ones
        private int[] m_phaseSpanMillis;
        private int m_totalTimePerIteration;
        private int m_currentPhase;
        private int m_tickCount;
        private int m_prevPhaseMillis;

        public ViewAnimator(ICamera camera, ITimer timer)
        {
            m_camera = camera;
            m_timer = timer;
            m_timer.TimerEvent += Tick;

            m_phaseSpanMillis = new int[World.NumPhases];
            // for now, leave the rest at 0
            m_phaseSpanMillis[(int)Phase.PLAYER] = 300;
            m_phaseSpanMillis[(int)Phase.REAL] = 300;

            m_totalTimePerIteration = m_phaseSpanMillis.Sum();
        }

        public void SetupCamera(Vector2 cameraInitialPosition)
        {
            m_camera.ChangePos(cameraInitialPosition);
            m_prevCamPos = cameraInitialPosition;
        }

        public void Animate(IEnumerable<HistoryData> historyData, Core.Utils.Vector.Vector2[] cameraData)
        {
            m_currentPhase = 0;
            m_tickCount = 0;
            m_prevPhaseMillis = 0;
            m_firstTimeThisPhase = true;
            m_currentData = historyData;
            m_currentCameraData = cameraData;
            m_timer.Start();
        }

        private bool m_firstTimeThisPhase;

        private void Tick(int millis)
        {
            float proportionIntoPhase = System.Math.Min(CalculateProportionIntoPhase(millis), 1);
            var animationInfo = new AnimationInfo
            {
                currentPhase = m_currentPhase,
                proportionIntoPhase = proportionIntoPhase,
                tickCount = m_tickCount,
                isFirstTimeInPhase = m_firstTimeThisPhase
            };

            // potentially call a transition state on scene ents
            if (m_firstTimeThisPhase)
            {
                if (m_currentPhase > 0)
                {
                    m_prevCamPos = m_currentCameraData[m_currentPhase - 1];
                }
            }

            foreach (var data in m_currentData)
            {
                var state = data.entityStatesAndSieves.states[m_currentPhase];
                var sieve = data.entityStatesAndSieves.sieves[m_currentPhase];
                data.sceneEnt.Update(state, sieve, animationInfo);
            }

            m_camera.ChangePos((m_currentCameraData[m_currentPhase] - m_prevCamPos) * proportionIntoPhase
                + m_prevCamPos);


            if (IsPastLastPhase(millis))
            {
                m_timer.Stop();
                m_prevCamPos = m_currentCameraData[m_currentPhase];
            }
            else
            {
                m_firstTimeThisPhase = TryAdvancePhase(millis);
            }
        }

        private float CalculateProportionIntoPhase(int millis)
        {
            return ((float)millis) / m_phaseSpanMillis[m_currentPhase];
        }

        private bool TryAdvancePhase(int millis)
        {
            if (m_currentPhase < World.NumPhases - 1
                && m_phaseSpanMillis[m_currentPhase] < millis)
            {
                millis -= m_phaseSpanMillis[m_currentPhase];
                m_currentPhase++;
                m_timer.Reset();
                return true;
            }
            return false;
        }

        private bool IsPastLastPhase(int millis)
        {
            return m_currentPhase == World.NumPhases - 1
                && millis > m_phaseSpanMillis[m_currentPhase];
        }
    }
}