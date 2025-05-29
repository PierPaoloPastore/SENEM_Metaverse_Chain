using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Group
{
    public string id;
    public string name;
    public string created_at;
}

[Serializable]
public class GroupData
{
    public List<Group> groups;
    public string next_page_token;
}

[Serializable]
public class GroupResponse
{
    public GroupData data;
}

[Serializable]
public class CreatedGroupResponse
{
    public Group data;
}

[Serializable]
public class PinataFile
{
    public string id;
    public string name;
    public string mime_type;
    public string cid;
    public string created_at;
}

[Serializable]
public class FileData
{
    public List<PinataFile> files;
}

[Serializable]
public class FileResponse
{
    public FileData data;
}
