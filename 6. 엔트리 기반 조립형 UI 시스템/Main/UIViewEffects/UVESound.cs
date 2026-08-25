using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UVESound : UIViewEffect
{
    private Coroutine soundCoroutine;
    public List<AudioClipInfo> uiOpenSound = new List<AudioClipInfo>();
    public List<AudioClipInfo> uiCloseSound = new List<AudioClipInfo>();


    private void playUISound(List<AudioClipInfo> soundClipInfos)
    {
        if (soundCoroutine != null)
        {
            StopCoroutine(soundCoroutine);
        }

        soundCoroutine = StartCoroutine(playUISoundIE(soundClipInfos));
    }

    public IEnumerator playUISoundIE(List<AudioClipInfo> soundClipInfos)
    {
        SoundController soundController = SoundController.GetInst;
        if (soundController == null)
        {
            Debug.LogWarning("씬에 SoundController가 없어 UI 효과음을 재생할 수 없습니다.", this);
            yield break;
        }

        foreach (var audioClipInfo in soundClipInfos)
        {
            if (audioClipInfo != null && audioClipInfo.audioClip != null)
            {
                soundController.playUISound(audioClipInfo, true);
                if (audioClipInfo.duration > 0)
                {
                    yield return new WaitForSeconds(audioClipInfo.duration);
                }
                else
                {
                    yield return new WaitForSeconds(audioClipInfo.audioClip.length);
                }
            }
        }
    }

    protected override void enableEffectDefault()
    {
        if (!uiView.isActive)
        {
            playUISound(uiOpenSound);
        }
    }

    protected override void disableEffectDefault()
    {
        if (uiView.isActive)
        {
            playUISound(uiCloseSound);
        }
    }
}
