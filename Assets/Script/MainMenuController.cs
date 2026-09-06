using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // ต้องมีเพื่อเรียกใช้ Slider
using TMPro; // ต้องมีเพื่อเรียกใช้ InputField

public class MainMenuController : MonoBehaviour
{
    [Header("หน้าต่างตั้งค่า UI")]
    public GameObject settingsPanel;
    public TMP_InputField nameInputField;
    public Slider volumeSlider;

    void Start()
    {
        // 1. ซ่อนหน้าต่างตั้งค่าตอนเริ่มเกม
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // 2. โหลดชื่อผู้เล่น (ถ้าไม่เคยตั้ง จะใช้คำว่า "คุณ (Player)")
        if (nameInputField != null)
        {
            nameInputField.text = PlayerPrefs.GetString("PlayerName", "คุณ (Player)");
        }

        // 3. โหลดระดับเสียง (ค่าเริ่มต้น 1.0 คือดังสุด)
        if (volumeSlider != null)
        {
            float savedVolume = PlayerPrefs.GetFloat("GameVolume", 1f);
            volumeSlider.value = savedVolume;
            AudioListener.volume = savedVolume; // ปรับเสียงทั้งเกม
        }
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("MainGame");
    }

    public void QuitGame()
    {
        Debug.Log("ออกจากเกม!");
        Application.Quit();
    }

    // --- ระบบปุ่มการตั้งค่า ---

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        // เซฟชื่อลงเครื่องตอนกดปิดหน้าต่าง
        PlayerPrefs.SetString("PlayerName", nameInputField.text);
        PlayerPrefs.Save();

        // สั่งให้กระดานคะแนนรีเฟรชเพื่อโชว์ชื่อใหม่ทันที
        LeaderboardManager lb = FindFirstObjectByType<LeaderboardManager>();
        if (lb != null) lb.DisplayLeaderboard();

        settingsPanel.SetActive(false);
    }

    public void OnVolumeChanged()
    {
        // ฟังก์ชันนี้จะถูกเรียกทุกครั้งที่ผู้เล่นเลื่อนหลอดเสียง
        float vol = volumeSlider.value;
        AudioListener.volume = vol; // ปรับเสียง Master ของ Unity
        PlayerPrefs.SetFloat("GameVolume", vol); // เซฟค่าเสียง
    }
}