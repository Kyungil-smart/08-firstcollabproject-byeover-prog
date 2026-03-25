using UnityEngine;
using MyGame2.Stage;

[CreateAssetMenu(fileName = "StageStateReferenceSO", menuName = "Scriptable Objects/Util/StageStateReferenceSO")]
public class StageStateReferenceSO : ScriptableObject
{
    //StageState를 외부에서 주입 없이 참조하기 위한 SO
    
    //StageState 생성 시 StageManager에서 주입
    public StageState Instance { get; set; }

    // 데이터 초기화 시 호출 (예: StageManager에서 실행)
    public void Register(StageState state) => Instance = state;
}
