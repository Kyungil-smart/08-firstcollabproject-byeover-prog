using UnityEngine;
using TMPro;

public class LanguageDropdownLinker : MonoBehaviour
{
    private TMP_Dropdown dropdown;

    void Start()
    {
        dropdown = GetComponent<TMP_Dropdown>();

        // 드롭 다운 선택시 언어 변경될때 세팅하는 ChangeLanguage함수 실행. 
        if (dropdown != null && LocalizationManager.Instance != null)
        {
            dropdown.onValueChanged.AddListener(LocalizationManager.Instance.ChangeLanguage);
        }
    }
}