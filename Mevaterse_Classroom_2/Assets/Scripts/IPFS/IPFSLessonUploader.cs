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

    public string bearerToken = "INSERISCI_IL_TUO_BEARER_TOKEN";

    private const string apiUrlGroups = "https://api.pinata.cloud/v3/groups/public";
    private const string apiUrlFiles = "https://uploads.pinata.cloud/v3/files";

    void Awake()
    {
        panelUpload = GameObject.Find("Panel_FileManager_Upload");
        buttonUploadFolder = GameObject.Find("ButtonUploadFolder").GetComponent<Button>();
        buttonUploadSlide = GameObject.Find("ButtonUploadSlide").GetComponent<Button>();
        uiController = FindObjectOfType<LessonStorageUIController>();

        panelUpload.SetActive(false);

        buttonUploadFolder.onClick.AddListener(UploadNewLesson);
        buttonUploadSlide.onClick.AddListener(UploadSingleSlide);
    }

    void Start() => panelUpload.SetActive(false);

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
        request.SetRequestHeader("Authorization", "Bearer " + bearerToken);
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
        int index = 1;
        foreach (var path in filePaths)
        {
            if (!File.Exists(path)) continue;

            var ext = Path.GetExtension(path).ToLower();
            if (ext != ".jpg" && ext != ".jpeg") continue;

            var raw = Path.GetFileNameWithoutExtension(path);
            var safe = System.Text.RegularExpressions.Regex.Replace(raw, @"[^a-zA-Z0-9_\-]", "_");
            var fname = $"{lessonName}_{index++}_{safe}{ext}";

            yield return StartCoroutine(UploadSingleFile(path, fname, groupId));
        }

        Debug.Log("Tutti i file sono stati caricati.");

        if (uiController != null)
        {
            uiController.TogglePanel(panelUpload); // Chiude il pannello

            // Nasconde il cursore e restituisce il controllo
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

    }

    IEnumerator UploadSingleFile(string filePath, string fileName, string groupId)
    {
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
        request.SetRequestHeader("Authorization", $"Bearer {bearerToken}");
        request.SetRequestHeader("Content-Type", $"multipart/form-data; boundary={boundary}");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Errore caricamento {fileName}: {request.error}");
            Debug.LogError("Risposta server: " + request.downloadHandler.text);
        }
        else
        {
            Debug.Log($"Slide caricata: {fileName}");
        }
    }

    /* ----------  Stub futura slide singola  ---------- */
    void UploadSingleSlide() => Debug.Log("Funzione caricamento singola slide (da implementare).");
}
