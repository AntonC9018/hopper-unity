using System;
using Hopper.ViewModel;
using UnityEngine;

namespace Hopper.View
{
    public class Timer : MonoBehaviour, ITimer
    {
        public event Action<int> TimerEvent;

        private float m_millis = 0;
        private bool m_isWorking = false;

        private void Update()
        {
            // TODO: this should be a stoppable coroutine
            if (m_isWorking)
            {
                m_millis += Time.deltaTime;
                FireEvent();
            }
        }

        void ITimer.Pause()
        {
            m_isWorking = false;
        }

        public void Reset()
        {
            m_millis = 0;
        }

        void ITimer.Resume()
        {
            m_isWorking = true;
        }

        void ITimer.Start()
        {
            m_isWorking = true;
            Reset();
        }

        void ITimer.Stop()
        {
            m_isWorking = false;
            Reset();
        }

        void ITimer.Set(int millis)
        {
            m_millis = millis;
        }

        private void FireEvent()
        {
            TimerEvent?.Invoke((int)(m_millis * 1000));
        }
    }
}