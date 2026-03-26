using System.Collections.Generic;
using MyGame2.Stage;
using UnityEngine;
using CameraType = MyGame2.Stage.CameraType;

[CreateAssetMenu(fileName = "CameraCmp", menuName = "Scriptable Objects/EntityFunction/CameraCmp")]
public class CameraCmp: EntityFunctionSO
{
    // --- 설정 ---
    [SerializeField] CameraType _pattern;
    [SerializeField] private bool ReverseRotation;
    
    public override IComponentData CreateComponent(EntityState owner)
    {
        return new CameraData(_pattern, ReverseRotation);
    }
}
