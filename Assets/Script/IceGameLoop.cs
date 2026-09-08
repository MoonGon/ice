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
    public TextMeshProUGUI popupScoreText;
    public float popupDuration = 1f;
    public float popupFloatSpeed = 50f;
    private RectTransform popupRect;
    private Vector2 popupStartPos;

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

    [Header("ระบบอนิเมชันแก้วน้ำ")]
    public Transform cupTransform;
    public Vector3 cupOffScreenPos = new Vector3(0f, -6f, 0f);
    public Vector3 cupOnScreenPos = new Vector3(0f, -2f, 0f);
    public float cupSlideSpeed = 15f;

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

    [Header("ระบบเวทย์ Overload (Meltdown)")]
    public float meltDuration = 3f;
    private bool isFrozen = false;
    private float currentMeltTimer = 0f;

    [Header("ระบบความยาก: เอฟเฟกต์น้ำแข็งสะสมเกาะจอ")]
    public Image frostOverlay;
    public float frostBuildSpeed = 0.35f;
    public float frostWaitDelay = 1f;
    public float frostMeltSpeed = 0.5f;
    private float currentFrostAlpha = 0f;
    private float frostWaitTimer = 0f;

    [Header("ระบบเสียงเอฟเฟกต์ (SFX)")]
    public AudioSource sfxSource;     // ใส่ AudioSource สำหรับเล่นเสียง
    public AudioClip sfxCupSlide;     // เสียงเลื่อนแก้ว
    public AudioClip sfxMelting;      // เสียงน้ำหยด (เล่นตอนรอละลาย)
    public AudioClip sfxFreeze;       // เสียงพังทลาย/กระจกแตก
    public AudioClip sfxIceBreak;
    public AudioClip sfxPerfect;      // เสียง Good/Perfect
    public AudioClip sfxBad;          // เสียงฟ้าผ่า/พลาด

    private float currentPower = 0f;
    private float resultDisplayTimer = 2f;
    private float currentResultTimer = 0f;
    private float meltDripTimer = 0f; // ตัวจับเวลาสำหรับเล่นเสียงน้ำหยดรัวๆ

    void Start()
    {
        if (popupScoreText != null)
        {
            popupRect = popupScoreText.GetComponent<RectTransform>();
            popupStartPos = popupRect.anchoredPosition;
            popupScoreText.gameObject.SetActive(false);
        }

        currentFrostAlpha = 0f;
        UpdateFrostAlpha(0f);

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

            if (!isFrozen)
            {
                if (currentState == GameState.Playing && Input.GetKey(KeyCode.Space))
                {
                    currentFrostAlpha += frostBuildSpeed * Time.deltaTime;
                    frostWaitTimer = frostWaitDelay;
                }
                else
                {
                    if (frostWaitTimer > 0)
                    {
                        frostWaitTimer -= Time.deltaTime;
                    }
                    else
                    {
                        currentFrostAlpha -= frostMeltSpeed * Time.deltaTime;
                    }
                }
                currentFrostAlpha = Mathf.Clamp01(currentFrostAlpha);
                UpdateFrostAlpha(currentFrostAlpha);
            }
        }

        switch (currentState)
        {
            case GameState.Order:
                if (!isCustomerInPosition)
                {
                    customerTransform.position = Vector3.MoveTowards(customerTransform.position, onScreenPos, slideSpeed * Time.deltaTime);

                    if (cupTransform != null)
                    {
                        cupTransform.position = Vector3.MoveTowards(cupTransform.position, cupOnScreenPos, cupSlideSpeed * Time.deltaTime);
                    }

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

                    if (currentFrostAlpha >= 1f || currentPower >= 100f)
                    {
                        currentPower = 100f;
                        UpdateBarVisual();
                        UpdateIceShape();
                        UpdateIceScale();
                        CheckResult();
                    }
                }
                else if (Input.GetKeyUp(KeyCode.Space))
                {
                    CheckResult();
                }
                break;

            case GameState.Result:
                if (isFrozen)
                {
                    currentMeltTimer -= Time.deltaTime;
                    meltDripTimer -= Time.deltaTime;

                    // เล่นเสียงน้ำหยดทุกๆ 0.5 วินาทีระหว่างที่กำลังรอละลาย
                    if (meltDripTimer <= 0f)
                    {
                        PlaySFX(sfxMelting);
                        meltDripTimer = 0.5f;
                    }

                    dialogText.text = "เวทย์ Meltdown!\nรอน้ำแข็งที่ตัวละลาย... " + currentMeltTimer.ToString("F1") + " วินาที";

                    currentFrostAlpha = currentMeltTimer / meltDuration;
                    UpdateFrostAlpha(currentFrostAlpha);

                    if (currentMeltTimer <= 0)
                    {
                        isFrozen = false;
                        currentFrostAlpha = 0f;
                        UpdateFrostAlpha(0f);
                        StartOrderPhase();
                    }
                }
                else
                {
                    currentResultTimer -= Time.deltaTime;
                    if (currentResultTimer <= 0)
                    {
                        StartOrderPhase();
                    }
                }
                break;

            case GameState.GameOver:
                if (Input.GetKeyDown(KeyCode.R))
                {
                    currentFrostAlpha = 0f;
                    UpdateFrostAlpha(0f);
                    timeRemaining = gameDuration;
                    currentScore = 0;
                    UpdateScoreUI();
                    StartOrderPhase();
                }
                else if (Input.GetKeyDown(KeyCode.M))
                {
                    SceneManager.LoadScene("MainMenu");
                }
                break;
        }
    }

    // ฟังก์ชันเสริมสำหรับเล่นเสียง
    void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    void UpdateFrostAlpha(float alpha)
    {
        if (frostOverlay != null)
        {
            Color c = frostOverlay.color;
            c.a = Mathf.Clamp01(alpha);
            frostOverlay.color = c;
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
        dialogText.text = "หมดเวลา!\nคะแนนรวมของคุณคือ: " + currentScore + "\n(กด R เล่นใหม่ หรือ กด M กลับหน้าเมนู)";

        currentFrostAlpha = 0f;
        UpdateFrostAlpha(0f);
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
        if (cupTransform != null)
        {
            cupTransform.position = cupOffScreenPos;
        }

        if (currentCustomer != null)
        {
            customerSpriteRenderer.sprite = currentCustomer.faceEntering;
        }

        iceObject.SetActive(false);
        if (powerBarContainer != null) powerBarContainer.SetActive(false);

        dialogText.text = "";
        powerBarFill.fillAmount = 0f;
        powerBarFill.color = Color.white;

        PlaySFX(sfxCupSlide); // เล่นเสียงตอนแก้วเริ่มสไลด์ขึ้นมาบนโต๊ะ
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
            dialogText.text = "แตกหมดแล้ว!";
            customerSpriteRenderer.sprite = currentCustomer.faceBad;

            isFrozen = true;
            currentMeltTimer = meltDuration;
            currentFrostAlpha = 1f;
            UpdateFrostAlpha(1f);

            PlaySFX(sfxIceBreak);
            PlaySFX(sfxFreeze); // เล่นเสียงเพล้ง/แข็ง
            meltDripTimer = 0.5f; // เริ่มจับเวลาเสียงน้ำหยด
        }
        else if (distance <= greenZone)
        {
            dialogText.text = "PERFECT!";
            customerSpriteRenderer.sprite = currentCustomer.facePerfect;
            currentScore += scorePerfect;
            ShowPopupScore(scorePerfect);

            PlaySFX(sfxPerfect); // เล่นเสียง Perfect
        }
        else if (distance <= yellowZone)
        {
            dialogText.text = "ก็โอเค หยวนๆ ให้ละกัน";
            customerSpriteRenderer.sprite = currentCustomer.faceGood;
            currentScore += scoreGood;
            ShowPopupScore(scoreGood);

            PlaySFX(sfxPerfect); // ให้เสียง Good ใช้เสียงเดียวกับ Perfect ไปก่อน
        }
        else
        {
            dialogText.text = "แบบนี้ไม่กินโว้ยยย!";
            customerSpriteRenderer.sprite = currentCustomer.faceBad;

            PlaySFX(sfxBad); // เล่นเสียงฟ้าผ่า/พลาด
        }

        UpdateScoreUI();
    }

    void ShowPopupScore(int addedScore)
    {
        if (popupScoreText == null) return;
        StopCoroutine("AnimatePopupScore");
        StartCoroutine("AnimatePopupScore", addedScore);
    }

    IEnumerator AnimatePopupScore(int addedScore)
    {
        popupScoreText.gameObject.SetActive(true);
        popupScoreText.text = "+" + addedScore.ToString();

        popupRect.anchoredPosition = popupStartPos;
        Color textColor = popupScoreText.color;
        textColor.a = 1f;
        popupScoreText.color = textColor;

        float timer = 0f;
        while (timer < popupDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / popupDuration;

            popupRect.anchoredPosition += Vector2.up * popupFloatSpeed * Time.deltaTime;

            textColor.a = Mathf.Lerp(1f, 0f, progress);
            popupScoreText.color = textColor;

            yield return null;
        }

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