using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Log
using System;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ForwardMovement : MonoBehaviour
{
    public float moveSpeed = 5f; // ปรับความเร็วให้เหมาะสม
    public float moveDuration = 0.5f; // ระยะเวลาในการเคลื่อนที่แต่ละครั้ง (ปรับได้ตามต้องการ)
    public float rotationSpeed = 100f; // ความเร็วในการหมุน

    private Vector2 touchStartPosition;
    private Vector2 touchEndPosition;

    private StreamWriter writer; // ตัวแปรสำหรับเขียนข้อมูลลงไฟล์
    private string formattedTime;
    DateTime now;

    public Text touchInfoText;

    //Algorithm 3
    private bool isRotating = false; //  ตัวแปรสำหรับตรวจสอบว่ากำลังหมุนอยู่หรือไม่

    void Start()
    {
        writer = new StreamWriter("data.log", true); // สร้าง StreamWriter และเปิดไฟล์ในโหมด append
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LoadScene("Main");
        }

        now = DateTime.Now;
        formattedTime = now.ToString("dd/MM/yyyy HH:mm:ss:fff");

        // ---  Touch 0 (สำหรับเดินหน้า/ถอยหลัง) ---
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            LogTouchData(touch);
            Debug.Log("GetTouch 0, FingerID=" + touch.fingerId + ",position=" + touch.position);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPosition = touch.position;
                    break;

                case TouchPhase.Moved:
                    //  (ไม่มีการกระทำใน TouchPhase.Moved สำหรับ Touch 0)
                    break;

                case TouchPhase.Ended:
                    touchEndPosition = touch.position;
                    Debug.Log("Ended Position:" + touchEndPosition);
                    Vector2 swipeDirection = touchEndPosition - touchStartPosition;

                    // คำนวณระยะทางการลากนิ้ว (swipeDistance)
                    float swipeDistance = swipeDirection.magnitude;

                    // คำนวณระยะทางการเคลื่อนที่ของวัตถุ (moveDistance) โดยอิงจาก swipeDistance และขนาดหน้าจอ
                    float screenDiagonal = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
                    float moveDistance = (swipeDistance / screenDiagonal) * 10f; // ปรับ 10f ตามความเหมาะสม

                    // ตรวจสอบทิศทางการปัด และเริ่ม Coroutine การเคลื่อนที่
                    if (swipeDirection.y < 0 && Mathf.Abs(swipeDirection.y) > Mathf.Abs(swipeDirection.x))
                    {
                        // ตวัดลง = เดินหน้า
                        StartCoroutine(MoveCoroutine(moveDistance, 1)); // 1 คือ เดินหน้า
                    }
                    else if (swipeDirection.y > 0 && Mathf.Abs(swipeDirection.y) > Mathf.Abs(swipeDirection.x))
                    {
                        // ตวัดขึ้น = เดินถอยหลัง
                        StartCoroutine(MoveCoroutine(moveDistance, -1)); // -1 คือ เดินถอยหลัง
                    }
                    break;
            }
        }


        // ---  Touch 1 (สำหรับหมุน) ---
        // --- Rotation-in-place (Touch Input) ---
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            // คำนวณมุมระหว่างนิ้วสองนิ้วในเฟรมปัจจุบัน
            float currentAngle = Vector2.SignedAngle(touch0.position - touch1.position, Vector2.right);

            // คำนวณมุมระหว่างนิ้วสองนิ้วในเฟรมก่อนหน้า
            float previousAngle = Vector2.SignedAngle(
                (touch0.position - touch0.deltaPosition) - (touch1.position - touch1.deltaPosition),
                Vector2.right
            );

            // หาผลต่างของมุมเพื่อใช้ในการหมุน
            float rotateAmount = currentAngle - previousAngle;

            // หมุนตัวละคร
            transform.Rotate(0f, rotateAmount, 0f);

            // จำกัดการหมุน 90 องศา โดยอ้างอิงจากตำแหน่งเริ่มต้นของ touch0
            Vector3 currentRotation = transform.eulerAngles;
            if (touch0.position.y > Screen.width / 2)
            {
                currentRotation.y = Mathf.Clamp(currentRotation.y, -180f, 0f);
            }

            transform.eulerAngles = currentRotation;
        }

    }

    // Coroutine สำหรับการเคลื่อนที่ (เดินหน้า/ถอยหลัง)
    IEnumerator MoveCoroutine(float moveDistance, int direction)
    {
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = transform.position + transform.forward * moveDistance * direction;

        while (elapsedTime < moveDuration)
        {
            Debug.Log("targetPosition: " + targetPosition);
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
            // รอเฟรมถัดไป
        }
        transform.position = targetPosition; // ตรวจสอบให้แน่ใจว่าถึงตำแหน่งเป้าหมาย
    }

    IEnumerator MoveForwardCoroutine(float moveDistance)
    {
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = transform.position + transform.forward * moveDistance;

        while (elapsedTime < moveDuration)
        {
            Debug.Log("targetPosition: " + targetPosition);
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null; // รอเฟรมถัดไป
        }

        transform.position = targetPosition; // ตรวจสอบให้แน่ใจว่าถึงตำแหน่งเป้าหมาย
    }

    //เก็บ LogTouch
    private void LogTouchData(Touch touch)
    {
        formattedTime = now.ToString("dd/MM/yyyy HH:mm:ss:fff");
        string logMessage = string.Format(
            "Finger ID: {0}\n" +
            "Position: {1}\n" +
            "Delta Position: {2}\n" +
            "Phase: {3}\n" +
            "Tap Count: {4}\n" +
            "Time: {5}\n",
            touch.fingerId, touch.position, touch.deltaPosition, touch.phase, touch.tapCount, formattedTime
        );

        Debug.Log(logMessage); // ยังคงแสดงผลใน Console

        if (writer != null)
        {
            writer.WriteLine(logMessage); // เขียนข้อมูลลงไฟล์
            writer.Flush(); // บังคับเขียนข้อมูลลงไฟล์ทันที
        }

        if (touchInfoText != null)
        {
            touchInfoText.text = logMessage;
        }
    }

    void OnDestroy()
    {
        if (writer != null)
        {
            writer.Close();
        }
    }

    //การกลับไป Scene แรก
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneObject(sceneName));
    }

    public IEnumerator LoadSceneObject(string sceneName)
    {
        AsyncOperation async = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        async.allowSceneActivation = false;

        // Loop เพื่อตรวจสอบว่าโหลด Object เสร็จหรือยัง
        while (!async.isDone)
        {
            // ทำการคำนวณ progress
            float progress = Mathf.Clamp01(async.progress / 0.9f);
            Debug.Log("Loading progress: " + (progress * 100).ToString("n0") + "%");

            // Loading completed
            if (progress == 1f)
            {
                async.allowSceneActivation = true;
            }
            yield return null;
        }
    }
}