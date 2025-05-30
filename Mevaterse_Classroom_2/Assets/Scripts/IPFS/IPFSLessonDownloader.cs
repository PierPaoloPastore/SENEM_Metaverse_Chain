using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

/// <summary>
/// Gestisce il download delle lezioni e delle slide da IPFS tramite Pinata.
/// Si occupa di mostrare i gruppi disponibili, scaricare tutte le slide di una lezione
/// e applicare le texture alle lavagne.
/// </summary>
public class IPFSLessonDownloader : MonoBehaviour
{
    public NotificationUI notificationUI;
    public GroupListUI groupListUI;

    private List<Material> currentLesson = new List<Material>();
    private List<BoardController> boards = new List<BoardController>();
    public List<Group> gruppi = new List<Group>(); // Ogni gruppo su Pinata equivale ad una lezione

    
    void Start()
    {
        var ui = UIReferenceManager.Instance;
        groupListUI = ui.groupListUI;
        notificationUI = ui.notificationUI;

        boards = new List<BoardController>(FindObjectsOfType<BoardController>());
    }

    // All’apertura del pannello download, mostra i gruppi disponibili
    private void OnEnable()
    {
        LessonStorageUIController.OnPanelDownloadOpened += HandlePanelOpened;
    }

    private void OnDisable()
    {
        LessonStorageUIController.OnPanelDownloadOpened -= HandlePanelOpened;
    }

    private void HandlePanelOpened()
    {
        if (groupListUI != null)
            groupListUI.gameObject.SetActive(true);

        StartCoroutine(PopolaListaGruppi());
    }

    /// <summary>
    /// Popola la lista dei gruppi disponibili (lezioni) dalla API Pinata
    /// </summary>
    private IEnumerator PopolaListaGruppi()
    {
        bool done = false;
        List<Group> result = null;
        yield return StartCoroutine(
            PinataAPIManager.Instance.GetGroups(
                (gruppiTrovati) => { result = gruppiTrovati; done = true; },
                (err) => { Debug.LogError("Errore gruppi: " + err); done = true; }
            )
        );
        while (!done) yield return null;

        gruppi = result ?? new List<Group>();
        if (gruppi.Count == 0)
        {
            notificationUI?.Show("Nessuna lezione trovata!");
            yield break;
        }
        groupListUI.ShowGroups(gruppi, GroupListUI.GroupSelectionMode.Download);
    }

    /// <summary>
    /// Avviato da GroupListUI: scarica e mostra tutte le slide della lezione selezionata
    /// </summary>
    public IEnumerator GetFilesInGroup(string groupId)
    {
        // Recupera la lista dei file della lezione
        bool done = false;
        List<PinataFile> files = null;
        yield return StartCoroutine(
            PinataAPIManager.Instance.GetFilesInGroup(
                groupId,
                (f) => { files = f; done = true; },
                (err) => { Debug.LogError("Errore files: " + err); done = true; }
            )
        );
        while (!done) yield return null;

        if (files == null || files.Count == 0)
        {
            notificationUI?.Show("Nessuna slide trovata.");
            yield break;
        }

        files.Sort((a, b) => a.name.CompareTo(b.name));

        currentLesson.Clear();

        int total = files.Count;
        int completed = 0;
        foreach (var file in files)
        {
            completed++;
            notificationUI?.Show($"Download slide {completed}/{total}...");
            yield return StartCoroutine(ScaricaESettaTexture(file.cid));
        }
        notificationUI?.Show("Download completato!");

        // Applica la lezione caricata a tutte le lavagne
        foreach (var board in boards)
            board.ChangeLoadedLesson(currentLesson);
    }

    /// <summary>
    /// Scarica la texture tramite CID da IPFS (gateway Pinata) e la aggiunge alla lezione corrente
    /// </summary>
    private IEnumerator ScaricaESettaTexture(string cid)
    {
        if (string.IsNullOrEmpty(cid))
            yield break;

        string url = PinataAPIManager.Instance.GetGatewayUrl(cid);
        UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url);
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;
            Material mat = new Material(Shader.Find("Standard")) { mainTexture = texture };
            currentLesson.Add(mat);
            Debug.Log("Immagine caricata e aggiunta alla lezione.");
        }
        else if (webRequest.responseCode == 429)
        {
            Debug.LogError("Errore 429 - Troppe richieste! Attendere...");
            yield return new WaitForSeconds(5f);
            yield return StartCoroutine(ScaricaESettaTexture(cid));
        }
        else
        {
            Debug.LogError("Errore download immagine: " + webRequest.error);
            notificationUI?.Show("Errore download: " + webRequest.error);
        }
    }
}
