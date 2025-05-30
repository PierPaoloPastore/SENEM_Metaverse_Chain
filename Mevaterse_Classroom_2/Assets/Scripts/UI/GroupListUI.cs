using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;  

public class GroupListUI : MonoBehaviour
{
    public GameObject buttonPrefab;
    public Transform contentPanel;
    public IPFSLessonDownloader downloadManager;

    public enum GroupSelectionMode { Download, Upload }
    public GroupSelectionMode currentMode = GroupSelectionMode.Download;
    public System.Action<Group> OnGroupSelected;


    public void Start()
    {
        downloadManager = UIReferenceManager.Instance.ipfsLessonDownloader;


        this.gameObject.SetActive(false);
    }

    //Data una lista di gruppi, istanzia un bottone per ogni gruppo trovato
    public void ShowGroups(List<Group> gruppi, GroupSelectionMode mode = GroupSelectionMode.Download)
    {
        currentMode = mode;

        if (buttonPrefab == null)
        {
            Debug.LogError("buttonPrefab è null! Assegna il prefab nell'Inspector.");
            return;
        }
        if (contentPanel == null)
        {
            Debug.LogError("contentPanel è null! Assegna il contentPanel nell'Inspector.");
            return;
        }

        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

        foreach (Group gruppo in gruppi)
        {
            GameObject buttonObj = Instantiate(buttonPrefab, contentPanel);
            Button btn = buttonObj.GetComponent<Button>();
            if (btn == null)
            {
                Debug.LogError("Il prefab non ha il componente Button!");
                continue;
            }

            TextMeshProUGUI txt = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt == null)
            {
                Debug.LogError("Il prefab non ha il componente TextMeshProUGUI figlio!");
                continue;
            }

            txt.text = gruppo.name;

            // Crea una copia locale del gruppo per evitare il problema del closure
            Group selectedGroup = gruppo;

            btn.onClick.AddListener(() =>
            {
                Debug.Log("Selezionato gruppo: " + selectedGroup.name);

                // SE sei in modalità download
                if (currentMode == GroupSelectionMode.Download && downloadManager != null && downloadManager.enabled)
                {
                    downloadManager.StartCoroutine(downloadManager.GetFilesInGroup(selectedGroup.id));
                }

                // SE sei in modalità upload, chiama la callback solo in quella modalità
                if (currentMode == GroupSelectionMode.Upload && OnGroupSelected != null)
                {
                    OnGroupSelected(selectedGroup);
                    OnGroupSelected = null;
                }

                this.gameObject.SetActive(false);
                CursorManager.Instance.HideCursor();
            });

            CursorManager.Instance.ShowCursor();
        }
    }

}

