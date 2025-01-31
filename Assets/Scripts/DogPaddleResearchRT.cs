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

    private string logFilePath = "data.csv";
    private string logFilePathTXT = "data.log";
    private Vector3 startPosition;
    private Vector3 stopPosition;

    private StreamWriter writer;
    private string formattedTime;
    DateTime now;

    public Text touchInfoText;
    private bool isRotating = false;

    void Start()
    {
        writer = new StreamWriter("data.log", true);
        if (!File.Exists(logFilePath))
        {
            File.WriteAllText(logFilePath, "Timestamp,EventType,Pos_X,Pos_Y,Pos_Z,Distance,Duration,Speed\n");
        }

        if (!File.Exists(logFilePathTXT))
        {
            File.WriteAllText(logFilePathTXT, "Timestamp,EventType,Distance,Duration\n");
        }
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

                    LogDataCSV("start", startPosition, 0, 0, 0);
                    LogDataTXT("start", 0, 0);
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

                    LogDataCSV("move", transform.position, swipeDirection.magnitude, Time.deltaTime, moveAmount / Time.deltaTime);
                    break;

                case TouchPhase.Ended:
                    touchEndTime = Time.time;
                    stopPosition = transform.position;

                    float distanceTraveled = Vector3.Distance(startPosition, stopPosition);
                    float duration = touchEndTime - touchStartTime;
                    float speed = (duration > 0) ? distanceTraveled / duration : 0;

                    LogDataCSV("stop", stopPosition, distanceTraveled, duration, speed);
                    LogDataTXT("stop", distanceTraveled, duration);
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
        string logMessage = string.Format(
            "{0},{1},{2},{3},{4},{5},{6}",
            "dogpaddle-updated",
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


    private void LogDataCSV(string eventType, Vector3 position, float distance, float duration, float speed)
    {
        string formattedTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss:fff");


        using (StreamWriter writer = new StreamWriter(logFilePath, true))
        {
            writer.WriteLine($"{formattedTime},{eventType},{position.x:F2},{position.y:F2},{position.z:F2},{distance:F2},{duration:F2},{speed:F2}");
        }

        Debug.Log($"Logged CSV: {formattedTime},{eventType},{position},{distance},{duration},{speed}");
    }

    private void LogDataTXT(string eventType, float distance, float duration)
    {
        string formattedTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss:fff");

        using (StreamWriter writer = new StreamWriter(logFilePathTXT, true))
        {
            writer.WriteLine($"{formattedTime},{eventType},{distance:F2},{duration:F2}");
        }

        Debug.Log($"Logged TXT: {formattedTime},{eventType},{distance},{duration}");
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
