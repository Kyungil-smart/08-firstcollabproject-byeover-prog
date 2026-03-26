using System.Collections.Generic;
using MyGame2.Stage;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerCmp", menuName = "Scriptable Objects/EntityFunction/PlayerCmp")]
public class PlayerCmp: EntityFunctionSO
{
    //--- 설정 ---
    [Tooltip("1,2로 플래이어 설정")][SerializeField] private int _playerSlot;
    public override IComponentData CreateComponent(EntityState owner)
    {
        return new PlayerData(_playerSlot);
    }
}
