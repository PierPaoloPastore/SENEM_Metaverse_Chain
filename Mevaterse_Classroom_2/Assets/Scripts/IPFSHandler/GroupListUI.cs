using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;  

public class GroupListUI : MonoBehaviour
{
    public GameObject buttonPrefab;
    public Transform contentPanel;
    public IPFSDownload downloadManager;

    public void Start()
    {
        this.gameObject.SetActive(false);
    }

    public void ShowGroups(List<Group> gruppi)
    {
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
                downloadManager.StartCoroutine(downloadManager.GetFilesInGroup(selectedGroup.id));
            });
        }

    }
}

