using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using SFB; // File browser per selezione cartelle e file

public class UploadManager : MonoBehaviour
{
    public GameObject panelUpload;
    public Button buttonUploadFolder;
    public Button buttonUploadSlide;

    private string bearerToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySW5mb3JtYXRpb24iOnsiaWQiOiI1Y2QyNTllOC1mZDQ4LTQ0MzktYWY3MC0zYTU3ZmZlYjcxMWYiLCJlbWFpbCI6InBpZXJwaWVsZUBnbWFpbC5jb20iLCJlbWFpbF92ZXJpZmllZCI6dHJ1ZSwicGluX3BvbGljeSI6eyJyZWdpb25zIjpbeyJkZXNpcmVkUmVwbGljYXRpb25Db3VudCI6MSwiaWQiOiJGUkExIn0seyJkZXNpcmVkUmVwbGljYXRpb25Db3VudCI6MSwiaWQiOiJOWUMxIn1dLCJ2ZXJzaW9uIjoxfSwibWZhX2VuYWJsZWQiOmZhbHNlLCJzdGF0dXMiOiJBQ1RJVkUifSwiYXV0aGVudGljYXRpb25UeXBlIjoic2NvcGVkS2V5Iiwic2NvcGVkS2V5S2V5IjoiYmQ1NzRmYzlkNWJkODNjYjVlODAiLCJzY29wZWRLZXlTZWNyZXQiOiIyNjlmM2I2YWIxZjhhMGE2YTcyZjQzMDYzYjQ3YjYwY2UzMGZiMDFmYzUxYjk1NWFlYmVjYzFjYjFhYTlhNzNjIiwiZXhwIjoxNzc0NjE1NzEyfQ.wn_AidOK3c1aB5ZUymn_LTgSWNd3J-av8Md7M0l3fXY";
    private string apiUrlFiles = "https://api.pinata.cloud/v3/files";
    private string apiUrlGroups = "https://api.pinata.cloud/v3/groups/public";

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
        // Crea un nuovo gruppo su Pinata
        string groupId = null;
        var jsonBody = "{\"name\": \"" + lessonName + "\"}";
        var request = new UnityWebRequest(apiUrlGroups, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", "Bearer " + bearerToken);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string response = request.downloadHandler.text;

            CreatedGroupResponse groupResponse = JsonUtility.FromJson<CreatedGroupResponse>(response);
            groupId = groupResponse.data.id;


            Debug.Log("Gruppo creato: " + groupId);

            string[] filePaths = Directory.GetFiles(folderPath);
            StartCoroutine(CaricaSlideNelGruppo(filePaths, lessonName, groupId));
        }
        else
        {
            Debug.LogError("Errore nella creazione del gruppo: " + request.error);
        }
    }

    private IEnumerator CaricaSlideNelGruppo(string[] filePaths, string lessonName, string groupId)
    {
        int index = 1;

        foreach (string filePath in filePaths)
        {
            if (!File.Exists(filePath)) continue;

            string ext = Path.GetExtension(filePath).ToLower();
            if (ext != ".jpg" && ext != ".jpeg") continue;

            string estensione = Path.GetExtension(filePath);
            string fileNameRaw = Path.GetFileNameWithoutExtension(filePath);

            // Rimuovi caratteri non validi
            string fileNameSanificato = System.Text.RegularExpressions.Regex.Replace(fileNameRaw, @"[^a-zA-Z0-9_\-]", "_");

            // Crea un nome sicuro per l'upload
            string fileName = $"{lessonName}_{index}_{fileNameSanificato}{estensione}";

            index++;

            byte[] fileData = File.ReadAllBytes(filePath);
            WWWForm form = new WWWForm();
            form.AddBinaryData("file", fileData, fileName, "image/jpeg");

            form.AddField("group", groupId);

            UnityWebRequest uploadRequest = UnityWebRequest.Post(apiUrlFiles, form);
            uploadRequest.SetRequestHeader("Authorization", $"Bearer {bearerToken}");

            yield return uploadRequest.SendWebRequest();

            if (uploadRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Errore caricamento {fileName}: {uploadRequest.error}");
            }
            else
            {
                Debug.Log($"Slide caricata: {fileName}");
            }
        }


        Debug.Log("Tutte le slide sono state caricate.");
    }

    private void UploadSingleSlide()
    {
        Debug.Log("Funzione caricamento singola slide (da implementare).");
    }

    [System.Serializable]
    private class GroupData
    {
        public string id;
        public string name;
    }

    [System.Serializable]
    private class GroupResponseWrapper
    {
        public GroupData data;
    }
}
