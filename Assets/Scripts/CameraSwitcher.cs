using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public Camera thirdPersonCamera;
    public Camera firstPersonCamera;

    void Start()
    {
        // ตั้งค่ากล้องเริ่มต้น (เช่น Third Person)
        thirdPersonCamera.enabled = true;
        firstPersonCamera.enabled = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            SwitchCamera();
        }
    }

    public void SwitchCamera()
    {
        // สลับการใช้งานกล้อง
        thirdPersonCamera.enabled = !thirdPersonCamera.enabled;
        firstPersonCamera.enabled = !firstPersonCamera.enabled;
    }
}
