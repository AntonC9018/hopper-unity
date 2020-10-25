using UnityEngine;
using Core;
using Core.History;

public class CandaceAnimationManager : MonoBehaviour    //WARNING: EXTREMELY ROUGH SCRIPT
{

    private World _world = null;
    private Animator _playerAnimator = null;

    private void Update()
    {
        foreach (var player in _world.m_state.Players)
        {
            foreach (var updateInfo in player.History.Updates)
            {
                if (updateInfo.updateCode == UpdateCode.move_do)
                {
                    _playerAnimator.Play("Candace_Jump");
                    print("Candace_Jump");
                }
            }
            player.History.Clear();
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