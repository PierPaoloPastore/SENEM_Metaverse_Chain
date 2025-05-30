using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class PinataAPIManager : MonoBehaviour
{
    public static PinataAPIManager Instance { get; private set; }

    [Header("Configurazione API")]
    [SerializeField] private string bearerToken;
    public string BearerToken => bearerToken;//Getter pubblico
    [SerializeField] private string baseUrl = "https://api.pinata.cloud/v3";
    [SerializeField] private string publicGateway = "https://scarlet-generous-vulture-659.mypinata.cloud/ipfs/";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    public string GetGatewayUrl(string cid) => publicGateway + cid;

    public IEnumerator GetGroups(System.Action<List<Group>> onSuccess, System.Action<string> onError = null)
    {
        UnityWebRequest request = UnityWebRequest.Get($"{baseUrl}/groups/public");
        request.SetRequestHeader("Authorization", "Bearer " + bearerToken);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(request.error);
        }
        else
        {
            GroupResponse response = JsonConvert.DeserializeObject<GroupResponse>(request.downloadHandler.text);
            onSuccess?.Invoke(response.data.groups);
        }
    }

    public IEnumerator GetFilesInGroup(string groupId, System.Action<List<PinataFile>> onSuccess, System.Action<string> onError = null)
    {
        UnityWebRequest request = UnityWebRequest.Get($"{baseUrl}/files/public?group={groupId}");
        request.SetRequestHeader("Authorization", "Bearer " + bearerToken);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(request.error);
        }
        else
        {
            FileResponse response = JsonConvert.DeserializeObject<FileResponse>(request.downloadHandler.text);
            onSuccess?.Invoke(response.data.files);
        }
    }
}
