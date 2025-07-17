//Classe il cui unico scopo è fare declutter della console
using UnityEngine;

public class WalletConnectLogFilter : MonoBehaviour, ILogHandler
{
    [Tooltip("Se disabilitato, mostra di nuovo tutti i log di WalletConnect")]
    public bool abilitato = true;

    private ILogHandler originalHandler;

    void Awake()
    {
        originalHandler = Debug.unityLogger.logHandler;
        Debug.unityLogger.logHandler = this;
    }

    public void LogFormat(LogType logType, Object context, string format, params object[] args)
    {
        string message = string.Format(format, args);

        if (abilitato && message.Contains("[WalletConnect SDK]"))
            return;

        originalHandler.LogFormat(logType, context, format, args);
    }

    public void LogException(System.Exception exception, Object context)
    {
        originalHandler.LogException(exception, context);
    }

    void OnDestroy()
    {
        if (Debug.unityLogger.logHandler == this)
            Debug.unityLogger.logHandler = originalHandler;
    }
}
