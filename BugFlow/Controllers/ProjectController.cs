using BugFlow.DTOs;
using BugFlow.Models;
using BugFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BugFlow.Controllers;

[Authorize]
[ApiController]
[Route("api/project")]
public class ProjectController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ProjectService _service;

    public ProjectController(
        AppDbContext context,
        ProjectService service)
    {
        _context = context;
        _service = service;
    }
    
    [HttpGet("company-dashboard")]
    public async Task<IActionResult> GetCompanyDashboard()
    {
        var userId = int.Parse(User.FindFirst("UserId").Value);

    var company = await _context.Companies
        .FirstOrDefaultAsync(x => x.OwnerId == userId);

    if (company != null)
    {
        var totalProjects = await _context.Projects
            .CountAsync(x => x.CompanyId == company.Id);

        var finishedProjects = await _context.Projects
            .CountAsync(x =>
                x.CompanyId == company.Id &&
                x.EndDate < DateTime.UtcNow);

        var contractorsCount = await _context.ProjectMembers
            .Where(x =>
                x.Project.CompanyId == company.Id &&
                x.Role == ProjectRole.Contractor)
            .Select(x => x.UserId)
            .Distinct()
            .CountAsync();

        var recentProjects = await _context.Projects
            .Where(x => x.CompanyId == company.Id)
            .OrderByDescending(x => x.StartDate)
            .Take(10)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code,
                x.StartDate,
                x.EndDate,
                x.TestType
            })
            .ToListAsync();

        return Ok(new
        {
            Type = "CompanyOwner",
            company.Name,
            totalProjects,
            contractorsCount,
            finishedProjects,
            recentProjects
        });
    }

    var memberProjects = await _context.ProjectMembers
        .Where(x => x.UserId == userId)
        .Select(x => x.ProjectId)
        .ToListAsync();

    var totalMemberProjects = memberProjects.Count;

    var finishedMemberProjects = await _context.Projects
        .CountAsync(x =>
            memberProjects.Contains(x.Id) &&
            x.EndDate < DateTime.UtcNow);

    var vulnerabilities = await _context.Vulnerabilities
        .CountAsync(x => x.CreatedBy == userId);

    var pendingVulnerabilities = await _context.Vulnerabilities
        .CountAsync(x =>
            x.CreatedBy == userId &&
            x.Status == VulnerabilityStatus.Pending);

    var recentReports = await _context.Vulnerabilities
        .Where(x => x.CreatedBy == userId)
        .OrderByDescending(x => x.Id)
        .Take(10)
        .Select(x => new
        {
            x.Id,
            x.Title,
            x.Severity,
            x.Status,
            x.ProjectId
        })
        .ToListAsync();

    return Ok(new
    {
        Type = "Contractor",
        totalProjects = totalMemberProjects,
        finishedProjects = finishedMemberProjects,
        totalReports = vulnerabilities,
        pendingReports = pendingVulnerabilities,
        recentReports
    });
    }



    [HttpPost("create")]
    public async Task<IActionResult> Create( CreateProjectDto dto)
    {

        var userId = int.Parse(User.FindFirst("UserId").Value);

        var company = await _context.Companies
            .FirstOrDefaultAsync(x => x.OwnerId == userId);

        if (company == null)
            return Forbid();
     

        if (company == null)
            return NotFound("شرکت یافت نشد");

        if (company.OwnerId != userId)
            return Forbid(); 

        var project = new Project
        {
            CompanyId = company.Id,
            Name = dto.Name,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            TestType = dto.TestType
        };

        var result = await _service.CreateProject(project);

        _context.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = result.Id,
            UserId = userId,
            Role = ProjectRole.Manager
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            result.Id,
            result.Name,
            result.Code,
            result.StartDate,
            result.EndDate,
            result.TestType
        });
        
    }

    [HttpPost("{projectId}/invite")]
    public async Task<IActionResult> Invite(
        int projectId,
        InviteDto dto)
    {
        var userId = int.Parse(User.FindFirst("UserId")!.Value);

        var isManager = await _context.ProjectMembers
            .AnyAsync(x =>
                x.ProjectId == projectId &&
                x.UserId == userId &&
                x.Role == ProjectRole.Manager);

        if (!isManager)
            return Forbid();

        var result = await _service.InviteUser(
            projectId,
            dto.Name,
            dto.Phone,
            dto.Role);

        if (!result.Success)
            return BadRequest("کاربر قبلاً عضو پروژه است");

        return Ok(new
        {
            message = "کاربر اضافه شد",
            tempPassword = result.TempPassword
        });
    }
    
    [HttpGet("list")]
    public async Task<IActionResult> GetProjects()
    {
        var userId = int.Parse(User.FindFirst("UserId").Value);

        var company = await _context.Companies
            .FirstOrDefaultAsync(x => x.OwnerId == userId);

        if (company != null)
        {
            var projects = await _context.Projects
                .Where(x => x.CompanyId == company.Id)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Code,
                    p.StartDate,
                    p.EndDate,
                    p.TestType,

                    Contractors = p.Members
                        .Where(m => m.Role == ProjectRole.Contractor)
                        .Select(m => new
                        {
                            m.UserId,
                            m.User.Name,
                            m.User.Username
                        })
                        .ToList()
                })
                .OrderByDescending(x => x.StartDate)
                .ToListAsync();

            return Ok(new
            {
                Type = "CompanyOwner",
                Projects = projects
            });
        }

        var memberProjects = await _context.ProjectMembers
            .Where(x => x.UserId == userId)
            .Include(x => x.Project)
            .Select(x => new
            {
                x.ProjectId,
                x.Project.Name,
                x.Project.Code,
                x.Project.StartDate,
                x.Project.EndDate,
                x.Project.TestType,
                Role = x.Role.ToString()
            })
            .OrderByDescending(x => x.StartDate)
            .ToListAsync();

        return Ok(new
        {
            Type = "ProjectMember",
            Projects = memberProjects
        });
    }

    [HttpGet("contractors")]
    public async Task<IActionResult> GetCompanyContractors()
    {
        var userId = int.Parse(User.FindFirst("UserId").Value);

        var company = await _context.Companies
            .FirstOrDefaultAsync(x => x.OwnerId == userId);

        if (company == null)
            return Forbid();

        var contractors = await _context.ProjectMembers
            .Where(pm => pm.Role == ProjectRole.Contractor &&
                         pm.Project.CompanyId == company.Id)
            .Select(pm => new
            {
                pm.UserId,
                pm.User.Name,
                pm.User.Username
            })
            .Distinct()
            .ToListAsync();

        return Ok(contractors);
    }


    [HttpPost("{projectId}/scope")]
    public async Task<IActionResult> AddScope(int projectId, Scope scope)
    {
        var userId = int.Parse(User.FindFirst("UserId").Value);

        var isMember = await _context.ProjectMembers
            .AnyAsync(x => x.ProjectId == projectId && x.UserId == userId);

        if (!isMember)
            return Forbid();

        scope.ProjectId = projectId;

        _context.Scopes.Add(scope);
        await _context.SaveChangesAsync();

        return Ok(scope);
    }
    
    [HttpDelete("{projectId}/remove-contractor/{contractorUserId}")]
    public async Task<IActionResult> RemoveContractor(
        int projectId,
        int contractorUserId)
    {
        var userId = int.Parse(User.FindFirst("UserId")!.Value);
    
        var isManager = await _context.ProjectMembers
            .AnyAsync(x =>
                x.ProjectId == projectId &&
                x.UserId == userId &&
                x.Role == ProjectRole.Manager);
    
        if (!isManager)
            return Forbid();
    
        var member = await _context.ProjectMembers
            .FirstOrDefaultAsync(x =>
                x.ProjectId == projectId &&
                x.UserId == contractorUserId &&
                x.Role == ProjectRole.Contractor);
    
        if (member == null)
            return NotFound("پیمانکار در این پروژه یافت نشد");
    
        _context.ProjectMembers.Remove(member);
        await _context.SaveChangesAsync();
    
        return Ok(new { message = "پیمانکار با موفقیت حذف شد" });
    }

    [HttpPost("{projectId}/add-contractor")]
    public async Task<IActionResult> AddContractor(
        int projectId,
        [FromBody] int contractorUserId)
    {
        var userId = int.Parse(User.FindFirst("UserId")!.Value);

        var isManager = await _context.ProjectMembers
            .AnyAsync(x =>
                x.ProjectId == projectId &&
                x.UserId == userId &&
                x.Role == ProjectRole.Manager);

        if (!isManager)
            return Forbid();

        var alreadyMember = await _context.ProjectMembers
            .AnyAsync(x =>
                x.ProjectId == projectId &&
                x.UserId == contractorUserId);

        if (alreadyMember)
            return BadRequest("کاربر قبلاً عضو پروژه است");

        _context.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = projectId,
            UserId = contractorUserId,
            Role = ProjectRole.Contractor
        });

        await _context.SaveChangesAsync();

        return Ok(new { message = "پیمانکار با موفقیت اضافه شد" });
    }

    

}

