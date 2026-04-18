namespace OrganizationProject.Core.Entities;

public class Note
{
    public Guid id { get; init; } = Guid.NewGuid();
    public string name { get; set; }
    public string description { get; set; } = "";
    public string color { get; set; } = "White";
    public List<ListModule> assignedLists { get; private set; }
    bool unassigned = true;
    //public List<CalendarModule> assignedCalendars { get; private set; }
    public List<TextDocument> assignedTexts { get; private set; }
    public Note(string name, string description="",string color="White")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.");

        this.name = name;
        this.description = description;
        this.color = color;
        assignedLists = new List<ListModule>();
        //assignedCalendars = new List<CalendarModule>();
        assignedTexts = new List<TextDocument>();
        DataHolder.unassignedNotesList.AddNote(this);
    }
    //These will be called by the modules themselves so no duplicate protection is necessary
    public void assign(ListModule list)
    {
        assignedLists.Add(list);
        checkAllModules();
    }
    /*public void assign(CalendarModule calendar)
    {
        assignedCalendars.Add(calendar);
    }*/
    public void assign(TextDocument text)
    {
        if(!assignedTexts.Contains(text)) assignedTexts.Add(text);
        checkAllModules();
    }

    public void remove(ListModule list)
    {
        assignedLists.Remove(list);
        checkAllModules();
    }
    public void unassignAll()
    {
        //note, this will create an infinite loop if a note somehow thinks it is assigned to a module while it is in reality not
        while (assignedLists != null && assignedLists.Count > 0)
        {
            assignedLists[0].RemoveNote(this);
        }
        //while (assignedTexts != null && assignedTexts.Count > 0)
        //{
        //    assignedTexts[0].RemoveNote(this);
        //}
        //while (assignedCalendars != null && assignedCalendars.Count > 0)
        //{
        //    assignedCalendars[0].RemoveNote(this);
        //}
    }
    /*public void remove(CalendarModule calendar)
    {
        assignedCalendars.Remove(calendar);
        checkAllModules();
    }*/
    public void remove(TextDocument text)
    {
        assignedTexts.Remove(text);
        checkAllModules();
    }

    //this is where a note will assign itself or remove itself from the list of unassigned notes
    //commented out because the DataHolder class needs to initialize and have static reference to the unassigned notes list before this will work
    private void checkAllModules()
    {
        //are we in nothing? Then assign us to the unassigned list
        if (assignedLists.Count == 0/*&&assignedCalendars.Count==0*//*&&assignedTexts.Count==0*/)
        {
            DataHolder.unassignedNotesList.AddNote(this);
            unassigned = true;
        }
        //are we in something? besides the unassigned notes list
        else if (assignedLists.Count >= 1 + (assignedLists.Contains(DataHolder.unassignedNotesList) ? 1 : 0)/*|| assignedCalendars.Count >= 1*//*||assignedTexts.Count>=1*/)
        {
            //if we are, mark us as not unassigned and remove us from that list
            unassigned = false;
            DataHolder.unassignedNotesList.RemoveNote(this);
        }
    }

}