using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DogPaddleResearchRT : MonoBehaviour
{
    public float moveSpeed = 0.05f;
    public float inertiaAcc = 0.6f; // ค่าความเฉื่อย
    private Vector2 touchStartPosition;
    private Vector2 currentTouchPosition;
    private Vector2 previousTouchPosition;
    private float touchStartTime;
    private float touchEndTime;

    private string logFilePath;
    private Vector3 startPosition;
    private Vector3 stopPosition;
    private Vector3 prevAvatarPosition;
    private Vector3 velocity;
    private bool isMoving = false;

    private StreamWriter csvWriter;
    private string formattedTime;
    private DateTime now;

    public Text touchInfoText;
    private bool isRotating = false;

    void Start()
    {
        // ตั้งค่าการทำงานของ Frame Rate
        Application.targetFrameRate = 60;

        logFilePath = Path.Combine(Application.dataPath, "data_realtime.csv");

        // ตรวจสอบและสร้างไฟล์หากไม่มี
        if (!File.Exists(logFilePath))
        {
            using (StreamWriter writer = new StreamWriter(logFilePath, false))
            {
                writer.WriteLine("Timestamp,EventType,Touch_X,Touch_Y,Pos_X,Pos_Y,Pos_Z");
            }
        }

        // เปิดใช้งาน StreamWriter ด้วย FileMode.Append
        csvWriter = new StreamWriter(new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
        {
            AutoFlush = true
        };

        // ตั้งค่าตำแหน่งเริ่มต้น
        prevAvatarPosition = transform.position;
        velocity = Vector3.zero;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LoadScene("Main");
        }

        now = DateTime.Now;
        formattedTime = now.ToString("dd/MM/yyyy HH:mm:ss:fff");

        if (Input.touchCount > 0 && !isRotating)
        {
            Touch touch = Input.GetTouch(0);
            LogTouchData(touch);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPosition = touch.position;
                    previousTouchPosition = touchStartPosition;
                    touchStartTime = Time.time;
                    startPosition = transform.position;

                    // หยุด Inertia เมื่อมีการสัมผัสใหม่
                    velocity = Vector3.zero;
                    isMoving = true;

                    LogDataCSV("Began", touchStartPosition, startPosition);
                    break;

                case TouchPhase.Moved:
                    currentTouchPosition = touch.position;
                    Vector2 touchMoveDistance = currentTouchPosition - previousTouchPosition;
                    Vector2 swipeDirection = currentTouchPosition - touchStartPosition;

                    float adjustedSpeed = moveSpeed * (60f / Mathf.Max(30f, 1f / Time.deltaTime)); // ปรับความเร็วให้คงที่
                    float moveAmount = Mathf.Abs(touchMoveDistance.y) * adjustedSpeed;

                    // อัปเดตตำแหน่งตามทิศทางที่ผู้ใช้ลากนิ้ว
                    if (touchMoveDistance.y < 0)
                    {
                        transform.Translate(Vector3.forward * moveAmount);
                    }
                    else if (touchMoveDistance.y > 0)
                    {
                        transform.Translate(Vector3.back * moveAmount);
                    }

                    previousTouchPosition = currentTouchPosition;
                    LogDataCSV("Moved", currentTouchPosition, transform.position);
                    break;

                case TouchPhase.Ended:
                    touchEndTime = Time.time;
                    stopPosition = transform.position;

                    float deltaTime = touchEndTime - touchStartTime;
                    float distanceTraveled = Vector3.Distance(startPosition, stopPosition);
                    float speed = (deltaTime > 0) ? distanceTraveled / deltaTime : 0;

                    // คำนวณแรงเฉื่อย
                    velocity = inertiaAcc * (stopPosition - prevAvatarPosition) / deltaTime;

                    isMoving = true;
                    prevAvatarPosition = stopPosition;

                    LogDataCSV("Ended", touch.position, stopPosition);
                    break;
            }
        }

        if (isMoving)
        {
            ApplyInertia();
        }

        if (Input.touchCount == 2)
        {
            isRotating = true;

            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            float currentAngle = Vector2.SignedAngle(touch0.position - touch1.position, Vector2.right);
            float previousAngle = Vector2.SignedAngle(
                (touch0.position - touch0.deltaPosition) - (touch1.position - touch1.deltaPosition),
                Vector2.right
            );

            float rotateAmount = currentAngle - previousAngle;
            transform.Rotate(0f, rotateAmount, 0f);

            Vector3 currentRotation = transform.eulerAngles;
            if (touch0.position.y > Screen.width / 2)
            {
                currentRotation.y = Mathf.Clamp(currentRotation.y, -180f, 0f);
            }
            transform.eulerAngles = currentRotation;
        }
        else
        {
            isRotating = false;
        }
    }

    private void ApplyInertia()
    {
        if (velocity.magnitude > 0.01f)
        {
            transform.position += velocity * Time.deltaTime;
            velocity *= 0.95f; // ลดความเร็วจากแรงเฉื่อย
        }
        else
        {
            isMoving = false; // หยุดเมื่อความเร็วต่ำมาก
        }
    }

    private void LogTouchData(Touch touch)
    {
        string logMessage = $"{formattedTime},{touch.phase},{touch.position.x},{touch.position.y},{transform.position.x:F2},{transform.position.y:F2},{transform.position.z:F2}";

        csvWriter.WriteLine(logMessage);

        if (touchInfoText != null)
        {
            touchInfoText.text = logMessage;
        }

        Debug.Log(logMessage);
    }

    private void LogDataCSV(string eventType, Vector2 touchPos, Vector3 avatarPos)
    {
        string logEntry = $"{formattedTime},{eventType},{touchPos.x:F2},{touchPos.y:F2},{avatarPos.x:F2},{avatarPos.y:F2},{avatarPos.z:F2}";
        csvWriter.WriteLine(logEntry);
        Debug.Log($"Logged CSV: {logEntry}");
    }

    private void OnApplicationQuit()
    {
        csvWriter.Close();
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
