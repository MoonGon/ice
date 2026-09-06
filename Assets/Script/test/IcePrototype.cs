using UnityEngine;
using UnityEngine.UI; // ต้องมีบรรทัดนี้เพื่อเรียกใช้คำสั่ง UI

public class IcePrototype : MonoBehaviour
{
    [Header("การแสดงผล (ลาก UI มาใส่)")]
    public Image powerBarFill;

    [Header("ตั้งค่าเกม (ปรับตัวเลขเทสความสนุกได้เลย)")]
    public float fillSpeed = 50f;     // ความเร็วในการเติมหลอด
    public float targetPoint = 75f;   // จุดที่ลูกค้าพอดี (จาก 0 ถึง 100)

    // ตั้งค่าระยะความห่างจากจุดพอดีเพื่อเปลี่ยนสี
    public float greenZone = 5f;      // ถ้าห่างเป้าหมายแค่ 5 = เขียว (เป๊ะมาก)
    public float yellowZone = 15f;    // ถ้าห่างเป้าหมายไม่เกิน 15 = เหลือง (ใกล้เคียง)
    // ถ้าเกินกว่านั้นจะเป็น สีแดง (ไกลไป)

    private float currentPower = 0f;
    private bool isPlaying = true;

    void Update()
    {
        // ถ้ารอบนี้จบแล้ว ให้กด R เพื่อรีเซ็ตเล่นใหม่
        if (!isPlaying && Input.GetKeyDown(KeyCode.R))
        {
            ResetGame();
        }

        // ถ้าเล่นจบแล้ว (ปล่อยสเปซบาร์ไปแล้ว) ให้หยุดการทำงานข้างล่าง
        if (!isPlaying) return;

        // ถ้าผู้เล่นกด Spacebar ค้างไว้
        if (Input.GetKey(KeyCode.Space))
        {
            // เพิ่มค่าพลังงาน
            currentPower += fillSpeed * Time.deltaTime;
            currentPower = Mathf.Clamp(currentPower, 0f, 100f); // ล็อคไม่ให้หลอดทะลุ 100

            UpdateBarVisual(); // อัปเดตภาพและสีของหลอด
        }

        // ถ้าผู้เล่นปล่อย Spacebar (เสิร์ฟน้ำแข็ง)
        if (Input.GetKeyUp(KeyCode.Space))
        {
            isPlaying = false;
            CheckResult(); // ตรวจผลลัพธ์
        }
    }

    void UpdateBarVisual()
    {
        // 1. ทำให้หลอด UI เติมตามค่าที่เรากดได้ (UI มันรับค่าแค่ 0-1 เลยต้องเอาพลังไปหาร 100)
        powerBarFill.fillAmount = currentPower / 100f;

        // 2. คำนวณว่าตอนนี้พลังงานเรา ห่างจากจุดเป้าหมายแค่ไหน
        float distance = Mathf.Abs(currentPower - targetPoint);

        // 3. เปลี่ยนสีตามระยะห่าง
        if (distance <= greenZone)
        {
            powerBarFill.color = Color.green; // พอดี = เขียว
        }
        else if (distance <= yellowZone)
        {
            powerBarFill.color = Color.yellow; // ใกล้ๆ = เหลือง
        }
        else
        {
            powerBarFill.color = Color.red; // ไกลเกิน = แดง
        }
    }

    void CheckResult()
    {
        // พิมพ์ผลลัพธ์ลงใน Console เพื่อดูว่าเราทำได้ระดับไหน
        float distance = Mathf.Abs(currentPower - targetPoint);

        if (distance <= greenZone)
            Debug.Log("PERFECT! สีเขียว! ลูกค้าชอบมาก");
        else if (distance <= yellowZone)
            Debug.Log("เกือบไป! สีเหลือง! ลูกค้าพอรับได้");
        else if (currentPower < targetPoint)
            Debug.Log("FAIL! สีแดง (น้อยไป) น้ำแข็งละลายเป็นน้ำ");
        else
            Debug.Log("FAIL! สีแดง (มากไป) น้ำแข็งแตกกระจาย");

        Debug.Log("== กดปุ่ม R เพื่อเล่นรอบใหม่ ==");
    }

    void ResetGame()
    {
        currentPower = 0f;
        powerBarFill.fillAmount = 0f;
        powerBarFill.color = Color.red;
        isPlaying = true;
        Debug.Log("เริ่มรอบใหม่! กด Spacebar ค้างไว้");
    }
}