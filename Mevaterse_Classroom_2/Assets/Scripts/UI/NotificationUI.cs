using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NotificationUI : MonoBehaviour
{
    private TextMeshProUGUI notificationText;
    private Button closeButton;

    void Awake()
    {
        // Trova il TMP del messaggio
        notificationText = transform.Find("Notification_Text")?.GetComponent<TextMeshProUGUI>();

        // Trova il pulsante di chiusura
        closeButton = transform.Find("Notification_Close")?.GetComponent<Button>();    

        // Assicura che funzioni anche se elementi non trovati
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    public void Start()
    {
        Hide();
    }
    public void Show(string message)
    {
        if (notificationText != null)
            notificationText.text = message;

        gameObject.SetActive(true);
        CancelInvoke(); // annulla eventuali chiamate precedenti
        Invoke(nameof(Hide), 2f); // nasconde dopo 2 secondi
    }


    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
