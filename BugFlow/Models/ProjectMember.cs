namespace BugFlow.Models;

public class ProjectMember
{
    public int ProjectId { get; set; }

    public Project Project { get; set; }

    public int UserId { get; set; }

    public User User { get; set; }

    public ProjectRole Role { get; set; }
}