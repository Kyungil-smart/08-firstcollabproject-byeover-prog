using UnityEngine;
using TMPro;

// 각 TextMeshPro에 등록해서 언어 변환 가능하게 해줌.
public class LocalizedText : MonoBehaviour
{
    public string key; // 구글 시트에 적은 Key값과 일치하게 익스펙터에서 지정.
    public TextMeshProUGUI textComponent; // 실제 표시되는 텍스트

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

    // 언어바뀔때 텍스트 수정. 
    void UpdateText()
    {
        if (LocalizationManager.Instance != null)
            textComponent.text = LocalizationManager.Instance.GetText(key);
    }
}