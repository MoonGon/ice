using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

[System.Serializable]
public class CustomerData
{
    public string customerName;
    public Sprite faceEntering;
    public Sprite faceSpeaking;
    public Sprite faceWaiting;
    public Sprite faceGood;
    public Sprite faceBad;
    public Sprite facePerfect;
}

public class IceGameLoop : MonoBehaviour
{
    public enum GameState { Order, Playing, Result, GameOver }
    private GameState currentState;

    [Header("UI และเวลาเกม")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public float gameDuration = 60f;
    private float timeRemaining;

    [Header("ระบบ Popup คะแนน")]
    public TextMeshProUGUI popupScoreText; // ช่องใส่ UI Popup
    public float popupDuration = 1f;       // เวลาที่ลอยก่อนหายไป
    public float popupFloatSpeed = 50f;    // ความเร็วในการลอยขึ้น
    private RectTransform popupRect;
    private Vector2 popupStartPos;         // เก็บตำแหน่งเริ่มต้น

    [Header("ระบบคะแนน")]
    public int scorePerfect = 100;
    public int scoreGood = 50;
    private int currentScore = 0;

    [Header("UI ลูกค้าและข้อความ")]
    public TextMeshProUGUI dialogText;
    public GameObject iceObject;

    [Header("ระบบพิมพ์ข้อความ (Typewriter)")]
    public float textSpeed = 0.05f;
    private bool isTypingFinished = false;
    private string fullOrderText = "";
    private Coroutine typingCoroutine;

    [Header("ระบบอนิเมชันลูกค้า")]
    public Transform customerTransform;
    public Vector3 offScreenPos = new Vector3(10f, 0f, 0f);
    public Vector3 onScreenPos = new Vector3(5f, 0f, 0f);
    public float slideSpeed = 10f;
    private bool isCustomerInPosition = false;

    [Header("รายชื่อลูกค้าทั้งหมด")]
    public CustomerData[] allCustomers;
    private CustomerData currentCustomer;
    public SpriteRenderer customerSpriteRenderer;

    [Header("UI หลอดพลัง")]
    public GameObject powerBarContainer;
    public Image powerBarFill;
    public float fillSpeed = 50f;

    [Header("ระบบสุ่มเป้าหมาย")]
    public float minTarget = 20f;
    public float maxTarget = 90f;
    private float targetPoint;

    [Header("ระยะความคลาดเคลื่อน")]
    public float greenZone = 5f;
    public float yellowZone = 15f;

    [Header("รูปร่างและขนาดน้ำแข็ง")]
    public SpriteRenderer iceSpriteRenderer;
    public Sprite waterSprite, iceSprite, brokenIceSprite;
    public float minScale = 1.5f;
    public float maxScale = 5.0f;

    private float currentPower = 0f;
    private float resultDisplayTimer = 2f;
    private float currentResultTimer = 0f;

    void Start()
    {
        // ตั้งค่า Popup ตอนเริ่มเกม
        if (popupScoreText != null)
        {
            popupRect = popupScoreText.GetComponent<RectTransform>();
            popupStartPos = popupRect.anchoredPosition; // จำตำแหน่งตั้งต้นไว้
            popupScoreText.gameObject.SetActive(false); // ซ่อนไว้ก่อน
        }

        timeRemaining = gameDuration;
        currentScore = 0;
        UpdateScoreUI();
        StartOrderPhase();
    }

    void Update()
    {
        if (currentState != GameState.GameOver)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerUI();

            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                UpdateTimerUI();
                EndGame();
                return;
            }
        }

        switch (currentState)
        {
            case GameState.Order:
                if (!isCustomerInPosition)
                {
                    customerTransform.position = Vector3.MoveTowards(customerTransform.position, onScreenPos, slideSpeed * Time.deltaTime);
                    if (Vector3.Distance(customerTransform.position, onScreenPos) < 0.1f)
                    {
                        isCustomerInPosition = true;
                        customerSpriteRenderer.sprite = currentCustomer.faceSpeaking;

                        fullOrderText = currentCustomer.customerName + " : ขอน้ำแข็งระดับ " + targetPoint.ToString("F0") + "!\n(กด Spacebar เพื่อเสก)";
                        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                        typingCoroutine = StartCoroutine(TypeText(fullOrderText));
                    }
                }
                else
                {
                    if (!isTypingFinished)
                    {
                        if (Input.GetKeyDown(KeyCode.Space))
                        {
                            StopCoroutine(typingCoroutine);
                            dialogText.text = fullOrderText;
                            FinishTyping();
                        }
                    }
                    else
                    {
                        if (Input.GetKeyDown(KeyCode.Space)) StartPlayingPhase();
                    }
                }
                break;

            case GameState.Playing:
                if (Input.GetKey(KeyCode.Space))
                {
                    currentPower += fillSpeed * Time.deltaTime;
                    currentPower = Mathf.Clamp(currentPower, 0f, 100f);
                    UpdateBarVisual();
                    UpdateIceShape();
                    UpdateIceScale();
                }
                if (Input.GetKeyUp(KeyCode.Space))
                {
                    CheckResult();
                }
                break;

            case GameState.Result:
                currentResultTimer -= Time.deltaTime;
                if (currentResultTimer <= 0)
                {
                    StartOrderPhase();
                }
                break;

            case GameState.GameOver:
                if (Input.GetKeyDown(KeyCode.R))
                {
                    timeRemaining = gameDuration;
                    currentScore = 0;
                    UpdateScoreUI();
                    StartOrderPhase();
                }
                else if (Input.GetKeyDown(KeyCode.M))
                {
                    SceneManager.LoadScene("MainMenu"); // ต้องพิมพ์ชื่อฉากให้ตรงกับที่ตั้งไว้
                }
                break;
        }
    }

    IEnumerator TypeText(string textToType)
    {
        dialogText.text = "";
        foreach (char c in textToType.ToCharArray())
        {
            dialogText.text += c;
            yield return new WaitForSeconds(textSpeed);
        }
        FinishTyping();
    }

    void FinishTyping()
    {
        isTypingFinished = true;
        iceObject.SetActive(true);
        iceSpriteRenderer.sprite = waterSprite;
        UpdateIceScale();

        if (powerBarContainer != null)
            powerBarContainer.SetActive(true);
    }

    void UpdateTimerUI()
    {
        timerText.text = "เวลา: " + Mathf.CeilToInt(timeRemaining).ToString();
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "คะแนน: " + currentScore.ToString();
    }

    void EndGame()
    {
        currentState = GameState.GameOver;
        iceObject.SetActive(false);
        if (powerBarContainer != null) powerBarContainer.SetActive(false);
        customerObjectCheck();
        customerSpriteRenderer.sprite = currentCustomer.faceEntering;
        dialogText.text = "หมดเวลา!\nคะแนนรวมของคุณคือ: " + currentScore + "\n(กด R เพื่อเล่นรอบใหม่)";
        LeaderboardManager.RecordMatch(currentScore);
    }

    void customerObjectCheck()
    {
        customerTransform.gameObject.SetActive(true);
    }

    void StartOrderPhase()
    {
        currentState = GameState.Order;
        currentPower = 0f;
        isCustomerInPosition = false;
        isTypingFinished = false;
        targetPoint = Random.Range(minTarget, maxTarget);

        if (allCustomers.Length > 0)
        {
            int randomIndex = Random.Range(0, allCustomers.Length);
            currentCustomer = allCustomers[randomIndex];
        }

        customerTransform.position = offScreenPos;
        if (currentCustomer != null)
        {
            customerSpriteRenderer.sprite = currentCustomer.faceEntering;
        }

        iceObject.SetActive(false);
        if (powerBarContainer != null) powerBarContainer.SetActive(false);

        dialogText.text = "";
        powerBarFill.fillAmount = 0f;
        powerBarFill.color = Color.white;
    }

    void StartPlayingPhase()
    {
        currentState = GameState.Playing;
        dialogText.text = "กำลังเสก... (ปล่อยเมื่อพอดี)";
        customerSpriteRenderer.sprite = currentCustomer.faceWaiting;

        iceObject.SetActive(true);
        if (powerBarContainer != null) powerBarContainer.SetActive(true);
    }

    void CheckResult()
    {
        currentState = GameState.Result;
        currentResultTimer = resultDisplayTimer;

        float distance = Mathf.Abs(currentPower - targetPoint);

        if (currentPower >= 100f)
        {
            dialogText.text = "น้ำแข็งแตกกระจาย!";
            customerSpriteRenderer.sprite = currentCustomer.faceBad;
        }
        else if (distance <= greenZone)
        {
            dialogText.text = "PERFECT! เย็นชื่นใจสุดๆ!"; // เอาตัวเลขคะแนนออก
            customerSpriteRenderer.sprite = currentCustomer.facePerfect;
            currentScore += scorePerfect;
            ShowPopupScore(scorePerfect); // เรียกโชว์ Popup
        }
        else if (distance <= yellowZone)
        {
            dialogText.text = "ก็โอเค หยวนๆ ให้ละกัน"; // เอาตัวเลขคะแนนออก
            customerSpriteRenderer.sprite = currentCustomer.faceGood;
            currentScore += scoreGood;
            ShowPopupScore(scoreGood); // เรียกโชว์ Popup
        }
        else
        {
            dialogText.text = "พลาด! แบบนี้กินไม่ได้!";
            customerSpriteRenderer.sprite = currentCustomer.faceBad;
        }

        UpdateScoreUI();
    }

    // ฟังก์ชันสั่งรัน Popup คะแนน
    void ShowPopupScore(int addedScore)
    {
        if (popupScoreText == null) return;
        StopCoroutine("AnimatePopupScore"); // หยุดของเก่าถ้ามันยังเล่นไม่จบ
        StartCoroutine("AnimatePopupScore", addedScore);
    }

    // Coroutine สำหรับอนิเมชันลอยและเฟด
    IEnumerator AnimatePopupScore(int addedScore)
    {
        popupScoreText.gameObject.SetActive(true);
        popupScoreText.text = "+" + addedScore.ToString();

        // รีเซ็ตตำแหน่งและสีกลับเป็นค่าเริ่มต้น
        popupRect.anchoredPosition = popupStartPos;
        Color textColor = popupScoreText.color;
        textColor.a = 1f; // ปรับให้ทึบ 100%
        popupScoreText.color = textColor;

        float timer = 0f;
        while (timer < popupDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / popupDuration; // ค่าจาก 0 ไป 1

            // ขยับตัวอักษรให้ลอยขึ้น
            popupRect.anchoredPosition += Vector2.up * popupFloatSpeed * Time.deltaTime;

            // ทำให้สีค่อยๆ จางลง (Alpha จาก 1 ไป 0)
            textColor.a = Mathf.Lerp(1f, 0f, progress);
            popupScoreText.color = textColor;

            yield return null;
        }

        // พอเล่นจบก็ซ่อนไว้
        popupScoreText.gameObject.SetActive(false);
    }

    void UpdateBarVisual()
    {
        powerBarFill.fillAmount = currentPower / 100f;
        float distance = Mathf.Abs(currentPower - targetPoint);
        Color currentColor = distance <= greenZone ? Color.green : (distance <= yellowZone ? Color.yellow : Color.red);
        powerBarFill.color = currentColor;
        iceSpriteRenderer.color = currentColor;
    }

    void UpdateIceShape()
    {
        if (currentPower >= 100f) iceSpriteRenderer.sprite = brokenIceSprite;
        else if (currentPower >= 3f) iceSpriteRenderer.sprite = iceSprite;
        else iceSpriteRenderer.sprite = waterSprite;
    }

    void UpdateIceScale()
    {
        float currentScale = Mathf.Lerp(minScale, maxScale, currentPower / 100f);
        iceSpriteRenderer.transform.localScale = new Vector3(currentScale, currentScale, 1f);
    }

}