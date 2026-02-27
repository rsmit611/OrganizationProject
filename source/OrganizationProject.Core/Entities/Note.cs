namespace OrganizationProject.Core.Entities;

public class Note
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; private set; }
    public string Description { get; set; } = "";
    public string Color { get; set; } = "White";

    public Note(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.");

        Name = name;
    }
}