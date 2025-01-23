using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class DragNGo : MonoBehaviour
{
    public Transform player;
    public Camera vrCamera;
    public LayerMask raycastLayers;
    public float maxDistance = 50f;
    public float moveSpeed = 5f;
    public float screenHeight;
    public Text touchInfoText;

    private Vector3 raycastTarget;
    private Vector3 startTouchPosition;
    private Vector3 endTouchPosition;
    private bool isDragging = false;

    private StreamWriter writer;
    private string formattedTime;
    private DateTime now;

    void Start()
    {
        screenHeight = Screen.height;
        writer = new StreamWriter("data.log", true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LoadScene("Main");
        }

        now = DateTime.Now;
        formattedTime = now.ToString("dd/MM/yyyy HH:mm:ss:fff");

        if (Input.touchCount == 1)
        {
            HandleTouchInput();
        }
        else
        {
            isDragging = false;
        }
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
        Touch touch = Input.GetTouch(0);
        LogTouchData(touch);

        if (touch.phase == TouchPhase.Began)
        {
            RaycastHit hit;
            Ray ray = vrCamera.ScreenPointToRay(touch.position);

            if (Physics.Raycast(ray, out hit, maxDistance, raycastLayers))
            {
                raycastTarget = hit.point;
            }
            else
            {
                raycastTarget = vrCamera.transform.position + vrCamera.transform.forward * maxDistance;
            }

            startTouchPosition = touch.position;
            isDragging = true;
        }
        else if (touch.phase == TouchPhase.Moved && isDragging)
        {
            CalculateAndMove(touch);
        }
        else if (touch.phase == TouchPhase.Ended && isDragging)
        {
            isDragging = false;
        }
    }

    void CalculateAndMove(Touch touch)
    {
        // ตรวจสอบว่าเริ่ม Gesture หรือไม่
        if (!isDragging) return;

        // ระยะที่นิ้วลากตั้งแต่จุดเริ่มต้น (d2 - d)
        float dragDistance = touch.position.y - startTouchPosition.y;

        // ระยะรวมระหว่าง Starting Position และ Bottom End ของ Surface (SDdi - d)
        float initialTouchOffset = screenHeight - startTouchPosition.y;

        // ระยะระหว่างตำแหน่ง Original และ Target ของ Raycast (VErc)
        float VRDistance = Vector3.Distance(player.position, raycastTarget);

        // ตรวจสอบว่า Scale มีค่าหรือไม่
        if (initialTouchOffset <= 0 || VRDistance <= 0) return;

        // คำนวณ Scale
        float scale = VRDistance / Mathf.Max(initialTouchOffset, 1f);

        // ปรับ Scale ให้สัมพันธ์กับตำแหน่งเริ่มต้นการลาก
        float touchPositionFactor = Mathf.Clamp01(startTouchPosition.y / screenHeight);
        scale *= 1f + (1f - touchPositionFactor); // เพิ่ม Scale เมื่อเริ่มจากตำแหน่งกลางหน้าจอ

        // ระยะทางที่นิ้วลากไป (d2 - d)
        float totalDragDistance = Mathf.Max(0, dragDistance);

        // ระยะทางที่ Avatar ควรเคลื่อนที่ (VEdi)
        float moveDistance = totalDragDistance * scale;

        // ทิศทางการเคลื่อนที่ของ Avatar (Normalized Vector)
        Vector3 direction = (raycastTarget - player.position).normalized;

        // คำนวณตำแหน่งใหม่
        Vector3 targetPosition = player.position + direction * moveDistance;

        // จำกัดตำแหน่ง Avatar ให้ถึงแค่จุด Raycast
        if (Vector3.Distance(player.position, targetPosition) > VRDistance)
        {
            targetPosition = raycastTarget;
        }

        // รักษาความสูงเดิม (ไม่เปลี่ยนแกน Y)
        targetPosition.y = player.position.y;

        // เคลื่อนที่ Avatar
        player.position = Vector3.MoveTowards(player.position, targetPosition, moveSpeed * Time.deltaTime);
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
