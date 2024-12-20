using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Log
using System;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SecondHypotheses : MonoBehaviour
{
    public float baseSpeed = 5f; // ความเร็วคงที่ของการเคลื่อนที่
    public float rotationSpeed = 100f; // ความเร็วการหมุน
    public float maxDistance = 5f; // ระยะทางสูงสุดที่เคลื่อนที่ได้

    private Vector2 touchStartPosition;
    private Vector2 touchEndPosition;
    private float currentSpeed = 0f;
    private Vector3 velocity = Vector3.zero;
    private bool isMoving = false;

    private Vector3 startPosition; // ตำแหน่งเริ่มต้นของการเคลื่อนที่

    private List<Vector2> touchPositions = new List<Vector2>();
    private Vector2 initialTouchPosition;

    private StreamWriter writer; // ตัวแปรสำหรับเขียนข้อมูลลงไฟล์
    private string formattedTime; // เก็บเวลาที่จัดรูปแบบ
    private DateTime now; // เวลาปัจจุบัน

    public Text touchInfoText;

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

        if (Input.touchCount == 2) // แตะสองนิ้วค้างเพื่อหมุน
        {
            HandleRotation();
        }
        else if (Input.touchCount == 1) // แตะนิ้วเดียวเพื่อเคลื่อนที่
        {
            Touch touch = Input.GetTouch(0);
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

        if (isMoving)
        {
            Move();
        }
    }

    private void HandleSwipe()
    {
        // คำนวณทิศทางและระยะการตวัด
        Vector2 swipeDirection = touchEndPosition - touchStartPosition;
        float swipeDistance = swipeDirection.magnitude;

        // หากระยะการตวัดต่ำกว่าค่าที่กำหนด (เช่น 10px) จะไม่ทำงาน
        if (swipeDistance < 10f) return;

        // บันทึกตำแหน่งเริ่มต้นของการเคลื่อนที่
        startPosition = transform.position;

        // คำนวณทิศทางและระยะทางที่ต้องเคลื่อนที่
        if (swipeDirection.y < 0) // ตวัดลง -> เดินหน้า
        {
            velocity = transform.forward * baseSpeed;
        }
        else if (swipeDirection.y > 0) // ตวัดขึ้น -> ถอยหลัง
        {
            velocity = -transform.forward * baseSpeed;
        }

        isMoving = true; // เริ่มการเคลื่อนที่
    }

    private void HandleRotation()
    {
        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

        Vector2 currentDelta = touch0.position - touch1.position;
        Vector2 previousDelta = (touch0.position - touch0.deltaPosition) - (touch1.position - touch1.deltaPosition);

        float angleDelta = Vector2.SignedAngle(previousDelta, currentDelta);

        // หมุนตัวละครในแกน Y
        transform.Rotate(0f, angleDelta * rotationSpeed * Time.deltaTime, 0f);
    }

    private void Move()
    {
        // คำนวณระยะทางที่เคลื่อนที่จากตำแหน่งเริ่มต้น
        float distanceTraveled = Vector3.Distance(startPosition, transform.position);

        // หากระยะทางเกินระยะสูงสุด ให้หยุดการเคลื่อนที่
        if (distanceTraveled >= maxDistance)
        {
            velocity = Vector3.zero;
            isMoving = false;
            return;
        }

        // เคลื่อนที่ในทิศทางที่กำหนด
        transform.position += velocity * Time.deltaTime;
    }

    private void LogTouchData(Touch touch)
    {
        touchPositions.Add(touch.position);

        string logMessage = string.Format(
            "{0},{1},{2},{3},{4},{5},{6}",
            "dogpaddle-old-no-inertia", // Fixed type
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
               // writer.WriteLine("type,fingerId,touchPosition,deltaPosition,touchPhase,tapCount,time");
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

