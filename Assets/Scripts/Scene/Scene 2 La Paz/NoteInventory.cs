using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NoteData
{
    public string noteContent;
    public Sprite noteSprite;
}

public class NoteInventory : MonoBehaviour
{
    public static NoteInventory instance;
    public List<NoteData> collectedNotes = new List<NoteData>();

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    public void AddNote(string content, Sprite sprite)
    {
        NoteData newNote = new NoteData
        {
            noteContent = content,
            noteSprite = sprite
        };

        collectedNotes.Add(newNote);
        Debug.Log($"Nota añadida al inventario: {content.Substring(0, Mathf.Min(20, content.Length))}...");
    }

    public List<NoteData> GetNotes()
    {
        return collectedNotes;
    }
}
