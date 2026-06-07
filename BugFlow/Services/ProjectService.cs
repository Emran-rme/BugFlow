using BugFlow.DTOs;
using BugFlow.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BugFlow.Services;
public class ProjectService
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;

    public ProjectService(AppDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>(); 

    }

    public async Task<Project> CreateProject(Project project)
    {
        project.Code = Guid.NewGuid()
            .ToString("N")
            .Substring(0, 8)
            .ToUpper();

        _context.Projects.Add(project);

        await _context.SaveChangesAsync();

        return project;
    }

    public async Task<InviteUserResult> InviteUser(
        int projectId,
        string name,
        string phone,
        ProjectRole role)
    {
        string? tempPassword = null;

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Username == phone);

        if (user == null)
        {
            user = new User
            {
                Username = phone,
                Name = name
            };

            tempPassword = Guid.NewGuid().ToString("N").Substring(0, 8);

            user.Password = _passwordHasher.HashPassword(user, tempPassword);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        var exists = await _context.ProjectMembers
            .AnyAsync(x =>
                x.ProjectId == projectId &&
                x.UserId == user.Id);

        if (exists)
            return new InviteUserResult { Success = false };

        _context.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = projectId,
            UserId = user.Id,
            Role = role
        });

        await _context.SaveChangesAsync();

        return new InviteUserResult
        {
            Success = true,
            TempPassword = tempPassword
        };
    }


}