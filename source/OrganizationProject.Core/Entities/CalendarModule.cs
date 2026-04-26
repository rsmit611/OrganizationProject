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

        private readonly HashSet<Guid> _hiddenListIds = new(); //toggle visibility of lists in calendar.

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
            note.assign(this); //Assign the calendar to the note 
            return true;
        }

        public void RemoveNote(Note note)
        {
            var calendarNote = Notes.FirstOrDefault(cn => cn.Note == note);
            if (calendarNote != null)
            {
                Notes.Remove(calendarNote);
                if (calendarNote.IsScheduled)
                {
                    calendarNote.CancelNotification();
                }//Cancels notification if one is set for the note.
                note.remove(this);
            }
        }
        public IEnumerable<CalendarNote> GetNotesOrganizedByDateTime()
        {
            return Notes.OrderBy(cn => cn.Date).ThenBy(cn => cn.Time);
        }

        public IEnumerable<CalendarNote> GetNotesInRange(DateTime startDate, DateTime endDate)
        {
            return Notes.Where(cn => cn.Date.HasValue && cn.Date.Value.Date >=
            startDate.Date && cn.Date.Value.Date <= endDate.Date)
                .OrderBy(cn => cn.Date).ThenBy(cn => cn.Time);
        }

        //List Visisblity 
        // Hides notes belonging to a specific list 
        public void HideList(Guid listId) => _hiddenListIds.Add(listId);
        //shows notes belonging to a specific list
        public void ShowList(Guid listId) => _hiddenListIds.Remove(listId);

        public void HideAllLists(IEnumerable<ListModule> allLists)
        {

            foreach (var list in allLists)
            {
                _hiddenListIds.Add(list.Id);
            }
        }
        //shows list at once 
        public void ShowAllLists() => _hiddenListIds.Clear();

        public IEnumerable<CalendarNote> GetVisibleNotes(IEnumerable<ListModule> allLists)
        {
            return Notes.Where(cn =>
            {
                var noteLists = cn.Note.GetLists();
                if (!noteLists.Any()) return true;

                return noteLists.Any(l => !_hiddenListIds.Contains(l.Id));
            });
        }
        public bool IsListHidden(Guid listId) => _hiddenListIds.Contains(listId);

    }




    public class CalendarNote
    {
        public Guid Id { get; } = Guid.NewGuid();
        public Note Note { get; }
        public DateTime? Date { get; set; }  //null = date/only note
        public TimeSpan? Time { get; set; }  // null = no time assigned 
        public bool IsScheduled { get; set; } = false;
        public Repeating? Repeating { get; private set; }
        public DateTime? NotificationDate { get; set; }
        public TimeSpan? NotificationTime { get; set; }

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
        // Date and Time 
        public void ChangeDate(DateTime newDate) => Date = newDate;
        public void ChangeTime(TimeSpan newTime) => Time = newTime;

        public void RemoveTime() => Time = null;

        public void RemoveDateTime()
        {
            Date = null;
            Time = null;
            //warn user when date/time is removed 

            if (IsScheduled)
            {
                CancelNotification();
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


        public void SetRepeatingSchedule(RepeatingType type, List<DayOfWeek>? daysOfWeek = null, int interval = 1)
        {
            Repeating = new Repeating
            {
                Type = type,
                DaysOfWeek = daysOfWeek ?? new List<DayOfWeek>(),
                Interval = interval
            };
        }

        public void RemoveRepeatingSchedule()
        {
            Repeating = null;
        }
    }
    public class Repeating
    {
        public RepeatingType Type { get; set; }
        public List<DayOfWeek> DaysOfWeek { get; set; } = new();
        public int Interval { get; set; } = 1;
    }
    public enum RepeatingType {None, Daily, SpecificDays, Weekly, Monthly, Yearly }

    public class CalendarRequirements
    {
        //shows list as readonly view, loops through calendars and displays them but cant modify. 
        private const int MaxCalendars = 100;
        private readonly List<Calendar> _calendars = new();

        public IReadOnlyList<Calendar> Calendars => _calendars.AsReadOnly();

        public Calendar CreateCalendar(string name)
        {
            if (_calendars.Count >= MaxCalendars)
                throw new InvalidOperationException($"Cannot create more than {MaxCalendars} calendars.");
            var calendar = new Calendar(name);
            _calendars.Add(calendar);
            return calendar;
        }

        public void DeleteCalendar(Guid id)
        {
            var cal = _calendars.FirstOrDefault(cal => cal.Id == id)
                ?? throw new KeyNotFoundException();

            foreach (var calendarNote in cal.Notes.ToList())
                calendarNote.Note.remove(cal);

            _calendars.Remove(cal);
        }

        public bool AddNote(Guid calendarId, Note note, DateTime? date, TimeSpan? time, RepeatingType? repeating = null)
        {
            var cal = _calendars.FirstOrDefault(c => c.Id == calendarId)
                ?? throw new KeyNotFoundException("Calendar not found.");

            return cal.AddNote(note, date, time, repeating);
        }


    }
}


            
      




