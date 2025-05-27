using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;

public class IPFSDownload : MonoBehaviour
{
    public NotificationUI notificationUI;
    private const string BEARER_Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySW5mb3JtYXRpb24iOnsiaWQiOiI1Y2QyNTllOC1mZDQ4LTQ0MzktYWY3MC0zYTU3ZmZlYjcxMWYiLCJlbWFpbCI6InBpZXJwaWVsZUBnbWFpbC5jb20iLCJlbWFpbF92ZXJpZmllZCI6dHJ1ZSwicGluX3BvbGljeSI6eyJyZWdpb25zIjpbeyJkZXNpcmVkUmVwbGljYXRpb25Db3VudCI6MSwiaWQiOiJGUkExIn0seyJkZXNpcmVkUmVwbGljYXRpb25Db3VudCI6MSwiaWQiOiJOWUMxIn1dLCJ2ZXJzaW9uIjoxfSwibWZhX2VuYWJsZWQiOmZhbHNlLCJzdGF0dXMiOiJBQ1RJVkUifSwiYXV0aGVudGljYXRpb25UeXBlIjoic2NvcGVkS2V5Iiwic2NvcGVkS2V5S2V5IjoiYmQ1NzRmYzlkNWJkODNjYjVlODAiLCJzY29wZWRLZXlTZWNyZXQiOiIyNjlmM2I2YWIxZjhhMGE2YTcyZjQzMDYzYjQ3YjYwY2UzMGZiMDFmYzUxYjk1NWFlYmVjYzFjYjFhYTlhNzNjIiwiZXhwIjoxNzc0NjE1NzEyfQ.wn_AidOK3c1aB5ZUymn_LTgSWNd3J-av8Md7M0l3fXY"; // Inserisci il tuo Bearer Token
    private const string BASE_URL = "https://api.pinata.cloud/v3/files/public/";
    private const string GATEWAY = "https://scarlet-generous-vulture-659.mypinata.cloud/ipfs/";//Inserire il proprio gateway

    public GroupListUI groupListUI;
    public string cid;
    private List<Material> newLesson = new List<Material>();//Lista di slide 
    private List<BoardController> boards;//Lista di lavagne da aggiornare
    public List<Group> gruppi = new List<Group>();//Ogni gruppo su Pinata equivale ad una lezione diversa

    // Inizializza riferimenti agli oggetti presenti nella scena
    private void Awake()
    {
        boards = new List<BoardController>(FindObjectsOfType<BoardController>());
        groupListUI = FindObjectOfType<GroupListUI>();
        notificationUI = FindObjectOfType<NotificationUI>();
    }

    // Attiva la UI della lista gruppi e inizia il recupero dei gruppi da Pinata
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            groupListUI.gameObject.SetActive(true);
            StartCoroutine(GetGroups());
        }
    }

    // Scarica un'immagine da IPFS tramite CID e la applica come texture; restituisce IEnumerator
    public IEnumerator DownloadImageFromCid(string cid)
    {
        newLesson.Clear();

        notificationUI?.Show("Download in corso...");

        if (!string.IsNullOrEmpty(cid))
        {
            string imageUrl = GATEWAY + cid;
            UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(imageUrl);
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;
                Renderer renderer = GetComponent<Renderer>();

                Material mat = new Material(Shader.Find("Standard")) { mainTexture = texture };

                if (renderer != null)
                {
                    renderer.material = mat;
                    Debug.Log("Immagine caricata e applicata!");
                    notificationUI?.Show("Download completato con successo!");
                }

                newLesson.Add(mat);

                foreach (var board in boards)
                {
                    board.ChangeLoadedLesson(newLesson);
                }
            }
            else if (webRequest.responseCode == 429)
            {
                Debug.LogError("Errore 429 - Troppe richieste! Attendere...");
                yield return new WaitForSeconds(5f);
                StartCoroutine(DownloadImageFromCid(cid));
            }
            else
            {
                Debug.LogError("Errore nel download dell'immagine: " + webRequest.error);
                notificationUI?.Show("Errore durante il download: " + webRequest.error);
            }
        }
        else
        {
            Debug.Log("Nessun CID rilevato!");
        }
    }

    // Recupera la lista dei gruppi pubblici su Pinata; restituisce IEnumerator
    public IEnumerator GetGroups()
    {
        string url = "https://api.pinata.cloud/v3/groups/public";

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", "Bearer " + BEARER_Token);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Errore nella richiesta: " + request.error);
        }
        else
        {
            var json = request.downloadHandler.text;
            GroupResponse groupResponse = JsonConvert.DeserializeObject<GroupResponse>(json);
            gruppi = groupResponse.data.groups;

            foreach (var g in gruppi)
            {
                Debug.Log($"Gruppo trovato: {g.name} (ID: {g.id})");
            }
            groupListUI.ShowGroups(gruppi);
        }
    }

    // Recupera la lista dei file associati a un determinato gruppo su Pinata; restituisce IEnumerator
    public IEnumerator GetFilesInGroup(string groupId)
    {
        string url = $"https://api.pinata.cloud/v3/files/public?group={groupId}";

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", "Bearer " + BEARER_Token);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Errore nel recupero file: " + request.error);
        }
        else
        {
            string json = request.downloadHandler.text;
            Debug.Log("File trovati nel gruppo selezionato:");
            Debug.Log(json);

            FileResponse fileResponse = JsonConvert.DeserializeObject<FileResponse>(json);

            foreach (var file in fileResponse.data.files)
            {
                Debug.Log($"Scarico immagine: {file.name} (CID: {file.cid})");
                StartCoroutine(DownloadImageFromCid(file.cid));
            }
        }
    }
}