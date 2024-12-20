using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class DragNGo : MonoBehaviour
{
    public Transform player;
    public Camera vrCamera;
    public LayerMask raycastLayers;
    public float maxDistance = 50f;
    public float moveSpeed = 5f;
    public float rotationSpeed = 5f;

    private Vector3 startTouchPosition;
    private Vector3 raycastTarget;
    private bool isDragging = false;
    private bool isRotating = false;

    private float screenHeight;
    public Text touchInfoText;

    private StreamWriter writer;
    private string formattedTime;
    private DateTime now;

    private List<Vector2> touchPositions = new List<Vector2>();

    void Start()
    {
        screenHeight = Screen.height;
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

        if (Input.touchCount == 1 && !isRotating)
        {
            HandleTouchInput();
        }
        else if (Input.touchCount == 2)
        {
            HandleRotationInput();
        }
        else
        {
            isDragging = false;
            isRotating = false;
        }

        if (isDragging)
        {
            MovePlayer();
        }
    }

    private void LogTouchData(Touch touch)
    {
        touchPositions.Add(touch.position);

        string logMessage = string.Format(
            "{0},{1},{2},{3},{4},{5},{6}",
            "dragngo", // Fixed type
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

    void HandleTouchInput()
    {
        Touch touch = Input.GetTouch(0);
        LogTouchData(touch); // บันทึกข้อมูลการสัมผัส

        if (touch.phase == TouchPhase.Began)
        {
            RaycastHit hit;
            Ray ray = vrCamera.ScreenPointToRay(touch.position);

            if (Physics.Raycast(ray, out hit, maxDistance, raycastLayers))
            {
                raycastTarget = hit.point;
            }
            else
            {
                raycastTarget = vrCamera.transform.position + vrCamera.transform.forward * maxDistance;
            }

            startTouchPosition = touch.position;
            isDragging = true;
        }
        else if (touch.phase == TouchPhase.Ended)
        {
            isDragging = false;
        }
    }

    void HandleRotationInput()
    {
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
            {
                isRotating = true;

                float currentAngle = Vector2.SignedAngle(touch0.position - touch1.position, Vector2.right);
                float previousAngle = Vector2.SignedAngle(
                    (touch0.position - touch0.deltaPosition) - (touch1.position - touch1.deltaPosition),
                    Vector2.right
                );

                float rotateAmount = currentAngle - previousAngle;

                player.Rotate(0f, rotateAmount * rotationSpeed * Time.deltaTime, 0f, Space.Self);
            }
        }
        else
        {
            isRotating = false;
        }
    }

    void MovePlayer()
    {
        if (isDragging)
        {
            Touch touch = Input.GetTouch(0);
            float dragDistance = startTouchPosition.y - touch.position.y;

            float touchOffset = screenHeight - startTouchPosition.y;

            float VRd = Vector3.Distance(player.position, raycastTarget);

            float sensitivity = Mathf.Clamp(VRd / Mathf.Max(touchOffset, 1f), 0f, 1f);

            Vector3 direction = (raycastTarget - player.position).normalized;
            Vector3 targetPosition;

            if (dragDistance > 0)
            {
                float moveDistance = Mathf.Clamp(dragDistance * sensitivity, 0f, VRd);
                targetPosition = player.position + direction * moveDistance;
            }
            else
            {
                float moveDistance = Mathf.Clamp(-dragDistance * sensitivity, 0f, VRd);
                targetPosition = player.position - direction * moveDistance;
            }

            targetPosition.y = player.position.y;

            player.position = Vector3.MoveTowards(player.position, targetPosition, moveSpeed * Time.deltaTime);
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
