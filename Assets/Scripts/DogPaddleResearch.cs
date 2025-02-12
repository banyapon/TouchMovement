using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DogPaddleResearch : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float moveDuration = 0.5f;
    public float inertiaAcc = 0.6f; // ค่าความเฉื่อย

    private Vector2 touchStartPosition;
    private Vector2 touchEndPosition;
    private float touchStartTime;
    private float touchEndTime;

    private string logFilePath;
    private Vector3 startPosition;
    private Vector3 stopPosition;
    private Vector3 prevAvatarPosition;
    private Vector3 velocity;
    private bool isMoving = false;

    private string formattedTime;
    DateTime now;
    public Text touchInfoText;

    private bool isRotating = false; // ตัวแปรสถานะสำหรับการหมุน

    void Start()
    {
        logFilePath = Path.Combine(Application.dataPath, "data.csv");

        if (!File.Exists(logFilePath))
        {
            using (StreamWriter fileWriter = new StreamWriter(logFilePath, true))
            {
                fileWriter.WriteLine("Timestamp,EventType,TouchPos,AvatarPos");
            }
        }

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

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            LogTouchData(touch);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPosition = touch.position;
                    touchStartTime = Time.time;
                    startPosition = transform.position;

                    // หยุด Inertia เมื่อมีการสัมผัสใหม่
                    velocity = Vector3.zero;
                    isMoving = true;

                    LogDataCSV("Began", touchStartPosition, startPosition);
                    break;

                case TouchPhase.Ended:
                    touchEndPosition = touch.position;
                    touchEndTime = Time.time;
                    stopPosition = transform.position;

                    // คำนวณตำแหน่งใหม่ของ Avatar ตามแรงเฉื่อย
                    float deltaTime = touchEndTime - touchStartTime;
                    velocity = inertiaAcc * (stopPosition - prevAvatarPosition) / deltaTime;

                    isMoving = true;
                    prevAvatarPosition = stopPosition;

                    ComputeAvatarMovement();
                    LogDataCSV("Ended", touchEndPosition, stopPosition);
                    break;
            }
        }

        if (isMoving)
        {
            ApplyInertia();
        }

        // --- Rotation-in-place (Touch Input) ---
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

    void ComputeAvatarMovement()
    {
        float swipeDistance = Vector2.Distance(touchStartPosition, touchEndPosition);
        float screenDiagonal = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
        float moveDistance = (swipeDistance / screenDiagonal) * 10f;

        int dragDirection = (touchEndPosition.y < touchStartPosition.y) ? 1 : -1;

        Vector3 movementDirection = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        Vector3 targetPosition = transform.position + movementDirection * moveDistance * dragDirection;

        Debug.Log($"Moving from {transform.position} to {targetPosition}");
        StartCoroutine(MoveCoroutine(targetPosition));
    }

    IEnumerator MoveCoroutine(Vector3 targetPosition)
    {
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;

        while (elapsedTime < moveDuration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
        LogDataCSV("Moved", touchEndPosition, targetPosition);
    }

    private void ApplyInertia()
    {
        if (velocity.magnitude > 0.01f)
        {
            transform.position += velocity * Time.deltaTime;
            velocity *= 0.95f;
        }
        else
        {
            isMoving = false;
        }
    }

    private void LogTouchData(Touch touch)
    {
        string logMessage = $"{formattedTime},{touch.phase},{touch.position.x},{touch.position.y},{transform.position.x:F2},{transform.position.y:F2},{transform.position.z:F2}";

        using (StreamWriter fileWriter = new StreamWriter(logFilePath, true))
        {
            fileWriter.WriteLine(logMessage);
        }

        if (touchInfoText != null)
        {
            touchInfoText.text = logMessage;
        }

        Debug.Log(logMessage);
    }

    private void LogDataCSV(string eventType, Vector2 touchPos, Vector3 avatarPos)
    {
        string logEntry = $"{formattedTime},{eventType},{touchPos.x:F2},{touchPos.y:F2},{avatarPos.x:F2},{avatarPos.y:F2},{avatarPos.z:F2}";

        using (StreamWriter fileWriter = new StreamWriter(logFilePath, true))
        {
            fileWriter.WriteLine(logEntry);
        }

        Debug.Log($"Logged: {logEntry}");
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
