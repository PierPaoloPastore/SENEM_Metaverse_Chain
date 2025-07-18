using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using ChainSafe.Gaming.Evm.Contracts.Custom;
using SFB;

public class IPFSLessonUploader : MonoBehaviour
{
    public GameObject panelUpload;
    public Button buttonUploadFolder;
    public Button buttonUploadSlide;

    private LessonStorageUIController uiController;

    void Start()
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
        panelUpload.SetActive(true);
    }

    // ------------ UPLOAD DI UNA NUOVA LEZIONE (CARTELLA) ------------

    void UploadNewLesson()
    {
        var paths = StandaloneFileBrowser.OpenFolderPanel("Seleziona la cartella della lezione", "", false);
        if (paths.Length == 0 || string.IsNullOrEmpty(paths[0])) return;

        string folderPath = paths[0];
        string lessonName = Path.GetFileName(folderPath);
        StartCoroutine(CreaGruppoECaricaTutteLeSlide(folderPath, lessonName));
    }

    IEnumerator CreaGruppoECaricaTutteLeSlide(string folderPath, string lessonName)
    {
        // Crea gruppo su Pinata
        var jsonBody = "{\"name\":\"" + lessonName + "\"}";
        using var request = new UnityWebRequest("https://api.pinata.cloud/v3/groups/public", "POST")
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
            UIReferenceManager.Instance.notificationUI?.Show("Errore creazione gruppo!");
            yield break;
        }

        var groupId = JsonUtility.FromJson<CreatedGroupResponse>(request.downloadHandler.text).data.id;
        Debug.Log("Gruppo creato: " + groupId);

        string[] filePaths = Directory.GetFiles(folderPath);
        List<string> validFiles = new List<string>();
        foreach (var path in filePaths)
        {
            var ext = Path.GetExtension(path).ToLower();
            if (ext == ".jpg" || ext == ".jpeg") validFiles.Add(path);
        }

        for (int i = 0; i < validFiles.Count; i++)
        {
            string filePath = validFiles[i];
            string fileName = GeneraNomeFile(lessonName, i + 1, filePath);

            // Notifica progresso
            UIReferenceManager.Instance.notificationUI?.Show($"Caricamento slide {i + 1}/{validFiles.Count}...");
            yield return StartCoroutine(UploadFile(filePath, fileName, groupId));
        }
        // ---------- REGISTRAZIONE SU BLOCKCHAIN ----------
        var handler = FindObjectOfType<LessonRegistryHandler>();
        if (handler != null && handler.IsReady())
        {
            bool done = false;
            bool success = false;

            var task = handler.RegisterLesson(lessonName, groupId)
                .ContinueWith(t =>
                {
                    success = t.Result;
                    done = true;
                });

            while (!done) yield return null;

            if (success)
                UIReferenceManager.Instance.notificationUI?.Show("Registrata anche su Blockchain!");
            else
                UIReferenceManager.Instance.notificationUI?.Show("Lezione già su Blockchain.");
        }
        else
        {
            Debug.LogWarning("LessonRegistryHandler non pronto o assente.");
        }
        // ---------- INTERAZIONE UI----------
        UIReferenceManager.Instance.notificationUI?.Show("Upload completato!");
        uiController?.TogglePanel(panelUpload);
        CursorManager.Instance.HideCursor();
    }

    // ------------ UPLOAD DI UNA SLIDE SINGOLA (GRUPPO ESISTENTE) ------------

    void UploadSingleSlide()
    {
        StartCoroutine(SelezionaGruppoECaricaSlide());
    }

    IEnumerator SelezionaGruppoECaricaSlide()
    {
        // Chiudi subito il pannello di upload
        panelUpload.SetActive(false);

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
            UIReferenceManager.Instance.notificationUI?.Show("Nessun gruppo trovato.");
            yield break;
        }

        // Mostra UI gruppi e aspetta la selezione
        Group selectedGroup = null;
        bool gruppoSelezionato = false;

        groupListUI.gameObject.SetActive(true);
        groupListUI.ShowGroups(gruppi, GroupListUI.GroupSelectionMode.Upload);
        groupListUI.OnGroupSelected = (Group g) =>
        {
            selectedGroup = g;
            gruppoSelezionato = true;
        };

        while (!gruppoSelezionato) yield return null;

        // Dopo selezione gruppo, scegli il file
        var paths = StandaloneFileBrowser.OpenFilePanel("Seleziona una slide", "", "jpg", false);
        if (paths.Length == 0 || string.IsNullOrEmpty(paths[0])) yield break;
        string filePath = paths[0];

        // Recupera la lista file già presenti per nome progressivo
        List<PinataFile> files = null;
        bool doneFiles = false;
        yield return StartCoroutine(
            PinataAPIManager.Instance.GetFilesInGroup(
                selectedGroup.id,
                (f) => { files = f; doneFiles = true; },
                (err) => { Debug.LogError("Errore lista file: " + err); doneFiles = true; }
            )
        );
        while (!doneFiles) yield return null;

        int nextIndex = (files != null ? files.Count : 0) + 1;
        string fileName = GeneraNomeFile(selectedGroup.name, nextIndex, filePath);

        yield return StartCoroutine(UploadFile(filePath, fileName, selectedGroup.id));
        UIReferenceManager.Instance.notificationUI?.Show("Slide caricata con successo!");
    }

    // ------------ FUNZIONE UNIFICATA PER CARICARE UN FILE ------------

    IEnumerator UploadFile(string filePath, string fileName, string groupId)
    {
        Debug.Log($"Uploading file: {filePath} as {fileName} in group {groupId}");

        byte[] fileBytes = File.ReadAllBytes(filePath);
        string boundary = "----Boundary" + System.DateTime.Now.Ticks.ToString("x");

        using var formBody = new MemoryStream();
        var enc = Encoding.ASCII;
        void W(string s) => formBody.Write(enc.GetBytes(s + "\r\n"), 0, enc.GetByteCount(s + "\r\n"));

        // Multipart body
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

        using var request = new UnityWebRequest("https://uploads.pinata.cloud/v3/files", "POST")
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
            UIReferenceManager.Instance.notificationUI?.Show("Slide caricata con successo!");
        }
        else
        {
            Debug.LogError($"Errore caricamento {fileName}: {request.error}");
            Debug.LogError("Risposta server: " + request.downloadHandler.text);
            UIReferenceManager.Instance.notificationUI?.Show("Errore durante l'upload della slide!");
        }
    }

    // ------------ HELPER PER GENERARE I NOMI FILE ------------

    string GeneraNomeFile(string baseName, int index, string filePath)
    {
        string raw = Path.GetFileNameWithoutExtension(filePath);
        string safe = System.Text.RegularExpressions.Regex.Replace(raw, @"[^a-zA-Z0-9_\-]", "_");
        string ext = Path.GetExtension(filePath).ToLower();
        return $"{baseName}_{index}_{safe}{ext}";
    }
}
