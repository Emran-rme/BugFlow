using BugFlow.Models;

namespace BugFlow.DTOs;

public class InviteDto
{
    public string Name { get; set; }

    public string Phone { get; set; }

    public ProjectRole Role { get; set; }
}