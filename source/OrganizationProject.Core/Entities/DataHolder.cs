using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace OrganizationProject.Core.Entities
{
    public class DataHolder
    {
        private float timeSinceLastSave;
        private float timeSinceLastBackup;

        public List<ListModule> allLists;
        public List<Note> allNotes;
        public List<TextDocument> allTextModules => textModule.GetAllDocuments();
        public TextModule textModule;

        public static ListModule unassignedNotesList;
        public ListModule? UnassignedNotesList;

        public int backupCycle = 0;
        public int maxBackups = 5;

        public DataHolder()
        {
            timeSinceLastSave = 0f;
            timeSinceLastBackup = 0f;
            allLists = new List<ListModule>();
            allNotes = new List<Note>();
            unassignedNotesList = new ListModule();
            textModule = new TextModule();
        }

        public void copy(DataHolder other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            timeSinceLastSave = other.timeSinceLastSave;
            timeSinceLastBackup = other.timeSinceLastBackup;
            allLists = (List<ListModule>)other.AllLists;
            allNotes = (List<Note>)other.AllNotes;
            textModule.SetAllDocuments((List<TextDocument>)other.AllTextModules);
            backupCycle = other.backupCycle;
            UnassignedNotesList = other.UnassignedNotesList;
        }

        public IReadOnlyList<ListModule> AllLists => allLists.AsReadOnly();
        public IReadOnlyList<Note> AllNotes => allNotes.AsReadOnly();
        public IReadOnlyList<TextDocument> AllTextModules => allTextModules.AsReadOnly();

        public void save()
        {
            timeSinceLastSave = 0f;
            UnassignedNotesList = unassignedNotesList;
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OmniNote", "save.txt");
            File.WriteAllText(path, JsonSerializer.Serialize<DataHolder>(this));
        }

        public void load()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OmniNote", "OmniSave.txt");
            load(path);
        }

        public void load(string location)
        {
            var loaded = JsonSerializer.Deserialize<DataHolder>(location);
            if (loaded != null) copy(loaded);
            timeSinceLastSave = 0f;
        }

        public void backup()
        {
            timeSinceLastBackup = 0f;
            backupCycle++;
            UnassignedNotesList = unassignedNotesList;
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OmniNote", "omninoteBackup" + backupCycle);
            File.WriteAllText(path, JsonSerializer.Serialize<DataHolder>(this));
            if (backupCycle >= maxBackups) backupCycle = 0;
        }

        public void addNote(Note note)
        {
            if (note == null) throw new ArgumentNullException(nameof(note));
            if (allNotes.Contains(note)) return;
            allNotes.Add(note);
        }

        public void removeNote(Note note)
        {
            if (note == null) throw new ArgumentNullException(nameof(note));
            allNotes.Remove(note);
        }

        public void addList(ListModule list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (ReferenceEquals(list, unassignedNotesList)) return;
            if (allLists.Contains(list)) return;
            allLists.Add(list);
        }

        public void removeList(ListModule list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (ReferenceEquals(list, unassignedNotesList)) return;
            if (!allLists.Remove(list)) return;
            foreach (var note in allNotes)
                note.remove(list);
        }

        // ── TextDocument methods (not TextModule) ──
        public void addTextDocument(TextDocument doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            textModule.AddDocument(doc);
        }

        public void removeTextDocument(TextDocument doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            textModule.RemoveDocument(doc);
            foreach (var note in allNotes)
                note.remove(doc);
        }
    }
}