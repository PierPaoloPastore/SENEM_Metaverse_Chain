using UnityEngine;
using UnityEngine.UI;

public class TestLessonRegister : MonoBehaviour
{
    public Button testButton;

    private async void Start()
    {
        testButton.onClick.AddListener(async () =>
        {
            var handler = FindObjectOfType<LessonRegistryHandler>();
            if (handler != null && handler.IsReady())
            {
                string testName = "Lezione 1";

                var result = await handler.TryGetLesson(testName);

                if (result.found)
                {
                    Debug.Log("La lezione esiste già. CID: " + result.cid);
                }
                else
                {
                    string testCID = "017c29cd-70bc-4049-891d-186df09a9711";
                    bool success = await handler.RegisterLesson(testName, testCID);
                    Debug.Log("Chiamata RegisterLesson completata con esito: " + success);
                }
            }
            else
            {
                Debug.LogWarning("Contratto non inizializzato o handler mancante.");
            }
        });
    }

}
