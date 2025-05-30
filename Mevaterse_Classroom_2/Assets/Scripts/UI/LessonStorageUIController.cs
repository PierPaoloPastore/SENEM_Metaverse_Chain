using System;
using System.Collections.Generic;
using UnityEngine;

public class LessonStorageUIController : MonoBehaviour
{
    //Eventi per apertura ui
    public static event Action OnPanelDownloadOpened;
    public static event Action OnPanelUploadOpened;


    public GameObject panelUpload;
    public GameObject panelDownload;

    private readonly List<GameObject> uiPanels = new List<GameObject>();

  


    void Awake()
    {
        var ui = UIReferenceManager.Instance;
        panelUpload = ui.panelUpload;
        panelDownload = ui.panelDownload;

        if (panelUpload != null) uiPanels.Add(panelUpload);
        if (panelDownload != null) uiPanels.Add(panelDownload);
    }



    void Update()
    {

        if (!UIReferenceManager.Instance.isInRoom)
            return; // Blocca tutto finché non entra nella stanza

        if (Input.GetKeyDown(KeyCode.U))
        {
            TogglePanel(panelUpload);
            if (panelUpload != null && panelUpload.activeSelf)
                OnPanelUploadOpened?.Invoke();
        }

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

        CursorManager.Instance.HideCursor();

    }


    public void TogglePanel(GameObject panel)
    {
        if (panel == null) return;

        bool isActive = panel.activeSelf;

        // Nasconde tutti gli altri
        foreach (var ui in uiPanels)
            ui.SetActive(false);

        panel.SetActive(!isActive);
       
        //Gestione del cursore
        if (!isActive)
            CursorManager.Instance.ShowCursor();
        else
            CursorManager.Instance.HideCursor();

    }
}
