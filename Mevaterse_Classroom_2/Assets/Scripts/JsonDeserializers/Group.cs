using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Group
{
    public string id;
    public string name;
    public string created_at;
}

[System.Serializable]
public class GroupData
{
    public List<Group> groups;
    public string next_page_token;
}

[System.Serializable]
public class GroupResponse
{
    public GroupData data;
}
