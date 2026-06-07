using System.ComponentModel.DataAnnotations;

namespace BugFlow.Models;

public class Company
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string RegistrationNumber { get; set; }

    public int OwnerId { get; set; }

    public User Owner { get; set; }
}
