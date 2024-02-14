using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TotalCreditComment : MonoBehaviour
{
    public static TotalCreditComment Instance { get; private set; } 
    public Text dayEndMessageText;

    void Awake()
    {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }

        UpdateDayEndMessage();
    }

    public void UpdateDayEndMessage()
    {
        if (GameMgr.In.dayRevenue >= 100) {
            dayEndMessageText.text = "����, ������ �������̾� ���߾�!";
        }
        else if (GameMgr.In.dayRevenue >= 1) {
            dayEndMessageText.text = "�׷��� ������ �ʳ�! ������!";
        }
        else {
            dayEndMessageText.text = "����.. �� ����ؾ߰ڴ°�?";
        }
    }
}
