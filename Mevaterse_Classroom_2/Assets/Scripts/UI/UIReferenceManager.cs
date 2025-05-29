using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIReferenceManager : MonoBehaviour
{
    public static UIReferenceManager Instance { get; private set; }

    

    // Riferimenti pubblici a elementi UI
    public GameObject panelUpload;
    public GameObject panelDownload;
    public Button buttonUploadFolder;
    public Button buttonUploadSlide;
    public GroupListUI groupListUI;
    public NotificationUI notificationUI;
    public LessonStorageUIController lessonStorageUI;
    [Header("Stato della sessione")]
    public bool isInRoom = false;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Inizializzazione riferimenti
        panelUpload = GameObject.Find("Panel_FileManager_Upload");
        panelDownload = GameObject.Find("Panel_FileManager_Download");
        buttonUploadFolder = GameObject.Find("ButtonUploadFolder")?.GetComponent<Button>();
        buttonUploadSlide = GameObject.Find("ButtonUploadSlide")?.GetComponent<Button>();
        groupListUI = FindObjectOfType<GroupListUI>();
        notificationUI = FindObjectOfType<NotificationUI>();
        lessonStorageUI = FindObjectOfType<LessonStorageUIController>();
    }
}
