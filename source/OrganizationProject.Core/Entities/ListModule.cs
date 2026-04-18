using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrganizationProject.Core.Entities
{
    public class ListModule
    {
        List<ListNote> notes;
        public ListModule()
        {
            notes = new List<ListNote>();
        }
        //takes in a note, returns whether it was added or not
        //if it's not added, that's because it's already in the list, so note that in the UI
        public bool AddNote(Note note)
        {
            //check if the note is already in the list
            for(int i = 0;i<notes.Count;i++)
            {
                if (note == notes[i].note)
                {
                    return false;
                }
            }
            //wrap the note and put it at the end of the list
            notes.Add(new ListNote(note));
            //added succesfully, so return true
            return true;
        }
        //Removes a note at index. throws an argument exception if index is out of range(note, that shouldn't ever happen)
        public void RemoveNote(int noteIndex)
        {
            if (noteIndex >= notes.Count || noteIndex < 0) { throw new ArgumentException("Remove note: index out of range"); }
            notes[noteIndex].note.remove(this);
            notes.RemoveAt(noteIndex);
        }
        //removes note by note reference
        public void RemoveNote(Note note)
        {
            if (note == null) { throw new ArgumentNullException("Passed null note into remove note"); }
            //iterate over our notes, call remove note on the first match found and return
            for (int i = 0; i < notes.Count; i++)
            {
                if (notes[i].note == note)
                {
                    notes[i].note.remove(this);
                    notes.RemoveAt(i);
                    return;
                }
            }
        }
        //Takes a note from one position and puts it in another. For moving notes.
        public void moveTo(int fromIndex, int toIndex)
        {
            //don't do anything if indexes are the same
            if (fromIndex == toIndex) { return; }
            //copy the note from the index to the index
            notes[toIndex] = notes[fromIndex];
            //remove at the old index. Add one if the to index is before from index so we hit the right one
            notes.RemoveAt(toIndex + fromIndex > toIndex ? 1 : 0);
        }
        //changes priority at index. throws an argument exception if index is out of range(note, that shouldn't ever happen)
        public void ChangePriority(int noteIndex, ListPriority priority)
        {
            if (noteIndex >= notes.Count || noteIndex < 0) { throw new ArgumentException("Remove note: index out of range"); }
            notes[noteIndex].priority = priority;
        }
    }
    //note: I'm not using polymorphism to avoid data duplication. No need to copy the same description 5 times when we can just have reference to the note
    //I'm also not implementing these as protected classes within list module so cross module functionality can more easily be done.
    //has reference to base note and a priority
    public class ListNote
    {
        public Note note;
        public ListPriority priority=ListPriority.None;
        public ListNote(Note baseNote)
        {
            note = baseNote;
        }
    }
    public enum ListPriority
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }
}
