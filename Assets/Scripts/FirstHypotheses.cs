using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class FirstHypotheses : MonoBehaviour
{
    public float baseSpeed = 1f;
    public float speedMultiplier = 0.5f;
    public float deceleration = 10f;
    public float maxSpeed = 10f;
    public float rotationSpeed = 100f; // ความเร็วการหมุน

    private Vector2 touchStartPosition;
    private Vector2 touchEndPosition;
    private float currentSpeed = 0f;
    private Vector3 velocity = Vector3.zero;
    private bool isMoving = false;
    private bool isRotating = false;

    private List<Vector2> touchPositions = new List<Vector2>();
    private Vector2 initialTouchPosition;

    public Text touchInfoText;

    private StreamWriter writer; // ตัวแปรสำหรับเขียนข้อมูลลงไฟล์
    private string formattedTime; // เก็บเวลาที่จัดรูปแบบ
    private DateTime now; // เวลาปัจจุบัน

    void Start()
    {
        writer = new StreamWriter("data.log", true); // เปิดไฟล์เขียน log
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LoadScene("Main");
        }

        now = DateTime.Now;
        formattedTime = now.ToString("dd/MM/yyyy HH:mm:ss:fff");

        // ตรวจจับการสัมผัส
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (Input.touchCount == 2) // หมุนตัวละครเมื่อแตะสองนิ้วค้าง
            {
                HandleRotation();
            }
            else
            {
                LogTouchData(touch);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        touchStartPosition = touch.position;
                        break;

                    case TouchPhase.Moved:
                        touchEndPosition = touch.position;
                        break;

                    case TouchPhase.Ended:
                        touchEndPosition = touch.position;
                        HandleSwipe();
                        break;
                }
            }
        }

        if (isMoving)
        {
            Move();
        }
    }

    private void HandleSwipe()
    {
        Vector2 swipeDirection = touchEndPosition - touchStartPosition;
        float swipeDistance = swipeDirection.magnitude;

        if (swipeDistance < 10f) return;

        // ตรวจสอบการลากขึ้นหรือลง
        if (swipeDirection.y > 0) // ลากขึ้น -> ถอยหลัง
        {
            float screenDiagonal = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
            float moveDistance = (swipeDistance / screenDiagonal) * 10f;

            currentSpeed += baseSpeed + (moveDistance * speedMultiplier);
            currentSpeed = Mathf.Clamp(currentSpeed, 0, maxSpeed);

            velocity = -transform.forward * currentSpeed; // ถอยหลัง
        }
        else if (swipeDirection.y < 0) // ลากลง -> เดินหน้า
        {
            float screenDiagonal = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
            float moveDistance = (swipeDistance / screenDiagonal) * 10f;

            currentSpeed += baseSpeed + (moveDistance * speedMultiplier);
            currentSpeed = Mathf.Clamp(currentSpeed, 0, maxSpeed);

            velocity = transform.forward * currentSpeed; // เดินหน้า
        }

        isMoving = true;
    }

    private void HandleRotation()
    {
        if (Input.touchCount == 2) // ใช้ 2 นิ้วหมุน
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            Vector2 currentDelta = touch0.position - touch1.position;
            Vector2 previousDelta = (touch0.position + touch0.deltaPosition) - (touch1.position + touch1.deltaPosition);

            float angleDelta = Vector2.SignedAngle(previousDelta, currentDelta);

            // หมุนตัวละครในแกน Y
            transform.Rotate(0f, angleDelta * rotationSpeed * Time.deltaTime, 0f);
            isRotating = true;
        }
        else
        {
            isRotating = false;
        }
    }

    private void Move()
    {
        Vector3 lockedVelocity = new Vector3(velocity.x, 0, velocity.z); // ล็อกแกน Y
        transform.position += lockedVelocity * Time.deltaTime;

        velocity = Vector3.MoveTowards(velocity, Vector3.zero, deceleration * Time.deltaTime);

        if (velocity.magnitude < 0.01f)
        {
            velocity = Vector3.zero;
            currentSpeed = 0f;
            isMoving = false;
        }
    }

    private void LogTouchData(Touch touch)
    {
        touchPositions.Add(touch.position);

        string logMessage = string.Format(
            "{0},{1},{2},{3},{4},{5},{6}",
            "dogpaddle-old-with-inertia", // Fixed type
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
