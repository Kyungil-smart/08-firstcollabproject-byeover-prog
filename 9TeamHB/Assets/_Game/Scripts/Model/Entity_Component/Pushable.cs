using UnityEngine;
using MyGame2.Stage;

public class Pushable : IComponentData
{
    // 플래이어가 밀 수 있는가를 정의하는 컴포넌트
    public bool CanBePushed { get; set; }
    
    public Pushable(bool canBePushed) { CanBePushed = canBePushed; }
    
}
