using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace OrganizationProject.Core.Entities
{
    /// <summary>
    /// Central in-memory storage class for OmniNote.
    /// Keeps track of notes and module instances and maintains the
    /// special list of notes that are not currently assigned to any module.
    /// </summary>
    public class DataHolder
    {
        private float timeSinceLastSave;
        private float timeSinceLastBackup;

        public List<ListModule> allLists;
        public List<Note> allNotes;
        public List<TextDocument> allTextModules =>textModule.GetAllDocuments();
        public TextModule textModule;
        // Uncomment when CalendarModule is added to the project.
        // private readonly List<CalendarModule> allCalendars;

        public static ListModule unassignedNotesList;

        public DataHolder()
        {
            timeSinceLastSave = 0f;
            timeSinceLastBackup = 0f;

            allLists = new List<ListModule>();
            allNotes = new List<Note>();
            //allTextModules = new List<TextModule>();

            // allCalendars = new List<CalendarModule>();

            unassignedNotesList = new ListModule();
            textModule=new TextModule();
        }
        //copy constructor
        public void copy(DataHolder other)
        {
            timeSinceLastSave = other.timeSinceLastSave;
            timeSinceLastBackup = other.timeSinceLastBackup;
            allLists = (List<ListModule>)other.AllLists;
            allNotes= (List<Note>)other.AllNotes;
            textModule.SetAllDocuments((List<TextDocument>) other.AllTextModules);
            backupCycle = other.backupCycle;
            unassignedNotesList = other.UnassignedNotesList;


        }
        public IReadOnlyList<ListModule> AllLists => allLists.AsReadOnly();
        public IReadOnlyList<Note> AllNotes => allNotes.AsReadOnly();
        public IReadOnlyList<TextDocument> AllTextModules => allTextModules.AsReadOnly();
        public ListModule UnassignedNotesList;

        // public IReadOnlyList<CalendarModule> AllCalendars => allCalendars.AsReadOnly();
        public int backupCycle=0;
        public int maxBackups = 5;
        public void save()
        {
            timeSinceLastSave = 0f;
            
            //assign the capital U unassigned notes list 
            UnassignedNotesList = unassignedNotesList;
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            path = Path.Combine(path, "OmniNote", "save.txt");
            File.WriteAllText(path,JsonSerializer.Serialize<DataHolder>(this));
        }

        public void load()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            path = Path.Combine(path, "OmniNote", "OmniSave.txt");
            load(path);
        }
        public void load(string location)
        {
            copy(JsonSerializer.Deserialize<DataHolder>(location));
            timeSinceLastSave = 0f;
        }
        public void backup()
        {
            timeSinceLastBackup = 0f;
            backupCycle++;
            //perform backup, backing it up to omninoteBackup(backupCycle)

            //assign the capital U unassigned notes list 
            UnassignedNotesList = unassignedNotesList;
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            path = Path.Combine(path, "OmniNote", "omninoteBackup"+backupCycle);
            File.WriteAllText(path, JsonSerializer.Serialize<DataHolder>(this));
            if (backupCycle>=maxBackups)
            {
                backupCycle = 0;
            }
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
            if (!allNotes.Remove(note)) return;

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
            {
                note.remove(list);
            }
        }

        public void addTextModule(TextModule textModule)
        {
            if (textModule == null) throw new ArgumentNullException(nameof(textModule));
            if (allTextModules.Contains(textModule)) return;

            allTextModules.Add(textModule);
            TextModule.SetTextModules(allTextModules);
        }

        public void removeTextModule(TextModule textModule)
        {
            if (textModule == null) throw new ArgumentNullException(nameof(textModule));
            if (!allTextModules.Remove(textModule)) return;

            foreach (var note in allNotes)
            {
                note.remove(textM);
            }

            TextModule.SetTextModules(allTextModules);
        }

        /*
        public void addCalendar(CalendarModule calendar)
        {
            if (calendar == null) throw new ArgumentNullException(nameof(calendar));
            if (allCalendars.Contains(calendar)) return;

            allCalendars.Add(calendar);
            updateUnassignedNotesList();
        }

        public void removeCalendar(CalendarModule calendar)
        {
            if (calendar == null) throw new ArgumentNullException(nameof(calendar));
            if (!allCalendars.Remove(calendar)) return;

            foreach (var note in allNotes)
            {
                note.remove(calendar);
            }

            updateUnassignedNotesList();
        }
        */
    }
}
