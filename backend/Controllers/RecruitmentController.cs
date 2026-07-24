using System.Security.Claims;
using EmployeeMonitoringAPI.Data;
using EmployeeMonitoringAPI.Interfaces;
using EmployeeMonitoringAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeMonitoringAPI.Controllers;

[ApiController, Route("api/hr/recruitment"), Authorize(Roles = "HR")]
public sealed class RecruitmentController(ApplicationDbContext db, IAuditService audit, IWebHostEnvironment environment) : ControllerBase
{
    static readonly string[] ValidStatuses = ["Applied", "Under Review", "Shortlisted", "Interview Scheduled", "Selected", "Rejected", "Withdrawn"];
    static readonly Dictionary<string, string[]> Transitions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Applied"] = ["Under Review", "Withdrawn"],
        ["Under Review"] = ["Shortlisted", "Rejected", "Withdrawn"],
        ["Shortlisted"] = ["Interview Scheduled", "Rejected", "Withdrawn"],
        ["Interview Scheduled"] = ["Selected", "Rejected", "Withdrawn"]
    };
    int HrId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    IQueryable<Applicant> OwnedApplications() => db.Applicants.Where(x => x.AssignedHrId == HrId());
    IQueryable<RecruitmentJob> OwnedJobs() => db.RecruitmentJobs.Where(x => x.AssignedHrId == HrId());

    [HttpGet("jobs")]
    public async Task<IActionResult> Jobs(string? search = null, string? department = null, string? status = null, int page = 1, int pageSize = 10)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 5, 100);
        var q = OwnedJobs();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.Title.Contains(search) || x.JobCode.Contains(search));
        if (!string.IsNullOrWhiteSpace(department)) q = q.Where(x => x.Department == department);
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
        var total = await q.CountAsync(); var now = DateTime.UtcNow.Date;
        var items = await q.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new
        {
            x.JobId, x.Title, x.JobCode, x.Department, x.Designation, x.EmploymentType, x.NumberOfOpenings,
            x.OpeningDate, x.ClosingDate, x.Status, x.RequiredExperience, x.RequiredSkills, x.Location, x.Description,
            totalApplicantsCount = db.Applicants.Count(a => a.AssignedHrId == x.AssignedHrId && a.JobId == x.JobId),
            shortlistedApplicantsCount = db.Applicants.Count(a => a.AssignedHrId == x.AssignedHrId && a.JobId == x.JobId && (a.Status == "Shortlisted" || a.Status == "Interview Scheduled")),
            selectedApplicantsCount = db.Applicants.Count(a => a.AssignedHrId == x.AssignedHrId && a.JobId == x.JobId && a.Status == "Selected"),
            acceptingApplications = x.Status == "Open" && x.OpeningDate <= now && (!x.ClosingDate.HasValue || x.ClosingDate.Value.Date >= now)
        }).ToListAsync();
        return Ok(Page(items, total, page, pageSize));
    }

    [HttpGet("jobs/{id:int}")]
    public async Task<IActionResult> Job(int id)
    {
        var x = await OwnedJobs().FirstOrDefaultAsync(j => j.JobId == id);
        return x == null ? NotFound() : Ok(x);
    }

    [HttpPost("jobs")]
    public async Task<IActionResult> CreateJob(JobRequest r)
    {
        var error = await ValidateJob(r); if (error != null) return error;
        var x = new RecruitmentJob { AssignedHrId = HrId(), CreatedAt = DateTime.UtcNow }; Apply(x, r); db.Add(x);
        await db.SaveChangesAsync(); await audit.RecordAsync("RecruitmentJobCreated", "RecruitmentJob", x.JobId.ToString(), actorUserId: HrId(), safeMetadata: new { x.JobCode, x.Status });
        return CreatedAtAction(nameof(Job), new { id = x.JobId }, new { message = "Job opening created", x.JobId });
    }

    [HttpPut("jobs/{id:int}")]
    public async Task<IActionResult> UpdateJob(int id, JobRequest r)
    {
        var x = await OwnedJobs().FirstOrDefaultAsync(j => j.JobId == id); if (x == null) return NotFound();
        var error = await ValidateJob(r, id); if (error != null) return error; Apply(x, r); await db.SaveChangesAsync();
        await audit.RecordAsync("RecruitmentJobUpdated", "RecruitmentJob", id.ToString(), actorUserId: HrId(), safeMetadata: new { x.JobCode, x.Status });
        return Ok(new { message = "Job opening updated" });
    }

    [HttpPatch("jobs/{id:int}/status")]
    public async Task<IActionResult> JobStatus(int id, RecruitmentStatusRequest r)
    {
        if (r.Status is not ("Open" or "Closed" or "On Hold" or "Filled" or "Cancelled")) return BadRequest(new { message = "Invalid job status" });
        var x = await OwnedJobs().FirstOrDefaultAsync(j => j.JobId == id); if (x == null) return NotFound();
        if (r.Status == "Open" && (x.OpeningDate.Date > DateTime.UtcNow.Date || x.ClosingDate?.Date < DateTime.UtcNow.Date)) return Conflict(new { message = "Opening dates do not allow applications" });
        x.Status = r.Status; await db.SaveChangesAsync(); await audit.RecordAsync("RecruitmentJobStatusChanged", "RecruitmentJob", id.ToString(), actorUserId: HrId(), safeMetadata: new { r.Status });
        return Ok(new { message = $"Job marked {r.Status}" });
    }

    [HttpGet("applicants")]
    public async Task<IActionResult> Applicants(string? search = null, int? jobId = null, string? department = null, string? status = null, DateTime? from = null, DateTime? to = null, string? experienceLevel = null, string sort = "newest", int page = 1, int pageSize = 10)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 5, 100);
        var q = OwnedApplications().Include(x => x.Job).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.FullName.Contains(search) || x.Email.Contains(search) || x.Phone.Contains(search));
        if (jobId.HasValue) q = q.Where(x => x.JobId == jobId); if (!string.IsNullOrWhiteSpace(department)) q = q.Where(x => x.Job!.Department == department);
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status); if (from.HasValue) q = q.Where(x => x.AppliedAt >= from.Value.Date);
        if (to.HasValue) q = q.Where(x => x.AppliedAt < to.Value.Date.AddDays(1)); if (!string.IsNullOrWhiteSpace(experienceLevel)) q = q.Where(x => x.ExperienceLevel == experienceLevel);
        q = sort == "oldest" ? q.OrderBy(x => x.AppliedAt) : sort == "name" ? q.OrderBy(x => x.FullName) : q.OrderByDescending(x => x.AppliedAt);
        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new
        {
            x.ApplicantId, x.JobId, jobTitle = x.Job != null ? x.Job.Title : null, jobCode = x.Job != null ? x.Job.JobCode : null, department = x.Job != null ? x.Job.Department : null,
            x.FullName, x.Email, x.Phone, x.AppliedAt, x.Qualification, x.Experience, x.ExperienceLevel, x.Status, interviewStatus = x.Interviews.OrderByDescending(i => i.ScheduledAt).Select(i => i.Status).FirstOrDefault(),
            assignedHr = db.Users.Where(u => u.UserId == x.AssignedHrId).Select(u => u.FullName).FirstOrDefault(), x.ResumeUrl, x.Remarks
        }).ToListAsync();
        return Ok(Page(items, total, page, pageSize));
    }

    [HttpGet("applicants/{id:int}")]
    public async Task<IActionResult> Applicant(int id)
    {
        var x = await OwnedApplications().Include(a => a.Job).Include(a => a.StatusHistory).Include(a => a.Interviews).FirstOrDefaultAsync(a => a.ApplicantId == id);
        if (x == null) return NotFound();
        return Ok(new { application = new { x.ApplicantId, x.FullName, x.Email, x.Phone, x.Qualification, x.Skills, x.Experience, x.ExperienceLevel, x.ResumeUrl, x.Remarks, x.RejectionReason, x.Status, x.AppliedAt, job = x.Job == null ? null : new { x.Job.JobId, x.Job.Title, x.Job.JobCode, x.Job.Department } }, statusHistory = x.StatusHistory.OrderByDescending(h => h.ChangedAt), interviews = x.Interviews.OrderByDescending(i => i.ScheduledAt), onboarding = await db.OnboardingRecords.FirstOrDefaultAsync(o => o.AssignedHrId == HrId() && o.ApplicantId == id) });
    }

    [HttpPost("applicants")]
    public async Task<IActionResult> CreateApplicant(ApplicantRequest r)
    {
        var job = await OwnedJobs().FirstOrDefaultAsync(x => x.JobId == r.JobId); if (job == null) return NotFound(new { message = "Job opening not found" });
        var today = DateTime.UtcNow.Date; if (job.Status != "Open" || job.OpeningDate.Date > today || job.ClosingDate?.Date < today) return Conflict(new { message = "Opening is closed, inactive, expired, or not yet open" });
        var email = r.Email.Trim().ToUpperInvariant(); var candidate = await db.RecruitmentCandidates.FirstOrDefaultAsync(x => x.AssignedHrId == HrId() && x.NormalizedEmail == email); if (candidate != null && await OwnedApplications().AnyAsync(x => x.JobId == r.JobId && x.CandidateId == candidate.CandidateId)) return Conflict(new { message = "This candidate has already applied for this job opening" });
        if (string.IsNullOrWhiteSpace(r.FullName) || string.IsNullOrWhiteSpace(r.Email)) return BadRequest(new { message = "Applicant name and email are required" });
        candidate ??= new RecruitmentCandidate { AssignedHrId = HrId(), FullName = r.FullName.Trim(), Email = r.Email.Trim(), NormalizedEmail = email, Phone = r.Phone.Trim() }; if (candidate.CandidateId == 0) { db.Add(candidate); await db.SaveChangesAsync(); } var x = new Applicant { AssignedHrId = HrId(), JobId = r.JobId, CandidateId = candidate.CandidateId, FullName = r.FullName.Trim(), Email = r.Email.Trim(), NormalizedEmail = email, Phone = r.Phone.Trim(), Qualification = r.Qualification.Trim(), Skills = r.Skills.Trim(), Experience = r.Experience.Trim(), ExperienceLevel = r.ExperienceLevel.Trim(), ResumeUrl = r.ResumeUrl?.Trim() ?? "", Remarks = r.Remarks?.Trim() ?? "", Status = "Applied", AppliedAt = DateTime.UtcNow };
        db.Add(x); await db.SaveChangesAsync(); AddHistory(x, "", "Applied", "Application received", r.Remarks); await db.SaveChangesAsync();
        await audit.RecordAsync("JobApplicationCreated", "Applicant", x.ApplicantId.ToString(), actorUserId: HrId(), safeMetadata: new { x.JobId });
        return CreatedAtAction(nameof(Applicant), new { id = x.ApplicantId }, new { message = "Application created", x.ApplicantId });
    }

    [HttpPatch("applicants/{id:int}/status")]
    public async Task<IActionResult> ChangeStatus(int id, RecruitmentStatusRequest r)
    {
        var x = await OwnedApplications().FirstOrDefaultAsync(a => a.ApplicantId == id); if (x == null) return NotFound();
        if (!ValidStatuses.Contains(r.Status) || !Transitions.TryGetValue(x.Status, out var allowed) || !allowed.Contains(r.Status)) return Conflict(new { message = $"Cannot move application from {x.Status} to {r.Status}" });
        if (r.Status == "Rejected" && string.IsNullOrWhiteSpace(r.Reason)) return BadRequest(new { message = "A rejection reason is required" });
        var old = x.Status; x.Status = r.Status; if (r.Status == "Rejected") x.RejectionReason = r.Reason!.Trim(); if (!string.IsNullOrWhiteSpace(r.Remarks)) x.Remarks = r.Remarks.Trim(); AddHistory(x, old, r.Status, r.Reason, r.Remarks); NotifyHr(StatusTitle(r.Status), $"{x.FullName} moved to {r.Status}.", $"recruitment:{x.ApplicantId}:{r.Status}:{DateTime.UtcNow.Ticks}");
        await db.SaveChangesAsync(); await audit.RecordAsync("ApplicationStatusChanged", "Applicant", id.ToString(), actorUserId: HrId(), safeMetadata: new { From = old, To = r.Status });
        return Ok(new { message = $"Candidate moved to {r.Status}" });
    }

    [HttpPut("applicants/{id:int}/remarks")]
    public async Task<IActionResult> Remarks(int id, RemarksRequest r)
    {
        var x = await OwnedApplications().FirstOrDefaultAsync(a => a.ApplicantId == id); if (x == null) return NotFound(); x.Remarks = r.Remarks.Trim(); await db.SaveChangesAsync(); return Ok(new { message = "HR remarks saved" });
    }

    [HttpPost("applicants/{id:int}/resume")]
    [RequestSizeLimit(5_242_880)]
    public async Task<IActionResult> UploadResume(int id, IFormFile file)
    {
        var x = await OwnedApplications().FirstOrDefaultAsync(a => a.ApplicantId == id); if (x == null) return NotFound();
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant(); if (file.Length == 0 || file.Length > 5_242_880 || ext is not (".pdf" or ".doc" or ".docx")) return BadRequest(new { message = "Resume must be PDF, DOC, or DOCX and no larger than 5 MB" });
        var dir = Path.Combine(environment.ContentRootPath, "App_Data", "resumes"); Directory.CreateDirectory(dir); var stored = $"{Guid.NewGuid():N}{ext}";
        await using (var stream = System.IO.File.Create(Path.Combine(dir, stored))) await file.CopyToAsync(stream); x.ResumeUrl = stored; await db.SaveChangesAsync(); return Ok(new { message = "Resume uploaded" });
    }

    [HttpGet("applicants/{id:int}/resume")]
    public async Task<IActionResult> Resume(int id)
    {
        var x = await OwnedApplications().FirstOrDefaultAsync(a => a.ApplicantId == id); if (x == null) return NotFound(); if (string.IsNullOrWhiteSpace(x.ResumeUrl)) return NotFound(new { message = "No resume uploaded" });
        var path = Path.Combine(environment.ContentRootPath, "App_Data", "resumes", Path.GetFileName(x.ResumeUrl)); if (!System.IO.File.Exists(path)) return NotFound(new { message = "Resume file is unavailable" });
        return PhysicalFile(path, Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase) ? "application/pdf" : "application/octet-stream", $"{SafeName(x.FullName)}-resume{Path.GetExtension(path)}");
    }

    [HttpGet("interviews")]
    public async Task<IActionResult> Interviews(int? applicantId = null, string? status = null, int page = 1, int pageSize = 10)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 5, 100); var q = db.Interviews.Where(x => x.AssignedHrId == HrId()).Include(x => x.Applicant).ThenInclude(a => a!.Job).AsQueryable();
        if (applicantId.HasValue) q = q.Where(x => x.ApplicantId == applicantId); if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status); var total = await q.CountAsync();
        var items = await q.OrderByDescending(x => x.ScheduledAt).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new { x.InterviewId, x.ApplicantId, candidate = x.Applicant!.FullName, jobTitle = x.Applicant.Job != null ? x.Applicant.Job.Title : null, jobCode = x.Applicant.Job != null ? x.Applicant.Job.JobCode : null, x.Round, x.ScheduledAt, x.EndTime, x.InterviewerId, interviewer = db.Users.Where(u => u.UserId == x.InterviewerId).Select(u => u.FullName).FirstOrDefault(), x.Mode, x.Location, x.MeetingUrl, x.Status, x.Feedback, x.Result, x.HrRemarks }).ToListAsync();
        return Ok(Page(items, total, page, pageSize));
    }

    [HttpPost("interviews")]
    public async Task<IActionResult> ScheduleInterview(InterviewRequest r)
    {
        var a = await OwnedApplications().FirstOrDefaultAsync(x => x.ApplicantId == r.ApplicantId); if (a == null) return NotFound(); if (a.Status is not ("Shortlisted" or "Interview Scheduled")) return Conflict(new { message = "Only shortlisted candidates can be scheduled" });
        var error = await ValidateInterview(r); if (error != null) return error; var x = new Interview { AssignedHrId = HrId() }; Apply(x, r); db.Add(x); if (a.Status == "Shortlisted") { AddHistory(a, a.Status, "Interview Scheduled", "Interview scheduled", r.HrRemarks); a.Status = "Interview Scheduled"; }
        await db.SaveChangesAsync(); NotifyHr("Interview scheduled", $"{a.FullName}: {x.Round} on {x.ScheduledAt:g}.", $"interview:{x.InterviewId}:scheduled"); if (x.InterviewerId.HasValue) Notify(x.InterviewerId.Value, "Interview scheduled", $"Interview with {a.FullName} on {x.ScheduledAt:g}.", $"interviewer:{x.InterviewId}:scheduled"); await db.SaveChangesAsync(); await audit.RecordAsync("InterviewScheduled", "Interview", x.InterviewId.ToString(), actorUserId: HrId(), safeMetadata: new { x.ApplicantId, x.ScheduledAt });
        return Ok(new { message = "Interview scheduled", x.InterviewId });
    }

    [HttpPut("interviews/{id:int}")]
    public async Task<IActionResult> Reschedule(int id, InterviewRequest r)
    {
        var x = await db.Interviews.Include(i => i.Applicant).FirstOrDefaultAsync(i => i.InterviewId == id && i.AssignedHrId == HrId()); if (x == null) return NotFound(); var error = await ValidateInterview(r); if (error != null) return error; Apply(x, r); x.Status = "Rescheduled"; await db.SaveChangesAsync(); NotifyHr("Interview rescheduled", $"{x.Applicant!.FullName}: {x.Round} moved to {x.ScheduledAt:g}.", $"interview:{id}:rescheduled:{DateTime.UtcNow.Ticks}"); if (x.InterviewerId.HasValue) Notify(x.InterviewerId.Value, "Interview rescheduled", $"Interview with {x.Applicant.FullName} moved to {x.ScheduledAt:g}.", $"interviewer:{id}:rescheduled:{DateTime.UtcNow.Ticks}"); await db.SaveChangesAsync(); await audit.RecordAsync("InterviewRescheduled", "Interview", id.ToString(), actorUserId: HrId(), safeMetadata: new { x.ScheduledAt }); return Ok(new { message = "Interview rescheduled" });
    }

    [HttpPatch("interviews/{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, RemarksRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Remarks)) return BadRequest(new { message = "Cancellation reason is required" }); var x = await db.Interviews.Include(i => i.Applicant).FirstOrDefaultAsync(i => i.InterviewId == id && i.AssignedHrId == HrId()); if (x == null) return NotFound(); if (x.Status is "Completed" or "Cancelled") return Conflict(new { message = "Interview cannot be cancelled" }); x.Status = "Cancelled"; x.HrRemarks = r.Remarks.Trim(); NotifyHr("Interview cancelled", $"{x.Applicant!.FullName}: {x.Round} cancelled.", $"interview:{id}:cancelled"); if (x.InterviewerId.HasValue) Notify(x.InterviewerId.Value, "Interview cancelled", $"Interview with {x.Applicant.FullName} was cancelled.", $"interviewer:{id}:cancelled"); await db.SaveChangesAsync(); await audit.RecordAsync("InterviewCancelled", "Interview", id.ToString(), actorUserId: HrId()); return Ok(new { message = "Interview cancelled" });
    }

    [HttpPatch("interviews/{id:int}/feedback")]
    public async Task<IActionResult> Feedback(int id, FeedbackRequest r)
    {
        if (r.Result is not ("Passed" or "Failed")) return BadRequest(new { message = "Result must be Passed or Failed" }); if (string.IsNullOrWhiteSpace(r.Feedback)) return BadRequest(new { message = "Feedback is required" });
        var x = await db.Interviews.Include(i => i.Applicant).FirstOrDefaultAsync(i => i.InterviewId == id && i.AssignedHrId == HrId()); if (x == null) return NotFound(); if (x.Status == "Cancelled") return Conflict(new { message = "Cancelled interview cannot receive feedback" }); x.Status = "Completed"; x.Result = r.Result; x.Feedback = r.Feedback.Trim(); x.HrRemarks = r.HrRemarks?.Trim() ?? x.HrRemarks;
        if (r.Result == "Passed" && r.MoveToSelected && x.Applicant!.Status == "Interview Scheduled") { AddHistory(x.Applicant, x.Applicant.Status, "Selected", "Interview passed", r.HrRemarks); x.Applicant.Status = "Selected"; NotifyHr("Candidate selected", $"{x.Applicant.FullName} was selected.", $"recruitment:{x.ApplicantId}:selected"); }
        await db.SaveChangesAsync(); await audit.RecordAsync("InterviewFeedbackRecorded", "Interview", id.ToString(), actorUserId: HrId(), safeMetadata: new { r.Result, r.MoveToSelected }); return Ok(new { message = "Interview feedback recorded" });
    }

    [HttpPost("applicants/{id:int}/offer")]
    public async Task<IActionResult> GenerateOffer(int id, OfferLetterRequest r)
    {
        var a = await OwnedApplications().Include(x => x.Job).FirstOrDefaultAsync(x => x.ApplicantId == id); if (a == null) return NotFound(); if (a.Status != "Selected") return Conflict(new { message = "Only selected candidates can receive an offer" }); if (r.JoiningDate.Date < DateTime.UtcNow.Date) return BadRequest(new { message = "Joining date cannot be in the past" });
        var dir = Path.Combine(environment.ContentRootPath, "App_Data", "offers"); Directory.CreateDirectory(dir); var stored = $"offer-{a.ApplicantId}-{Guid.NewGuid():N}.pdf"; var lines = new[] { "EMPLOYMENT OFFER", $"Candidate: {a.FullName}", $"Position: {r.Designation ?? a.Job?.Designation ?? a.Job?.Title}", $"Department: {a.Job?.Department}", $"Joining date: {r.JoiningDate:yyyy-MM-dd}", $"Annual compensation: {r.AnnualCompensation:0.00}", "This offer remains subject to successful onboarding verification." }; await System.IO.File.WriteAllBytesAsync(Path.Combine(dir, stored), OfferPdf(lines)); a.OfferLetterUrl = stored; a.OfferStatus = "Issued"; a.OfferedAt = DateTime.UtcNow; NotifyHr("Offer letter generated", $"Offer generated for {a.FullName}.", $"recruitment:{id}:offer"); await db.SaveChangesAsync(); await audit.RecordAsync("OfferLetterGenerated", "Applicant", id.ToString(), actorUserId: HrId(), safeMetadata: new { r.JoiningDate, r.AnnualCompensation }); return Ok(new { message = "Offer letter generated", a.OfferStatus, a.OfferedAt });
    }

    [HttpGet("applicants/{id:int}/offer")]
    public async Task<IActionResult> DownloadOffer(int id)
    {
        var a = await OwnedApplications().FirstOrDefaultAsync(x => x.ApplicantId == id); if (a == null) return NotFound(); if (string.IsNullOrWhiteSpace(a.OfferLetterUrl)) return NotFound(new { message = "No offer letter exists" }); var path = Path.Combine(environment.ContentRootPath, "App_Data", "offers", Path.GetFileName(a.OfferLetterUrl)); if (!System.IO.File.Exists(path)) return NotFound(new { message = "Offer letter file is unavailable" }); return PhysicalFile(path, "application/pdf", $"{SafeName(a.FullName)}-offer.pdf");
    }
    [HttpPost("applicants/{id:int}/onboarding")]
    public async Task<IActionResult> MoveToOnboarding(int id, OnboardingRequest r)
    {
        var a = await OwnedApplications().Include(x => x.Job).FirstOrDefaultAsync(x => x.ApplicantId == id); if (a == null) return NotFound(); if (a.Status != "Selected") return Conflict(new { message = "Only selected candidates can move to onboarding" }); if (await db.OnboardingRecords.AnyAsync(x => x.AssignedHrId == HrId() && x.ApplicantId == id)) return Conflict(new { message = "Onboarding already exists for this application" });
        var x = new OnboardingRecord { AssignedHrId = HrId(), ApplicantId = id, CandidateName = a.FullName, Role = r.Role is "Manager" ? "Manager" : "Employee", JoiningDate = r.JoiningDate, DepartmentId = r.DepartmentId, Department = a.Job?.Department ?? "", Designation = a.Job?.Designation ?? a.Job?.Title ?? "", EmploymentType = a.Job?.EmploymentType ?? "Full-time", ManagerId = r.ManagerId, OfficialEmail = a.Email, EmployeeCode = r.EmployeeCode.Trim(), Checklist = "Candidate Selected", Remarks = r.Remarks?.Trim() ?? "", Status = "Pending" };
        db.Add(x); NotifyHr("Candidate moved to onboarding", $"{a.FullName} is now in onboarding.", $"recruitment:{id}:onboarding"); await db.SaveChangesAsync(); await audit.RecordAsync("ApplicationMovedToOnboarding", "Applicant", id.ToString(), actorUserId: HrId(), safeMetadata: new { x.OnboardingId }); return Ok(new { message = "Candidate moved to onboarding; no user account was created", x.OnboardingId });
    }

    async Task<IActionResult?> ValidateJob(JobRequest r, int? id = null) { if (string.IsNullOrWhiteSpace(r.Title) || string.IsNullOrWhiteSpace(r.JobCode)) return BadRequest(new { message = "Title and job code are required" }); if (r.NumberOfOpenings < 1) return BadRequest(new { message = "At least one opening is required" }); if (r.ClosingDate.HasValue && r.ClosingDate.Value.Date < r.OpeningDate.Date) return BadRequest(new { message = "Closing date cannot precede opening date" }); if (await OwnedJobs().AnyAsync(x => x.JobCode == r.JobCode.Trim() && x.JobId != id)) return Conflict(new { message = "Job code already exists" }); return null; }
    async Task<IActionResult?> ValidateInterview(InterviewRequest r) { if (r.EndTime <= r.ScheduledAt) return BadRequest(new { message = "End time must be after start time" }); if (r.ScheduledAt < DateTime.UtcNow.AddMinutes(-1)) return BadRequest(new { message = "Interview must be scheduled in the future" }); if (r.Mode is not ("Online" or "Offline")) return BadRequest(new { message = "Mode must be Online or Offline" }); if (r.Mode == "Online" && string.IsNullOrWhiteSpace(r.MeetingUrl)) return BadRequest(new { message = "Meeting URL is required for online interviews" }); if (r.Mode == "Offline" && string.IsNullOrWhiteSpace(r.Location)) return BadRequest(new { message = "Location is required for offline interviews" }); if (r.InterviewerId.HasValue && !await db.Users.AnyAsync(x => x.UserId == r.InterviewerId && x.AssignedHrId == HrId() && x.IsActive)) return StatusCode(403, new { message = "Interviewer is outside your HR scope or inactive" }); return null; }
    static void Apply(RecruitmentJob x, JobRequest r) { x.Title = r.Title.Trim(); x.JobCode = r.JobCode.Trim(); x.Department = r.Department.Trim(); x.Designation = r.Designation.Trim(); x.EmploymentType = r.EmploymentType; x.NumberOfOpenings = r.NumberOfOpenings; x.RequiredExperience = r.RequiredExperience?.Trim() ?? ""; x.RequiredSkills = r.RequiredSkills?.Trim() ?? ""; x.Location = r.Location?.Trim() ?? ""; x.Description = r.Description?.Trim() ?? ""; x.OpeningDate = r.OpeningDate.Date; x.ClosingDate = r.ClosingDate?.Date; x.Status = r.Status; }
    static void Apply(Interview x, InterviewRequest r) { x.ApplicantId = r.ApplicantId; x.InterviewerId = r.InterviewerId; x.Round = r.Round.Trim(); x.ScheduledAt = r.ScheduledAt; x.EndTime = r.EndTime; x.Mode = r.Mode; x.Location = r.Location?.Trim() ?? ""; x.MeetingUrl = r.MeetingUrl?.Trim() ?? ""; x.Status = "Scheduled"; x.Result = "Pending"; x.HrRemarks = r.HrRemarks?.Trim() ?? ""; }
    void AddHistory(Applicant x, string from, string to, string? reason, string? remarks) => db.Add(new RecruitmentStatusHistory { ApplicantId = x.ApplicantId, AssignedHrId = HrId(), FromStatus = from, ToStatus = to, Reason = reason?.Trim() ?? "", Remarks = remarks?.Trim() ?? "", ChangedById = HrId(), ChangedAt = DateTime.UtcNow });
    void NotifyHr(string title, string message, string key) => Notify(HrId(), title, message, key);
    void Notify(int id, string title, string message, string key) => db.Add(new Notification { UserId = id, Title = title, Message = message, Type = "Recruitment", RelatedUrl = "/hr/recruitment", DeduplicationKey = key });
    static string StatusTitle(string status) => status switch { "Shortlisted" => "Candidate shortlisted", "Selected" => "Candidate selected", "Rejected" => "Candidate rejected", _ => "Application updated" };
    static string SafeName(string value) => string.Concat(value.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
    static byte[] OfferPdf(IEnumerable<string> lines) { var text = string.Join("\\n", lines).Replace("(", "[").Replace(")", "]"); var body = $"BT /F1 12 Tf 50 780 Td ({text.Replace("\\n", ") Tj 0 -24 Td (")}) Tj ET"; var objects = new[] { "<< /Type /Catalog /Pages 2 0 R >>", "<< /Type /Pages /Kids [3 0 R] /Count 1 >>", "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>", $"<< /Length {System.Text.Encoding.ASCII.GetByteCount(body)} >>\nstream\n{body}\nendstream", "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>" }; var b = new System.Text.StringBuilder("%PDF-1.4\n"); var offsets = new List<int>(); for (var i = 0; i < objects.Length; i++) { offsets.Add(System.Text.Encoding.ASCII.GetByteCount(b.ToString())); b.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n"); } var start = System.Text.Encoding.ASCII.GetByteCount(b.ToString()); b.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n"); foreach (var o in offsets) b.Append($"{o:0000000000} 00000 n \n"); b.Append($"trailer << /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{start}\n%%EOF"); return System.Text.Encoding.ASCII.GetBytes(b.ToString()); }
    static object Page<T>(List<T> items, int total, int page, int pageSize) => new { items, totalCount = total, page, pageSize, totalPages = (int)Math.Ceiling(total / (double)pageSize) };
}

public sealed class JobRequest { public string Title { get; set; } = ""; public string JobCode { get; set; } = ""; public string Department { get; set; } = ""; public string Designation { get; set; } = ""; public string EmploymentType { get; set; } = "Full-time"; public int NumberOfOpenings { get; set; } = 1; public string? RequiredExperience { get; set; } public string? RequiredSkills { get; set; } public string? Location { get; set; } public string? Description { get; set; } public DateTime OpeningDate { get; set; } public DateTime? ClosingDate { get; set; } public string Status { get; set; } = "Draft"; }
public sealed class ApplicantRequest { public int JobId { get; set; } public string FullName { get; set; } = ""; public string Email { get; set; } = ""; public string Phone { get; set; } = ""; public string Qualification { get; set; } = ""; public string Skills { get; set; } = ""; public string Experience { get; set; } = ""; public string ExperienceLevel { get; set; } = ""; public string? ResumeUrl { get; set; } public string? Remarks { get; set; } }
public sealed class RecruitmentStatusRequest { public string Status { get; set; } = ""; public string? Reason { get; set; } public string? Remarks { get; set; } }
public sealed class RemarksRequest { public string Remarks { get; set; } = ""; }
public sealed class InterviewRequest { public int ApplicantId { get; set; } public int? InterviewerId { get; set; } public string Round { get; set; } = "Round 1"; public DateTime ScheduledAt { get; set; } public DateTime EndTime { get; set; } public string Mode { get; set; } = "Online"; public string? Location { get; set; } public string? MeetingUrl { get; set; } public string? HrRemarks { get; set; } }
public sealed class FeedbackRequest { public string Feedback { get; set; } = ""; public string Result { get; set; } = ""; public string? HrRemarks { get; set; } public bool MoveToSelected { get; set; } }
public sealed class OfferLetterRequest { public string? Designation { get; set; } public DateTime JoiningDate { get; set; } public decimal AnnualCompensation { get; set; } }
public sealed class OnboardingRequest { public string Role { get; set; } = "Employee"; public DateTime JoiningDate { get; set; } public int? DepartmentId { get; set; } public int? ManagerId { get; set; } public string EmployeeCode { get; set; } = ""; public string? Remarks { get; set; } }
