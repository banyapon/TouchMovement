using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Log
using System;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class ForwardRealTime : MonoBehaviour
{
    public float moveSpeed = 5f; // ปรับความเร็วตามต้องการ
    private Vector2 touchStartPos;

    public Text touchInfoText;

    Rigidbody player;

    Vector2 swipeDirection;
    private Vector2 initialTouchPosition; // ตำแหน่งเริ่มต้นของการแตะ
    private Vector3 initialPlayerPosition; // ตำแหน่งเริ่มต้นของผู้เล่น

    private StreamWriter writer; // ตัวแปรสำหรับเขียนข้อมูลลงไฟล์
    private string formattedTime;
    DateTime now;
    void Start()
    {
        
        player = GetComponent<Rigidbody>();
        // บันทึกตำแหน่งเริ่มต้นของผู้เล่น
        initialPlayerPosition = transform.position;
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

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            LogTouchData(touch);
            Debug.Log("RealTime Touch 0, FingerID=" + touch.fingerId + ",position=" + touch.position);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPos = touch.position;
                    break;

                case TouchPhase.Moved:
                    Vector2 dragDirection = touch.position - touchStartPos;
                    dragDirection.Normalize();

                    player.velocity = new Vector3(-dragDirection.x, 0f, -dragDirection.y) * moveSpeed;

                    touchStartPos = touch.position; // อัปเดตตำแหน่งเริ่มต้นสำหรับเฟรมถัดไป
                    break;

                case TouchPhase.Ended:
                    player.velocity = Vector3.zero; // หยุดการเคลื่อนที่
                    Debug.Log("Ended Position:" + touch.position);
                    break;
            }
        }
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

