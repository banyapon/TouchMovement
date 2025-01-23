using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class NewDragNGo : MonoBehaviour
{
    public Transform player; // ตัวละครที่เราต้องการควบคุม
    public Camera vrCamera; // กล้องที่ใช้ในการยิง Raycast
    public LineRenderer laserPointer; // เลเซอร์ที่แสดงตำแหน่ง Raycast
    public LayerMask raycastLayers; // เลเยอร์ที่กำหนดให้ Raycast ตรวจจับ
    public float rotationSpeed = 5f; // ความเร็วในการหมุนของตัวละคร
    public float moveSpeed = 5f; // ความเร็วในการเคลื่อนที่ของตัวละคร

    private Vector2 initialTouchDelta; // ระยะห่างเริ่มต้นระหว่างนิ้วสองนิ้วในการหมุน
    private bool isRotating = false; // สถานะว่ากำลังหมุนอยู่หรือไม่
    private bool isDragging = false; // สถานะว่ากำลังลากนิ้วอยู่หรือไม่
    private Vector3 originalVEPosition; // ตำแหน่งเริ่มต้นของตัวละครเมื่อเริ่ม Gesture
    private Vector3 raycastTarget; // ตำแหน่งที่ Raycast ชี้ไป
    private float touchStartPosition; // ตำแหน่ง Y บนหน้าจอที่เริ่มสัมผัส
    private float screenHeight; // ความสูงของหน้าจอ (dynamic)

    public Text touchInfoText;
    private StreamWriter writer;
    private string formattedTime;
    private DateTime now;

    void Start()
    {
        screenHeight = Screen.height; // กำหนดความสูงหน้าจอตามอุปกรณ์
        writer = new StreamWriter("data.log", true);
    }

    void Update()
    {
        now = DateTime.Now;
        formattedTime = now.ToString("dd/MM/yyyy HH:mm:ss:fff");
        HandleTouchInput(); // จัดการการสัมผัสหน้าจอ
        UpdateLaserPointer(); // อัปเดตตำแหน่งของเลเซอร์
    }
    void OnDestroy()
    {
        if (writer != null)
        {
            writer.Close();
        }
    }

    void HandleTouchInput()
    {

        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            LogTouchData(touch);
            if (touch.phase == TouchPhase.Began)
            {
                RaycastHit hit;
                Vector3 laserStart = player.position + Vector3.up * 1.5f; // เริ่มยิงเลเซอร์จากตำแหน่งของ Player
                Vector3 laserDirection = vrCamera.transform.forward; // ทิศทางของเลเซอร์

                if (Physics.Raycast(laserStart, laserDirection, out hit, Mathf.Infinity, raycastLayers))
                {
                    raycastTarget = hit.point; // ตำแหน่งเป้าหมายที่ Raycast ตกกระทบ
                    originalVEPosition = player.position; // ตำแหน่งเริ่มต้นของ Player
                }

                touchStartPosition = touch.position.y; // ตำแหน่งเริ่มต้นของการสัมผัส
                isDragging = true; // เริ่มต้นการลาก
            }
            else if (touch.phase == TouchPhase.Ended && isDragging)
            {
                isDragging = false; // ยกเลิกสถานะการลาก

                float dragDistance = Mathf.Abs(touch.position.y - touchStartPosition); // ระยะทางที่ลากนิ้ว
                float initialTouchOffset = screenHeight - touchStartPosition; // ระยะจากจุดเริ่มต้นถึงขอบล่างของหน้าจอ
                float VRDistance = Vector3.Distance(originalVEPosition, raycastTarget); // ระยะทางจาก Player ถึงเป้าหมาย

                if (initialTouchOffset <= 0 || VRDistance <= 0) return; // ตรวจสอบว่าไม่มีค่าผิดพลาด

                float scale = VRDistance / initialTouchOffset; // คำนวณ Scale ตามสูตร (VErc / (SDdi - d))
                float moveDistance = dragDistance * scale; // ระยะทางที่ต้องเคลื่อนที่ = Scale * (d2-d)

                Vector3 direction = (raycastTarget - originalVEPosition).normalized; // ทิศทางจากตำแหน่งเริ่มต้นไปยังเป้าหมาย
                Vector3 targetPosition = originalVEPosition + direction * moveDistance; // ตำแหน่งเป้าหมายใหม่

                if (Vector3.Distance(originalVEPosition, targetPosition) > VRDistance)
                {
                    targetPosition = raycastTarget; // จำกัดไม่ให้เกินตำแหน่งเป้าหมาย Raycast
                }

                targetPosition.y = player.position.y; // รักษาระดับความสูงเดิม

                StartCoroutine(MoveToPosition(targetPosition)); // เริ่มเคลื่อนที่ไปยังเป้าหมาย
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            if (!isRotating)
            {
                initialTouchDelta = touch2.position - touch1.position; // คำนวณระยะห่างเริ่มต้น
                isRotating = true; // เริ่มสถานะการหมุน
            }
            else
            {
                Vector2 currentTouchDelta = touch2.position - touch1.position; // คำนวณระยะห่างปัจจุบัน
                float rotationDelta = (currentTouchDelta.x - initialTouchDelta.x) * rotationSpeed * Time.deltaTime; // คำนวณความเร็วการหมุน

                player.Rotate(Vector3.up, rotationDelta); // หมุน Player ตามแกน Y
                initialTouchDelta = currentTouchDelta; // อัปเดตระยะห่างเริ่มต้น
            }
        }
        else
        {
            isRotating = false; // ยกเลิกสถานะการหมุน
        }
    }

    void UpdateLaserPointer()
    {
        if (laserPointer == null || vrCamera == null) return;

        Vector3 laserStart = player.position + Vector3.up * 1.5f; // จุดเริ่มต้นของเลเซอร์
        Vector3 laserDirection = vrCamera.transform.forward; // ทิศทางเลเซอร์ตามกล้อง

        laserPointer.SetPosition(0, laserStart); // กำหนดจุดเริ่มต้นเลเซอร์

        RaycastHit hit;
        if (Physics.Raycast(laserStart, laserDirection, out hit, Mathf.Infinity, raycastLayers))
        {
            laserPointer.SetPosition(1, hit.point); // กำหนดจุดปลายทางเมื่อเลเซอร์ตกกระทบ

            float distance = Vector3.Distance(player.position, hit.point); // คำนวณระยะทางจาก Player ถึงจุดตกกระทบ
            Debug.Log($"Laser hit {hit.collider.name}, Distance: {distance}");
        }
        else
        {
            laserPointer.SetPosition(1, laserStart + laserDirection * 100f); // ยิงเลเซอร์ไกล 100 หน่วยถ้าไม่ชนวัตถุ
        }
    }

    private IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        while (Vector3.Distance(player.position, targetPosition) > 0.16f)
        {
            player.position = Vector3.MoveTowards(player.position, targetPosition, moveSpeed * Time.deltaTime); // เคลื่อนที่ไปยังเป้าหมายทีละนิด
            yield return null;
        }

        // หยุดเมื่อถึงเป้าหมาย
        player.position = targetPosition;
        Debug.Log("Player reached the target position and stopped.");
    }

    private void LogTouchData(Touch touch)
    {
        string logMessage = string.Format(
            "{0},{1},{2},{3},{4},{5},{6}",
            "dragngo",
            touch.fingerId,
            touch.position,
            touch.deltaPosition,
            touch.phase,
            touch.tapCount,
            formattedTime
        );

        if (writer != null)
        {
            writer.WriteLine(logMessage);
            writer.Flush();
        }

        if (touchInfoText != null)
        {
            touchInfoText.text = logMessage;
        }

        Debug.Log(logMessage);
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneObject(sceneName));
    }

    public IEnumerator LoadSceneObject(string sceneName)
    {
        AsyncOperation async = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        async.allowSceneActivation = false;

        while (!async.isDone)
        {
            float progress = Mathf.Clamp01(async.progress / 0.9f);
            Debug.Log("Loading progress: " + (progress * 100).ToString("n0") + "%");

            if (progress == 1f)
            {
                async.allowSceneActivation = true;
            }
            yield return null;
        }
    }
}
