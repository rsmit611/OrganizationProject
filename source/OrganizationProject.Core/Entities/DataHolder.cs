using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrganizationProject.Core.Entities
{
    public class DataHolder
    {
        private float timeSinceLastSave;
        private float timeSinceLastBackup;

        public List<ListModule> allLists;
        public List<Note> allNotes;
        public List<TextDocument> allTextDocuments;

        [JsonIgnore]
        public TextModule textModule;

        public List<Calendar> allCalendars; 


        public static ListModule unassignedNotesList;
        public ListModule? UnassignedNotesList;

        public int backupCycle = 0;
        public int maxBackups = 5;

        [JsonIgnore]
        public IReadOnlyList<ListModule> AllLists => allLists.AsReadOnly();
        [JsonIgnore]
        public IReadOnlyList<Note> AllNotes => allNotes.AsReadOnly();
        [JsonIgnore]
        public IReadOnlyList<TextDocument> AllTextDocuments => allTextDocuments.AsReadOnly();
        [JsonIgnore]
        public IReadOnlyList<Calendar> AllCalendars => allCalendars.AsReadOnly();


        public DataHolder()
        {
            timeSinceLastSave = 0f;
            timeSinceLastBackup = 0f;
            allLists = new List<ListModule>();
            allNotes = new List<Note>();
            allCalendars = new List<Calendar>();
            allTextDocuments = new List<TextDocument>();
            unassignedNotesList = new ListModule();
            textModule = new TextModule(this);
        }

        public void copy(DataHolder other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            timeSinceLastSave = other.timeSinceLastSave;
            timeSinceLastBackup = other.timeSinceLastBackup;

            allLists = other.allLists ?? new List<ListModule>();
            allNotes = other.allNotes ?? new List<Note>();
            allCalendars = other.allCalendars ?? new List<Calendar>();
            allTextDocuments = other.allTextDocuments ?? new List<TextDocument>();

            backupCycle = other.backupCycle;

            UnassignedNotesList = other.UnassignedNotesList;
            unassignedNotesList = UnassignedNotesList;
            maxBackups = other.maxBackups;

            textModule = new TextModule(this);
        }

        //replacing copy because I don't think it's working
        private void ApplyLoadedData(DataHolder loaded)
        {
            allNotes.Clear();
            foreach (var n in loaded.allNotes)
                allNotes.Add(n);

            allLists.Clear();
            foreach (var l in loaded.allLists)
                allLists.Add(l);

            allTextDocuments.Clear();
            foreach (var t in loaded.allTextDocuments)
                allTextDocuments.Add(t);

            allCalendars.Clear();
            foreach (var c in loaded.allCalendars)
                allCalendars.Add(c);

            UnassignedNotesList = loaded.UnassignedNotesList;
            unassignedNotesList = UnassignedNotesList;

            textModule = new TextModule(this);

            RebuildRelationships();
        }

        //Relationships are fun
        private void RebuildRelationships()
        {
            foreach (var note in allNotes)
            {
                if (note.assignedLists == null)
                    note.assignedLists = new();

                if (note.assignedTexts == null)
                    note.assignedTexts = new();

                if (note.assignedCalendars == null)
                    note.assignedCalendars = new();
            }
        }



        public void save()
        {
            timeSinceLastSave = 0f;
            UnassignedNotesList = unassignedNotesList;
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "OmniNote", "save.txt");

            string directory = Path.GetDirectoryName(path);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
                ReferenceHandler = ReferenceHandler.Preserve,
                WriteIndented = true
            };

            File.WriteAllText(path, JsonSerializer.Serialize(this, options));
        }

        public void load()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OmniNote", "save.txt");

            if (!File.Exists(path)){
                //Nothing exists to load.
            }else{
                load(path);
            }
        }

        public void load(string location)
        {
            var data = File.ReadAllText(location);
            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
                ReferenceHandler = ReferenceHandler.Preserve,
                WriteIndented = true
            };
            var loaded = JsonSerializer.Deserialize<DataHolder>(data, options);
            if (loaded != null)
            {
                ApplyLoadedData(loaded);
            }
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
            allTextDocuments.Add(doc);
        }

        public void removeTextDocument(TextDocument doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            allTextDocuments.Remove(doc);
            foreach (var note in allNotes)
                note.remove(doc);
        }

        public void addCalendar(Calendar calendar)
        {
            if (calendar == null) throw new ArgumentNullException(nameof (calendar));
            if (allCalendars.Count >= 100)
                throw new InvalidOperationException("Cannot create more than 100 calendars.");
            if (allCalendars.Contains(calendar)) return;
            allCalendars.Add(calendar);
        }
        public void removeCalendar(Calendar calendar)
        {
            if (calendar == null) throw new ArgumentNullException(nameof (calendar));   
            if (!allCalendars.Remove(calendar)) return;

            foreach(var calNote in calendar.Notes.ToList())
                calNote.Note.remove(calendar);
        }

    }
}