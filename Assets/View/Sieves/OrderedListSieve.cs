using System.Collections.Generic;
using Core.History;
using Hopper.ViewModel;
using UnityEngine;
using Utils.Vector;

namespace Hopper.View
{
    public class OrderedListSieve : ISieve
    {
        public int Weight { get; private set; }
        public bool IsFull => m_remainingCodes.Count == 0;

        private UpdateCode[] m_codes;
        private Queue<UpdateCode> m_remainingCodes;

        public OrderedListSieve(params UpdateCode[] codes)
        {
            m_codes = codes;
            m_remainingCodes = new Queue<UpdateCode>();
            Reset();
            Weight = codes.Length;
        }

        public OrderedListSieve(int weight, params UpdateCode[] codes)
        {
            m_codes = codes;
            m_remainingCodes = new Queue<UpdateCode>();
            Reset();
            Weight = weight;
        }

        public void Reset()
        {
            m_remainingCodes.Clear();
            foreach (var code in m_codes)
            {
                m_remainingCodes.Enqueue(code);
            }
        }

        public void Sieve(UpdateCode code)
        {
            if (IsFull == false && m_remainingCodes.Peek() == code)
            {
                m_remainingCodes.Dequeue();
            }
        }
    }
}