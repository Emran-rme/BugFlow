namespace BugFlow.DTOs;

public class CreateProjectDto
{
    public string Name { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string TestType { get; set; }
}