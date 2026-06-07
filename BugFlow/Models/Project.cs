using System.ComponentModel.DataAnnotations;

namespace BugFlow.Models;

public class Project
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public Company Company { get; set; }

    public string Name { get; set; }

    public string Code { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string TestType { get; set; }

    public ICollection<ProjectMember> Members { get; set; }

    public ICollection<Scope> Scopes { get; set; }

    public ICollection<Vulnerability> Vulnerabilities { get; set; }
}