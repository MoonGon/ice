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
    public AudioSource sfxSource;
    public AudioClip sfxCupSlide;
    public AudioClip sfxMelting;
    public AudioClip sfxFreeze;
    public AudioClip sfxIceBreak;
    public AudioClip sfxPerfect;
    public AudioClip sfxBad;

    [Header("ระบบหน้าจอจบเกม (Game Over UI)")]
    public RectTransform gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public float panelSlideSpeed = 2000f;
    private Vector2 panelHiddenPos = new Vector2(0f, -1500f);
    private Vector2 panelShowPos = new Vector2(0f, 0f);
    private bool isGameOverPanelMoving = false;

    private float currentPower = 0f;
    private float resultDisplayTimer = 2f;
    private float currentResultTimer = 0f;
    private float meltDripTimer = 0f;

    void Start()
    {
        if (popupScoreText != null)
        {
            popupRect = popupScoreText.GetComponent<RectTransform>();
            popupStartPos = popupRect.anchoredPosition;
            popupScoreText.gameObject.SetActive(false);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.anchoredPosition = panelHiddenPos;
            gameOverPanel.gameObject.SetActive(false);
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
                    if (frostWaitTimer > 0) frostWaitTimer -= Time.deltaTime;
                    else currentFrostAlpha -= frostMeltSpeed * Time.deltaTime;
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
                    if (cupTransform != null) cupTransform.position = Vector3.MoveTowards(cupTransform.position, cupOnScreenPos, cupSlideSpeed * Time.deltaTime);

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
                    else if (Input.GetKeyDown(KeyCode.Space)) StartPlayingPhase();
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
                else if (Input.GetKeyUp(KeyCode.Space)) CheckResult();
                break;

            case GameState.Result:
                if (isFrozen)
                {
                    currentMeltTimer -= Time.deltaTime;
                    meltDripTimer -= Time.deltaTime;

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
                    if (currentResultTimer <= 0) StartOrderPhase();
                }
                break;

            case GameState.GameOver:
                // จัดการสไลด์หน้าจอ GameOver
                if (isGameOverPanelMoving && gameOverPanel != null)
                {
                    gameOverPanel.anchoredPosition = Vector2.MoveTowards(gameOverPanel.anchoredPosition, panelShowPos, panelSlideSpeed * Time.deltaTime);
                    if (Vector2.Distance(gameOverPanel.anchoredPosition, panelShowPos) < 1f)
                    {
                        isGameOverPanelMoving = false; // สไลด์เสร็จแล้ว
                    }
                }

                // รองรับการกดคีย์บอร์ดด้วย (เผื่อคนไม่อยากคลิก)
                if (Input.GetKeyDown(KeyCode.R)) RestartGame();
                else if (Input.GetKeyDown(KeyCode.M)) GoToMainMenu();
                break;
        }
    }

    // --- ฟังก์ชันปุ่มกด (เรียกใช้จากปุ่ม UI) ---
    public void RestartGame()
    {
        if (gameOverPanel != null) gameOverPanel.gameObject.SetActive(false);
        currentFrostAlpha = 0f;
        UpdateFrostAlpha(0f);
        timeRemaining = gameDuration;
        currentScore = 0;
        UpdateScoreUI();
        StartOrderPhase();
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
    // ------------------------------------

    void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip);
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
        if (powerBarContainer != null) powerBarContainer.SetActive(true);
    }

    void UpdateTimerUI()
    {
        timerText.text = "เวลา: " + Mathf.CeilToInt(timeRemaining).ToString();
    }

    void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = "คะแนน: " + currentScore.ToString();
    }

    void EndGame()
    {
        currentState = GameState.GameOver;
        iceObject.SetActive(false);
        if (powerBarContainer != null) powerBarContainer.SetActive(false);
        customerObjectCheck();
        customerSpriteRenderer.sprite = currentCustomer.faceEntering;

        dialogText.text = "TIME OUT!";

        // สั่งโชว์และเลื่อนหน้าจอ Game Over
        if (gameOverPanel != null)
        {
            gameOverPanel.gameObject.SetActive(true);
            gameOverPanel.anchoredPosition = panelHiddenPos;

            if (finalScoreText != null)
            {
                finalScoreText.text = "TIME OUT!\n\nคะแนนที่ได้:\n" + currentScore;
            }
            isGameOverPanelMoving = true;
        }

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
        if (cupTransform != null) cupTransform.position = cupOffScreenPos;
        if (currentCustomer != null) customerSpriteRenderer.sprite = currentCustomer.faceEntering;

        iceObject.SetActive(false);
        if (powerBarContainer != null) powerBarContainer.SetActive(false);

        dialogText.text = "";
        powerBarFill.fillAmount = 0f;
        powerBarFill.color = Color.white;

        PlaySFX(sfxCupSlide);
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
            dialogText.text = "น้ำแข็งพังทลาย!";
            customerSpriteRenderer.sprite = currentCustomer.faceBad;

            isFrozen = true;
            currentMeltTimer = meltDuration;
            currentFrostAlpha = 1f;
            UpdateFrostAlpha(1f);

            PlaySFX(sfxIceBreak);
            PlaySFX(sfxFreeze);
            meltDripTimer = 0.5f;
        }
        else if (distance <= greenZone)
        {
            dialogText.text = "PERFECT! เย็นชื่นใจสุดๆ!";
            customerSpriteRenderer.sprite = currentCustomer.facePerfect;
            currentScore += scorePerfect;
            ShowPopupScore(scorePerfect);
            PlaySFX(sfxPerfect);
        }
        else if (distance <= yellowZone)
        {
            dialogText.text = "ก็โอเค หยวนๆ ให้ละกัน";
            customerSpriteRenderer.sprite = currentCustomer.faceGood;
            currentScore += scoreGood;
            ShowPopupScore(scoreGood);
            PlaySFX(sfxPerfect);
        }
        else
        {
            dialogText.text = "พลาด! แบบนี้กินไม่ได้!";
            customerSpriteRenderer.sprite = currentCustomer.faceBad;
            PlaySFX(sfxBad);
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