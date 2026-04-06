using UnityEngine;

public class VolumeController : MonoBehaviour
{
    public void SetBGMVolume(float volume)
    {
        InGameSoundManager.Instance.SetBGMVolume(volume);
    }
    
    public void SetSFXVolume(float volume)
    {
        InGameSoundManager.Instance.SetSFXVolume(volume);
    }
}
