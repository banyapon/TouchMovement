using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class FinalTimeRealtimeDeceleration : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float deceleration = 2f;
    private Vector2 touchStartPosition;
    private Vector2 currentTouchPosition;
    private Vector3 velocity = Vector3.zero;
    private bool isMoving = false;

    public Text touchInfoText;

    private StreamWriter writer;
    private string formattedTime;
    DateTime now;

    private List<Vector2> touchPositions = new List<Vector2>();

    private bool isRotating = false;

    void Start()
    {
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

        if (Input.touchCount > 0 && !isRotating)
        {
            Touch touch = Input.GetTouch(0);
            LogTouchData(touch);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPosition = touch.position;
                    isMoving = false;
                    velocity = Vector3.zero;
                    break;

                case TouchPhase.Moved:
                    currentTouchPosition = touch.position;
                    Vector2 swipeDirection = currentTouchPosition - touchStartPosition;

                    if (Mathf.Abs(swipeDirection.y) > Mathf.Abs(swipeDirection.x))
                    {
                        float moveAmount = Mathf.Abs(swipeDirection.y) * moveSpeed * Time.deltaTime;

                        if (swipeDirection.y < 0) // ตวัดลง -> เดินหน้า
                        {
                            velocity = transform.forward * moveAmount;
                            transform.Translate(velocity * Time.deltaTime);
                        }
                        else if (swipeDirection.y > 0) // ตวัดขึ้น -> ถอยหลัง
                        {
                            velocity = -transform.forward * moveAmount;
                            transform.Translate(velocity * Time.deltaTime);
                        }
                    }
                    break;

                case TouchPhase.Ended:
                    isMoving = true;
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
        }
        else
        {
            isRotating = false;
        }

        if (isMoving)
        {
            ApplyDeceleration();
        }
    }

    private void ApplyDeceleration()
    {
        velocity = Vector3.MoveTowards(velocity, Vector3.zero, deceleration * Time.deltaTime);
        transform.Translate(velocity * Time.deltaTime);

        if (velocity.magnitude < 0.01f)
        {
            velocity = Vector3.zero;
            isMoving = false;
        }
    }

    private void LogTouchData(Touch touch)
    {
        touchPositions.Add(touch.position);

        string logMessage = string.Format(
            "{0},{1},{2},{3},{4},{5},{6}",
            "dogpaddle-new-realtime-with-innertia", // Fixed type
            touch.fingerId,
            touch.position,
            touch.deltaPosition,
            touch.phase,
            touch.tapCount,
            formattedTime
        );

        if (writer != null)
        {
            if (touch.phase == TouchPhase.Began) // Add header only on first touch
            {
                //writer.WriteLine("type,fingerId,touchPosition,deltaPosition,touchPhase,tapCount,time");
            }

            writer.WriteLine(logMessage);
            writer.Flush(); // Ensure immediate write to file
        }

        if (touchInfoText != null)
        {
            touchInfoText.text = logMessage;
        }

        Debug.Log(logMessage);

        if (touch.phase == TouchPhase.Ended)
        {
            touchPositions.Clear();
        }
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
