using System;
using System.Collections.Generic;

namespace OrganizationProject.Core.Entities
{
    public class Note
    {
        public Guid id { get; set; } = Guid.NewGuid();
        public string name { get; set; }
        public string description { get; set; } = "";
        public string color { get; set; } = "White";
        public List<ListModule> assignedLists { get; set; } = new();
        public List<TextDocument> assignedTexts { get; set; } = new();
        public List<Calendar> assignedCalendars { get; set; } = new();
        public Note(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty.");
            this.name = name;
            this.description = description;
            this.color = color;
            assignedLists = new List<ListModule>();
            assignedCalendars = new List<Calendar>();
            assignedTexts = new List<TextDocument>();
            DataHolder.unassignedNotesList.AddNote(this);
        }

        public Note()
        {
            assignedLists = new List<ListModule>();
            assignedTexts = new List<TextDocument>();
            assignedCalendars = new List<Calendar>();
        }

        //These will be called by the modules themselves so no duplicate protection is necessary
        public void assign(ListModule list)
        {
            if (!assignedLists.Contains(list))
            {
                assignedLists.Add(list);
            }

            // 🔥 THIS IS THE IMPORTANT LINE
            list.dataHolder?.UpdateUnassignedNotesList();
        }
        public void assign(Calendar calendar)
        {
            assignedCalendars.Add(calendar);
            checkAllModules();
        }
        public void assign(TextDocument text)
        {
            assignedTexts.Add(text);
        }

        public IReadOnlyList<ListModule> GetLists() => assignedLists.AsReadOnly();

        public void remove(ListModule list)
        {
            assignedLists.Remove(list);
            checkAllModules();
        }

        public void remove(Calendar calendar)
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
            assignedCalendars = new List<Calendar>();
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
            if (assignedLists.Count == 0 && assignedCalendars.Count == 0 && assignedTexts.Count == 0)
            {
                DataHolder.unassignedNotesList.AddNote(this);
            }
            else if (assignedLists.Contains(DataHolder.unassignedNotesList) && (assignedLists.Count >= 2 || assignedCalendars.Count >= 1 || assignedTexts.Count >= 1))
            {
                DataHolder.unassignedNotesList.RemoveNote(this);
            }
        }
    }


}
