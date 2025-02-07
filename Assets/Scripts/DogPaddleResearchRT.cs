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
    private Vector2 touchStartPosition;
    private Vector2 currentTouchPosition;
    private float touchStartTime;
    private float touchEndTime;

    private string logFilePath;
    private string logFilePathTXT;
    private Vector3 startPosition;
    private Vector3 stopPosition;

    private StreamWriter csvWriter;
    //private StreamWriter txtWriter;
    private string formattedTime;
    DateTime now;

    public Text touchInfoText;
    private bool isRotating = false;

    void Start()
    {
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
                    touchStartTime = Time.time;
                    startPosition = transform.position;

                    LogDataCSV("Began", touchStartPosition, startPosition);
                    //LogDataTXT("Began", 0, 0);
                    break;

                case TouchPhase.Moved:
                    currentTouchPosition = touch.position;
                    Vector2 swipeDirection = currentTouchPosition - touchStartPosition;
                    float moveAmount = Mathf.Abs(swipeDirection.y) * moveSpeed * Time.deltaTime;

                    if (swipeDirection.y < 0)
                    {
                        transform.Translate(Vector3.forward * moveAmount);
                    }
                    else if (swipeDirection.y > 0)
                    {
                        transform.Translate(Vector3.back * moveAmount);
                    }

                    LogDataCSV("Moved", currentTouchPosition, transform.position);
                    break;

                case TouchPhase.Ended:
                    touchEndTime = Time.time;
                    stopPosition = transform.position;

                    float distanceTraveled = Vector3.Distance(startPosition, stopPosition);
                    float duration = touchEndTime - touchStartTime;
                    float speed = (duration > 0) ? distanceTraveled / duration : 0;

                    LogDataCSV("Ended", touch.position, stopPosition);
                    //LogDataTXT("Ended", distanceTraveled, duration);
                    break;
            }
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
        //txtWriter.Close();
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
