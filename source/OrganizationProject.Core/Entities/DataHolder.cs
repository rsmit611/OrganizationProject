using System;
using System.Collections.Generic;

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

        private readonly List<ListModule> allLists;
        private readonly List<Note> allNotes;
        private List<TextModule> allTextModules;

        // Uncomment when CalendarModule is added to the project.
        // private readonly List<CalendarModule> allCalendars;

        private readonly ListModule unassignedNotesList;

        public DataHolder()
        {
            timeSinceLastSave = 0f;
            timeSinceLastBackup = 0f;

            allLists = new List<ListModule>();
            allNotes = new List<Note>();
            allTextModules = new List<TextModule>(TextModule.GetTextModules());

            // allCalendars = new List<CalendarModule>();

            unassignedNotesList = new ListModule();
        }

        public IReadOnlyList<ListModule> AllLists => allLists.AsReadOnly();
        public IReadOnlyList<Note> AllNotes => allNotes.AsReadOnly();
        public IReadOnlyList<TextModule> AllTextModules => allTextModules.AsReadOnly();
        public ListModule UnassignedNotesList => unassignedNotesList;

        // public IReadOnlyList<CalendarModule> AllCalendars => allCalendars.AsReadOnly();

        public void save()
        {
            // TODO: Replace with real file persistence.
            timeSinceLastSave = 0f;
            allTextModules = new List<TextModule>(TextModule.GetTextModules());
        }

        public void load()
        {
            // TODO: Replace with real loading logic.
            TextModule.SetTextModules(allTextModules);
            updateUnassignedNotesList();
            timeSinceLastSave = 0f;
        }

        public void backup()
        {
            // TODO: Replace with real backup logic.
            timeSinceLastBackup = 0f;
        }

        public void addNote(Note note)
        {
            if (note == null) throw new ArgumentNullException(nameof(note));
            if (allNotes.Contains(note)) return;

            allNotes.Add(note);
            updateUnassignedNotesList();
        }

        public void removeNote(Note note)
        {
            if (note == null) throw new ArgumentNullException(nameof(note));
            if (!allNotes.Remove(note)) return;

            removeFromUnassignedListIfPresent(note);
        }

        public void addList(ListModule list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (ReferenceEquals(list, unassignedNotesList)) return;
            if (allLists.Contains(list)) return;

            allLists.Add(list);
            updateUnassignedNotesList();
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

            updateUnassignedNotesList();
        }

        public void addTextModule(TextModule textModule)
        {
            if (textModule == null) throw new ArgumentNullException(nameof(textModule));
            if (allTextModules.Contains(textModule)) return;

            allTextModules.Add(textModule);
            TextModule.SetTextModules(allTextModules);
            updateUnassignedNotesList();
        }

        public void removeTextModule(TextModule textModule)
        {
            if (textModule == null) throw new ArgumentNullException(nameof(textModule));
            if (!allTextModules.Remove(textModule)) return;

            foreach (var note in allNotes)
            {
                note.remove(textModule);
            }

            TextModule.SetTextModules(allTextModules);
            updateUnassignedNotesList();
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

        public void updateUnassignedNotesList()
        {
            foreach (var note in allNotes)
            {
                bool assignedToList = note.assignedLists.Count > 0;
                bool assignedToText = note.assignedTexts.Count > 0;
                // bool assignedToCalendar = note.assignedCalendars.Count > 0;

                bool isOnlyInUnassignedList =
                    assignedToList &&
                    note.assignedLists.Count == 1 &&
                    note.assignedLists.Contains(unassignedNotesList);

                bool shouldBeUnassigned = (!assignedToList && !assignedToText) || isOnlyInUnassignedList;
                bool isAlreadyInUnassignedList = note.assignedLists.Contains(unassignedNotesList);

                if (shouldBeUnassigned && !isAlreadyInUnassignedList)
                {
                    note.assign(unassignedNotesList);
                    unassignedNotesList.AddNote(note);
                }
                else if (!shouldBeUnassigned && isAlreadyInUnassignedList)
                {
                    removeFromUnassignedListIfPresent(note);
                }
            }
        }

        private void removeFromUnassignedListIfPresent(Note note)
        {
            if (!note.assignedLists.Contains(unassignedNotesList))
            {
                return;
            }

            // Keep the note's assignedLists in sync because ListModule.RemoveNote(note)
            // currently does not call note.remove(this).
            note.remove(unassignedNotesList);
            unassignedNotesList.RemoveNote(note);
        }
    }
}
