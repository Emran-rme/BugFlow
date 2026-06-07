namespace BugFlow.Models;

public class Scope
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public Project Project { get; set; }

    public string Type { get; set; }

    public string Value { get; set; }
}