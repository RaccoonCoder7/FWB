using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;

public enum SoundType
{
    Bgm,
    Effect
}

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioClip[] bgmList;
    [SerializeField] private AudioClip[] effectList;
    private static SoundManager instance;
    private AudioSource effectAudioSource;
    private AudioSource bgmAudioSource;
    private float masterVolume = 1f;

    [Header("UI Elements")]
    [SerializeField] private Scrollbar masterVolumeBar;
    [SerializeField] private Scrollbar bgmVolumeBar;
    [SerializeField] private Scrollbar effectVolumeBar;

    private Dictionary<SoundType, float> volumeLevels = new Dictionary<SoundType, float>()
    {
        { SoundType.Bgm, 1f },
        { SoundType.Effect, 1f }
    };

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            effectAudioSource = GetComponent<AudioSource>();
            bgmAudioSource = gameObject.AddComponent<AudioSource>();
            bgmAudioSource.loop = true; // BGM �⺻ �������� ����

            if (masterVolumeBar == null || bgmVolumeBar == null || effectVolumeBar == null)
            {
                FindVolumeBars();
            }

            masterVolumeBar.onValueChanged.AddListener(SetMasterVolume);
            bgmVolumeBar.onValueChanged.AddListener((value) => SetVolume(SoundType.Bgm, value));
            effectVolumeBar.onValueChanged.AddListener((value) => SetVolume(SoundType.Effect, value));

            // �ʱ� ���� ����
            masterVolumeBar.value = masterVolume;
            bgmVolumeBar.value = volumeLevels[SoundType.Bgm];
            effectVolumeBar.value = volumeLevels[SoundType.Effect];
        }
        else
        {
            Destroy(gameObject);
        }
    }
    //���� �� ����(������ ã��)
    private void FindVolumeBars()
    {
        masterVolumeBar = GameObject.Find("MasterBar").GetComponent<Scrollbar>();
        bgmVolumeBar = GameObject.Find("BgmBar").GetComponent<Scrollbar>();
        effectVolumeBar = GameObject.Find("EffectBar").GetComponent<Scrollbar>();
    }

    // ������ ���� ����
    public static void SetMasterVolume(float volume)
    {
        instance.masterVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }

    // ���� ���� ����
    public static void SetVolume(SoundType sound, float volume)
    {
        instance.volumeLevels[sound] = Mathf.Clamp01(volume);
        UpdateVolumes();
    }

    // ���� ������Ʈ
    private static void UpdateVolumes()
    {
        instance.bgmAudioSource.volume = instance.masterVolume * instance.volumeLevels[SoundType.Bgm];
        instance.effectAudioSource.volume = instance.masterVolume * instance.volumeLevels[SoundType.Effect];
    }

    // BGM �÷��̾� (���ѷ���)
    public static IEnumerator BGMPlayer()
    {
        string activeSceneName = SceneManager.GetActiveScene().name;
        AudioClip bgmClip = instance.bgmList.FirstOrDefault(x => x.name.Equals(activeSceneName));

        if (bgmClip != null && instance.bgmAudioSource.clip != bgmClip)
        {
            instance.bgmAudioSource.Stop();
            yield return null;
            instance.bgmAudioSource.clip = bgmClip;
            instance.bgmAudioSource.Play();
        }
        else
        {
            yield return null;
        }
    }

    // ȿ���� �÷��� (�ѹ�)
    public static void PlayOneShot(string audioName)
    {
        var clip = instance.effectList.FirstOrDefault(x => x.name.Equals(audioName));
        if (clip != null)
        {
            instance.effectAudioSource.PlayOneShot(clip);
        }
    }

    // �⺻�� ���� �÷���
    public static void PlaySound(SoundType sound, int index, float volume = 1f)
    {
        AudioClip clip = null;
        float soundVolume = instance.volumeLevels[sound];

        switch (sound)
        {
            case SoundType.Bgm:
                if (index >= 0 && index < instance.bgmList.Length)
                {
                    clip = instance.bgmList[index];
                    instance.bgmAudioSource.clip = clip;
                    instance.bgmAudioSource.volume = volume * instance.masterVolume * soundVolume;
                    instance.bgmAudioSource.Play();
                }
                break;
            case SoundType.Effect:
                if (index >= 0 && index < instance.effectList.Length)
                {
                    clip = instance.effectList[index];
                    instance.effectAudioSource.PlayOneShot(clip, volume * instance.masterVolume * soundVolume);
                }
                break;
        }
    }
}
