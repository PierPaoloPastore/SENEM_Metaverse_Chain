using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class NotificationUI : MonoBehaviour
{
    private TextMeshProUGUI notificationText;
    private Button buttonYes;
    private Button buttonNo;

    private void Awake()
    {
        notificationText = transform.Find("Notification_Text")?.GetComponent<TextMeshProUGUI>();
        buttonYes = transform.Find("Button_Yes")?.GetComponent<Button>();
        buttonNo = transform.Find("Button_No")?.GetComponent<Button>();

        Hide();
    }

    /// <summary>
    /// Mostra una notifica semplice, si chiude automaticamente dopo 3 secondi.
    /// </summary>
    public void Show(string message)
    {
        if (notificationText != null)
            notificationText.text = message;

        buttonYes?.gameObject.SetActive(false);
        buttonNo?.gameObject.SetActive(false);
        gameObject.SetActive(true);

        CancelInvoke();
        Invoke(nameof(Hide), 3f);
    }

    /// <summary>
    /// Mostra una notifica con due pulsanti: SÌ / NO (ad esempio per validazione blockchain).
    /// </summary>
    public void ShowValidationPrompt(string message, Action onYes, Action onNo)
    {
        if (notificationText != null)
            notificationText.text = message;

        CancelInvoke();
        gameObject.SetActive(true);

        buttonYes?.gameObject.SetActive(true);
        buttonNo?.gameObject.SetActive(true);

        buttonYes?.onClick.RemoveAllListeners();
        buttonNo?.onClick.RemoveAllListeners();

        buttonYes?.onClick.AddListener(() =>
        {
            Hide();
            onYes?.Invoke();
        });

        buttonNo?.onClick.AddListener(() =>
        {
            Hide();
            onNo?.Invoke();
        });
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
