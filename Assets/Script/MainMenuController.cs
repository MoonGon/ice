using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 
using TMPro; 

public class MainMenuController : MonoBehaviour
{
    [Header("หน้าต่างตั้งค่า UI")]
    public GameObject settingsPanel;
    public TMP_InputField nameInputField;
    public Slider volumeSlider;

    void Start()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (nameInputField != null)
        {
            nameInputField.text = PlayerPrefs.GetString("PlayerName", "คุณ (Player)");
        }

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
        Debug.Log("ออกเกม");
        Application.Quit();
    }

    // --- ระบบปุ่มการตั้งค่า ---

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        PlayerPrefs.SetString("PlayerName", nameInputField.text);
        PlayerPrefs.Save();

        LeaderboardManager lb = FindFirstObjectByType<LeaderboardManager>();
        if (lb != null) lb.DisplayLeaderboard();

        settingsPanel.SetActive(false);
    }

    public void OnVolumeChanged()
    {
        float vol = volumeSlider.value;
        AudioListener.volume = vol;
        PlayerPrefs.SetFloat("GameVolume", vol);
    }
}