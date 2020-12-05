using System.Collections.Generic;
using System.Linq;
using Hopper.Core;
using Hopper.Core.Behaviors;
using Hopper.Utils.Vector;
using UnityEngine;

namespace Hopper
{
    public class InputManager
    {
        private UnityEngine.KeyCode m_lastInput;
        private bool m_isKeyPressed = false;

        private static Dictionary<KeyCode, InputMapping> InputMap = new Dictionary<KeyCode, InputMapping> {
            { KeyCode.Space, InputMapping.Weapon_0 },
            { KeyCode.X, InputMapping.Weapon_1 },
        };
        private static Dictionary<KeyCode, IntVector2> VectorMapping = new Dictionary<KeyCode, IntVector2>{
            { KeyCode.UpArrow, IntVector2.Up },
            { KeyCode.DownArrow, IntVector2.Down },
            { KeyCode.LeftArrow, IntVector2.Left },
            { KeyCode.RightArrow, IntVector2.Right },
        };

        public bool TrySetAction(World world)
        {
            if (m_isKeyPressed)
            {
                TryReleaseKey();
                return false;
            }

            if (IsInputValid())
            {
                // TODO: use virtual buttons
                // Actual button -> virtual button -> input mapping -> action
                // Physical layer -> Unity layer   -> logic layer   
                foreach (var player in world.State.Players)
                {
                    player.Behaviors.Get<Acting>().NextAction = GetActionFor(player);
                }
                TryReleaseKey();
                return true;
            }

            return false;
        }

        private void TryReleaseKey()
        {
            if (UnityEngine.Input.GetKeyUp(m_lastInput))
            {
                m_isKeyPressed = false;
            }
        }

        private bool IsInputValid()
        {
            return InputMap.Keys.Any(i => UnityEngine.Input.GetKeyDown(i))
                || VectorMapping.Keys.Any(i => UnityEngine.Input.GetKeyDown(i));
        }

        private Action GetActionFor(Entity player)
        {
            Controllable input = player.Behaviors.Get<Controllable>();

            foreach (var code in InputMap.Keys)
            {
                if (UnityEngine.Input.GetKeyDown(code))
                {
                    m_lastInput = code;
                    m_isKeyPressed = true;
                    return input.ConvertInputToAction(InputMap[code]);
                }
            }

            foreach (var code in VectorMapping.Keys)
            {
                if (UnityEngine.Input.GetKeyDown(code))
                {
                    m_lastInput = code;
                    m_isKeyPressed = true;
                    return input.ConvertVectorToAction(VectorMapping[code]);
                }
            }
            return null;
        }
    }
}