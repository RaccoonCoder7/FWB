using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 앱 전반적으로 흔히 사용 될만한 기능을 모아둔 공용 툴
/// </summary>
public class CommonTool : SingletonMono<CommonTool>
{
    public GameObject alertPanel;
    public GameObject confirmPanel;
    public GameObject focusPanel;
    public ShopFollowUI shopFollowUI;
    public RectTransform focusRectTr;
    public RectTransform focusMaskRectTr;
    public RectTransform focusMaskRectTr_Left;
    public RectTransform focusMaskRectTr_Right;
    public RectTransform focusMaskRectTr_Top;
    public RectTransform focusMaskRectTr_Bottom;
    public Text alertText;
    public Text confirmText;
    public Button alertDodgeBtn;
    public Button confirmBtn;
    public Button cancelBtn;
    public Image fadeImage;
    public float fadeSpeed;
    public string playerName;
    public string mascotName = "나비";
    public string pcMascotName = "PC나비";
    public List<Script> scriptList = new List<Script>();
    public List<AudioClip> audioClipList = new List<AudioClip>();
    public Canvas canvas;

    private AudioSource audioSrc;


    [System.Serializable]
    public class Script
    {
        public string key;
        public TextAsset ta;
        [HideInInspector]
        public List<string> lines = new List<string>();
    }


    void Awake()
    {
        base.Awake();
        canvas = GetComponent<Canvas>();
        audioSrc = GetComponent<AudioSource>();

        alertPanel.SetActive(false);
        confirmPanel.SetActive(false);
        alertDodgeBtn.onClick.AddListener(() => alertPanel.SetActive(false));

        foreach (var script in scriptList)
        {
            string fileName = script.key + ".txt";
            var path = Path.Combine(Application.persistentDataPath, script.key + ".txt");
            if (!File.Exists(path))
            {
                File.WriteAllText(path, script.ta.text);
            }
            script.lines = File.ReadAllText(path).Split('\n').ToList();
        }
    }

    /// <summary>
    /// 사용자의 Yes/No 응답을 받는 패널을 열음
    /// </summary>
    /// <param name="text">패널에 띄울 메세지</param>
    /// <param name="OnConfirm">Yes 선택 시 동작할 Action</param>
    /// <param name="OnCancel">No 선택 시 동작할 Action</param>
    public void OpenConfirmPanel(string text, Action OnConfirm, Action OnCancel)
    {
        confirmPanel.SetActive(true);
        confirmText.text = text;
        confirmBtn.onClick.AddListener(() =>
        {
            confirmPanel.SetActive(false);
            OnConfirm.Invoke();
            confirmBtn.onClick.RemoveAllListeners();
        });
        cancelBtn.onClick.AddListener(() =>
        {
            confirmPanel.SetActive(false);
            OnCancel.Invoke();
            cancelBtn.onClick.RemoveAllListeners();
        });
    }

    /// <summary>
    /// 알림 패널을 열음
    /// </summary>
    /// <param name="alertText">알림 메세지</param>
    /// <param name="OnDodge">창이 닫혔을 경우 동작할 Action</param>
    public void OpenAlertPanel(string alertText, Action OnDodge = null)
    {
        alertPanel.SetActive(true);
        this.alertText.text = alertText;

        if (OnDodge != null)
        {
            alertDodgeBtn.onClick.RemoveAllListeners();
            alertDodgeBtn.onClick.AddListener(() =>
            {
                OnDodge.Invoke();
                alertPanel.SetActive(false);
                alertDodgeBtn.onClick.RemoveAllListeners();
                alertDodgeBtn.onClick.AddListener(() => { alertPanel.SetActive(false); });
            });
        }
    }

    /// <summary>
    /// 스크립트(대화)를 List<string> 형태로 가져옴 
    /// </summary>
    /// <param name="key">스크립트 키</param>
    public List<string> GetText(string key)
    {
        return scriptList.Find(x => x.key.Equals(key))?.lines;
    }

    /// <summary>
    /// 오디오를 1회 플레이 함
    /// </summary>
    /// <param name="audioName">오디오클립 이름</param>
    public void PlayOneShot(string audioName)
    {
        var clip = audioClipList.Find(x => x.name.Equals(audioName));
        if (clip != null)
        {
            audioSrc.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// 화면에 포커스를 줌
    /// </summary>
    public void SetFocus(Vector2 pos, Vector2 size)
    {
        focusPanel.SetActive(true);

        var focusMaskLocalPos = focusMaskRectTr.anchoredPosition;
        focusMaskLocalPos.x = pos.x;
        focusMaskLocalPos.y = pos.y;
        focusMaskRectTr.anchoredPosition = focusMaskLocalPos;
        focusMaskRectTr.sizeDelta = size;

        var focusLocalPos = focusRectTr.anchoredPosition;
        focusLocalPos.x = -pos.x;
        focusLocalPos.y = -pos.y;
        focusRectTr.anchoredPosition = focusLocalPos;

        focusMaskRectTr_Left.sizeDelta = new Vector2(pos.x, 1080);
        focusMaskRectTr_Right.sizeDelta = new Vector2(1920 - (pos.x + size.x), 1080);
        focusMaskRectTr_Top.sizeDelta = new Vector2(1920, 1080 - (pos.y + size.y));
        focusMaskRectTr_Bottom.sizeDelta = new Vector2(1920, pos.y);
    }

    /// <summary>
    /// 포커스 효과를 없앰
    /// </summary>
    public void SetFocusOff()
    {
        focusPanel.SetActive(false);
    }

    public IEnumerator FadeIn()
    {
        float fadeValue = 1;
        float actualSpeed = fadeSpeed * 0.01f;
        while (fadeValue > 0)
        {
            fadeValue -= actualSpeed;
            fadeImage.color = new Color(0, 0, 0, fadeValue);
            yield return new WaitForSeconds(actualSpeed);
        }
        fadeImage.gameObject.SetActive(false);
    }

    public IEnumerator FadeOut()
    {
        fadeImage.gameObject.SetActive(true);
        float fadeValue = 0;
        float actualSpeed = fadeSpeed * 0.01f;
        while (fadeValue < 1)
        {
            fadeValue += actualSpeed;
            fadeImage.color = new Color(0, 0, 0, fadeValue);
            yield return new WaitForSeconds(actualSpeed);
        }
    }

    public IEnumerator AsyncChangeScene(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        yield return StartCoroutine(FadeOut());

        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        asyncLoad.allowSceneActivation = true;
    }
}
