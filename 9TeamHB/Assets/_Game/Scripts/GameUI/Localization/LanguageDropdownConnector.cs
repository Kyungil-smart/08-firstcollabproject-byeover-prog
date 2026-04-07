using UnityEngine;
using TMPro;

public class LanguageDropdownLinker : MonoBehaviour
{
    private TMP_Dropdown dropdown;

    void Start()
    {
        dropdown = GetComponent<TMP_Dropdown>();

        if (dropdown != null && LocalizationManager.Instance != null)
        {
            // 저장된 언어로 드롭다운 초기값 세팅
            dropdown.SetValueWithoutNotify((int)LocalizationManager.Instance.currentLanguage);
        
            dropdown.onValueChanged.AddListener(LocalizationManager.Instance.ChangeLanguage);
        }
    }
}