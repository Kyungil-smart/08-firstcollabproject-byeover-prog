using UnityEngine;
using UnityEngine.UI;

public class SettingSync : MonoBehaviour
{
    public Slider bgmSlider;
    public Slider sfxSlider;

    private void OnEnable()
    {
        if (InGameSoundManager.Instance != null)
        {
            if (bgmSlider != null)
                bgmSlider.value = PlayerPrefs.GetFloat("BGMVolume", 1f);
            if (sfxSlider != null)
                sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
        }
    }
}