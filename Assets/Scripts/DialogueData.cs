using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueNode
{
    public string id;
    public string text;
    public string nextId;
    public List<Choice> choices = new List<Choice>();
    public Rect rect;  // 添加Rect字段
}

[System.Serializable]
public class Choice
{
    public string text;
    public string targetNodeId;
}

[System.Serializable]
public class DialogueList
{
    public List<DialogueNode> nodes;
}
