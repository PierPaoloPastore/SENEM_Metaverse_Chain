using ChainSafe.Gaming.UnityPackage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using ChainSafe.Gaming.Web3;
using ChainSafe.Gaming.Web3.Unity;
using ChainSafe.Gaming.Evm.Contracts.Custom;
using SFB;
using Newtonsoft.Json; // per la ricerca orfani (FileResponse/PinataFile sono in PinataModels.cs)

public class IPFSLessonUploader : MonoBehaviour
{
    // --- modelli minimi per parse risposta upload ---
    [Serializable] class UploadData { public string id; public string name; public string cid; public string group_id; }
    [Serializable] class UploadResponse { public UploadData data; }

    public GameObject panelUpload;
    public Button buttonUploadFolder;
    public Button buttonUploadSlide;

    private LessonStorageUIController uiController;

    private string ultimaLezione;
    private string ultimoGroupId;

    // prompt 409
    private bool? replaceChoice = null;
    // compat UI (non usato qui)
    private bool? singleReplaceChoice = null;

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

    private void OnEnable() { LessonStorageUIController.OnPanelUploadOpened += HandlePanelOpened; }
    private void OnDisable() { LessonStorageUIController.OnPanelUploadOpened -= HandlePanelOpened; }
    private void HandlePanelOpened() { panelUpload.SetActive(true); }

    // =============== UPLOAD CARTELLA (LEZIONE) ===============

    void UploadNewLesson()
    {
        var paths = StandaloneFileBrowser.OpenFolderPanel("Seleziona la cartella della lezione", "", false);
        if (paths.Length == 0 || string.IsNullOrEmpty(paths[0])) return;

        string folderPath = paths[0];
        string lessonName = Path.GetFileName(folderPath);

        // check: almeno un .jpg/.jpeg
        var hasJpg = Directory.GetFiles(folderPath).Any(p =>
        {
            var e = Path.GetExtension(p).ToLower();
            return e == ".jpg" || e == ".jpeg";
        });
        if (!hasJpg)
        {
            panelUpload.SetActive(false);
            UIReferenceManager.Instance.notificationUI.showConfirmAfterHide = false;
            UIReferenceManager.Instance.notificationUI?.Show("Nessuna slide .jpg trovata nella cartella selezionata.");
            return;
        }

        StartCoroutine(CreaGruppoECaricaTutteLeSlide(folderPath, lessonName));
    }

    IEnumerator CreaGruppoECaricaTutteLeSlide(string folderPath, string lessonName)
    {
        panelUpload.SetActive(false);
        CursorManager.Instance.HideCursor();

        // crea gruppo
        var jsonBody = "{\"name\":\"" + lessonName + "\"}";
        using (var request = new UnityWebRequest("https://api.pinata.cloud/v3/groups/public", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", "Bearer " + PinataAPIManager.Instance.BearerToken);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // gruppo nuovo: carica tutto con "ensure grouped"
                var groupId = JsonUtility.FromJson<CreatedGroupResponse>(request.downloadHandler.text).data.id;

                var files = Directory.GetFiles(folderPath)
                                     .Where(p => { var e = Path.GetExtension(p).ToLower(); return e == ".jpg" || e == ".jpeg"; })
                                     .OrderBy(p => p)
                                     .ToList();

                for (int i = 0; i < files.Count; i++)
                {
                    string fp = files[i];
                    string fn = GeneraNomeFile(lessonName, i + 1, fp);

                    UIReferenceManager.Instance.notificationUI?.Show($"Caricamento slide {i + 1}/{files.Count}...");
                    bool ok = false; string newId = null;
                    yield return StartCoroutine(UploadFile_EnsureGrouped(fp, fn, groupId, (success, id) => { ok = success; newId = id; }));
                    if (!ok) { UIReferenceManager.Instance.notificationUI?.Show("Errore durante l'upload della slide!"); yield break; }
                }

                ultimaLezione = lessonName;
                ultimoGroupId = groupId;

                UIReferenceManager.Instance.notificationUI.ShowConfirmUpload();
                UIReferenceManager.Instance.notificationUI.showConfirmAfterHide = true;
                UIReferenceManager.Instance.notificationUI?.Show("Upload completato!");
                CursorManager.Instance.HideCursor();
                yield break;
            }

            if (request.responseCode == 409)
            {
                // gruppo esistente → chiedi
                List<Group> gruppi = null; bool doneList = false;
                yield return StartCoroutine(
                    PinataAPIManager.Instance.GetGroups(
                        (g) => { gruppi = g; doneList = true; },
                        (err) => { doneList = true; }
                    )
                );
                while (!doneList) yield return null;

                var existing = gruppi?.FirstOrDefault(x => x.name == lessonName);
                if (existing == null)
                {
                    UIReferenceManager.Instance.notificationUI?.Show("409: gruppo esistente non recuperabile.");
                    yield break;
                }
                var groupId409 = existing.id;

                replaceChoice = null;
                UIReferenceManager.Instance.notificationUI.ShowConfirmReplace409();
                yield return new WaitUntil(() => replaceChoice.HasValue);

                if (replaceChoice.Value)
                {
                    // REPLACE per indice: carico prima i nuovi (ensure grouped), poi rimuovo i vecchi
                    yield return StartCoroutine(ReplaceLesson_ByIndex(folderPath, lessonName, groupId409, hardDeleteOldFiles: true));
                    UIReferenceManager.Instance.notificationUI?.Show("Lezione aggiornata!");
                }
                else
                {
                    // APPEND in fondo (ensure grouped)
                    yield return StartCoroutine(AppendLesson(folderPath, lessonName, groupId409));
                    UIReferenceManager.Instance.notificationUI?.Show("Append completato!");
                }

                ultimaLezione = lessonName;
                ultimoGroupId = groupId409;

                UIReferenceManager.Instance.notificationUI.showConfirmAfterHide = true;
                CursorManager.Instance.HideCursor();
                yield break;
            }

            Debug.LogError("Errore creazione gruppo: " + request.error);
            UIReferenceManager.Instance.notificationUI?.Show("Errore creazione gruppo!");
        }
    }

    // =============== UPLOAD SLIDE SINGOLA ===============

    void UploadSingleSlide()
    {
        StartCoroutine(SelezionaGruppoECaricaSlide());
    }

    IEnumerator SelezionaGruppoECaricaSlide()
    {
        panelUpload.SetActive(false);

        var downloader = UIReferenceManager.Instance.ipfsLessonDownloader;
        var groupListUI = UIReferenceManager.Instance.groupListUI;

        // 1) gruppi
        bool done = false; List<Group> gruppi = null;
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

        // 2) selezione gruppo
        Group selectedGroup = null; bool gruppoSelezionato = false;
        groupListUI.gameObject.SetActive(true);
        groupListUI.ShowGroups(gruppi, GroupListUI.GroupSelectionMode.Upload);
        groupListUI.OnGroupSelected = (Group g) => { selectedGroup = g; gruppoSelezionato = true; };
        while (!gruppoSelezionato) yield return null;

        // 3) file locale
        var paths = StandaloneFileBrowser.OpenFilePanel("Seleziona una slide", "", "jpg", false);
        if (paths.Length == 0 || string.IsNullOrEmpty(paths[0])) yield break;
        string filePath = paths[0];

        // 4) stato attuale del gruppo
        List<PinataFile> groupFiles = null; bool doneFiles = false;
        yield return StartCoroutine(
            PinataAPIManager.Instance.GetFilesInGroup(
                selectedGroup.id,
                (f) => { groupFiles = f ?? new List<PinataFile>(); doneFiles = true; },
                (err) => { Debug.LogError("Errore lista file: " + err); doneFiles = true; }
            )
        );
        while (!doneFiles) yield return null;

        // 5) rileva conflitto per SUFFISSO (stesso contenuto nominale, indice a parte)
        string localSuffix = GetLocalSafeSuffix(filePath);
        PinataFile conflict = null; int conflictIndex = 0;

        foreach (var rf in groupFiles)
        {
            if (TryParseIndexAndSuffix(rf.name, selectedGroup.name, out int idx, out string sufLower)
                && string.Equals(sufLower, localSuffix, StringComparison.OrdinalIgnoreCase))
            {
                conflict = rf;
                conflictIndex = idx;
                break;
            }
        }

        if (conflict != null)
        {
            // Prompt: sostituire o accodare?
            singleReplaceChoice = null;
            UIReferenceManager.Instance.notificationUI.ShowConfirmReplaceSingle();
            yield return new WaitUntil(() => singleReplaceChoice.HasValue);

            if (singleReplaceChoice.Value)
            {
                // REPLACE: mantieni lo stesso indice
                yield return StartCoroutine(ReplaceSingleSlideAtIndex(
                    selectedGroup.id,
                    selectedGroup.name,
                    conflictIndex,
                    conflict.id,
                    filePath
                ));
                yield break;
            }
            // else: continua ad accodare sotto
        }

        // 6) APPEND: indice = maxIndex+1 (no collisioni)
        int nextIndex = GetNextFreeIndex(selectedGroup.name, groupFiles);
        string fileName = GeneraNomeFile(selectedGroup.name, nextIndex, filePath);
        while ((groupFiles ?? new List<PinataFile>()).Any(f => string.Equals(f.name, fileName, StringComparison.OrdinalIgnoreCase)))
        {
            nextIndex++;
            fileName = GeneraNomeFile(selectedGroup.name, nextIndex, filePath);
        }

        bool okUp = false; string newId = null;
        yield return StartCoroutine(UploadFile_EnsureGrouped(filePath, fileName, selectedGroup.id, (success, id) => { okUp = success; newId = id; }));
        if (okUp) UIReferenceManager.Instance.notificationUI?.Show("Slide aggiunta.");
        else UIReferenceManager.Instance.notificationUI?.Show("Errore durante l'upload della slide.");
    }

    // =============== HTTP CORE ===============

    // Upload e GARANZIA che il file finisca nel gruppo (attach post-upload se serve)
    IEnumerator UploadFile_EnsureGrouped(string filePath, string fileName, string groupId, Action<bool, string> onDone)
    {
        byte[] fileBytes = File.ReadAllBytes(filePath);
        string boundary = "----Boundary" + DateTime.Now.Ticks.ToString("x");

        var formBody = new MemoryStream();
        var enc = Encoding.ASCII;
        void W(string s) { var b = enc.GetBytes(s + "\r\n"); formBody.Write(b, 0, b.Length); }

        // file
        W($"--{boundary}");
        W($"Content-Disposition: form-data; name=\"file\"; filename=\"{fileName}\"");
        W("Content-Type: image/jpeg");
        W(""); formBody.Write(fileBytes, 0, fileBytes.Length); W("");

        // name (facoltativo, aiuta la UI)
        W($"--{boundary}");
        W("Content-Disposition: form-data; name=\"name\"");
        W(""); W(fileName);

        // group_id (se Pinata lo ignora, forzeremo dopo)
        W($"--{boundary}");
        W("Content-Disposition: form-data; name=\"group_id\"");
        W(""); W(groupId);

        // network
        W($"--{boundary}");
        W("Content-Disposition: form-data; name=\"network\"");
        W(""); W("public");

        // keyvalues
        W($"--{boundary}");
        W("Content-Disposition: form-data; name=\"keyvalues\"");
        W(""); W("{}");

        W($"--{boundary}--");

        string fileId = null; string serverGroup = null;

        using (var req = new UnityWebRequest("https://uploads.pinata.cloud/v3/files", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(formBody.ToArray());
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Authorization", $"Bearer {PinataAPIManager.Instance.BearerToken}");
            req.SetRequestHeader("Content-Type", $"multipart/form-data; boundary={boundary}");

            Debug.Log($"Uploading file: {filePath} as {fileName} in group {groupId}");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Errore caricamento {fileName}: {req.error}");
                Debug.LogError("Risposta server: " + req.downloadHandler.text);
                onDone(false, null);
                yield break;
            }

            try
            {
                var resp = JsonUtility.FromJson<UploadResponse>(req.downloadHandler.text);
                fileId = resp?.data?.id;
                serverGroup = resp?.data?.group_id;
            }
            catch { fileId = null; }

            if (string.IsNullOrEmpty(fileId))
            {
                Debug.LogWarning("Upload ok ma fileId non presente.");
                onDone(false, null);
                yield break;
            }
        }

        // attach manuale se non risulta nel gruppo atteso
        if (string.IsNullOrEmpty(serverGroup) || !string.Equals(serverGroup, groupId, StringComparison.OrdinalIgnoreCase))
        {
            bool attached = false;
            yield return StartCoroutine(AddFileToGroup(groupId, fileId, succ => attached = succ));
            if (!attached)
            {
                Debug.LogError($"Impossibile agganciare {fileId} al gruppo {groupId}");
                onDone(false, null);
                yield break;
            }

        }

        Debug.Log($"Slide caricata: {fileName}");
        onDone(true, fileId);
    }



    // Aggiunge UN singolo file a un gruppo (endpoint corretto: PUT /v3/groups/public/{groupId}/ids/{fileId})
    IEnumerator AddFileToGroup(string groupId, string fileId, Action<bool> onDone)
    {
        string url = $"https://api.pinata.cloud/v3/groups/public/{groupId}/ids/{fileId}";

        using (var req = UnityWebRequest.Put(url, Array.Empty<byte>()))
        {
            req.SetRequestHeader("Authorization", $"Bearer {PinataAPIManager.Instance.BearerToken}");
            req.downloadHandler = new DownloadHandlerBuffer(); // importante, altrimenti Unity lamenta il downloadHandler
            // opzionale: req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            bool success = req.result == UnityWebRequest.Result.Success || (req.responseCode >= 200 && req.responseCode < 300);
            if (!success)
            {
                Debug.LogError($"AddFileToGroup fallita ({fileId} -> {groupId}): {req.error}");
                Debug.LogError("Risposta server: " + req.downloadHandler.text);
            }
            onDone?.Invoke(success);
        }
    }


    // togli dal gruppo (non unpin)
    IEnumerator RemoveFileFromGroup(string groupId, string fileId)
        {
            string url = $"https://api.pinata.cloud/v3/groups/public/{groupId}/ids/{fileId}";
            using (var req = UnityWebRequest.Delete(url))
            {
                req.SetRequestHeader("Authorization", $"Bearer {PinataAPIManager.Instance.BearerToken}");
                yield return req.SendWebRequest();
            }
        }

    // unpin (DELETE file)
    IEnumerator DeleteFileById(string fileId)
    {
        string url = $"https://api.pinata.cloud/v3/files/public/{fileId}";
        using (var req = UnityWebRequest.Delete(url))
        {
            req.SetRequestHeader("Authorization", $"Bearer {PinataAPIManager.Instance.BearerToken}");
            yield return req.SendWebRequest();
        }
    }

    // =============== REPLACE/APPEND PER 409 ===============

    IEnumerator ReplaceLesson_ByIndex(string folderPath, string lessonName, string groupId, bool hardDeleteOldFiles)
    {
        // mappa indice -> file remoto
        List<PinataFile> remoteFiles = null; bool done = false;
        yield return StartCoroutine(
            PinataAPIManager.Instance.GetFilesInGroup(
                groupId, (f) => { remoteFiles = f ?? new List<PinataFile>(); done = true; },
                (err) => { done = true; }
            )
        );
        while (!done) yield return null;

        var re = new Regex("^" + Regex.Escape(lessonName) + @"_(\d+)_", RegexOptions.IgnoreCase);
        var byIndex = new Dictionary<int, PinataFile>();
        foreach (var rf in remoteFiles)
        {
            var m = re.Match(rf.name);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int idx)) byIndex[idx] = rf;
        }

        var local = Directory.GetFiles(folderPath)
                             .Where(p => { var e = Path.GetExtension(p).ToLower(); return e == ".jpg" || e == ".jpeg"; })
                             .OrderBy(p => p)
                             .ToList();
        if (local.Count == 0)
        {
            UIReferenceManager.Instance.notificationUI?.Show("Nessuna slide .jpg trovata nella cartella selezionata.");
            yield break;
        }

        for (int i = 0; i < local.Count; i++)
        {
            int index = i + 1;
            string fp = local[i];
            string fn = GeneraNomeFile(lessonName, index, fp);

            UIReferenceManager.Instance.notificationUI?.Show($"Caricamento slide {index}/{local.Count}...");
            bool ok = false; string newId = null;
            // Upload che garantisce l’aggancio al gruppo e ci restituisce il fileId effettivo
            yield return StartCoroutine(UploadFile_EnsureGrouped(fp, fn, groupId, (success, id) => { ok = success; newId = id; }));
            if (!ok) yield break;

            // Se esisteva già una slide a questo indice…
            if (byIndex.TryGetValue(index, out var oldFile))
            {
                // **IMPORTANTE**: se Pinata ha deduplicato e il fileId è LO STESSO, NON rimuovere nulla
                if (!string.Equals(oldFile.id, newId, StringComparison.OrdinalIgnoreCase))
                {
                    // Vecchio diverso -> ok rimuovere dal gruppo e, opzionale, unpin
                    yield return StartCoroutine(RemoveFileFromGroup(groupId, oldFile.id));
                    if (hardDeleteOldFiles)
                        yield return StartCoroutine(DeleteFileById(oldFile.id));
                }
                // else: stesso fileId → non fare nulla (evita di cancellare il "nuovo")
            }
        }

        // rimuovi extra (indici > local.Count)
        foreach (var extraIdx in byIndex.Keys.Where(k => k > local.Count).OrderBy(k => k))
        {
            var file = byIndex[extraIdx];
            yield return StartCoroutine(RemoveFileFromGroup(groupId, file.id));
            if (hardDeleteOldFiles) yield return StartCoroutine(DeleteFileById(file.id));
        }
    }

    IEnumerator AppendLesson(string folderPath, string lessonName, string groupId)
    {
        // per maxIndex
        List<PinataFile> remoteFiles = null; bool done = false;
        yield return StartCoroutine(
            PinataAPIManager.Instance.GetFilesInGroup(
                groupId, (f) => { remoteFiles = f ?? new List<PinataFile>(); done = true; },
                (err) => { done = true; }
            )
        );
        while (!done) yield return null;

        int maxIndex = 0;
        var re = new Regex("^" + Regex.Escape(lessonName) + @"_(\d+)_", RegexOptions.IgnoreCase);
        foreach (var rf in remoteFiles)
        {
            var m = re.Match(rf.name);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int idx) && idx > maxIndex) maxIndex = idx;
        }

        var local = Directory.GetFiles(folderPath)
                             .Where(p => { var e = Path.GetExtension(p).ToLower(); return e == ".jpg" || e == ".jpeg"; })
                             .OrderBy(p => p)
                             .ToList();
        if (local.Count == 0)
        {
            UIReferenceManager.Instance.notificationUI?.Show("Nessuna slide .jpg trovata nella cartella selezionata.");
            yield break;
        }

        for (int i = 0; i < local.Count; i++)
        {
            int index = maxIndex + 1 + i;
            string fp = local[i];
            string fn = GeneraNomeFile(lessonName, index, fp);

            UIReferenceManager.Instance.notificationUI?.Show($"Aggiunta slide {i + 1}/{local.Count}...");
            bool ok = false; string newId = null;
            yield return StartCoroutine(UploadFile_EnsureGrouped(fp, fn, groupId, (success, id) => { ok = success; newId = id; }));
            if (!ok) yield break;
        }
    }
 
    IEnumerator ReplaceSingleSlideAtIndex(string groupId, string lessonName, int index, string oldFileId, string localPath)
    {
        string newName = GeneraNomeFile(lessonName, index, localPath);

        // carica e assicurati che sia nel gruppo
        bool ok = false; string newId = null;
        yield return StartCoroutine(UploadFile_EnsureGrouped(localPath, newName, groupId, (success, id) => { ok = success; newId = id; }));
        if (!ok) { UIReferenceManager.Instance.notificationUI?.Show("Errore durante la sostituzione."); yield break; }

        // DEDUP-SAFE: se Pinata ha restituito lo stesso fileId, non rimuovere nulla
        if (!string.Equals(oldFileId, newId, StringComparison.OrdinalIgnoreCase))
        {
            yield return StartCoroutine(RemoveFileFromGroup(groupId, oldFileId));
            // opzionale: unpin del vecchio
            yield return StartCoroutine(DeleteFileById(oldFileId));
        }

        UIReferenceManager.Instance.notificationUI?.Show("Slide sostituita.");
    }



    // Suffix locale "safeBase+ext" coerente con i nomi remoti
    string GetLocalSafeSuffix(string filePath)
    {
        string raw = Path.GetFileNameWithoutExtension(filePath);
        string safe = Regex.Replace(raw, @"[^a-zA-Z0-9_\-]", "_");
        string ext = Path.GetExtension(filePath).ToLower();
        return (safe + ext).ToLower();
    }

    // Parsea "LessonName_{index}_{suffix}" → index e suffixLower
    bool TryParseIndexAndSuffix(string remoteName, string lessonName, out int index, out string suffixLower)
    {
        var m = Regex.Match(remoteName, "^" + Regex.Escape(lessonName) + @"_(\d+)_(.+)$", RegexOptions.IgnoreCase);
        if (m.Success && int.TryParse(m.Groups[1].Value, out index))
        {
            suffixLower = m.Groups[2].Value.ToLower();
            return true;
        }
        index = 0; suffixLower = null; return false;
    }
    // =============== UTILS ===============

    string GeneraNomeFile(string baseName, int index, string filePath)
    {
        string raw = Path.GetFileNameWithoutExtension(filePath);
        string safe = Regex.Replace(raw, @"[^a-zA-Z0-9_\-]", "_");
        string ext = Path.GetExtension(filePath).ToLower();
        return $"{baseName}_{index}_{safe}{ext}";
    }

    // prossimo indice libero basato su max index
    int GetNextFreeIndex(string lessonName, List<PinataFile> groupFiles)
    {
        if (groupFiles == null || groupFiles.Count == 0) return 1;

        var re = new Regex("^" + Regex.Escape(lessonName) + @"_(\d+)_", RegexOptions.IgnoreCase);
        int maxIndex = 0;
        foreach (var f in groupFiles)
        {
            var m = re.Match(f.name);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int idx))
                if (idx > maxIndex) maxIndex = idx;
        }
        return maxIndex + 1;
    }

    // Cerca file ovunque per nome esatto
    IEnumerator FindFilesByExactName(string fileName, Action<List<PinataFile>> onResult)
    {
        string url = "https://api.pinata.cloud/v3/files/public?limit=100&name=" + UnityWebRequest.EscapeURL(fileName);
        using (var req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Authorization", "Bearer " + PinataAPIManager.Instance.BearerToken);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onResult?.Invoke(new List<PinataFile>());
                yield break;
            }
            var resp = JsonConvert.DeserializeObject<FileResponse>(req.downloadHandler.text);
            onResult?.Invoke(resp?.data?.files ?? new List<PinataFile>());
        }
    }

    // Elimina duplicati con lo stesso nome che NON appartengono al gruppo corrente
    IEnumerator RemoveOrphansByFileName(string fileName, HashSet<string> keepIdsInGroup)
    {
        List<PinataFile> hits = null;
        yield return StartCoroutine(FindFilesByExactName(fileName, (list) => hits = list));
        if (hits == null || hits.Count == 0) yield break;

        foreach (var f in hits)
        {
            if (keepIdsInGroup.Contains(f.id)) continue; // già nel gruppo selezionato
            yield return StartCoroutine(DeleteFileById(f.id)); // unpin orfano
            Debug.Log($"Orfano eliminato: {f.name} ({f.id})");
        }
    }

    // =============== UI compat (409 / single) ===============
    public void HandleReplaceDecision(bool replaceAll) { replaceChoice = replaceAll; }
    public void HandleReplaceSingleDecision(bool replace)
    {
        singleReplaceChoice = replace;
    }

    // =============== BLOCKCHAIN ===============

    IEnumerator InviaTransazioneBlockchain(string lessonName, string groupId)
    {
        UIReferenceManager.Instance.notificationUI.ShowPersistent("In attesa di conferma della transazione...\nControlla il telefono");

        bool done = false, success = false;
        var handler = FindObjectOfType<LessonRegistryHandler>();
        if (handler == null || !handler.IsReady())
        {
            UIReferenceManager.Instance.notificationUI.Show("Errore: Handler non disponibile.");
            yield break;
        }

        var task = handler.RegisterLesson(lessonName, groupId)
            .ContinueWith(t => { success = t.Result; done = true; });

        while (!done) yield return null;

        if (success) UIReferenceManager.Instance.notificationUI.Show("Registrazione completata con successo!");
        else UIReferenceManager.Instance.notificationUI.Show("Errore durante la registrazione su blockchain.");
    }

    public void HandleUploadConfirmation(bool yes)
    {
        if (!yes) return;

        if (!Web3Unity.Connected)
        {
            UIReferenceManager.Instance.notificationUI.Show("Collega prima il wallet e riprova.");
            return;
        }
        StartCoroutine(InviaTransazioneBlockchain(ultimaLezione, ultimoGroupId));
    }
}
