using System;
using System.Collections.Generic;

namespace OrganizationProject.Core.Entities
{
    public class Note
    {
        public Guid id { get; init; } = Guid.NewGuid();
        public string name { get; set; }
        public string description { get; set; } = "";
        public string color { get; set; } = "White";
        public List<ListModule> assignedLists { get; private set; }
        public List<TextDocument> assignedTexts { get; private set; }
        public List<CalendarModule> assignedCalendars;
        public Note(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty.");
            this.name = name;
            this.description = description;
            this.color = color;
            assignedLists = new List<ListModule>();
            assignedCalendars = new List<CalendarModule>();
            assignedTexts = new List<TextDocument>();
            DataHolder.unassignedNotesList.AddNote(this);
        }

        //These will be called by the modules themselves so no duplicate protection is necessary
        public void assign(ListModule list)
        {
            assignedLists.Add(list);
            checkAllModules();
        }
        public void assign(CalendarModule calendar)
        {
            assignedCalendars.Add(calendar);
        }
        public void assign(TextDocument text)
        {
            assignedTexts.Add(text);
        }

        public void remove(ListModule list)
        {
            assignedLists.Remove(list);
            checkAllModules();
        }

        public void remove(CalendarModule calendar)
        {
            assignedCalendars.Remove(calendar);
            checkAllModules();
        }

        public void remove(TextDocument text)
        {
            assignedTexts.Remove(text);
            checkAllModules();
        }

        public Note(string name, string description = "", string color = "White")
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty.");

            this.name = name;
            this.description = description;
            this.color = color;
            assignedLists = new List<ListModule>();
            assignedTexts = new List<TextDocument>();
            DataHolder.unassignedNotesList.AddNote(this);
        }
        public void unassignAll()
        {
            while (assignedLists != null && assignedLists.Count > 0)
                assignedLists[0].RemoveNote(this);
            while (assignedTexts != null && assignedTexts.Count > 0)
                assignedTexts[0].RemoveNote(this);
            while (assignedCalendars != null && assignedCalendars.Count > 0)
                assignedCalendars[0].RemoveNote(this);
        }

        private void checkAllModules()
        {
            if (assignedLists.Count == 0)
            {
                DataHolder.unassignedNotesList.AddNote(this);
            }
            else if (assignedLists.Count >= 1 + (assignedLists.Contains(DataHolder.unassignedNotesList) ? 1 : 0))
            {
                DataHolder.unassignedNotesList.RemoveNote(this);
            }
        }
    }
}
