using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using SFB;

public class UploadManager : MonoBehaviour
{
    public GameObject panelUpload;
    public Button buttonUploadFolder;
    public Button buttonUploadSlide;

    public string bearerToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySW5mb3JtYXRpb24iOnsiaWQiOiI1Y2QyNTllOC1mZDQ4LTQ0MzktYWY3MC0zYTU3ZmZlYjcxMWYiLCJlbWFpbCI6InBpZXJwaWVsZUBnbWFpbC5jb20iLCJlbWFpbF92ZXJpZmllZCI6dHJ1ZSwicGluX3BvbGljeSI6eyJyZWdpb25zIjpbeyJkZXNpcmVkUmVwbGljYXRpb25Db3VudCI6MSwiaWQiOiJGUkExIn0seyJkZXNpcmVkUmVwbGljYXRpb25Db3VudCI6MSwiaWQiOiJOWUMxIn1dLCJ2ZXJzaW9uIjoxfSwibWZhX2VuYWJsZWQiOmZhbHNlLCJzdGF0dXMiOiJBQ1RJVkUifSwiYXV0aGVudGljYXRpb25UeXBlIjoic2NvcGVkS2V5Iiwic2NvcGVkS2V5S2V5IjoiYmQ1NzRmYzlkNWJkODNjYjVlODAiLCJzY29wZWRLZXlTZWNyZXQiOiIyNjlmM2I2YWIxZjhhMGE2YTcyZjQzMDYzYjQ3YjYwY2UzMGZiMDFmYzUxYjk1NWFlYmVjYzFjYjFhYTlhNzNjIiwiZXhwIjoxNzc0NjE1NzEyfQ.wn_AidOK3c1aB5ZUymn_LTgSWNd3J-av8Md7M0l3fXY"; // Inserisci il tuo Bearer Token

    private string apiUrlGroups = "https://api.pinata.cloud/v3/groups/public";
    private string apiUrlFiles = "https://uploads.pinata.cloud/v3/files";

    private void Awake()
    {
        panelUpload = GameObject.Find("Panel_FileManager_Upload");
        buttonUploadFolder = GameObject.Find("ButtonUploadFolder").GetComponent<Button>();
        buttonUploadSlide = GameObject.Find("ButtonUploadSlide").GetComponent<Button>();

        panelUpload.SetActive(false);

        buttonUploadFolder.onClick.AddListener(UploadNewLesson);
        buttonUploadSlide.onClick.AddListener(UploadSingleSlide);
    }

    private void Start()
    {
        panelUpload.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            panelUpload.SetActive(!panelUpload.activeSelf);
        }
    }

    private void UploadNewLesson()
    {
        var paths = StandaloneFileBrowser.OpenFolderPanel("Seleziona la cartella della lezione", "", false);
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            string folderPath = paths[0];
            string lessonName = Path.GetFileName(folderPath);
            StartCoroutine(CreaGruppoEUploada(folderPath, lessonName));
        }
    }

    private IEnumerator CreaGruppoEUploada(string folderPath, string lessonName)
    {
        string groupId = null;
        var jsonBody = "{\"name\": \"" + lessonName + "\"}";
        var request = new UnityWebRequest(apiUrlGroups, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", "Bearer " + bearerToken);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            CreatedGroupResponse groupResponse = JsonUtility.FromJson<CreatedGroupResponse>(request.downloadHandler.text);
            groupId = groupResponse.data.id;
            Debug.Log("Gruppo creato: " + groupId);

            string[] filePaths = Directory.GetFiles(folderPath);
            StartCoroutine(CaricaSlides(filePaths, lessonName, groupId));
        }
        else
        {
            Debug.LogError("Errore creazione gruppo: " + request.error);
        }
    }

    private IEnumerator CaricaSlides(string[] filePaths, string lessonName, string groupId)
    {
        int index = 1;
        foreach (string path in filePaths)
        {
            if (!File.Exists(path)) continue;

            string ext = Path.GetExtension(path).ToLower();
            if (ext != ".jpg" && ext != ".jpeg") continue;

            string rawName = Path.GetFileNameWithoutExtension(path);
            string safeName = System.Text.RegularExpressions.Regex.Replace(rawName, @"[^a-zA-Z0-9_\\-]", "_");
            string fileName = $"{lessonName}_{index}_{safeName}{ext}";
            index++;

            yield return StartCoroutine(UploadSingleFile(path, fileName, groupId));
        }

        Debug.Log("Tutti i file sono stati caricati.");
    }

    private IEnumerator UploadSingleFile(string filePath, string fileName, string groupId)
    {
        byte[] fileBytes = File.ReadAllBytes(filePath);
        string boundary = "----Boundary" + System.DateTime.Now.Ticks.ToString("x");

        var formBody = new MemoryStream();
        var encoding = Encoding.ASCII;

        void WriteString(string str)
        {
            byte[] bytes = encoding.GetBytes(str + "\r\n");
            formBody.Write(bytes, 0, bytes.Length);
        }

        WriteString($"--{boundary}");
        WriteString($"Content-Disposition: form-data; name=\"file\"; filename=\"{fileName}\"");
        WriteString("Content-Type: image/jpeg");
        WriteString(""); // riga vuota = fine header

        formBody.Write(fileBytes, 0, fileBytes.Length);
        WriteString(""); // riga vuota dopo file

        WriteString($"--{boundary}");
        WriteString("Content-Disposition: form-data; name=\"group_id\"");
        WriteString("");
        WriteString(groupId);

        WriteString($"--{boundary}");
        WriteString("Content-Disposition: form-data; name=\"network\"");
        WriteString("");
        WriteString("public");

        WriteString($"--{boundary}");
        WriteString("Content-Disposition: form-data; name=\"keyvalues\"");
        WriteString("");
        WriteString("{}");

        WriteString($"--{boundary}--");

        byte[] body = formBody.ToArray();

        UnityWebRequest request = new UnityWebRequest(apiUrlFiles, "POST");
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
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

        request.Dispose(); // libera la Native Collection correttamente
    }


    private void UploadSingleSlide()
    {
        Debug.Log("Funzione caricamento singola slide (da implementare).");
    }
}
