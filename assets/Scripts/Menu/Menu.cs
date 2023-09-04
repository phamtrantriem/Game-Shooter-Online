using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{
    public string menuName;
    [HideInInspector] public bool isOpen;
    private void Awake()
    {
        if (gameObject.activeSelf == true)
        {
            isOpen = true;
        } else
        {
            isOpen = false;
        }
    }
    public void Open()
    {
        isOpen = true;
        gameObject.SetActive(true);
    }
    public void Close()
    {
        isOpen = false;
        gameObject.SetActive(false);
    }
}
