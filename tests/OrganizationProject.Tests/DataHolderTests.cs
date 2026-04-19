using System.Collections.Generic;
using OrganizationProject.Core.Entities;
using Xunit;

namespace OrganizationProject.Core.Tests.Entities
{
    public class DataHolderTests
    {
        public DataHolderTests()
        {
            // TextModule uses a static registry, so reset it for isolation.
            TextModule.SetTextModules(new List<TextModule>());
        }

        [Fact]
        public void Constructor_InitializesExpectedCollections()
        {
            var dataHolder = new DataHolder();

            Assert.Empty(dataHolder.AllNotes);
            Assert.Empty(dataHolder.AllLists);
            Assert.Empty(dataHolder.AllTextModules);
            Assert.NotNull(dataHolder.UnassignedNotesList);
        }

        [Fact]
        public void AddNote_AddsUniqueNoteToCollection()
        {
            var dataHolder = new DataHolder();
            var note = new Note("Test Note", "Description", "Blue");

            dataHolder.addNote(note);

            Assert.Contains(note, dataHolder.AllNotes);
            Assert.Single(dataHolder.AllNotes);
        }

        [Fact]
        public void AddNote_DoesNotAddDuplicateReferenceTwice()
        {
            var dataHolder = new DataHolder();
            var note = new Note("Test Note");

            dataHolder.addNote(note);
            dataHolder.addNote(note);

            Assert.Single(dataHolder.AllNotes);
        }

        [Fact]
        public void RemoveNote_RemovesExistingNoteFromCollection()
        {
            var dataHolder = new DataHolder();
            var note = new Note("Test Note");

            dataHolder.addNote(note);
            dataHolder.removeNote(note);

            Assert.Empty(dataHolder.AllNotes);
        }

        [Fact]
        public void AddList_AddsUniqueListToCollection()
        {
            var dataHolder = new DataHolder();
            var list = new ListModule();

            dataHolder.addList(list);

            Assert.Contains(list, dataHolder.AllLists);
            Assert.Single(dataHolder.AllLists);
        }

        [Fact]
        public void RemoveList_RemovesListAndClearsNoteAssociation()
        {
            var dataHolder = new DataHolder();
            var note = new Note("Task");
            var list = new ListModule();

            dataHolder.addNote(note);
            dataHolder.addList(list);
            list.AddNote(note);

            dataHolder.removeList(list);

            Assert.DoesNotContain(list, dataHolder.AllLists);
            Assert.DoesNotContain(list, note.assignedLists);
        }

        [Fact]
        public void AddTextModule_AddsUniqueTextModuleAndSyncsRegistry()
        {
            TextModule.SetTextModules(new List<TextModule>());
            var dataHolder = new DataHolder();
            var textModule = new TextModule();

            // DataHolder captured the registry at construction time, so add this new module through it.
            dataHolder.addTextModule(textModule);

            Assert.Contains(textModule, dataHolder.AllTextModules);
            Assert.Contains(textModule, TextModule.GetTextModules());
        }

        [Fact]
        public void RemoveTextModule_RemovesModuleAndClearsNoteAssociation()
        {
            var textModule = new TextModule();
            TextModule.SetTextModules(new List<TextModule> { textModule });

            var dataHolder = new DataHolder();
            var note = new Note("Draft");
            note.assign(textModule);
            dataHolder.addNote(note);

            dataHolder.removeTextModule(textModule);

            Assert.DoesNotContain(textModule, dataHolder.AllTextModules);
            Assert.DoesNotContain(textModule, note.assignedTexts);
        }

        [Fact]
        public void UpdateUnassignedNotesList_AddsNoteWithNoModulesToSpecialList()
        {
            var dataHolder = new DataHolder();
            var note = new Note("Loose Note");

            dataHolder.addNote(note);
            dataHolder.updateUnassignedNotesList();

            Assert.Contains(dataHolder.UnassignedNotesList, note.assignedLists);
        }

        [Fact]
        public void UpdateUnassignedNotesList_RemovesNoteFromSpecialListOnceAssignedElsewhere()
        {
            var dataHolder = new DataHolder();
            var note = new Note("Assigned Note");
            var list = new ListModule();

            dataHolder.addNote(note);
            dataHolder.addList(list);
            dataHolder.updateUnassignedNotesList();

            note.assign(list);
            list.AddNote(note);
            dataHolder.updateUnassignedNotesList();

            Assert.Contains(list, note.assignedLists);
            Assert.DoesNotContain(dataHolder.UnassignedNotesList, note.assignedLists);
        }
    }
}
