using ChainSafe.Gaming.UnityPackage;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NotificationUI : MonoBehaviour
{
    private TextMeshProUGUI notificationText;
    private Button ButtonYes;
    private Button ButtonNo;
    private IPFSLessonUploader uploader;

    public bool showConfirmAfterHide = false; // resettata dentro Hide()

    // << NEW: contesto dei pulsanti >>
    public enum ConfirmContext { None, Blockchain, ReplaceGroupOn409, ReplaceSingleSlide } // << aggiunto ReplaceSingleSlide
    private ConfirmContext currentContext = ConfirmContext.None;

    void Awake()
    {
        notificationText = transform.Find("Notification_Text")?.GetComponent<TextMeshProUGUI>();
        ButtonYes = transform.Find("Button_Yes")?.GetComponent<Button>();
        ButtonNo = transform.Find("Button_No")?.GetComponent<Button>();

        ButtonYes.gameObject.SetActive(false);
        ButtonNo.gameObject.SetActive(false);
    }

    public void Start()
    {
        uploader = UIReferenceManager.Instance.ipfsLessonUploader;
        ButtonYes.onClick.AddListener(OnClickYes);
        ButtonNo.onClick.AddListener(OnClickNo);
        gameObject.SetActive(false);
    }

    public void Show(string message)
    {
        if (notificationText != null) notificationText.text = message;
        ButtonYes.gameObject.SetActive(false);
        ButtonNo.gameObject.SetActive(false);
        CursorManager.Instance.ShowCursor();
        gameObject.SetActive(true);
        CancelInvoke();
        Invoke(nameof(Hide), 2f);
    }

    // Conferma per registrazione su blockchain (già esistente), ora con contesto
    public void ShowConfirmUpload()
    {
        if (notificationText != null)
            notificationText.text = "Vuoi registrare la lezione anche sulla blockchain?";

        currentContext = ConfirmContext.Blockchain;

        CursorManager.Instance.ShowCursor();
        ButtonYes.gameObject.SetActive(true);
        ButtonNo.gameObject.SetActive(true);
        gameObject.SetActive(true);
        CancelInvoke();
    }

    // << NEW: conferma per 409 - sostituzione completa oppure no >>
    public void ShowConfirmReplace409()
    {
        if (notificationText != null)
            notificationText.text = "Esiste già una lezione con questo nome.\nVuoi SOSTITUIRE completamente i contenuti?";

        currentContext = ConfirmContext.ReplaceGroupOn409;

        CursorManager.Instance.ShowCursor();
        ButtonYes.gameObject.SetActive(true);
        ButtonNo.gameObject.SetActive(true);
        gameObject.SetActive(true);
        CancelInvoke();
    }

    public void ShowPersistent(string message)
    {
        if (notificationText != null) notificationText.text = message;
        ButtonYes?.gameObject.SetActive(false);
        ButtonNo?.gameObject.SetActive(false);
        CursorManager.Instance.ShowCursor();
        CancelInvoke();
        gameObject.SetActive(true);
    }

    public void ShowConfirmReplaceSingle()
    {
        if (notificationText != null)
            notificationText.text = "Esiste già una slide con questo indice/nome.\nVuoi SOSTITUIRE la slide esistente?";

        currentContext = ConfirmContext.ReplaceSingleSlide;

        CursorManager.Instance.ShowCursor();
        ButtonYes.gameObject.SetActive(true);
        ButtonNo.gameObject.SetActive(true);
        gameObject.SetActive(true);
        CancelInvoke();
    }

    private void OnClickYes()
    {
        Hide();
        var ctx = currentContext; currentContext = ConfirmContext.None;
        if (ctx == ConfirmContext.Blockchain) UIReferenceManager.Instance.ipfsLessonUploader.HandleUploadConfirmation(yes: true);
        else if (ctx == ConfirmContext.ReplaceGroupOn409) UIReferenceManager.Instance.ipfsLessonUploader.HandleReplaceDecision(replaceAll: true);
        else if (ctx == ConfirmContext.ReplaceSingleSlide) UIReferenceManager.Instance.ipfsLessonUploader.HandleReplaceSingleDecision(replace: true);
    }

    private void OnClickNo()
    {
        Hide();
        var ctx = currentContext; currentContext = ConfirmContext.None;
        if (ctx == ConfirmContext.Blockchain) UIReferenceManager.Instance.ipfsLessonUploader.HandleUploadConfirmation(yes: false);
        else if (ctx == ConfirmContext.ReplaceGroupOn409) UIReferenceManager.Instance.ipfsLessonUploader.HandleReplaceDecision(replaceAll: false);
        else if (ctx == ConfirmContext.ReplaceSingleSlide) UIReferenceManager.Instance.ipfsLessonUploader.HandleReplaceSingleDecision(replace: false);
    }
    public void Hide()
    {
        gameObject.SetActive(false);

        if (showConfirmAfterHide)
        {
            showConfirmAfterHide = false;
            ShowConfirmUpload();
        }
        else
        {
            CursorManager.Instance.HideCursor();
        }
    }
}
