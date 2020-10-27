using Core.History;
using Hopper.ViewModel;
using UnityEngine;
using Utils.Vector;

namespace Hopper.View
{
    public class SimpleSieve : ISieve
    {
        public int Weight { get; private set; }
        public bool IsFull { get; private set; }

        private UpdateCode m_code;

        public SimpleSieve(UpdateCode code, int weight = 1)
        {
            m_code = code;
            Weight = weight;
        }

        public void Reset()
        {
            IsFull = false;
        }

        public void Sieve(UpdateCode code)
        {
            if (code == m_code)
            {
                IsFull = true;
            }
        }
    }
}