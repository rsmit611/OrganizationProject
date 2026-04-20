using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrganizationProject.Core.Entities
{
    public class Calendar
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; set; }
        public DateTime Created { get; } = DateTime.Now;
        public List<CalendarNote> Notes { get; } = new();

        public Calendar(string name)
        {
            Name = name;
        }

        public bool AddNote(Note note, DateTime? date, TimeSpan? time, RepeatingType? repeating = null)
        {
            if (Notes.Any(cn => cn.Note == note))
                return false;

            var calendarNote = new CalendarNote(note, date, time, repeating);
            Notes.Add(calendarNote);
            note.Assign(this); //Assign the calendar to the note 
            return true;
        }

        public void RemoveNote(Note note)
        {
            var calendarNote = Notes.FirstOrDefault(cn => cn.Note == note);
            if (calendarNote != null)
            {
                Notes.Remove(calendarNote);
                note.Remove(this);
            }
        }
        public IEnumerabke<CalendarNote> GetNotesOrganizedByDateTime()
        {
            return Notes.OrderBy(cn => cn.Date).ThenBy(cn => cn.Time);
        }
    }


    public class CalendarNote
    {
        public Guid Id { get; } = Guid.NewGuid();
        public Note Note { get; }
        public DateTime? Date { get; set; }  //null = date/only note
        public TimeSpan? Time { get; set; }  // null = no time assigned 
        public bool IsScheduled { get; set; } = false;
        public Repeating? Repeating { get; private set; }

        public CalendarNote(Note baseNote, DateTime? date, TimeSpan? time, RepeatingType? repeatingType = null)
        {
            Note = baseNote;
            Date = date;
            Time = time;


            if (repeatingType.HasValue)
            {
                SetRepeatingSchedule(repeatingType.Value);
            }
        }

        public void ScheduleNotification(DateTime notifyDate, TimeSpan notifyTime)
        {
            IsScheduled = true;
            NotificationDate = notifyDate;
            NotificationTime = notifyTime;
        }

        public void CancelNotification()
        {
            IsScheduled = false;
            NotificationDate = null;
            NotificationTime = null;
        }

    }
    public void SetRepeatingSchedyle(RepeatingType type, List<DayOfWeek>? daysOfWeek = null, int interval = 1)
    {
        Repeating = new Repeating
        {
            Type = type,
            daysOfWeek = daysOfWeek ?? new List<DayOfWeek>(),
            interval = interval
        };
    }

    public void RemoveRepeatingSchedule()
    {
        Repeating = null;
    }

    public class Repeating
    {
        public RepeatingType Type { get; set; }
        public List<DayOfWeek> daysOfWeek { get; set; } = new(); // For SpecificDays type
        public int interval { get; set; } = 1; // For Weekly, Monthly, Yearly types
    }


    public enum RepeatingType { Daily, SpecificDays, Weekly, Monthly, Yearly }

    public class CalendarRequirements
    {
        //shows list as readonly view, loops through calendars and displays them but cant modify. 
        private const int MaxCalendars = 100;
        private readonly List<Calendar> _calendars = new();

        public IReadOnlyList<Calendar> Calendars => _calendars.AsReadOnly();

        public class CreateManager
        {
            //checks calendars limit if its at 100 
            private const int MinimumCapictyRequired = 100;
            private readonly List<Calendar> _calendars = new();

            public IReadOnlyListCalendar(string name)
            {
                var calendars = new Calendar(name);
                _calendars.Add(calendar);
                return calendar;
            }

            
        }

        public void DeleteCalendar(Guid id)
        {
            var cal = _calendars.FirstOrDefault(cal => cal.Id == id)
            ?? throw new KeyNotFoundException(); //checks if calendar exists so it can delete it. if no match throws exception 


            foreach (var calendarNote in cal.Notes.ToList())
            {
                calendarNote.note.Remove(cal); //Sets notes calendar id to empty so they dont belong to a calendar but arent deleted. 
            }

            _calendars.Remove(cal); //Remvoes the calendar object from the list. 
        }

        public bool AddNote(Guid calendarId, Note note, DateTime? date, TimeSpan? time,
            RepeatingType? repeating = null)
        {
            var cal = _calendars.FirstOrDefault(c => c.Id == calendarId)
                ?? throw new KeyNotFoundException("Calendar not found.");

            return cal.AddNote(note, date, time, repeating);
        }

    }
}




