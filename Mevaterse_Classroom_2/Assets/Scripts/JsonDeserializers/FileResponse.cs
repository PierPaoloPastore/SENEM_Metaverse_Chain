using System;
using System.Collections.Generic;

[System.Serializable]
public class FileResponse
{
    public FileData data;
}

[System.Serializable]
public class FileData
{
    public List<PinataFile> files;
}

[System.Serializable]
public class PinataFile
{
    public string id;
    public string name;
    public string mime_type;
    public string cid;
    public string created_at;
}
