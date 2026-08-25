using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// UI 효과음 재생에 필요한 최소 기능만 담당한다.
/// 독립 재생을 요청하면 임시 AudioSource를 사용해 기존 효과음을 끊지 않는다.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SoundController : MonoBehaviour
{
    public static SoundController GetInst
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<SoundController>();
            }

            return instance;
        }
    }

    private static SoundController instance;
    private AudioSource sharedSource;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        sharedSource = GetComponent<AudioSource>();
        sharedSource.playOnAwake = false;
    }

    public void playUISound(AudioClipInfo clipInfo, bool independent = false)
    {
        if (clipInfo?.audioClip == null)
        {
            return;
        }

        StartCoroutine(playUISoundIE(clipInfo, independent));
    }

    private IEnumerator playUISoundIE(AudioClipInfo clipInfo, bool independent)
    {
        if (clipInfo.delay > 0f)
        {
            yield return new WaitForSecondsRealtime(clipInfo.delay);
        }

        GameObject temporaryObject = null;
        AudioSource source = sharedSource;

        if (independent)
        {
            temporaryObject = new GameObject($"UI Audio - {clipInfo.audioClip.name}");
            temporaryObject.transform.SetParent(transform, false);
            source = temporaryObject.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.clip = clipInfo.audioClip;
        source.volume = Mathf.Clamp01(clipInfo.volume);
        float requestedPitch = Mathf.Clamp(clipInfo.pitch, -3f, 3f);
        source.pitch = Mathf.Abs(requestedPitch) < 0.01f ? 1f : requestedPitch;
        source.time = Mathf.Clamp(clipInfo.startTime, 0f, clipInfo.audioClip.length);
        source.Play();

        float remainingDuration = (clipInfo.audioClip.length - source.time)
                                  / Mathf.Max(0.01f, Mathf.Abs(source.pitch));
        float playDuration = clipInfo.duration > 0f
            ? Mathf.Min(clipInfo.duration, remainingDuration)
            : remainingDuration;

        yield return new WaitForSecondsRealtime(playDuration);

        if (temporaryObject != null)
        {
            Destroy(temporaryObject);
        }
        else if (clipInfo.duration > 0f && source.isPlaying)
        {
            source.Stop();
        }
    }
}

[Serializable]
public class AudioClipInfo
{
    public AudioClip audioClip;
    [Min(0f)] public float startTime;
    [Min(0f)] public float delay;
    [Min(0f)] public float duration;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(-3f, 3f)] public float pitch = 1f;

    public AudioClipInfo(
        AudioClip audioClip,
        float volume = 1f,
        float pitch = 1f,
        float delay = 0f,
        float duration = 0f,
        float startTime = 0f)
    {
        this.audioClip = audioClip;
        this.volume = volume;
        this.pitch = pitch;
        this.delay = delay;
        this.duration = duration;
        this.startTime = startTime;
    }
}
