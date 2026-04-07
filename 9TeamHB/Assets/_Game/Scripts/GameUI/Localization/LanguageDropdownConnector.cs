using UnityEngine;
using TMPro;

public class LanguageDropdownLinker : MonoBehaviour
{
    private TMP_Dropdown dropdown;

    void OnEnable()
    {
        if (dropdown == null)
        {
            dropdown = GetComponent<TMP_Dropdown>();
            if (dropdown != null && LocalizationManager.Instance != null)
                dropdown.onValueChanged.AddListener(LocalizationManager.Instance.ChangeLanguage);
        }

        if (dropdown != null && LocalizationManager.Instance != null)
            dropdown.SetValueWithoutNotify((int)LocalizationManager.Instance.currentLanguage);
    }
}