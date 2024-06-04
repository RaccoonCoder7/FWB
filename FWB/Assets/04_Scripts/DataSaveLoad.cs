using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters.Binary;

[System.Serializable]
public class PlayerData
{
    public string playerName;
    public int playerCredit;
    public int weekData;
    public GameMgr.Day dayData;
    public int playerTend;
    public int playerFame;
    public int playerDayFame;
    public int playerDayTend;
    public int playerDaySpent;
    public int customerCnt;

    public PlayerData(string name, int credit, int week, GameMgr.Day day, int tend, int fame, int dayFame, int dayTend, int daySpent, int cusCnt)
    {
        playerName = name;
        playerCredit = credit;
        weekData = week;
        dayData = day;
        playerTend = tend;
        playerFame = fame;
        playerDayFame = dayFame;
        playerDayTend = dayTend;
        playerDaySpent = daySpent;
        customerCnt = cusCnt;
    }
}

public class DataSaveLoad : MonoBehaviour
{
    public bool isLoaded = false;
    private string folderPath;
    
    public static DataSaveLoad dataSave { get; private set; }

    private DataSaveLoad() {}

    private void Awake()
    {
        if (dataSave == null)
        {
            dataSave = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        folderPath = Application.persistentDataPath + "/saves";
    }
    
    public void SaveData(string fileName)
    {
        Debug.Log("data saved");
        PlayerData data = new PlayerData(CommonTool.In.playerName,
            GameMgr.In.credit,
            GameMgr.In.week,
            GameMgr.In.day,
            GameMgr.In.tendency,
            GameMgr.In.fame,
            GameMgr.In.dayFame,
            GameMgr.In.dayTendency,
            GameMgr.In.daySpendCredit,
            GameMgr.In.dayCustomerCnt);

        string json = JsonUtility.ToJson(data);
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
        string newJson = System.Convert.ToBase64String(bytes);
        
        Directory.CreateDirectory(folderPath);
        string filePath = folderPath + "/"+ fileName + ".json";
        
        File.WriteAllText(filePath, newJson);
    }

    public void LoadData(string fileName)
    {
        isLoaded = true;
        
        string filePath = folderPath + "/"+ fileName + ".json";
        Debug.Log("data loaded");
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            byte[] bytes = System.Convert.FromBase64String(json);
            string newJson = System.Text.Encoding.UTF8.GetString(bytes);
            PlayerData data = JsonUtility.FromJson<PlayerData>(newJson);
        
            CommonTool.In.playerName = data.playerName;
            GameMgr.In.credit = data.playerCredit;
            GameMgr.In.week = data.weekData;
            GameMgr.In.day = data.dayData;
            GameMgr.In.tendency = data.playerTend;
            GameMgr.In.fame = data.playerFame;
            GameMgr.In.dayFame = data.playerDayFame;
            GameMgr.In.dayTendency = data.playerDayTend;
            GameMgr.In.daySpendCredit = data.playerDaySpent;
            GameMgr.In.dayCustomerCnt = data.customerCnt;
            
            StartCoroutine(CommonTool.In.AsyncChangeScene("GameScene"));
        }
        else
        {
            Debug.LogError("no data");
        }
    }
}