using System.Collections.Generic;
using MyGame2.Stage;
using UnityEngine;

[CreateAssetMenu(fileName = "Pushable_Fn", menuName = "Scriptable Objects/EntityFunction/Pushable_Fn")]
public class Pushable_Fn : EntityFunctionSO
{
    [SerializeField] private List<PlayerType> CanPushPlayer;
    
    public bool CanBePushedBy(PlayerType playerType)
    {
        return CanPushPlayer.Contains(playerType);
    }
}
