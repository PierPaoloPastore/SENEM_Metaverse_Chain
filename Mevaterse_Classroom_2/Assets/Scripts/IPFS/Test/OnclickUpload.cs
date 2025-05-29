using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OnclickUpload : MonoBehaviour
{
    // Start is called before the first frame update

    public Button yourButton;
    public IpfsSample script;
    void Start()
    {
        Button btn = yourButton.GetComponent<Button>();
        btn.onClick.AddListener(TaskOnClick);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void TaskOnClick()
    {
        script.IPFSUploadImage();
        Debug.Log("You have clicked the button!");
    }

}
