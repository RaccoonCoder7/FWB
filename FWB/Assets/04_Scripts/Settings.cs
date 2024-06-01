using UnityEngine;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    [SerializeField] private Button autoTextOnButton;
    [SerializeField] private Button autoTextOffButton;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private GameObject settingsMenu;

    [SerializeField] private Button textSpeedSlowButton;
    [SerializeField] private Button textSpeedMidButton;
    [SerializeField] private Button textSpeedFastButton;


    void Start()
    {
        // ��ư�� �̺�Ʈ ������ ����
        autoTextOnButton.onClick.AddListener(EnableAutoText);
        autoTextOffButton.onClick.AddListener(DisableAutoText);
        closeSettingsButton.onClick.AddListener(CloseSettings);

        textSpeedSlowButton.onClick.AddListener(SetTextSpeedSlow);
        textSpeedMidButton.onClick.AddListener(SetTextSpeedMid);
        textSpeedFastButton.onClick.AddListener(SetTextSpeedFast);
    }

    private void EnableAutoText()
    {
    }

    private void DisableAutoText()
    {
    }

    private void SetTextSpeedSlow()
    {
    }

    private void SetTextSpeedMid()
    {
    }

    private void SetTextSpeedFast()
    {
    }

    private void CloseSettings()
    {
        // ���� �޴� ��Ȱ��ȭ
        settingsMenu.SetActive(false);
    }
}
