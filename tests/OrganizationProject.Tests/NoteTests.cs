using Xunit;
using OrganizationProject.Core.Entities;

namespace OrganizationProject.Tests;

public class NoteTests
{
    [Fact]
    public void CreatingNote_ShouldSetName()
    {
        var note = new Note("Test Note");

        Assert.Equal("Test Note", note.Name);
    }
}