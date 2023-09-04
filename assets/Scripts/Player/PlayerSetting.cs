using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSetting : MonoBehaviour
{
    private bool isCursorLocked;
    [SerializeField] Canvas setting;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {

            if (setting.isActiveAndEnabled)
            {
                ClosePanel();
            }
            else
            {
                OpenPanel();
            }
        }
        if (isCursorLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void OpenPanel()
    {
        setting.gameObject.SetActive(true);
        isCursorLocked = false;
    }

    public void ClosePanel()
    {
        setting.gameObject.SetActive(false);
        isCursorLocked = true;
    }
}
