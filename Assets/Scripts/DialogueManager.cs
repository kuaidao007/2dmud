using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DialogueManager : MonoBehaviour
{
    public TextAsset dialogueJson;
    public Text dialogueText;
    public GameObject choicesPanel;
    public Button choiceButtonPrefab;

    private List<DialogueNode> dialogues;
    private DialogueNode currentDialogue;

    void Start()
    {
        dialogues = JsonUtility.FromJson<DialogueList>(dialogueJson.text).nodes;
        StartDialogue("1"); // 从对话ID 1 开始
        AddClickEvent(dialogueText);
    }

    public void StartDialogue(string id)
    {
        currentDialogue = dialogues.Find(d => d.id == id);
        DisplayDialogue();
    }

    void DisplayDialogue()
    {
        dialogueText.text = currentDialogue.text;
        foreach (Transform child in choicesPanel.transform)
        {
            Destroy(child.gameObject);
        }

        dialogueText.text += "\n\n(点击继续)";
    }

    public void AddClickEvent(Text text)
    {
        EventTrigger trigger = text.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = text.gameObject.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((eventData) => { DisplayChoicesOrContinue(); });
        trigger.triggers.Add(entry);
    }

    void DisplayChoicesOrContinue()
    {
        if (currentDialogue.choices.Count == 0)
        {
            Debug.Log(currentDialogue.nextId);
            // 如果没有选项，直接继续到下一个对话节点
            if (!string.IsNullOrEmpty(currentDialogue.nextId))
            {
                StartDialogue(currentDialogue.nextId);
            }
        }
        else
        {
            // 如果有选项，显示选项按钮
            for (int i = 0; i < currentDialogue.choices.Count; i++)
            {
                var choice = currentDialogue.choices[i];
                Button choiceButton = Instantiate(choiceButtonPrefab, choicesPanel.transform);
                choiceButton.GetComponentInChildren<Text>().text = choice.text;
                int choiceIndex = i; // capture index for use in the lambda
                choiceButton.onClick.AddListener(() => MakeChoice(choiceIndex));
            }
        }
    }

    public void MakeChoice(int choiceIndex)
    {
        string nextId = currentDialogue.choices[choiceIndex].targetNodeId;
        StartDialogue(nextId);
    }
}

