using System.Collections.Generic;
using MyGame2.Stage;
using UnityEngine;

[CreateAssetMenu(fileName = "Slide_Fn", menuName = "Scriptable Objects/EntityFunction/Slide_Fn")]
public class Slide_Fn: EntityFunctionSO, IUpdate
{
    [SerializeField] private float _moveSpeed;
    
    public void Slide()
    {
        //업데이트에서 돌릴 이동로직
    }
    
    public void Update()
    {
        Slide();
    }
}
