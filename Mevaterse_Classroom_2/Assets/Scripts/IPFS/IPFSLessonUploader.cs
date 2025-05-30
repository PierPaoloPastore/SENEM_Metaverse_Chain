using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using SFB;

public class IPFSLessonUploader : MonoBehaviour
{
    public GameObject panelUpload;
    public Button buttonUploadFolder;
    public Button buttonUploadSlide;

    private LessonStorageUIController uiController;
    private const string apiUrlGroups = "https://api.pinata.cloud/v3/groups/public";
    private const string apiUrlFiles = "https://uploads.pinata.cloud/v3/files";

    void Awake()
    {
        var ui = UIReferenceManager.Instance;

        panelUpload = ui.panelUpload;
        buttonUploadFolder = ui.buttonUploadFolder;
        buttonUploadSlide = ui.buttonUploadSlide;
        uiController = ui.lessonStorageUI;

        panelUpload.SetActive(false);

        buttonUploadFolder.onClick.AddListener(UploadNewLesson);
        buttonUploadSlide.onClick.AddListener(UploadSingleSlide);
    }

    private void OnEnable()
    {
        LessonStorageUIController.OnPanelUploadOpened += HandlePanelOpened;
    }

    private void OnDisable()
    {
        LessonStorageUIController.OnPanelUploadOpened -= HandlePanelOpened;
    }

    private void HandlePanelOpened()
    {
        panelUpload.SetActive(true); // questo è ridondante, ma lo lasciamo per sicurezza
        /*Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;*/
    }



    /* ----------  LOGICA UPLOAD  ---------- */

    void UploadNewLesson()
    {
        var paths = StandaloneFileBrowser.OpenFolderPanel("Seleziona la cartella della lezione", "", false);
        if (paths.Length == 0 || string.IsNullOrEmpty(paths[0])) return;

        string folderPath = paths[0];
        string lessonName = Path.GetFileName(folderPath);
        StartCoroutine(CreaGruppoEUploada(folderPath, lessonName));
    }

    IEnumerator CreaGruppoEUploada(string folderPath, string lessonName)
    {
        var jsonBody = "{\"name\":\"" + lessonName + "\"}";
        using var request = new UnityWebRequest(apiUrlGroups, "POST")
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("Authorization", "Bearer " + PinataAPIManager.Instance.BearerToken);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Errore creazione gruppo: " + request.error);
            yield break;
        }

        var groupId = JsonUtility.FromJson<CreatedGroupResponse>(request.downloadHandler.text).data.id;
        Debug.Log("Gruppo creato: " + groupId);

        yield return StartCoroutine(CaricaSlides(Directory.GetFiles(folderPath), lessonName, groupId));
    }

    IEnumerator CaricaSlides(string[] filePaths, string lessonName, string groupId)
    {
        var notificationUI = UIReferenceManager.Instance.notificationUI;

        // Filtra solo i file jpg
        List<string> validFiles = new List<string>();
        foreach (var path in filePaths)
        {
            if (File.Exists(path))
            {
                var ext = Path.GetExtension(path).ToLower();
                if (ext == ".jpg" || ext == ".jpeg")
                    validFiles.Add(path);
            }
        }
        int total = validFiles.Count;
        int completed = 0;

        int index = 1;
        foreach (var path in validFiles)
        {
            var raw = Path.GetFileNameWithoutExtension(path);
            var safe = System.Text.RegularExpressions.Regex.Replace(raw, @"[^a-zA-Z0-9_\-]", "_");
            var fname = $"{lessonName}_{index++}_{safe}{Path.GetExtension(path).ToLower()}";

            // Aggiorna la notifica PRIMA di iniziare l’upload di questo file
            if (notificationUI != null)
                notificationUI.Show($"Caricamento slide {completed + 1}/{total}...");

            yield return StartCoroutine(UploadSingleFile(path, fname, groupId));

            completed++;
        }

        if (notificationUI != null)
            notificationUI.Show("Upload completato!");

        // Chiudi il pannello upload dopo l'upload
        if (uiController != null)
            uiController.TogglePanel(panelUpload);

        // Nascondi il cursore
        CursorManager.Instance.HideCursor();

    }


    IEnumerator UploadSingleFile(string filePath, string fileName, string groupId)
    {
        Debug.Log($"ENTRO in UploadSingleFile con: {filePath}, {fileName}, {groupId}");

        byte[] fileBytes = File.ReadAllBytes(filePath);
        string boundary = "----Boundary" + System.DateTime.Now.Ticks.ToString("x");

        // Costruzione manuale del multipart/form-data
        using var formBody = new MemoryStream();
        var enc = Encoding.ASCII;
        void W(string s) => formBody.Write(enc.GetBytes(s + "\r\n"), 0, enc.GetByteCount(s + "\r\n"));

        W($"--{boundary}");
        W($"Content-Disposition: form-data; name=\"file\"; filename=\"{fileName}\"");
        W("Content-Type: image/jpeg");
        W("");
        formBody.Write(fileBytes, 0, fileBytes.Length);
        W("");

        W($"--{boundary}");
        W("Content-Disposition: form-data; name=\"group_id\"");
        W("");
        W(groupId);

        W($"--{boundary}");
        W("Content-Disposition: form-data; name=\"network\"");
        W("");
        W("public");

        W($"--{boundary}");
        W("Content-Disposition: form-data; name=\"keyvalues\"");
        W("");
        W("{}");

        W($"--{boundary}--");

        using var request = new UnityWebRequest(apiUrlFiles, "POST")
        {
            uploadHandler = new UploadHandlerRaw(formBody.ToArray()),
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("Authorization", $"Bearer {PinataAPIManager.Instance.BearerToken}");
        request.SetRequestHeader("Content-Type", $"multipart/form-data; boundary={boundary}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"Slide caricata: {fileName}");

            Debug.Log("Risposta server: " + request.downloadHandler.text);


            // MOSTRA NOTIFICA
            var notificationUI = UIReferenceManager.Instance.notificationUI;
            if (notificationUI != null)
                notificationUI.Show("Slide caricata con successo!");

        }
        else
        {
            Debug.LogError($"Errore caricamento {fileName}: {request.error}");
            Debug.LogError("Risposta server: " + request.downloadHandler.text);

            // (Opzionale: notifica errore)
            var notificationUI = UIReferenceManager.Instance.notificationUI;
            if (notificationUI != null)
                notificationUI.Show("Errore durante l'upload della slide!");
        }

    }


    void UploadSingleSlide()
    {
        var groupListUI = UIReferenceManager.Instance.groupListUI;
        var panelUpload = UIReferenceManager.Instance.panelUpload;

        if (groupListUI == null || panelUpload == null)
        {
            Debug.LogError("UI non trovata!");
            return;
        }

        // Nascondi il pannello upload
        panelUpload.SetActive(false);

        // Mostra la selezione gruppi aggiornata
        StartCoroutine(ShowGroupSelectionForSingleSlide());
    }
    IEnumerator ShowGroupSelectionForSingleSlide()
    {
        var downloader = UIReferenceManager.Instance.ipfsLessonDownloader;
        var groupListUI = UIReferenceManager.Instance.groupListUI;
        bool done = false;
        List<Group> gruppi = null;

        yield return downloader.StartCoroutine(
            PinataAPIManager.Instance.GetGroups(
                (g) => { gruppi = g; done = true; },
                (err) => { Debug.LogError("Errore nel caricare i gruppi: " + err); done = true; }
            )
        );
        while (!done) yield return null;

        if (gruppi == null || gruppi.Count == 0)
        {
            Debug.LogWarning("Nessun gruppo trovato.");
            // (eventualmente riapri il pannello upload qui)
            yield break;
        }

        // ATTIVA il pannello gruppi (fondamentale!)
        groupListUI.gameObject.SetActive(true);

        // Mostra la lista gruppi aggiornata
        groupListUI.ShowGroups(gruppi, GroupListUI.GroupSelectionMode.Upload);


        groupListUI.OnGroupSelected = (Group selectedGroup) =>
        {
            StartCoroutine(UploadSlideWithProgressive(selectedGroup));
            groupListUI.gameObject.SetActive(false);
        };

    }
    IEnumerator UploadSlideWithProgressive(Group selectedGroup)
    {
        var paths = SFB.StandaloneFileBrowser.OpenFilePanel("Seleziona una slide", "", "jpg", false);
        if (paths.Length == 0 || string.IsNullOrEmpty(paths[0])) yield break;
        string filePath = paths[0];

        // Scarica la lista file già presenti
        List<PinataFile> files = null;
        bool done = false;
        yield return StartCoroutine(
            PinataAPIManager.Instance.GetFilesInGroup(
                selectedGroup.id,
                (f) => { files = f; done = true; },
                (err) => { Debug.LogError("Errore lista file: " + err); done = true; }
            )
        );
        while (!done) yield return null;

        int nextIndex = (files != null ? files.Count : 0) + 1;

        string raw = System.IO.Path.GetFileNameWithoutExtension(filePath);
        string safe = System.Text.RegularExpressions.Regex.Replace(raw, @"[^a-zA-Z0-9_\-]", "_");
        string fname = $"{selectedGroup.name}_{nextIndex}_{safe}{System.IO.Path.GetExtension(filePath).ToLower()}";
        Debug.Log($"Chiamo UploadSingleFile con: {filePath}, {fname}, {selectedGroup.id}");

        yield return StartCoroutine(UploadSingleFile(filePath, fname, selectedGroup.id));

        // Mostra notifica finale
        var notificationUI = UIReferenceManager.Instance.notificationUI;
        if (notificationUI != null)
            notificationUI.Show("Slide caricata con successo!");
    }




}
