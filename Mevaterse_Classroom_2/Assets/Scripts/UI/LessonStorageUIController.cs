using System;
using System.Collections.Generic;
using UnityEngine;

public class LessonStorageUIController : MonoBehaviour
{
    public static event Action OnPanelDownloadOpened;

    private GameObject panelUpload;
    private GameObject panelDownload;

    private readonly List<GameObject> uiPanels = new List<GameObject>();

    void Awake()
    {
        panelUpload = GameObject.Find("Panel_FileManager_Upload");
        panelDownload = GameObject.Find("Panel_FileManager_Download");

        if (panelUpload != null) uiPanels.Add(panelUpload);
        if (panelDownload != null) uiPanels.Add(panelDownload);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
            TogglePanel(panelUpload);

        if (Input.GetKeyDown(KeyCode.F))
        {
            TogglePanel(panelDownload);
            if (panelDownload != null && panelDownload.activeSelf)
                OnPanelDownloadOpened?.Invoke();
        }
    }


    public void ClosePanel(GameObject panel)
    {
        if (panel == null || !panel.activeSelf) return;

        panel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    public void TogglePanel(GameObject panel)
    {
        if (panel == null) return;

        bool isActive = panel.activeSelf;

        // Nasconde tutti gli altri
        foreach (var ui in uiPanels)
            ui.SetActive(false);

        panel.SetActive(!isActive);

        Cursor.lockState = !isActive ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = !isActive;
    }
}
