using System;
using System.Threading.Tasks;
using UnityEngine;
using ChainSafe.Gaming.Evm.Contracts.Custom;
using ChainSafe.Gaming.Web3;
using ChainSafe.Gaming.UnityPackage;

//Gestore del Contratto LessonRegistry.sol
public class LessonRegistryHandler : MonoBehaviour
{
    [Header("Configurazione contratto")]
    [SerializeField] private string contractAddress;

    private LessonRegistry lessonRegistry;
    private Web3 web3;

    private void Awake()
    {
        Web3Unity.Web3Initialized += OnWeb3Initialized;
    }

    private void OnDestroy()
    {
        Web3Unity.Web3Initialized -= OnWeb3Initialized;
        DisposeContract();
    }

    private async void OnWeb3Initialized((Web3 web3, bool isLightweight) context)
    {
        this.web3 = context.web3;
        await DisposeContract();

        lessonRegistry = await BuildWithRetry(contractAddress);
        if (lessonRegistry != null)
            Debug.Log("[LessonRegistryHandler] Contratto istanziato correttamente.");
    }

    private async Task<LessonRegistry> BuildWithRetry(string address, int maxRetries = 20, int delayMs = 500)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                return await web3.ContractBuilder.Build<LessonRegistry>(address);
            }
            catch
            {
                Debug.LogWarning($"[LessonRegistryHandler] Tentativo {i + 1}: signer non pronto. Riprovo...");
                await Task.Delay(delayMs);
            }
        }

        Debug.LogError("[LessonRegistryHandler] Errore: impossibile istanziare il contratto dopo vari tentativi.");
        return null;
    }

    private async Task DisposeContract()
    {
        if (lessonRegistry != null)
        {
            await lessonRegistry.DisposeAsync();
            lessonRegistry = null;
        }
    }

    public bool IsReady() => lessonRegistry != null;

    public async Task<(bool found, string cid, string uploader)> TryGetLesson(string lessonName)
    {
        if (lessonRegistry == null)
        {
            Debug.LogWarning("[LessonRegistryHandler] Contratto non inizializzato.");
            return (false, null, null);
        }

        try
        {
            var (cid, uploader) = await lessonRegistry.GetLesson(lessonName);
            Debug.Log($"[LessonRegistryHandler] Lezione trovata: {lessonName} -> CID: {cid}, Uploader: {uploader}");
            return (true, cid, uploader);
        }
        catch (Exception ex)
        {
            Debug.Log($"[LessonRegistryHandler] GetLesson fallita per '{lessonName}': {ex.Message}");
            return (false, null, null);
        }
    }

    public async Task<bool> RegisterLesson(string lessonName, string cid, NotificationUI notificationUI = null)
    {
        if (lessonRegistry == null)
        {
            Debug.LogWarning("[LessonRegistryHandler] Contratto non inizializzato.");
            notificationUI?.Show("Contratto non inizializzato.");
            return false;
        }

        // 1. Controllo esistenza
        var check = await TryGetLesson(lessonName);
        if (check.found)
        {
            Debug.LogWarning($"[LessonRegistryHandler] Lezione '{lessonName}' già registrata. CID: {check.cid}");
            notificationUI?.Show($"Lezione '{lessonName}' già registrata.");
            return false;
        }

        // 2. Registrazione
        try
        {
            var receipt = await lessonRegistry.RegisterLessonWithReceipt(lessonName, cid);
            Debug.Log($"[LessonRegistryHandler] Registrazione completata! TxHash: {receipt.TransactionHash}");
            notificationUI?.Show("Lezione registrata con successo.");
            return true;
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("revert"))
                Debug.LogError("[LessonRegistryHandler] Il contratto ha rifiutato la transazione (revert). Probabilmente input non valido.");
            else
                Debug.LogError($"[LessonRegistryHandler] Errore durante la registrazione: {ex.Message}");

            notificationUI?.Show("Errore durante la registrazione.");
            return false;
        }
    }

}
