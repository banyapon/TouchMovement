using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Log
using System;
using System.IO;
using UnityEngine.UI;
public class ForwardMovement : MonoBehaviour
{
    public float moveSpeed = 5f; // ปรับความเร็วให้เหมาะสม
    public float moveDuration = 0.5f; // ระยะเวลาในการเคลื่อนที่แต่ละครั้ง (ปรับได้ตามต้องการ)

    private Vector2 touchStartPosition;
    private Vector2 touchEndPosition;

    private StreamWriter writer; // ตัวแปรสำหรับเขียนข้อมูลลงไฟล์
    private string formattedTime;
    DateTime now;

    public Text touchInfoText;
    void Start()
    {
        
        
        writer = new StreamWriter("data.log", true); // สร้าง StreamWriter และเปิดไฟล์ในโหมด append
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        now = DateTime.Now;
        formattedTime = now.ToString("dd/MM/yyyy HH:mm:ss:fff");
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
                    touchEndPosition = touch.position;
                    break;

                case TouchPhase.Ended:
                    Vector2 swipeDirection = touchEndPosition - touchStartPosition;
                    if (swipeDirection.y < 0 && Mathf.Abs(swipeDirection.y) > Mathf.Abs(swipeDirection.x))
                    {
                        StartCoroutine(MoveForwardCoroutine());
                    }
                    break;
            }

            //Touch 1
            if (Input.touchCount > 1)
            {
                Touch touch1 = Input.GetTouch(1);
                LogTouchData(touch1);
                Debug.Log("GetTouch 1, FingerID=" + touch1.fingerId + ",position=" + touch1.position);

                switch (touch1.phase)
                {
                    case TouchPhase.Began:
                        touchStartPosition = touch1.position;
                        break;

                    case TouchPhase.Moved:
                        touchEndPosition = touch1.position;
                        break;

                    case TouchPhase.Ended:
                        Vector2 swipeDirection = touchEndPosition - touchStartPosition;
                        if (swipeDirection.y < 0 && Mathf.Abs(swipeDirection.y) > Mathf.Abs(swipeDirection.x))
                        {
                            StartCoroutine(MoveForwardCoroutine());
                        }
                        break;
                }
            }
        }

        /* เอาไว้เช็ค
        //Check Direct In Direct, Stylus
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.type == TouchType.Direct)
            {
                Debug.Log("Direct touch detected!");
            }
            else if (touch.type == TouchType.Indirect)
            {
                Debug.Log("Indirect touch detected!");
            }
            else if (touch.type == TouchType.Stylus)
            {
                Debug.Log("Stylus touch detected!");
            }
        }
        */
    }

    IEnumerator MoveForwardCoroutine()
    {
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = transform.position + transform.forward * moveSpeed;

        while (elapsedTime < moveDuration)
        {
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
            "Tap Count: {4}\n"+
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
}
