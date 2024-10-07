using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public GameObject[] group;
    public GameObject cameraMock;

    void Start()
    {
        cameraMock.SetActive(true);
        // ปิดการแสดงผลของ GameObjects ทั้งหมดในตอนเริ่มต้น
        foreach (GameObject obj in group)
        {
            obj.SetActive(false);
        }
    }

    public void selectGroup(int index)
    {
        cameraMock.SetActive(false);
        // ตรวจสอบว่า index อยู่ในช่วงที่ถูกต้อง
        if (index >= 0 && index < group.Length)
        {
            // ปิดการแสดงผลของ GameObjects ทั้งหมดก่อน
            foreach (GameObject obj in group)
            {
                obj.SetActive(false);
            }

            // เปิดการแสดงผลของ GameObject ที่ตำแหน่ง index
            group[index].SetActive(true);
        }
        else
        {
            Debug.LogError("Invalid index for selectGroup: " + index);
        }
    }
}