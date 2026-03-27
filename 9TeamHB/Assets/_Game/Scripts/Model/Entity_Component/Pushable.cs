using UnityEngine;
using MyGame2.Stage;

public class Pushable : IComponentData
{
    // 플래이어가 밀 수 있는가를 정의하는 컴포넌트
    public bool CanBePushed { get; set; }
    
    // 주입시 SO에서 설정한대로 bool값 받음
    public Pushable(bool canBePushed) { CanBePushed = canBePushed; }
    
}
