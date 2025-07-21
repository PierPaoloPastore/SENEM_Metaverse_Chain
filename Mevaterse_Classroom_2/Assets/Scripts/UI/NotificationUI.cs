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


    void Awake()
    {
        // Trova il TMP del messaggio
        //notificationText = transform.Find("Notification_Text")?.GetComponent<TextMeshProUGUI>();
        notificationText = transform.Find("Notification_Text")?.GetComponent<TextMeshProUGUI>();
        // Trova i riferimenti ai pulsanti e li "spegne" finchè non necessari
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
        //Hide();
        gameObject.SetActive(false);//Lo faccio manualmente perchè hide serve per altro
    }

    public void Show(string message)
    {
        if (notificationText != null)
            notificationText.text = message;

        ButtonYes.gameObject.SetActive(false);
        ButtonNo.gameObject.SetActive(false);

        CursorManager.Instance.ShowCursor();


        gameObject.SetActive(true);
        CancelInvoke(); // annulla eventuali chiamate precedenti
        Invoke(nameof(Hide), 2f); // nasconde dopo 2 secondi
    }

    public void ShowConfirmUpload()
    {
        if (notificationText != null)
            notificationText.text = "Vuoi registrare la lezione anche sulla blockchain?";

        CursorManager.Instance.ShowCursor();

        ButtonYes.gameObject.SetActive(true);
        ButtonNo.gameObject.SetActive(true);
        gameObject.SetActive(true);
    }

    public void ShowPersistent(string message)
    {
        if (notificationText != null)
            notificationText.text = message;

        ButtonYes?.gameObject.SetActive(false);
        ButtonNo?.gameObject.SetActive(false);

        CursorManager.Instance.ShowCursor();

        CancelInvoke(); // importante: impedisce chiusura automatica

        gameObject.SetActive(true);
    }



    private void OnClickYes()
    {
        Hide();
        uploader.HandleUploadConfirmation(yes: true);
    }

    private void OnClickNo()
    {
        Hide();
        uploader.HandleUploadConfirmation(yes: false);
    }


    public void Hide()
    {
        gameObject.SetActive(false);

        if (showConfirmAfterHide)
        {
            showConfirmAfterHide = false; // resetto subito per sicurezza
            ShowConfirmUpload();
        }
        else
        {
            CursorManager.Instance.HideCursor(); // << SOLO SE NON DEVO APRIRE SUBITO LA SUCCESSIVA
        }

    }


}
