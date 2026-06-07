using System.ComponentModel.DataAnnotations;

namespace BugFlow.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    public string Username { get; set; }

    [Required]
    public string Password { get; set; }

    public string? Name { get; set; }   
    

    public ICollection<ProjectMember> Projects { get; set; }
}