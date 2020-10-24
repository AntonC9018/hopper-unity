using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core;


public class CandaceAnimationManager : MonoBehaviour    //WARNING: EXTREMELY ROUGH SCRIPT
{
    
    private World _world = null;
    private Animator _playerAnimator = null;

    private void Update()
    {
        if (_world != null && _playerAnimator != null)
        {
            foreach (var player in _world.m_state.Players)
            {
                foreach (var updateInfo in player.History.Phases[0])    //player phase
                {
                    if (updateInfo.updateCode == History.UpdateCode.move_do)
                    {
                        _playerAnimator.Play("Candace_Jump");
                        print("Candace_Jump");
                    }
                }
                player.History.Clear();
            }
        }
    }

    public void SetWorld(World world)
    {
        _world = world;
    }

    public void SetPlayerAnimator(Animator playerAnimator)
    {
        _playerAnimator = playerAnimator;
    }
    
    
    
    
}