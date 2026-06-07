using BugFlow.Models;

namespace BugFlow.DTOs;

public class ChangeStatusDto
{
    
        public string? Comment { get; set; } 
  
    public VulnerabilityStatus Status { get; set; }

}