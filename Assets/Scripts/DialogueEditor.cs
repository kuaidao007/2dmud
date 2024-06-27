using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class DialogueEditor : EditorWindow
{
    private static List<DialogueNode> nodes = new List<DialogueNode>();
    private Vector2 scrollPosition;
    private DialogueNode selectedNode;
    private Vector2 offset;
    private DialogueNode resizingNode;
    private bool draggingNode;
    private bool rightMouseDragging; // 右键拖拽标志
    private Vector2 rightMouseDragStart; // 右键拖拽开始位置
    private const float HandleSize = 10f;

    [MenuItem("Window/Dialogue Editor/Window")]
    public static void ShowWindow()
    {
        GetWindow<DialogueEditor>("Dialogue Editor");
    }

    [MenuItem("Window/Dialogue Editor/Save Dialogue")]
    public static void SaveDialogue()
    {
        string path = EditorUtility.SaveFilePanel("Save Dialogue JSON", Application.dataPath, "dialogue.json", "json");
        if (!string.IsNullOrEmpty(path))
        {
            DialogueList dialogueList = new DialogueList { nodes = nodes };
            string json = JsonUtility.ToJson(dialogueList, true);
            File.WriteAllText(path, json);
        
            Debug.Log("Dialogue saved successfully to " + path);
        }
        else
        {
            Debug.LogWarning("No file selected or invalid file path.");
        }
    }


    [MenuItem("Window/Dialogue Editor/Load Dialogue")]
    public static void LoadDialogue()
    {
        string path = EditorUtility.OpenFilePanel("Select Dialogue JSON", Application.dataPath, "json");
        if (!string.IsNullOrEmpty(path))
        {
            string json = File.ReadAllText(path);

            DialogueList dialogueList = JsonUtility.FromJson<DialogueList>(json);
            nodes = dialogueList.nodes;

            // Optionally, you can log the nodes to confirm they've been loaded correctly
            foreach (var node in nodes)
            {
                Debug.Log($"Node ID: {node.id}, Text: {node.text}");
            }
        }
        else
        {
            Debug.LogWarning("No file selected or invalid file path.");
        }
    }

    private void OnGUI()
    {
        HandleEvents(); // 在BeginScrollView之前处理事件

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        BeginWindows();
        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i].rect = GUILayout.Window(i, nodes[i].rect, DrawNodeWindow, nodes[i].id);
        }
        EndWindows();

        foreach (var node in nodes)
        {
            foreach (var choice in node.choices)
            {
                if (!string.IsNullOrEmpty(choice.targetNodeId))
                {
                    DialogueNode targetNode = nodes.Find(n => n.id == choice.targetNodeId);
                    if (targetNode != null)
                    {
                        Rect choiceRect = new Rect(node.rect.x, node.rect.y + 70 + node.choices.IndexOf(choice) * 20, node.rect.width, 20);
                        Rect removeButtonRect = new Rect(node.rect.x + node.rect.width - 85, node.rect.y + 70 + node.choices.IndexOf(choice) * 20, 75, 20);
                        DrawChoiceCurve(node.rect, targetNode.rect, choiceRect, removeButtonRect);
                    }
                }
            }
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Add Node"))
        {
            nodes.Add(new DialogueNode
            {
                id = System.Guid.NewGuid().ToString(),
                text = "New Node",
                rect = new Rect(10, 10, 200, 100)
            });
        }
    }

    private void DrawNodeWindow(int id)
    {
        DialogueNode node = nodes[id];
        node.text = EditorGUILayout.TextArea(node.text, GUILayout.Height(50));

        foreach (var choice in node.choices)
        {
            GUILayout.BeginHorizontal();
            choice.text = EditorGUILayout.TextField("Choice Text", choice.text);
            choice.targetNodeId = EditorGUILayout.TextField("Target Node ID", choice.targetNodeId);
            if (GUILayout.Button("Remove Choice", GUILayout.Width(75)))
            {
                node.choices.Remove(choice);
                break;
            }
            GUILayout.EndHorizontal();
        }
        node.nextId = EditorGUILayout.TextField("Choice Text", node.nextId);

        if (GUILayout.Button("Add Choice"))
        {
            node.choices.Add(new Choice { text = "New Choice", targetNodeId = "" });
        }

        GUI.DragWindow(new Rect(0, 0, node.rect.width - HandleSize, node.rect.height - HandleSize));

        Rect resizeHandleRect = new Rect(node.rect.width - HandleSize, node.rect.height - HandleSize, HandleSize, HandleSize);
        GUI.DrawTexture(resizeHandleRect, EditorGUIUtility.whiteTexture);
        EditorGUIUtility.AddCursorRect(resizeHandleRect, MouseCursor.ResizeUpLeft);
        if (Event.current.type == EventType.MouseDown && resizeHandleRect.Contains(Event.current.mousePosition))
        {
            resizingNode = node;
            Event.current.Use();  // 捕获鼠标事件以避免传播
        }
        if (Event.current.type == EventType.MouseUp)
        {
            resizingNode = null;
        }
        if (resizingNode == node && Event.current.type == EventType.MouseDrag)
        {
            node.rect.width = Mathf.Max(100, Event.current.mousePosition.x - node.rect.x);
            node.rect.height = Mathf.Max(50, Event.current.mousePosition.y - node.rect.y);
            Repaint();
        }
    }

    private void HandleEvents()
    {
        Event e = Event.current;

        if (e.button == 0 && e.type == EventType.MouseDown && resizingNode == null)
        {
            selectedNode = GetNodeAtPoint(e.mousePosition + scrollPosition);
            if (selectedNode != null)
            {
                offset = selectedNode.rect.position - e.mousePosition;
                draggingNode = true;
            }
        }

        if (e.button == 0 && e.type == EventType.MouseDrag && draggingNode && selectedNode != null)
        {
            selectedNode.rect.position = e.mousePosition + offset;
            Repaint();
        }

        if (e.button == 0 && e.type == EventType.MouseUp)
        {
            draggingNode = false;
            selectedNode = null;
        }

        if (e.button == 1 && e.type == EventType.MouseDown)
        {
            rightMouseDragging = true;
            rightMouseDragStart = e.mousePosition;
            Repaint();
        }

        if (e.button == 1 && e.type == EventType.MouseDrag && rightMouseDragging)
        {
            Vector2 dragOffset = e.mousePosition - rightMouseDragStart;
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].rect.position += dragOffset;
            }
            rightMouseDragStart = e.mousePosition;
            Repaint();
        }

        if (e.button == 1 && e.type == EventType.MouseUp)
        {
            rightMouseDragging = false;
            Repaint();
        }

        if (e.type == EventType.MouseDrag && resizingNode != null)
        {
            resizingNode.rect.width = Mathf.Max(100, e.mousePosition.x - resizingNode.rect.x);
            resizingNode.rect.height = Mathf.Max(50, e.mousePosition.y - resizingNode.rect.y);
            Repaint();
        }
    }

    private DialogueNode GetNodeAtPoint(Vector2 point)
    {
        foreach (var node in nodes)
        {
            if (node.rect.Contains(point))
            {
                return node;
            }
        }
        return null;
    }

    private void DrawNodeCurve(Rect start, Rect end)
    {
        Vector3 startPos = new Vector3(start.x + start.width, start.y + start.height / 2, 0);
        Vector3 endPos = new Vector3(end.x, end.y + end.height / 2, 0);
        Vector3 startTan = startPos + Vector3.right * 50;
        Vector3 endTan = endPos + Vector3.left * 50;
        Handles.DrawBezier(startPos, endPos, startTan, endTan, Color.black, null, 3);
    }
    
    private void DrawChoiceCurve(Rect start, Rect end, Rect choiceRect, Rect removeButtonRect)
    {
        Vector3 startPos = new Vector3(removeButtonRect.x + removeButtonRect.width / 2, removeButtonRect.y + removeButtonRect.height / 2, 0);
        Vector3 endPos = new Vector3(end.x, end.y + end.height / 2, 0);
        Vector3 startTan = startPos + Vector3.right * 50;
        Vector3 endTan = endPos + Vector3.left * 50;
        Handles.DrawBezier(startPos, endPos, startTan, endTan, Color.black, null, 3);
    }

}