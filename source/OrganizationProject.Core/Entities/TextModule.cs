namespace OrganizationProject.Core.Entities;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class TextModule
{
    private List<TextDocument> documents;
    private const int MaxDocuments = 150; //Our minimum is 100, so I just went 50% more
    private static List<TextModule> TextModuleList {get; set; } = new(); //Will hold all instantiated modules for storage

    //For a data holder class to get all text modules
    public static List<TextModule> GetTextModules()
    {
        return TextModuleList;
    }

    //For a data holder to reset all text modules, will null protection
    public static void SetTextModules(List<TextModule> textModules){
        TextModuleList = textModules ?? new List<TextModule>();
    }

    public TextModule()
    {
        //The list of documents for the note to have
        documents = new List<TextDocument>();
        TextModuleList.Add(this);
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
    public List<CommentAssignment> Comments { get; }

    public const int MaxLength = 1_500_000; //Our minimum was 1,000,000, so I went 50% more

    public TextDocument(string title)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Content = string.Empty;
        Formatting = new List<TextFormatting>();
        Comments = new List<CommentAssignment>();
    }

    //Text editing methods
    public void EditContent(string newContent)
    {
        if (newContent == null) throw new ArgumentNullException(nameof(newContent));
        if (newContent.Length > MaxLength) throw new ArgumentException("Document exceeds max length");
        Content = newContent;
        Formatting.Clear();
        Comments.Clear();
    }

    public void AppendText(string text)
    {
        if (text == null) throw new ArgumentNullException(nameof(text));
        if (Content.Length + text.Length > MaxLength)
            throw new ArgumentException("Document exceeds max length");
        Content += text;
    }

    public void InsertText(int index, string text)
    {
        if (text == null) throw new ArgumentNullException(nameof(text));
        if (index < 0 || index > Content.Length) throw new ArgumentOutOfRangeException(nameof(index));
        if (Content.Length + text.Length > MaxLength)
            throw new ArgumentException("Document exceeds max length");
        Content = Content.Insert(index, text);
    }

    public void DeleteText(int startIndex, int length)
    {
        if (startIndex < 0 || startIndex >= Content.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        if (length < 0 || startIndex + length > Content.Length)
            throw new ArgumentOutOfRangeException(nameof(length));
        Content = Content.Remove(startIndex, length);
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

    public void AssignComment(int startIndex, int length, Comment comment)
    {
        if (comment == null) throw new ArgumentNullException(nameof(comment));
        if (startIndex < 0 || startIndex + length > Content.Length)
            throw new ArgumentOutOfRangeException();
        Comments.Add(new CommentAssignment(startIndex, length, comment));
    }

    public void RemoveComment(Comment comment)
    {
        Comments.RemoveAll(c => c.AssignedComment == comment);
    }

    public void ClearAllComments()
    {
        Comments.Clear();
    }

    public void AssignCommentByPattern(string pattern, Comment comment)
    {
        if (comment == null) throw new ArgumentNullException(nameof(comment));
        var matches = Regex.Matches(Content, pattern);
        foreach (Match match in matches)
            Comments.Add(new CommentAssignment(match.Index, match.Length, comment));
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

// Represents a comment on a section of text
public class Comment
{
    public string Author { get; init; }
    public string Text { get; set; }

    public Comment(string author, string text)
    {
        Author = author;
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }
}

// Maps a comment to a text section
public class CommentAssignment
{
    public int StartIndex { get; }
    public int Length { get; }
    public Comment AssignedComment { get; }

    public CommentAssignment(int startIndex, int length, Comment comment)
    {
        StartIndex = startIndex;
        Length = length;
        AssignedComment = comment ?? throw new ArgumentNullException(nameof(comment));
    }
}