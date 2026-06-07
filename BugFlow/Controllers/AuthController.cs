using System.Security.Cryptography;
using BugFlow.DTOs;
using BugFlow.Models;
using BugFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BugFlow.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthController(
        AppDbContext context,
        TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordHasher = new PasswordHasher<User>(); 
    }
    [HttpPost("register-company")]
    public async Task<IActionResult> Register(RegisterCompanyDto dto)
    {
        var admin = new User
        {
            Username = dto.Username
        };

        admin.Password = _passwordHasher.HashPassword(admin, dto.Password);

        _context.Users.Add(admin);
        await _context.SaveChangesAsync();

        var company = new Company
        {
            Name = dto.CompanyName,
            RegistrationNumber = dto.RegistrationNumber,
            OwnerId = admin.Id
        };

        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        var token = _tokenService.GenerateCompanyToken(admin);


        return Ok(new
        {
            CompanyId = company.Id,
            Token = token
        });
    }

  [AllowAnonymous]
[HttpPost("login")]
public async Task<IActionResult> Login(LoginDto dto)
{
    var user = await _context.Users
        .FirstOrDefaultAsync(x => x.Username == dto.Phone);

    if (user == null)
        return Unauthorized(new { message = "کاربر یافت نشد" });

    var result = _passwordHasher.VerifyHashedPassword(
        user,
        user.Password,
        dto.Password
    );

    if (result != PasswordVerificationResult.Success)
        return Unauthorized(new { message = "پسورد اشتباه است" });

    var company = await _context.Companies
        .FirstOrDefaultAsync(x => x.OwnerId == user.Id);

    if (company != null)
    {
        var token = _tokenService.GenerateCompanyToken(user);

        var companyProjects = await _context.Projects
            .Where(p => p.CompanyId == company.Id)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Code,
                p.StartDate,
                p.EndDate,
                p.TestType
            })
            .ToListAsync();

        return Ok(new
        {
            Type = "CompanyOwner",
            Token = token,
            Company = new
            {
                company.Id,
                company.Name,
                company.RegistrationNumber
            },
            Projects = companyProjects
        });
    }

    var projects = await _context.ProjectMembers
        .Where(x => x.UserId == user.Id)
        .Include(x => x.Project)
        .Select(x => new
        {
            x.ProjectId,
            ProjectName = x.Project.Name,
            x.Project.Code,
            x.Project.StartDate,
            x.Project.EndDate,
            x.Project.TestType,
            Role = x.Role.ToString()
        })
        .ToListAsync();

    if (projects.Count == 0)
    {
        return Ok(new
        {
            Type = "User",
            Message = "پروژه‌ای برای شما ثبت نشده است",
            Projects = new List<object>()
        });
    }

    if (projects.Count == 1)
    {
        var member = await _context.ProjectMembers
            .Include(x => x.User)
            .FirstAsync(x => x.UserId == user.Id);

        var token = _tokenService.GenerateProjectToken(
            member.User,
            member.ProjectId,
            member.Role
        );

        return Ok(new
        {
            Type = "SingleProject",
            Token = token,
            CurrentProject = projects.First(),
            Projects = projects
        });
    }

    var firstProject = await _context.ProjectMembers
        .Include(x => x.User)
        .FirstAsync(x => x.UserId == user.Id);

    var multiToken = _tokenService.GenerateProjectToken(
        firstProject.User,
        firstProject.ProjectId,
        firstProject.Role
    );

    return Ok(new
    {
        Type = "MultiProject",
        Token = multiToken,
        CurrentProject = projects.First(),
        Projects = projects
    });
}



    
    [HttpPost("select-project")]
    public async Task<IActionResult> SelectProject(SelectProjectDto dto)
    {
        var member = await _context.ProjectMembers
            .Include(x => x.User)
            .Include(x => x.Project)
            .FirstOrDefaultAsync(x =>
                x.UserId == dto.UserId &&
                x.ProjectId == dto.ProjectId);

        if (member == null)
            return Unauthorized();

        var token = _tokenService.GenerateProjectToken(
            member.User,
            member.ProjectId,
            member.Role
        );


        return Ok(new
        {
            Token = token,
            Role = member.Role.ToString(),
            ProjectName = member.Project.Name
        });
    }

    

    // [HttpPost("contractor-login")]
    // public async Task<IActionResult> ContractorLogin(LoginDto dto)
    // {
    //     var project = await _context.Projects
    //         .Include(x => x.Members)
    //         .ThenInclude(x => x.User)
    //         .FirstOrDefaultAsync(x => x.Code == dto.ProjectCode);
    //
    //     if (project == null)
    //         return NotFound("پروژه یافت نشد");
    //
    //     var member = project.Members
    //         .FirstOrDefault(x => x.User.Phone == dto.Phone);
    //
    //     if (member == null)
    //         return Unauthorized();
    //
    //     var token = _tokenService.Generate(
    //         dto.Phone,
    //         member.Role.ToString());
    //
    //     return Ok(new
    //     {
    //         token,
    //         project.Name
    //     });
    // }
}
