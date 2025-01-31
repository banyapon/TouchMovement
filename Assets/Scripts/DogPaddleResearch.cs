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

    private Vector2 touchStartPosition;
    private Vector2 touchEndPosition;
    private float touchStartTime;
    private float touchEndTime;

    private string logFilePath = "data.csv";
    private Vector3 startPosition;
    private Vector3 stopPosition;


    private StreamWriter writer;
    private string formattedTime;
    DateTime now;

    public Text touchInfoText;

    void Start()
    {
        writer = new StreamWriter("data.log", true);
        if (!File.Exists(logFilePath))
        {
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine("Timestamp,EventType,Pos_X,Pos_Y,Pos_Z,Distance,Duration,Speed");
            }
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
                    LogDataCSV("start", startPosition, 0, 0, 0);
                    break;

                case TouchPhase.Ended:
                    touchEndPosition = touch.position;
                    touchEndTime = Time.time;
                    stopPosition = transform.position;

                    float swipeDistance = (touchEndPosition - touchStartPosition).magnitude;
                    float screenDiagonal = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
                    float moveDistance = (swipeDistance / screenDiagonal) * 10f;

                    float duration = touchEndTime - touchStartTime;
                    float distanceTraveled = Vector3.Distance(startPosition, stopPosition);
                    float speed = distanceTraveled / duration;

                    LogData("stop", stopPosition);
                    LogData("distance", distanceTraveled);
                    LogData("duration", duration);
                    LogData("speed", speed);
                    float speedCSV = (duration > 0) ? distanceTraveled / duration : 0;

                    LogDataCSV("stop", stopPosition, distanceTraveled, duration, speedCSV);


                    if (touchEndPosition.y < touchStartPosition.y)
                    {
                        StartCoroutine(MoveCoroutine(moveDistance, 1));
                    }
                    else if (touchEndPosition.y > touchStartPosition.y)
                    {
                        StartCoroutine(MoveCoroutine(moveDistance, -1));
                    }
                    break;
            }
        }
    }

    IEnumerator MoveCoroutine(float moveDistance, int direction)
    {
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = transform.position + transform.forward * moveDistance * direction;

        while (elapsedTime < moveDuration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPosition;
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

        Debug.Log($"Logged: {formattedTime},{eventType},{position},{distance},{duration},{speed}");
    }


    private void LogData(string type, object value)
    {
        string logMessage = string.Format("{0},{1},{2}", formattedTime, type, value);
        if (writer != null)
        {
            writer.WriteLine(logMessage);
            writer.Flush();
        }
        Debug.Log(logMessage);
    }

    void OnDestroy()
    {
        if (writer != null)
        {
            writer.Close();
        }
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
