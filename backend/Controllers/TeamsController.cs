using EmployeeMonitoringAPI.Data;
using EmployeeMonitoringAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeMonitoringAPI.DTOs;

namespace EmployeeMonitoringAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TeamsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TeamsController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetTeams()
        {
            var teams = await _context.Teams
                .Include(t => t.Manager)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Description,
                    t.ManagerId,

                    Manager =
                        t.Manager != null
                        ? t.Manager.FullName
                        : null
                })
                .ToListAsync();

            return Ok(teams);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTeam(
            int id)
        {
            var team = await _context.Teams
                .Include(t => t.Manager)
                .FirstOrDefaultAsync(
                    t => t.Id == id);

            if (team == null)
                return NotFound();

            return Ok(team);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,HR")]

        public async Task<IActionResult> CreateTeam(
    CreateTeamDto dto)
        {
            var team = new Team
            {
                Name = dto.Name,
                Description = dto.Description,
                ManagerId = dto.ManagerId
            };

            _context.Teams.Add(team);

            await _context.SaveChangesAsync();

            return Ok(team);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> UpdateTeam(
    int id,
    UpdateTeamDto dto)
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(x => x.Id == id);

            if (team == null)
                return NotFound();

            team.Name = dto.Name;
            team.Description = dto.Description;
            team.ManagerId = dto.ManagerId;

            await _context.SaveChangesAsync();

            return Ok(team);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTeam(
            int id)
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(
                    x => x.Id == id);

            if (team == null)
                return NotFound();

            _context.Teams.Remove(team);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Team deleted successfully"
            });
        }
        [HttpPost("{teamId}/members/{userId}")]
        [Authorize(Roles = "Admin,HR,Manager")]
        public async Task<IActionResult> AssignMember(
    int teamId,
    int userId)
        {
            var exists = await _context.TeamMembers
                .AnyAsync(x =>
                    x.TeamId == teamId &&
                    x.UserId == userId);

            if (exists)
            {
                return BadRequest(new
                {
                    message = "Employee already assigned"
                });
            }

            var teamMember = new TeamMember
            {
                TeamId = teamId,
                UserId = userId
            };

            _context.TeamMembers.Add(teamMember);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Employee assigned successfully"
            });
        }
        [HttpGet("{teamId}/members")]
        public async Task<IActionResult> GetTeamMembers(
    int teamId)
        {
            var members = await _context.TeamMembers
                .Include(x => x.User)
                .Where(x => x.TeamId == teamId)
                .Select(x => new
                {
                    x.UserId,

                    FullName =
                        x.User != null
                        ? x.User.FullName
                        : null,

                    Email =
                        x.User != null
                        ? x.User.Email
                        : null,

                    Role =
                        x.User != null
                        ? x.User.Role
                        : null
                })
                .ToListAsync();

            return Ok(members);
        }
        [HttpDelete("{teamId}/members/{userId}")]
        [Authorize(Roles = "Admin,HR,Manager")]
        public async Task<IActionResult> RemoveMember(
    int teamId,
    int userId)
        {
            var member = await _context.TeamMembers
                .FirstOrDefaultAsync(x =>
                    x.TeamId == teamId &&
                    x.UserId == userId);

            if (member == null)
                return NotFound();

            _context.TeamMembers.Remove(member);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Member removed successfully"
            });
        }
    }
}