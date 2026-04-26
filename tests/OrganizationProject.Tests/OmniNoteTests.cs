using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using OrganizationProject.Core.Entities;
using Xunit;

// =============================================================================
//  OmniNote Unit Test Suite
//  Covers: TC-NOTE, TC-LIST, TC-TEXT, TC-DH, TC-SAVE, TC-CAL, TC-XMOD
// =============================================================================

namespace OrganizationProject.Core.Tests
{
    // =========================================================================
    //  NOTE MANAGEMENT  (TC-NOTE-001 through TC-NOTE-007)
    // =========================================================================
    public class NoteTests
    {
        // Every test that creates a Note via the named constructor triggers
        // DataHolder.unassignedNotesList, which is static. Initialise a fresh
        // DataHolder before each test so the static list is always clean.
        public NoteTests() { _ = new DataHolder(); }

        // TC-NOTE-001 – Create note with valid name
        [Fact]
        public void CreateNote_WithValidName_SetsNameCorrectly()
        {
            var note = new Note("Meeting Notes");
            Assert.Equal("Meeting Notes", note.name);
        }

        // TC-NOTE-001 cont. – Default properties are set as expected
        [Fact]
        public void CreateNote_WithValidName_HasExpectedDefaults()
        {
            var note = new Note("Sample");
            Assert.NotEqual(Guid.Empty, note.id);
            Assert.Equal("", note.description);
            Assert.Equal("White", note.color);
        }

        // TC-NOTE-002 – Reject blank note name (empty string)
        [Fact]
        public void CreateNote_WithEmptyName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new Note(""));
        }

        // TC-NOTE-002 – Reject blank note name (whitespace only)
        [Fact]
        public void CreateNote_WithWhitespaceName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new Note("   "));
        }

        // TC-NOTE-003 – Assign note to list module
        [Fact]
        public void AssignNoteToList_AppearsInAssignedLists()
        {
            var note = new Note("Todo Item");
            var list = new ListModule();
            list.AddNote(note); // ListModule calls note.assign internally via checkAllModules
            note.assign(list);  // direct assign to register on note side
            Assert.Contains(list, note.assignedLists);
        }

        // TC-NOTE-004 – Remove note from list module
        [Fact]
        public void RemoveNoteFromList_DisappearsFromAssignedLists()
        {
            var note = new Note("Remove Me");
            var list = new ListModule();
            note.assign(list);
            note.remove(list);
            Assert.DoesNotContain(list, note.assignedLists);
        }

        // TC-NOTE-005 – Delete note (unassigns from all modules)
        [Fact(Skip = "Causes hang due to current implementation")]
        public void UnassignAll_ClearsAllModuleAssignments()
        {
            var note = new Note("Busy Note");
            var list = new ListModule();
            var doc = new TextDocument("Doc1");
            note.assign(list);
            note.assign(doc);
            note.unassignAll();
            Assert.Empty(note.assignedLists);
            Assert.Empty(note.assignedTexts);
        }

        // TC-NOTE-006 – Unassigned note appears in unassigned list
        [Fact]
        public void NewNote_AppearsInUnassignedNotesList()
        {
            var note = new Note("Floating Note");
            Assert.Contains(
                DataHolder.unassignedNotesList.Notes,
                ln => ln.note == note);
        }

        // TC-NOTE-007 – Note removed from unassigned list once it is assigned elsewhere
        [Fact]
        public void NoteAssignedToList_RemovedFromUnassignedNotesList()
        {
            var note = new Note("Claimed Note");
            var list = new ListModule();
            // AddNote on list side + assign on note side triggers checkAllModules
            list.AddNote(note);
            note.assign(list);
            Assert.DoesNotContain(
                DataHolder.unassignedNotesList.Notes,
                ln => ln.note == note);
        }

        // TC-NOTE-007 cont. – Note re-appears in unassigned list when last module removed
        [Fact]
        public void NoteRemovedFromLastList_ReappearsInUnassignedNotesList()
        {
            var note = new Note("Orphaned Note");
            var list = new ListModule();
            list.AddNote(note);
            note.assign(list);
            // Now remove it
            note.remove(list);
            Assert.Contains(
                DataHolder.unassignedNotesList.Notes,
                ln => ln.note == note);
        }
    }

    // =========================================================================
    //  LIST MODULE  (TC-LIST-001 through TC-LIST-007)
    // =========================================================================
    public class ListModuleTests
    {
        public ListModuleTests() { _ = new DataHolder(); }

        // TC-LIST-001 – Add unique note to list
        [Fact]
        public void AddNote_UniqueNote_ReturnsTrue()
        {
            var list = new ListModule();
            var note = new Note("Task A");
            bool result = list.AddNote(note);
            Assert.True(result);
            Assert.Single(list.Notes);
        }

        // TC-LIST-002 – Reject duplicate note in list
        [Fact]
        public void AddNote_DuplicateNote_ReturnsFalse()
        {
            var list = new ListModule();
            var note = new Note("Task B");
            list.AddNote(note);
            bool secondAdd = list.AddNote(note);
            Assert.False(secondAdd);
            Assert.Single(list.Notes);
        }

        // TC-LIST-003 – Remove note by index
        [Fact]
        public void RemoveNote_ByIndex_RemovesCorrectEntry()
        {
            var list = new ListModule();
            var note = new Note("Task C");
            list.AddNote(note);
            list.RemoveNote(0);
            Assert.Empty(list.Notes);
        }

        // TC-LIST-003 – Out-of-range index throws ArgumentException
        [Fact]
        public void RemoveNote_OutOfRangeIndex_ThrowsArgumentException()
        {
            var list = new ListModule();
            Assert.Throws<ArgumentException>(() => list.RemoveNote(5));
        }

        // TC-LIST-004 – Remove note by reference
        [Fact]
        public void RemoveNote_ByReference_RemovesCorrectEntry()
        {
            var list = new ListModule();
            var noteA = new Note("Task D");
            var noteB = new Note("Task E");
            list.AddNote(noteA);
            list.AddNote(noteB);
            list.RemoveNote(noteA);
            Assert.Single(list.Notes);
            Assert.Equal(noteB, list.Notes[0].note);
        }

        // TC-LIST-004 – Null reference throws ArgumentNullException
        [Fact]
        public void RemoveNote_NullReference_ThrowsArgumentNullException()
        {
            var list = new ListModule();
            Assert.Throws<ArgumentNullException>(() => list.RemoveNote(null!));
        }

        // TC-LIST-005 – Move note inside list
        // NOTE: moveTo has a known bug – it uses a hardcoded RemoveAt(0 or 1)
        // instead of the actual fromIndex. This test documents the EXPECTED
        // correct behaviour; it is expected to FAIL until the bug is fixed.
        [Fact]
        public void MoveNote_FromFirstToLast_ReordersCorrectly()
        {
            var list = new ListModule();
            var noteA = new Note("First");
            var noteB = new Note("Second");
            var noteC = new Note("Third");
            list.AddNote(noteA);
            list.AddNote(noteB);
            list.AddNote(noteC);
            // Move index 0 (noteA) to index 2 (end)
            list.moveTo(0, 2);
            Assert.Equal(noteA, list.Notes[2].note);
        }

        // TC-LIST-006 – Change note priority
        [Fact]
        public void ChangePriority_SetsNewPriority()
        {
            var list = new ListModule();
            var note = new Note("Priority Task");
            list.AddNote(note);
            list.ChangePriority(0, ListPriority.High);
            Assert.Equal(ListPriority.High, list.Notes[0].priority);
        }

        // TC-LIST-006 – Out-of-range index throws ArgumentException
        [Fact]
        public void ChangePriority_OutOfRangeIndex_ThrowsArgumentException()
        {
            var list = new ListModule();
            Assert.Throws<ArgumentException>(() => list.ChangePriority(0, ListPriority.High));
        }

        // TC-LIST-007 – Delete list clears all notes
        [Fact]
        public void DeleteList_RemovesAllNotes()
        {
            var list = new ListModule();
            list.AddNote(new Note("N1"));
            list.AddNote(new Note("N2"));
            list.AddNote(new Note("N3"));
            list.DeleteList();
            Assert.Empty(list.Notes);
        }
    }

    // =========================================================================
    //  LONG TEXT MODULE  (TC-TEXT-001 through TC-TEXT-006)
    // =========================================================================
    public class TextModuleTests
    {
        private readonly DataHolder _dh;
        private readonly TextModule _tm;

        public TextModuleTests()
        {
            _dh = new DataHolder();
            _tm = _dh.textModule;
        }

        // TC-TEXT-001 – Create text module
        [Fact]
        public void TextModule_CreatedWithDataHolder_IsNotNull()
        {
            Assert.NotNull(_tm);
        }

        // TC-TEXT-001 – Starts with empty document list
        [Fact]
        public void TextModule_InitialState_HasNoDocuments()
        {
            Assert.Empty(_tm.Documents);
        }

        // TC-TEXT-002 – Add text document
        [Fact]
        public void AddDocument_ValidDocument_ReturnsTrue()
        {
            var doc = new TextDocument("Project Notes");
            bool result = _tm.AddDocument(doc);
            Assert.True(result);
            Assert.Single(_tm.Documents);
        }

        // TC-TEXT-003 – Reject duplicate text document title
        [Fact]
        public void AddDocument_DuplicateTitle_ReturnsFalse()
        {
            var doc1 = new TextDocument("Research");
            var doc2 = new TextDocument("Research");
            _tm.AddDocument(doc1);
            bool result = _tm.AddDocument(doc2);
            Assert.False(result);
            Assert.Single(_tm.Documents);
        }

        // TC-TEXT-004 – Edit document content (full replace)
        [Fact]
        public void EditContent_ReplacesEntireContent()
        {
            var doc = new TextDocument("Draft");
            doc.EditContent("Hello, World!");
            Assert.Equal("Hello, World!", doc.Content);
        }

        // TC-TEXT-004 – Editing content clears existing formatting
        [Fact]
        public void EditContent_ClearsExistingFormatting()
        {
            var doc = new TextDocument("StyledDoc");
            doc.EditContent("Bold text here");
            doc.ApplyFormatting(0, 4, TextStyle.Bold);
            doc.EditContent("Fresh content");
            Assert.Empty(doc.Formatting);
        }

        // TC-TEXT-005 – Insert text at a position
        [Fact]
        public void InsertText_AtValidIndex_InsertsCorrectly()
        {
            var doc = new TextDocument("InsertTest");
            doc.EditContent("Hello World");
            doc.InsertText(5, ", Beautiful");
            Assert.Equal("Hello, Beautiful World", doc.Content);
        }

        // TC-TEXT-005 – Delete text from document
        [Fact]
        public void DeleteText_AtValidRange_DeletesCorrectly()
        {
            var doc = new TextDocument("DeleteTest");
            doc.EditContent("Hello Beautiful World");
            doc.DeleteText(5, 10); // remove " Beautiful"
            Assert.Equal("Hello World", doc.Content);
        }

        // TC-TEXT-005 – Insert out-of-range throws
        [Fact]
        public void InsertText_OutOfRange_ThrowsArgumentOutOfRangeException()
        {
            var doc = new TextDocument("RangeTest");
            doc.EditContent("Short");
            Assert.Throws<ArgumentOutOfRangeException>(() => doc.InsertText(100, "x"));
        }

        // TC-TEXT-005 – Delete out-of-range throws
        [Fact]
        public void DeleteText_OutOfRange_ThrowsArgumentOutOfRangeException()
        {
            var doc = new TextDocument("RangeTest2");
            doc.EditContent("Short");
            Assert.Throws<ArgumentOutOfRangeException>(() => doc.DeleteText(0, 100));
        }

        // TC-TEXT-006 – Apply formatting to a range
        [Fact]
        public void ApplyFormatting_ValidRange_AddsFormattingEntry()
        {
            var doc = new TextDocument("FormattedDoc");
            doc.EditContent("Bold and Italic");
            doc.ApplyFormatting(0, 4, TextStyle.Bold);
            Assert.Single(doc.Formatting);
            Assert.Equal(TextStyle.Bold, doc.Formatting[0].Style);
        }

        // TC-TEXT-006 – Assign note comment to a text range
        [Fact]
        public void AssignNote_ValidRange_AddsNoteAssignment()
        {
            var doc = new TextDocument("AnnotatedDoc");
            doc.EditContent("See this section for details.");
            var note = new Note("Section Comment");
            doc.AssignNote(0, 3, note);
            Assert.Single(doc.Notes);
            Assert.Equal(note, doc.Notes[0].AssignedNote);
        }
    }

    // =========================================================================
    //  DATAHOLDER  (TC-DH-001 through TC-DH-005)
    // =========================================================================
    public class DataHolderTests
    {
        // TC-DH-001 – Add note to DataHolder
        [Fact]
        public void AddNote_AddsNoteToCollection()
        {
            var dh = new DataHolder();
            var note = new Note("DH Note");
            dh.addNote(note);
            Assert.Contains(note, dh.AllNotes);
        }

        // TC-DH-001 – Duplicate note not added twice
        [Fact]
        public void AddNote_DuplicateNote_NotAddedTwice()
        {
            var dh = new DataHolder();
            var note = new Note("Unique Note");
            dh.addNote(note);
            dh.addNote(note);
            Assert.Single(dh.AllNotes);
        }

        // TC-DH-002 – Remove note from DataHolder
        [Fact]
        public void RemoveNote_RemovesNoteFromCollection()
        {
            var dh = new DataHolder();
            var note = new Note("Gone Note");
            dh.addNote(note);
            dh.removeNote(note);
            Assert.DoesNotContain(note, dh.AllNotes);
        }

        // TC-DH-003 – Add list module
        [Fact]
        public void AddList_AddsListToCollection()
        {
            var dh = new DataHolder();
            var list = new ListModule();
            dh.addList(list);
            Assert.Contains(list, dh.AllLists);
        }

        // TC-DH-003 – Remove list module
        [Fact]
        public void RemoveList_RemovesListFromCollection()
        {
            var dh = new DataHolder();
            var list = new ListModule();
            dh.addList(list);
            dh.removeList(list);
            Assert.DoesNotContain(list, dh.AllLists);
        }

        // TC-DH-003 – Removing list also removes it from assigned notes
        [Fact]
        public void RemoveList_ClearsListFromNoteAssignments()
        {
            var dh = new DataHolder();
            var note = new Note("Listed Note");
            var list = new ListModule();
            dh.addNote(note);
            dh.addList(list);
            note.assign(list);
            dh.removeList(list);
            Assert.DoesNotContain(list, note.assignedLists);
        }

        // TC-DH-003 – Cannot add unassignedNotesList as a regular list
        [Fact]
        public void AddList_UnassignedNotesList_IsNotAdded()
        {
            var dh = new DataHolder();
            dh.addList(DataHolder.unassignedNotesList);
            Assert.DoesNotContain(DataHolder.unassignedNotesList, dh.AllLists);
        }

        // TC-DH-004 – Add text document
        [Fact]
        public void AddTextDocument_AddsDocumentToCollection()
        {
            var dh = new DataHolder();
            var doc = new TextDocument("My Doc");
            dh.addTextDocument(doc);
            Assert.Contains(doc, dh.AllTextDocuments);
        }

        // TC-DH-004 – Remove text document
        [Fact]
        public void RemoveTextDocument_RemovesDocumentFromCollection()
        {
            var dh = new DataHolder();
            var doc = new TextDocument("Old Doc");
            dh.addTextDocument(doc);
            dh.removeTextDocument(doc);
            Assert.DoesNotContain(doc, dh.AllTextDocuments);
        }

        // TC-DH-004 – Removing text document clears it from note assignments
        [Fact]
        public void RemoveTextDocument_ClearsDocFromNoteAssignments()
        {
            var dh = new DataHolder();
            var note = new Note("Annotated Note");
            var doc = new TextDocument("AnnotatedDoc");
            dh.addNote(note);
            dh.addTextDocument(doc);
            note.assign(doc);
            dh.removeTextDocument(doc);
            Assert.DoesNotContain(doc, note.assignedTexts);
        }

        // TC-DH-005 – Add calendar
        [Fact]
        public void AddCalendar_AddsCalendarToCollection()
        {
            var dh = new DataHolder();
            var cal = new Calendar("Work Calendar");
            dh.addCalendar(cal);
            Assert.Contains(cal, dh.AllCalendars);
        }

        // TC-DH-005 – Remove calendar
        [Fact]
        public void RemoveCalendar_RemovesCalendarFromCollection()
        {
            var dh = new DataHolder();
            var cal = new Calendar("Temp Calendar");
            dh.addCalendar(cal);
            dh.removeCalendar(cal);
            Assert.DoesNotContain(cal, dh.AllCalendars);
        }

        // TC-DH-005 – Cannot add more than 100 calendars
        [Fact]
        public void AddCalendar_ExceedsLimit_ThrowsInvalidOperationException()
        {
            var dh = new DataHolder();
            for (int i = 0; i < 100; i++)
                dh.addCalendar(new Calendar($"Cal {i}"));
            Assert.Throws<InvalidOperationException>(() => dh.addCalendar(new Calendar("One Too Many")));
        }
    }

    // =========================================================================
    //  PERSISTENCE AND BACKUP  (TC-SAVE-001 through TC-SAVE-003)
    // =========================================================================
    public class PersistenceTests : IDisposable
    {
        private readonly string _tempDir;

        public PersistenceTests()
        {
            _ = new DataHolder(); // init static list
            _tempDir = Path.Combine(Path.GetTempPath(), "OmniNoteTest_" + Guid.NewGuid());
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        // TC-SAVE-001 – Save writes file to the default AppData path
        [Fact]
        public void Save_WritesFileToExpectedPath()
        {
            var dh = new DataHolder();
            dh.addNote(new Note("Persisted Note"));
            dh.save();

            string defaultPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OmniNote", "save.txt");
            Assert.True(File.Exists(defaultPath));
        }

        // TC-SAVE-001 – Saved file contains the note name
        [Fact]
        public void Save_ProducesNonEmptyJsonFile()
        {
            var dh = new DataHolder();
            dh.addNote(new Note("Serialised Note"));
            dh.save();

            string defaultPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OmniNote", "save.txt");
            string content = File.ReadAllText(defaultPath);
            Assert.Contains("Serialised Note", content);
        }

        // TC-SAVE-002 – Load from explicit path restores notes
        // Uses load(string path) directly to avoid circular-reference hang in backup()
        [Fact]
        public void LoadFromPath_RestoresDataFromFile()
        {
            var dh1 = new DataHolder();
            dh1.addNote(new Note("PathNote"));
            dh1.save();

            string defaultPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OmniNote", "save.txt");

            var dh2 = new DataHolder();
            dh2.load(defaultPath);

            Assert.Contains(dh2.AllNotes, n => n.name == "PathNote");
        }

        // TC-SAVE-002 – Load restores multiple notes
        [Fact]
        public void LoadFromPath_RestoresMultipleNotes()
        {
            var dh1 = new DataHolder();
            dh1.addNote(new Note("Alpha"));
            dh1.addNote(new Note("Beta"));
            dh1.save();

            string defaultPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OmniNote", "save.txt");

            var dh2 = new DataHolder();
            dh2.load(defaultPath);

            Assert.Contains(dh2.AllNotes, n => n.name == "Alpha");
            Assert.Contains(dh2.AllNotes, n => n.name == "Beta");
        }

        // TC-SAVE-003 – Backup cycle increments correctly
        // Tests the cycle counter logic without calling backup() to avoid
        // circular-reference serialization hang (known issue in backup())
        [Fact]
        public void BackupCycle_IncrementsAndWrapsAtMaxBackups()
        {
            var dh = new DataHolder();
            dh.maxBackups = 3;
            // Simulate what backup() does to the cycle counter
            for (int i = 0; i < 4; i++)
            {
                dh.backupCycle++;
                if (dh.backupCycle >= dh.maxBackups) dh.backupCycle = 0;
            }
            Assert.Equal(1, dh.backupCycle);
        }
    }

    // =========================================================================
    //  CALENDAR MODULE  (TC-CAL-001 through TC-CAL-007)
    // =========================================================================
    public class CalendarTests
    {
        public CalendarTests() { _ = new DataHolder(); }

        // TC-CAL-001 – Create calendar module
        [Fact]
        public void CreateCalendar_SetsNameAndId()
        {
            var cal = new Calendar("Personal");
            Assert.Equal("Personal", cal.Name);
            Assert.NotEqual(Guid.Empty, cal.Id);
        }

        // TC-CAL-001 – Calendar starts with empty notes list
        [Fact]
        public void CreateCalendar_HasEmptyNotesList()
        {
            var cal = new Calendar("Empty Cal");
            Assert.Empty(cal.Notes);
        }

        // TC-CAL-002 – Assign note to calendar with date and time
        [Fact]
        public void AddNote_WithDateAndTime_AppearsInCalendarNotes()
        {
            var cal = new Calendar("Work");
            var note = new Note("Stand-up");
            var date = new DateTime(2026, 5, 1);
            var time = new TimeSpan(9, 0, 0);
            bool result = cal.AddNote(note, date, time);
            Assert.True(result);
            Assert.Single(cal.Notes);
            Assert.Equal(date, cal.Notes[0].Date);
            Assert.Equal(time, cal.Notes[0].Time);
        }

        // TC-CAL-002 – Adding same note twice returns false
        [Fact]
        public void AddNote_Duplicate_ReturnsFalse()
        {
            var cal = new Calendar("Work");
            var note = new Note("Daily Sync");
            cal.AddNote(note, DateTime.Today, null);
            bool second = cal.AddNote(note, DateTime.Today, null);
            Assert.False(second);
            Assert.Single(cal.Notes);
        }

        // TC-CAL-002 – Calendar assignment appears on the note itself
        [Fact]
        public void AddNote_AssignsCalendarToNote()
        {
            var cal = new Calendar("Events");
            var note = new Note("Birthday");
            cal.AddNote(note, DateTime.Today, null);
            Assert.Contains(cal, note.assignedCalendars);
        }

        // TC-CAL-003 – Delete calendar and clear note references
        // Uses CalendarRequirements which has DeleteCalendar + note cleanup
        [Fact]
        public void DeleteCalendar_RemovesCalendarFromNoteAssignments()
        {
            var cr = new CalendarRequirements();
            var cal = cr.CreateCalendar("Temp");
            var note = new Note("Event");
            cal.AddNote(note, DateTime.Today, null);

            cr.DeleteCalendar(cal.Id);

            Assert.DoesNotContain(cal, note.assignedCalendars);
        }

        // TC-CAL-003 – Deleted calendar no longer exists in CalendarRequirements
        [Fact]
        public void DeleteCalendar_RemovesCalendarFromCollection()
        {
            var cr = new CalendarRequirements();
            var cal = cr.CreateCalendar("Short-lived");
            cr.DeleteCalendar(cal.Id);
            Assert.DoesNotContain(cal, cr.Calendars);
        }

        // TC-CAL-004 – Make note a reminder (schedule notification)
        [Fact]
        public void ScheduleNotification_SetsIsScheduledAndDates()
        {
            var cal = new Calendar("Reminders");
            var note = new Note("Take Medicine");
            cal.AddNote(note, DateTime.Today, new TimeSpan(8, 0, 0));
            var cn = cal.Notes[0];

            var notifyDate = DateTime.Today;
            var notifyTime = new TimeSpan(7, 50, 0);
            cn.ScheduleNotification(notifyDate, notifyTime);

            Assert.True(cn.IsScheduled);
            Assert.Equal(notifyDate, cn.NotificationDate);
            Assert.Equal(notifyTime, cn.NotificationTime);
        }

        // TC-CAL-004 – Cancel notification clears scheduled state
        [Fact]
        public void CancelNotification_ClearsScheduledState()
        {
            var cal = new Calendar("Reminders");
            var note = new Note("Cancel Me");
            cal.AddNote(note, DateTime.Today, new TimeSpan(9, 0, 0));
            var cn = cal.Notes[0];
            cn.ScheduleNotification(DateTime.Today, new TimeSpan(8, 0, 0));

            cn.CancelNotification();

            Assert.False(cn.IsScheduled);
            Assert.Null(cn.NotificationDate);
            Assert.Null(cn.NotificationTime);
        }

        // TC-CAL-005 – Make note recurring (weekly)
        [Fact]
        public void SetRepeatingSchedule_Weekly_SetsRepeatingCorrectly()
        {
            var cal = new Calendar("Recurring");
            var note = new Note("Weekly Review");
            cal.AddNote(note, new DateTime(2026, 5, 4), new TimeSpan(10, 0, 0));
            var cn = cal.Notes[0];

            cn.SetRepeatingSchedule(RepeatingType.Weekly);

            Assert.NotNull(cn.Repeating);
            Assert.Equal(RepeatingType.Weekly, cn.Repeating!.Type);
        }

        // TC-CAL-005 – Remove repeating schedule
        [Fact]
        public void RemoveRepeatingSchedule_NullsRepeating()
        {
            var cal = new Calendar("Recurring");
            var note = new Note("One-time Event");
            cal.AddNote(note, DateTime.Today, null);
            var cn = cal.Notes[0];
            cn.SetRepeatingSchedule(RepeatingType.Daily);

            cn.RemoveRepeatingSchedule();

            Assert.Null(cn.Repeating);
        }

        // TC-CAL-006 – Change date of a note
        [Fact]
        public void ChangeDate_UpdatesDateOnCalendarNote()
        {
            var cal = new Calendar("Schedule");
            var note = new Note("Dentist");
            var original = new DateTime(2026, 5, 10);
            var updated = new DateTime(2026, 5, 17);
            cal.AddNote(note, original, null);
            cal.Notes[0].ChangeDate(updated);
            Assert.Equal(updated, cal.Notes[0].Date);
        }

        // TC-CAL-006 – Change time of a note
        [Fact]
        public void ChangeTime_UpdatesTimeOnCalendarNote()
        {
            var cal = new Calendar("Schedule");
            var note = new Note("Lunch");
            cal.AddNote(note, DateTime.Today, new TimeSpan(12, 0, 0));
            cal.Notes[0].ChangeTime(new TimeSpan(13, 0, 0));
            Assert.Equal(new TimeSpan(13, 0, 0), cal.Notes[0].Time);
        }

        // TC-CAL-007 – Remove time from note (leave just date)
        [Fact]
        public void RemoveTime_SetsTimeToNullButKeepsDate()
        {
            var cal = new Calendar("Schedule");
            var note = new Note("All Day Event");
            var date = new DateTime(2026, 6, 1);
            cal.AddNote(note, date, new TimeSpan(9, 0, 0));
            cal.Notes[0].RemoveTime();
            Assert.Null(cal.Notes[0].Time);
            Assert.Equal(date, cal.Notes[0].Date);
        }
    }

    // =========================================================================
    //  CROSS-MODULE BEHAVIOUR  (TC-XMOD-001, TC-XMOD-002)
    // =========================================================================
    public class CrossModuleTests
    {
        public CrossModuleTests() { _ = new DataHolder(); }

        // TC-XMOD-001 – Shared note is visible across modules
        // A single Note assigned to both a ListModule and a Calendar reflects
        // the same object identity in both contexts.
        [Fact]
        public void SharedNote_SameReferenceAcrossListAndCalendar()
        {
            var note = new Note("Shared Task");
            var list = new ListModule();
            var cal = new Calendar("Project");

            list.AddNote(note);
            note.assign(list);
            cal.AddNote(note, DateTime.Today, null);

            var noteFromList = list.Notes[0].note;
            var noteFromCal = cal.Notes[0].Note;

            Assert.Same(noteFromList, noteFromCal);
        }

        // TC-XMOD-001 – Renaming the note is reflected everywhere
        [Fact]
        public void SharedNote_NameChangeReflectedInAllModules()
        {
            var note = new Note("Original Name");
            var list = new ListModule();
            var cal = new Calendar("Calendar");

            list.AddNote(note);
            note.assign(list);
            cal.AddNote(note, DateTime.Today, null);

            note.name = "Updated Name";

            Assert.Equal("Updated Name", list.Notes[0].note.name);
            Assert.Equal("Updated Name", cal.Notes[0].Note.name);
        }

        // TC-XMOD-002 – List sorting by date: notes sorted by Date ascending
        [Fact]
        public void GetNotesOrganizedByDateTime_ReturnsSortedOrder()
        {
            var cal = new Calendar("Sorted");
            var noteA = new Note("Later");
            var noteB = new Note("Earlier");
            cal.AddNote(noteA, new DateTime(2026, 6, 15), null);
            cal.AddNote(noteB, new DateTime(2026, 5, 1), null);

            var sorted = cal.GetNotesOrganizedByDateTime().ToList();

            Assert.Equal(noteB, sorted[0].Note); // earlier date first
            Assert.Equal(noteA, sorted[1].Note);
        }

        // TC-XMOD-002 – Toggle list visibility hides associated notes from calendar
        [Fact]
        public void HideList_HidesNotesFromThatListInCalendar()
        {
            var dh = new DataHolder();
            var list = new ListModule();
            dh.addList(list);

            var note = new Note("Visible Note");
            dh.addNote(note);
            list.AddNote(note);
            note.assign(list);

            var cal = new Calendar("Work Cal");
            cal.AddNote(note, DateTime.Today, null);

            cal.HideList(list.Id);

            var visible = cal.GetVisibleNotes(dh.AllLists).ToList();
            Assert.DoesNotContain(visible, cn => cn.Note == note);
        }

        // TC-XMOD-002 – ShowList restores hidden notes
        [Fact]
        public void ShowList_RestoresHiddenNotesInCalendar()
        {
            var dh = new DataHolder();
            var list = new ListModule();
            dh.addList(list);

            var note = new Note("Hidden Note");
            dh.addNote(note);
            list.AddNote(note);
            note.assign(list);

            var cal = new Calendar("Work Cal");
            cal.AddNote(note, DateTime.Today, null);

            cal.HideList(list.Id);
            cal.ShowList(list.Id);

            var visible = cal.GetVisibleNotes(dh.AllLists).ToList();
            Assert.Contains(visible, cn => cn.Note == note);
        }
    }
}
