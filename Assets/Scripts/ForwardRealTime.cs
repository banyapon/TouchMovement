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

    private Vector2 touchStartPosition;
    private Vector2 currentTouchPosition;

    //Improve Algorithm
    private List<Vector2> touchPositions = new List<Vector2>(); // เก็บตำแหน่ง touch ในแต่ละเฟรม

    private bool isRotating = false; // ตัวแปรสถานะสำหรับการหมุน
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

        if (Input.touchCount > 0 && !isRotating)
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
                    currentTouchPosition = touch.position;
                    Vector2 swipeDirection = currentTouchPosition - touchStartPosition;

                    // ตรวจสอบว่าลากนิ้วลง
                    if (swipeDirection.y < 0 && Mathf.Abs(swipeDirection.y) > Mathf.Abs(swipeDirection.x))
                    {
                        // เคลื่อนที่ไปข้างหน้าแบบ Realtime ตามระยะทางที่ลากนิ้ว
                        float moveAmount = -swipeDirection.y * moveSpeed * Time.deltaTime;
                        transform.Translate(Vector3.forward * moveAmount);
                    }
                    // ตรวจสอบว่าลากนิ้วขึ้น
                    else if (swipeDirection.y > 0 && Mathf.Abs(swipeDirection.y) > Mathf.Abs(swipeDirection.x))
                    {
                        // เคลื่อนที่ไปข้างหลังแบบ Realtime ตามระยะทางที่ลากนิ้ว
                        float moveAmount = swipeDirection.y * moveSpeed * Time.deltaTime;
                        transform.Translate(Vector3.back * moveAmount); // หรือ transform.Translate(-Vector3.forward * moveAmount);
                    }
                    break;

                case TouchPhase.Ended:
                    // หยุดการเคลื่อนที่เมื่อปล่อยนิ้ว
                    break;
            }
        }

        // --- Rotation-in-place (Touch Input) ---
        if (Input.touchCount == 2)
        {
            isRotating = true; // ตั้งค่าเป็นกำลังหมุน

            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            // คำนวณมุมระหว่างนิ้วสองนิ้วในเฟรมปัจจุบัน
            float currentAngle = Vector2.SignedAngle(touch0.position - touch1.position, Vector2.right);

            // คำนวณมุมระหว่างนิ้วสองนิ้วในเฟรมก่อนหน้า
            float previousAngle = Vector2.SignedAngle(
                (touch0.position - touch0.deltaPosition) - (touch1.position - touch1.deltaPosition),
                Vector2.right
            );

            // หาผลต่างของมุมเพื่อใช้ในการหมุน
            float rotateAmount = currentAngle - previousAngle;

            // หมุนตัวละคร
            transform.Rotate(0f, rotateAmount, 0f);

            // จำกัดการหมุน 90 องศา โดยอ้างอิงจากตำแหน่งเริ่มต้นของ touch0
            Vector3 currentRotation = transform.eulerAngles;
            if (touch0.position.y > Screen.width / 2)
            {
                currentRotation.y = Mathf.Clamp(currentRotation.y, -180f, 0f);
            }

            transform.eulerAngles = currentRotation;
        }
        else
        {
            isRotating = false; // เมื่อไม่ได้หมุนแล้ว
        }
    }

    //เก็บ LogTouch
    private void LogTouchData(Touch touch)
    {
        touchPositions.Add(touch.position);

        string logMessage = string.Format(
            "{0},{1},{2},{3},{4},{5},{6}",
            "dogpaddle-new-realtime", // Fixed type
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

