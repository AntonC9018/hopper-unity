using System.Collections.Generic;
using Core.History;
using Hopper.ViewModel;
using UnityEngine;
using Utils.Vector;

namespace Hopper.View
{
    public class UnorderedListSieve : ISieve
    {
        public int Weight { get; private set; }
        public bool IsFull => m_remainingCodes.Count == 0;

        private UpdateCode[] m_codes;
        private HashSet<UpdateCode> m_remainingCodes;

        public UnorderedListSieve(params UpdateCode[] codes)
        {
            m_codes = codes;
            m_remainingCodes = new HashSet<UpdateCode>(codes);
            Weight = codes.Length;
        }

        public UnorderedListSieve(int weight, params UpdateCode[] codes)
        {
            m_codes = codes;
            m_remainingCodes = new HashSet<UpdateCode>(codes);
            Weight = weight;
        }

        public void Reset()
        {
            m_remainingCodes.UnionWith(m_codes);
        }

        public void Sieve(UpdateCode code)
        {
            if (m_remainingCodes.Contains(code))
            {
                m_remainingCodes.Remove(code);
            }
        }
    }
}