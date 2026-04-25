namespace OrganizationProject.Core.Entities;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class TextModule
{
    private List<TextDocument> documents;
    private const int MaxDocuments = 150; //Our minimum is 100, so I just went 50% more

    //For the data holder to get and save all the documents
    public List<TextDocument> GetAllDocuments(){
        return documents;
    }
    public void SetAllDocuments(List<TextDocument> texts)
    {
        documents = texts;
    }

    //For the data holder to load all documents into a fresh module.
    public void SetDocuments(List<TextDocument> docs){
        documents = docs ?? new List<TextDocument>();
    }

    public TextModule()
    {
        //The list of documents for the module to have. We will only have one instantiated module, so this will be for the whole application
        documents = new List<TextDocument>();
    }

    //Adds a new document. Returns false if max documents reached or title exists
    public bool AddDocument(TextDocument doc)
    {
        if (doc == null) throw new ArgumentNullException(nameof(doc));
        if (documents.Count >= MaxDocuments) return false;
        if (documents.Exists(d => d.Title == doc.Title)) return false;

        documents.Add(doc);
        return true;
    }

    //Removes document by reference
    public void RemoveDocument(TextDocument doc)
    {
        if (doc == null) throw new ArgumentNullException(nameof(doc));
        documents.Remove(doc);
    }

    //Removes document by index
    public void RemoveDocument(int index)
    {
        if (index < 0 || index >= documents.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        documents.RemoveAt(index);
    }

    //Retrieves document by title. If the document doesn't exist, it returns null
    public TextDocument? GetDocument(string title)
    {
        return documents.Find(d => d.Title == title);
    }

    //Retrieves document by index.
    public TextDocument GetDocument(int index){
        if (index < 0 || index >= documents.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        return documents[index];
    }

    //List of documents
    public IReadOnlyList<TextDocument> Documents => documents;
}

//long-form document class
public class TextDocument
{
    public string Title { get; private set; }
    public string Content { get; private set; }
    public List<TextFormatting> Formatting { get; }
    public List<NoteAssignment> Notes { get; }

    public const int MaxLength = 1_500_000; //Our minimum was 1,000,000, so I went 50% more

    public TextDocument(string title)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Content = string.Empty;
        Formatting = new List<TextFormatting>();
        Notes = new List<NoteAssignment>();
    }

    //Text editing methods
    public void EditContent(string newContent) //This is for when the entire document is being replaced with text
    {
        if (newContent == null) throw new ArgumentNullException(nameof(newContent));
        if (newContent.Length > MaxLength) throw new ArgumentException("Document exceeds max length");
        Content = newContent;
        Formatting.Clear();
        Notes.Clear();
    }

    public void AppendText(string text) //Adding text to the end
    {
        if (text == null) throw new ArgumentNullException(nameof(text));
        if (Content.Length + text.Length > MaxLength)
            throw new ArgumentException("Document exceeds max length");
        Content += text;
    }

    public void InsertText(int index, string text) //Inserting text somewhere within the document
    {
        if (text == null) throw new ArgumentNullException(nameof(text));
        if (index < 0 || index > Content.Length) throw new ArgumentOutOfRangeException(nameof(index));
        if (Content.Length + text.Length > MaxLength)
            throw new ArgumentException("Document exceeds max length");
        Content = Content.Insert(index, text);
        AdjustRanges(index, text.Length, false); //New AdjustRanges method to keep indexes intact
    }

    public void DeleteText(int startIndex, int length)
    {
        if (startIndex < 0 || startIndex >= Content.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        if (length < 0 || startIndex + length > Content.Length)
            throw new ArgumentOutOfRangeException(nameof(length));
        Content = Content.Remove(startIndex, length);
        AdjustRanges(startIndex, length, true); //New AdjustRanges method to keep indexes intact
    }

    //Title editing
    public void EditTitle(string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle)) throw new ArgumentException("Empty title not allowed");
        Title = newTitle;
    }

    //Text formatting
    public void ApplyFormatting(int startIndex, int length, TextStyle style)
    {
        if (startIndex < 0 || startIndex + length > Content.Length)
            throw new ArgumentOutOfRangeException();
        Formatting.Add(new TextFormatting(startIndex, length, style));
    }

    public void AssignNote(int startIndex, int length, Note note)
    {
        if (note == null) throw new ArgumentNullException(nameof(note));
        if (startIndex < 0 || startIndex + length > Content.Length)
            throw new ArgumentOutOfRangeException();
        Notes.Add(new NoteAssignment(startIndex, length, note));
    }

    public void RemoveNote(Note note)
    {
        Notes.RemoveAll(c => c.AssignedNote == note);
        note.remove(this);
    }

    public void ClearAllNotes()
    {
        Notes.Clear();
    }

    public void AssignNoteByPattern(string pattern, Note note)
    {
        if (note == null) throw new ArgumentNullException(nameof(note));
        var matches = Regex.Matches(Content, pattern);
        foreach (Match match in matches)
            Notes.Add(new NoteAssignment(match.Index, match.Length, note));
    }

    //Handling insertions and deletions for notes and formatting
    //For where deletions intersect notes and formatting, we'll delete the formatting entirely, and notes will shift to where the deletion was.
    //When insertions are done within formatting or notes, we'll just expand the range.
    //Otherwise, everything will shift.

    private void AdjustRanges(int index, int changeLength, bool isDeletion)
    {
        //Formatting logic
        for (int i = Formatting.Count - 1; i >= 0; i--)
        {
            var f = Formatting[i];
            int start = f.StartIndex;
            int end = start + f.Length;

            if (isDeletion) //If things are being deleted, formatting is out
            {
                int deleteEnd = index + changeLength;

                if (end <= index) 
                {
                    //When our whole formatting is before the deletion, we do nothing
                }
                else if (deleteEnd <= start)
                {
                    //All formatting is after the delete, so we shift backwards
                    Formatting[i] = new TextFormatting(start - changeLength, f.Length, f.Style);
                }
                else //All other cases are overlap. So we're removing 
                {
                    Formatting.RemoveAt(i);
                }
            }
            else //Otherwise, shift as needed
            {
                if (index <= start)
                {
                    //Shift forward
                    Formatting[i] = new TextFormatting(start + changeLength, f.Length, f.Style);
                }
                else if (index < end)
                {
                    //Insert inside, so expand formatting
                    Formatting[i] = new TextFormatting(start, f.Length + changeLength, f.Style);
                }
            }
        }
        //Note logic
        for (int i = Notes.Count - 1; i >= 0; i--)
        {
            var c = Notes[i];
            int start = c.StartIndex;
            int end = start + c.Length;

            if (isDeletion)
            {
                int deleteEnd = index + changeLength;

                if (end <= index)
                {
                    //Note before deletion, do nothing
                }
                else if (deleteEnd <= start)
                {
                    //Note after deletion, so shift backward
                    Notes[i] = new NoteAssignment(start - changeLength, c.Length, c.AssignedNote);
                }
                else
                {
                    //Overlap
                    if (index <= start && deleteEnd >= end)
                    {
                        //Fully deleted, remove
                        Notes.RemoveAt(i);
                    }
                    else
                    {
                        //Partial deletion, move to deletion boundary
                        Notes[i] = new NoteAssignment(
                            index,
                            Math.Max(0, (end - changeLength) - index), //We have to shift the length so the end is still the same
                            c.AssignedNote);
                    }
                }
            }
            else
            {
                // FIX: Changed from "index <= start" to "index < start"
                // This prevents notes from extending when you type right after them
                if (index < start)
                {
                    //Insertion before the note - shift forward
                    Notes[i] = new NoteAssignment(start + changeLength, c.Length, c.AssignedNote);
                }
                else if (index < end)
                {
                    //Insert inside the note - expand it
                    Notes[i] = new NoteAssignment(start, c.Length + changeLength, c.AssignedNote);
                }
                //If index == start (insertion at the beginning) or index >= end (after the note), 
                //the note doesn't change. This prevents unwanted extension when typing after a note.
            }
        }
    }

}

//Formatting flags for text
public class TextFormatting
{
    public int StartIndex { get; }
    public int Length { get; }
    public TextStyle Style { get; }

    public TextFormatting(int start, int length, TextStyle style)
    {
        StartIndex = start;
        Length = length;
        Style = style;
    }
}

[Flags]
public enum TextStyle
{
    None = 0,
    Bold = 1,
    Italic = 2
}


// Maps a note to a text section
public class NoteAssignment
{
    public int StartIndex { get; }
    public int Length { get; }
    public Note AssignedNote { get; }

    public NoteAssignment(int startIndex, int length,  Note note)
    {
        StartIndex = startIndex;
        Length = length;
        AssignedNote = note ?? throw new ArgumentNullException(nameof(note));
    }
}