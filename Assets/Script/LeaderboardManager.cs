using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq; // ต้องใช้ตัวนี้เพื่อช่วยเรียงลำดับคะแนน

public class LeaderboardManager : MonoBehaviour
{
    [Header("ตั้งชื่อคู่แข่งทั้ง 4 คน")]
    public string[] rivalNames = { "สมชาย", "พ่อมดดำ", "เจ๊น้ำแข็ง", "จอมเวทฝึกหัด" };

    [Header("UI แสดงผลคะแนน (ลาก Text มาใส่ 5 ช่อง)")]
    public TextMeshProUGUI[] rankTexts;

    void Start()
    {
        SetupInitialRivals();
        DisplayLeaderboard();
    }

    void SetupInitialRivals()
    {
        // เช็คว่าถ้าเพิ่งเปิดเกมครั้งแรก (ยังไม่มีคะแนนเซฟไว้) ให้สุ่มคะแนนตั้งต้นให้คู่แข่ง
        for (int i = 0; i < 4; i++)
        {
            if (!PlayerPrefs.HasKey("RivalScore_" + i))
            {
                PlayerPrefs.SetString("RivalName_" + i, rivalNames[i]);
                PlayerPrefs.SetInt("RivalScore_" + i, Random.Range(100, 300)); // คะแนนตั้งต้น (ปรับได้)
            }
        }
    }

    public void DisplayLeaderboard()
    {
        // 1. สร้าง List สำหรับเก็บข้อมูลทุกคน
        List<PlayerData> allPlayers = new List<PlayerData>();

        // 2. ดึงคะแนนสูงสุดของผู้เล่น (ถ้าไม่เคยเล่นจะได้ 0)
        int myBestScore = PlayerPrefs.GetInt("MyBestScore", 0);
        // ดึงชื่อที่ตั้งไว้มาใช้ ถ้าไม่มีให้ใช้ "คุณ (Player)"
        string myName = PlayerPrefs.GetString("PlayerName", "คุณ (Player)");
        allPlayers.Add(new PlayerData(myName, myBestScore));

        // 3. ดึงชื่อและคะแนนของคู่แข่งทั้ง 4 คนจากเครื่อง
        for (int i = 0; i < 4; i++)
        {
            // ถ้าดึงชื่อไม่ได้ ให้ใช้ชื่อที่ตั้งไว้ใน Inspector แทน
            string rName = PlayerPrefs.GetString("RivalName_" + i, rivalNames[i]);
            int rScore = PlayerPrefs.GetInt("RivalScore_" + i, 0);
            allPlayers.Add(new PlayerData(rName, rScore));
        }

        // 4. สั่งเรียงลำดับคะแนนจาก มาก ไป น้อย (OrderByDescending)
        allPlayers = allPlayers.OrderByDescending(p => p.score).ToList();

        // 5. ส่งข้อความไปแสดงบน UI ทั้ง 5 อันดับ
        for (int i = 0; i < rankTexts.Length; i++)
        {
            if (i < allPlayers.Count && rankTexts[i] != null)
            {
                rankTexts[i].text = (i + 1) + ". " + allPlayers[i].name + " : " + allPlayers[i].score;
            }
        }
    }

    // ฟังก์ชันนี้เป็น static เพื่อให้สคริปต์อื่นเรียกใช้ได้ง่ายๆ ตอนจบเกม
    public static void RecordMatch(int finalScore)
    {
        // 1. อัปเดตคะแนนสูงสุดของผู้เล่น
        int currentBest = PlayerPrefs.GetInt("MyBestScore", 0);
        if (finalScore > currentBest)
        {
            PlayerPrefs.SetInt("MyBestScore", finalScore);
        }

        // 2. นับจำนวนรอบที่เล่น
        int playCount = PlayerPrefs.GetInt("TotalPlayCount", 0);
        playCount++;
        PlayerPrefs.SetInt("TotalPlayCount", playCount);

        // 3. ทุกๆ 3 รอบ ให้คู่แข่งทั้ง 4 คนอัปเลเวล (สุ่มบวกคะแนน)
        if (playCount % 3 == 0)
        {
            for (int i = 0; i < 4; i++)
            {
                int currentRivalScore = PlayerPrefs.GetInt("RivalScore_" + i, 0);
                int boost = Random.Range(20, 150); // สุ่มบวกคะแนน 20 ถึง 150 แต้ม
                PlayerPrefs.SetInt("RivalScore_" + i, currentRivalScore + boost);
            }
            Debug.Log("คู่แข่งอัปเลเวลแล้ว! ผ่านมา 3 รอบแล้วสินะ");
        }

        PlayerPrefs.Save(); // สั่งเซฟข้อมูลลงเครื่อง
    }
}

// คลาสเล็กๆ สำหรับเก็บโครงสร้างข้อมูลชื่อและคะแนน
class PlayerData
{
    public string name;
    public int score;

    public PlayerData(string n, int s)
    {
        name = n;
        score = s;
    }
}