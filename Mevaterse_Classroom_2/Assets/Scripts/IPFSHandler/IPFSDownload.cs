using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using Newtonsoft.Json;

public class IPFSDownload : MonoBehaviour
{
    private const string BEARER_Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySW5mb3JtYXRpb24iOnsiaWQiOiI1Y2QyNTllOC1mZDQ4LTQ0MzktYWY3MC0zYTU3ZmZlYjcxMWYiLCJlbWFpbCI6InBpZXJwaWVsZUBnbWFpbC5jb20iLCJlbWFpbF92ZXJpZmllZCI6dHJ1ZSwicGluX3BvbGljeSI6eyJyZWdpb25zIjpbeyJkZXNpcmVkUmVwbGljYXRpb25Db3VudCI6MSwiaWQiOiJGUkExIn0seyJkZXNpcmVkUmVwbGljYXRpb25Db3VudCI6MSwiaWQiOiJOWUMxIn1dLCJ2ZXJzaW9uIjoxfSwibWZhX2VuYWJsZWQiOmZhbHNlLCJzdGF0dXMiOiJBQ1RJVkUifSwiYXV0aGVudGljYXRpb25UeXBlIjoic2NvcGVkS2V5Iiwic2NvcGVkS2V5S2V5IjoiYmQ1NzRmYzlkNWJkODNjYjVlODAiLCJzY29wZWRLZXlTZWNyZXQiOiIyNjlmM2I2YWIxZjhhMGE2YTcyZjQzMDYzYjQ3YjYwY2UzMGZiMDFmYzUxYjk1NWFlYmVjYzFjYjFhYTlhNzNjIiwiZXhwIjoxNzc0NjE1NzEyfQ.wn_AidOK3c1aB5ZUymn_LTgSWNd3J-av8Md7M0l3fXY"; // Inserisci il tuo Bearer Token qui
    private const string BASE_URL = "https://api.pinata.cloud/v3/files/public/"; // URL base per l'endpoint get-file-by-id
    private const string GATEWAY = "https://scarlet-generous-vulture-659.mypinata.cloud/ipfs/";
    private const string url_pubblico = "https://gateway.pinata.cloud/ipfs/bafkreiadp3ch3cbxyg6grfkkclbbz3zo3upjajrpw6g5zgu24u4lcbtw2y";
    public GroupListUI groupListUI; // lo collegherai da Inspector
    public string cid;
    //Buffer per le nuove slide 
    private List<Material> newLesson = new List<Material>();
    private List<BoardController> boards;
    // private const string cid = "bafkreiadp3ch3cbxyg6grfkkclbbz3zo3upjajrpw6g5zgu24u4lcbtw2y";
    public List<Group> gruppi = new List<Group>();



    private void Start()
    {
        boards = new List<BoardController>(FindObjectsOfType<BoardController>());
        groupListUI = FindObjectOfType<GroupListUI>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("Tasto Premuto");
            // Inserisci il tuo CID (file hash) per fare la richiesta al file specifico
            string fileHash = "0195d21f-5021-7d38-9991-9c4d8514ca0a"; // Esempio, sostituiscilo con il tuo hash
            //StartCoroutine(DownloadFileById(fileHash));
            if (cid == null)
            {
                Debug.Log("CID ASSENTE! INSERIRE CID NEL TestDownload");
            }
            StartCoroutine(DownloadImageFromCid(cid));
            StartCoroutine(GetGroups());


        }
    }

    //Accede direttamente all' IPFS pubblico tramite il gateway pubblico del progetto, e restituisce un
    public IEnumerator DownloadImageFromCid(string cid)
    {
        newLesson.Clear();//Pulisco in caso di utilizzo passato
        if (cid != null)
        {                    
        string imageUrl = GATEWAY + cid;
        UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(imageUrl);

        yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;
                Renderer renderer = GetComponent<Renderer>();

                Material mat = new Material(Shader.Find("Standard"));
                mat.mainTexture = texture;

                if (renderer != null)
                {
                    renderer.material = mat;
                    Debug.Log("Immagine caricata e applicata!");
                }
                //Aggiungo al buffer
                newLesson.Add(mat);


                foreach (var board in boards)//aggiorna tutti i board controller
                {
                    board.ChangeLoadedLesson(newLesson);

                }

                // Ora stai aggiungendo un Material, non una Texture2D

            }
            else if (webRequest.responseCode == 429)
            {
                Debug.LogError("Errore 429 - Troppo molte richieste! Attendere...");
                yield return new WaitForSeconds(5f); // Aspetta 5 secondi prima di riprovare
                StartCoroutine(DownloadImageFromCid(cid)); // Riprova la richiesta
            }
            else
            {
                Debug.LogError("Errore nel download dell'immagine: " + webRequest.error);
            }
        }
        else
        {
            Debug.Log("Nessun cid rilevato!");
        }
    }


        //Metodo che restituisce un file Json
        private IEnumerator DownloadFileById(string fileHash)
    {
        // Costruisci l'URL completo con l'hash del file
        string url = BASE_URL + fileHash;
        Debug.Log("URL montato: " + url);


        // Crea la richiesta GET
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // Imposta l'header di autorizzazione Bearer
            webRequest.SetRequestHeader("Authorization", "Bearer " + BEARER_Token);

            // Invia la richiesta e aspetta la risposta
            yield return webRequest.SendWebRequest();

            // Verifica il risultato della richiesta
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                // Ricevi i byte raw del file
                byte[] fileData = webRequest.downloadHandler.data;

                // Crea una nuova texture
                Texture2D texture = new Texture2D(2, 2); // Inizializza una texture temporanea
                if (texture.LoadImage(fileData)) // Carica l'immagine dai byte
                {
                    // Ottieni il Renderer del GameObject a cui vuoi applicare la texture
                    Renderer renderer = GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        // Crea un nuovo materiale se necessario
                        Material material = new Material(renderer.material);
                        renderer.material = material;

                        // Assegna la texture al materiale
                        renderer.material.mainTexture = texture;
                        Debug.Log("Immagine applicata con successo!");
                    }
                }
            }
            else
            {
                // In caso di errore o codice di stato diverso
                Debug.LogError("Errore: Codice di stato - " + webRequest.responseCode);
                Debug.LogError("Messaggio di errore: " + webRequest.error);

                // Log della risposta completa per diagnosi
                if (webRequest.isNetworkError || webRequest.isHttpError)
                {
                    Debug.LogError("Corpo della risposta: " + webRequest.downloadHandler.text);
                }
            }
        }
    }
    /* questa è la versione per testare 
    IEnumerator GetGroups()
    {
        string url = "https://api.pinata.cloud/v3/groups/public";

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", "Bearer " + BEARER_Token);

        yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        if (request.result != UnityWebRequest.Result.Success)
#else
        if (request.isNetworkError || request.isHttpError)
#endif
        {
            Debug.LogError("Errore nella richiesta: " + request.error);
        }
        else
        {
            Debug.Log("Risposta dalla API:");
            Debug.Log(request.downloadHandler.text);
        }
    }
    */

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
            groupListUI.ShowGroups(gruppi);//DA CANCELLARE SE NON VA!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            // ESEMPIO: prendi il primo gruppo
            if (gruppi.Count > 0)
            {
                string selectedGroupId = gruppi[0].id;
                StartCoroutine(GetFilesInGroup(selectedGroupId));
            }
        }
    }



    public IEnumerator GetFilesInGroup(string groupId)
    {
        string url = $"https://api.pinata.cloud/v3/files/public?group_id={groupId}";

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

            // Se vuoi: deserializza qui il json in una lista di oggetti file
        }
    }

}
