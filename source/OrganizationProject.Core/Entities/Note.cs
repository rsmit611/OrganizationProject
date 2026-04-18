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

    //Notes will not have their own text module, so we instead of this
    //  public List<TextModule> assignedTexts { get; private set; }
    //We'll have it be a list of TextDocuments
    public List<TextDocument> assignedDocuments{ get; private set; }

    public Note(string name, string description="",string color="White")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.");

        this.name = name;
        this.description = description;
        this.color = color;
        assignedLists = new List<ListModule>();
        //assignedCalendars = new List<CalendarModule>();
        assignedDocuments = new List<TextDocument>();
        //DataHolder.unassignedNotesList.AddNote(this);
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
        assignedDocuments.Add(text);
        checkAllModules();
    }

    public void remove(ListModule list)
    {
        assignedLists.Remove(list);
        checkAllModules();
    }
    
    /*public void remove(CalendarModule calendar)
    {
        assignedCalendars.Remove(calendar);
        checkAllModules();
    }*/
    
    public void remove(TextDocument text)
    {
        assignedDocuments.Remove(text);
        checkAllModules();
    }

    //this is where a note will assign itself or remove itself from the list of unassigned notes
    //commented out because the DataHolder class needs to initialize and have static reference to the unassigned notes list before this will work
    private void checkAllModules()
    {
        ////are we in nothing? Then assign us to the unassigned list
        //if(assignedLists.Count==0/*&&assignedCalendars.Count==0*//*&&assignedDocuments.Count==0*/)
        //{
        //    DataHolder.unassignedNotesList.AddNote(this);
        //    unassigned = true;
        //}
        ////are we in something? besides the unassigned notes list
        //else if (assignedLists.Count >= (1+assignedLists.Contains(DataHolder.unassignedNotesList)?1:0)/*|| assignedCalendars.Count >= 1*//*||assignedDocuments.Count>=1*/)
        //{
        //    //if we are, mark us as not unassigned and remove us from that list
        //    unassigned = false;
        //    DataHolder.unassignedNotesList.remove(this);
        //}
    }

}