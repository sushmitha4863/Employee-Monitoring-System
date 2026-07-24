using EmployeeMonitoringAPI.Data;
using EmployeeMonitoringAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EmployeeMonitoringAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public DocumentsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        [Authorize(Roles = "Employee,Manager,HR")]
        public async Task<IActionResult> GetDocuments()
        {
            var docs = await _context.DocumentFiles
                .Include(x => x.UploadedBy)
                .OrderByDescending(x => x.UploadedAt)
                .Select(x => new
                {
                    x.DocumentId,
                    x.OriginalFileName,
                    x.ContentType,
                    x.Size,
                    x.Category,
                    x.UploadedAt,
                    UploadedBy = x.UploadedBy != null ? x.UploadedBy.FullName : null
                })
                .ToListAsync();
            return Ok(docs);
        }

        [HttpPost("upload")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Upload(IFormFile file, [FromForm] string category = "General")
        {
            if (file == null || file.Length == 0) return BadRequest(new { message = "File is required" });
            var uploadsRoot = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "documents");
            Directory.CreateDirectory(uploadsRoot);
            var storedName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var fullPath = Path.Combine(uploadsRoot, storedName);
            await using (var stream = new FileStream(fullPath, FileMode.Create)) await file.CopyToAsync(stream);
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var document = new DocumentFile { FileName = storedName, OriginalFileName = file.FileName, ContentType = file.ContentType, Size = file.Length, Category = category, FilePath = fullPath, UploadedById = userId, UploadedAt = DateTime.UtcNow };
            _context.DocumentFiles.Add(document);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Document uploaded", document.DocumentId });
        }

        [HttpGet("{id}/download")]
        [Authorize(Roles = "Employee,Manager,HR")]
        public async Task<IActionResult> Download(int id)
        {
            var document = await _context.DocumentFiles.FirstOrDefaultAsync(x => x.DocumentId == id);
            if (document == null || !System.IO.File.Exists(document.FilePath)) return NotFound(new { message = "Document not found" });
            return PhysicalFile(document.FilePath, document.ContentType, document.OriginalFileName);
        }

        [HttpGet("{id}/preview")]
        [Authorize(Roles = "Employee,Manager,HR")]
        public async Task<IActionResult> Preview(int id)
        {
            var document = await _context.DocumentFiles.FirstOrDefaultAsync(x => x.DocumentId == id);
            if (document == null || !System.IO.File.Exists(document.FilePath)) return NotFound(new { message = "Document not found" });
            return PhysicalFile(document.FilePath, document.ContentType);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Delete(int id)
        {
            var document = await _context.DocumentFiles.FirstOrDefaultAsync(x => x.DocumentId == id);
            if (document == null) return NotFound(new { message = "Document not found" });
            if (System.IO.File.Exists(document.FilePath)) System.IO.File.Delete(document.FilePath);
            _context.DocumentFiles.Remove(document);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Document deleted" });
        }
    }
}
