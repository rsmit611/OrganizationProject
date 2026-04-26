using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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


        public static ListModule unassignedNotesList = new ListModule();
        public ListModule? UnassignedNotesList { get; set; }
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
            UnassignedNotesList = unassignedNotesList;

            textModule = new TextModule(this);
        }

        public void UpdateUnassignedNotesList()
        {
            foreach (var note in allNotes)
            {
                bool hasRealListAssignment =
                    note.assignedLists != null &&
                    note.assignedLists.Any(list => !ReferenceEquals(list, unassignedNotesList));

                bool hasAssignments =
                    hasRealListAssignment ||
                    (note.assignedCalendars != null && note.assignedCalendars.Count > 0) ||
                    (note.assignedTexts != null && note.assignedTexts.Count > 0);

                var existingEntries = unassignedNotesList.Notes
                    .Where(n => n.note == note)
                    .ToList();

                if (!hasAssignments)
                {
                    if (!existingEntries.Any())
                    {
                        note.assign(unassignedNotesList);
                    }
                }
                else
                {
                    foreach (var entry in existingEntries)
                    {
                        unassignedNotesList.RemoveNote(note);
                    }

                    note.remove(unassignedNotesList);
                }
            }
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
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
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
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
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

            // Prevent duplicate reference
            if (allNotes.Any(n => n == note))
                return;

            allNotes.Add(note);

            UpdateUnassignedNotesList();
        }

        public void removeNote(Note note)
        {
            if (note == null) throw new ArgumentNullException(nameof(note));

            allNotes.Remove(note);

            // Remove from unassigned list if present
            if (unassignedNotesList.Notes.Any(n => n.note == note))
            {
                unassignedNotesList.RemoveNote(note);
            }
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

            UpdateUnassignedNotesList();
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

            UpdateUnassignedNotesList();
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

            UpdateUnassignedNotesList();
        }

    }
}