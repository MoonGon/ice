using UnityEngine;

public class IceBlockController : MonoBehaviour
{
    [Header("ตั้งค่าขนาดน้ำแข็ง")]
    public float growSpeed = 2f;      // ความเร็วในการขยายขนาด
    public float maxScale = 5f;       // ขนาดใหญ่สุดที่ขยายได้
    public float targetScale = 3f;    // ขนาดที่ลูกค้าต้องการ (เป้าหมาย)

    [Header("ระยะความคลาดเคลื่อน (ความห่างของสเกล)")]
    public float greenZone = 0.5f;    // ห่างจากเป้าหมายแค่ 0.5 = สีเขียว (พอดีเป๊ะ)
    public float yellowZone = 1.0f;   // ห่าง 1.0 = สีเหลือง (ใกล้เคียง)

    private Vector3 originalScale;
    private bool isPlaying = true;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        // เก็บสเกลเริ่มต้นและเรียกใช้คอมโพเนนต์สีของ Sprite
        originalScale = transform.localScale;
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = Color.red; // เริ่มต้นเป็นสีแดง (เล็กไป)
    }

    void Update()
    {
        if (!isPlaying && Input.GetKeyDown(KeyCode.R))
        {
            ResetGame();
        }

        if (!isPlaying) return;

        if (Input.GetKey(KeyCode.Space))
        {
            // ขยายขนาดก้อนน้ำแข็ง (แกน X และ Y โตไปพร้อมกัน)
            transform.localScale += Vector3.one * growSpeed * Time.deltaTime;

            // ล็อคขนาดไม่ให้ขยายเกิน maxScale
            if (transform.localScale.x > maxScale)
            {
                transform.localScale = new Vector3(maxScale, maxScale, 1f);
            }

            UpdateColorFeedback();
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            isPlaying = false;
            CheckResult();
        }
    }

    void UpdateColorFeedback()
    {
        float currentSize = transform.localScale.x;
        float distance = Mathf.Abs(currentSize - targetScale);

        if (distance <= greenZone)
            spriteRenderer.color = Color.green;
        else if (distance <= yellowZone)
            spriteRenderer.color = Color.yellow;
        else
            spriteRenderer.color = Color.red;
    }

    void CheckResult()
    {
        float currentSize = transform.localScale.x;
        float distance = Mathf.Abs(currentSize - targetScale);

        if (distance <= greenZone)
            Debug.Log("PERFECT! สีเขียว! ขนาดพอดีแก้วเป๊ะ");
        else if (distance <= yellowZone)
            Debug.Log("เกือบไป! สีเหลือง! ลูกค้าพอรับได้");
        else if (currentSize < targetScale)
            Debug.Log("FAIL! เล็กไป น้ำแข็งละลายเป็นน้ำ");
        else
            Debug.Log("FAIL! ใหญ่ไป น้ำแข็งแตกกระจาย");

        Debug.Log("== กด R เพื่อเริ่มใหม่ ==");
    }

    void ResetGame()
    {
        transform.localScale = originalScale;
        spriteRenderer.color = Color.red;
        isPlaying = true;
    }
}