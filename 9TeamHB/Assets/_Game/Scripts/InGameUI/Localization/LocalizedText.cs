using UnityEngine;
using TMPro;

// 각 TextMeshPro에 등록해서 언어 변환 가능하게 해줌.
public class LocalizedText : MonoBehaviour
{
    public string key; // 구글 시트에 적은 Key값과 일치하게 익스펙터에서 지정.
    public TextMeshProUGUI textComponent; // 실제 표시되는 텍스트

    private object[] currentArgs; //변수 저장. param으로 써서 개수 자유 
    void Start()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        LocalizationManager.LanguageChangedEvent += UpdateText; 
        UpdateText();
    }

    void OnDestroy()
    {
        LocalizationManager.LanguageChangedEvent -= UpdateText; //성능 최적화
        
    }

    public void SetVariables(params object[] args)
    {
        currentArgs = args;
        UpdateText(); // 화면 글씨 새로고침
    }
    
    // 언어바뀔때 텍스트 수정. 
    void UpdateText()
    {
        if (LocalizationManager.Instance != null && textComponent != null)
        {
            // 번역된 원본 문장 받기.
            string localizedString = LocalizationManager.Instance.GetText(key);

            // 변수가 있을 시 Format으로 합치기.
            if (currentArgs != null && currentArgs.Length > 0)
            {
                textComponent.text = string.Format(localizedString, currentArgs);

                for (int i = 0; i < currentArgs.Length; i++)
                {
                    //Debug.Log(currentArgs[i]);
                }
            }
            // 변수가 없는 일반 텍스트는 그냥 출력.
            else  
            {
                textComponent.text = localizedString;
            }
        }
    }
}