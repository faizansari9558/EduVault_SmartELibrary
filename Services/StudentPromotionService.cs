using SmartELibrary.Enums;
using Microsoft.EntityFrameworkCore;
using SmartELibrary.Data;
using SmartELibrary.Models;
using SmartELibrary.ViewModels;

namespace SmartELibrary.Services;

public class StudentPromotionService(ApplicationDbContext dbContext) : IStudentPromotionService
{
    public async Task<SemesterPromotionDashboardViewModel> BuildDashboardAsync(int? semesterId = null)
    {
        var semesters = await dbContext.Semesters.AsNoTracking().ToListAsync();
        var orderedSemesters = semesters.OrderBy(GetSemesterOrder).ThenBy(x => x.Id).ToList();

        var currentSemester = semesterId.HasValue
            ? orderedSemesters.FirstOrDefault(x => x.Id == semesterId.Value)
            : DetermineCurrentSemester(orderedSemesters);

        if (currentSemester is null)
        {
            return new SemesterPromotionDashboardViewModel();
        }

        var currentIndex = orderedSemesters.FindIndex(x => x.Id == currentSemester.Id);
        var nextSemester = currentIndex >= 0 && currentIndex + 1 < orderedSemesters.Count
            ? orderedSemesters[currentIndex + 1]
            : null;

        var studentStates = await LoadStudentStatesAsync();
        var studentRows = studentStates
            .Where(x => x.ResolvedSemesterId == currentSemester.Id)
            .OrderBy(x => x.StudentName)
            .Select(x => new SemesterPromotionStudentRowViewModel
            {
                StudentId = x.StudentId,
                StudentName = x.StudentName,
                EnrollmentNumber = x.EnrollmentNumber,
                PhoneNumber = x.PhoneNumber,
                PromotionStatus = x.PromotionStatus.ToString(),
                CurrentSemesterName = x.ResolvedSemesterName
            })
            .ToList();

        return new SemesterPromotionDashboardViewModel
        {
            CurrentSemesterId = currentSemester.Id,
            CurrentSemesterName = currentSemester.Name,
            NextSemesterId = nextSemester?.Id,
            NextSemesterName = nextSemester?.Name ?? string.Empty,
            TotalStudents = studentRows.Count,
            StudentsToBePromoted = studentRows.Count,
            StudentsOnHold = studentRows.Count(x => string.Equals(x.PromotionStatus, PromotionStatus.Hold.ToString(), StringComparison.OrdinalIgnoreCase)),
            Students = studentRows
        };
    }

    public async Task UpdateHoldStatusesAsync(int semesterId, IReadOnlyCollection<int> holdStudentIds)
    {
        var resolvedStates = await LoadStudentStatesAsync();
        var holdLookup = new HashSet<int>(holdStudentIds);

        var targetStudentIds = resolvedStates
            .Where(x => x.ResolvedSemesterId == semesterId)
            .Select(x => x.StudentId)
            .ToHashSet();

        var students = await dbContext.Students
            .Where(x => targetStudentIds.Contains(x.UserId))
            .ToListAsync();

        foreach (var student in students)
        {
            var state = resolvedStates.FirstOrDefault(x => x.StudentId == student.UserId);
            if (state is null || state.ResolvedSemesterId != semesterId)
            {
                continue;
            }

            student.PromotionStatus = holdLookup.Contains(student.UserId)
                ? PromotionStatus.Hold
                : PromotionStatus.Auto;
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task<(bool Success, string Message)> PromoteStudentsAsync(int semesterId, int? adminUserId)
    {
        var dashboard = await BuildDashboardAsync(semesterId);
        if (dashboard.CurrentSemesterId == 0)
        {
            return (false, "Current semester not found.");
        }

        if (!dashboard.NextSemesterId.HasValue)
        {
            return (false, "Next semester not found.");
        }

        var currentSemesterId = dashboard.CurrentSemesterId;
        var nextSemesterId = dashboard.NextSemesterId.Value;
        var now = DateTime.UtcNow;

        var resolvedStates = await LoadStudentStatesAsync();
        var stateLookup = resolvedStates.ToDictionary(x => x.StudentId);
        var targetStudentIds = resolvedStates
            .Where(x => x.ResolvedSemesterId == currentSemesterId)
            .Select(x => x.StudentId)
            .ToHashSet();

        var students = await dbContext.Students
            .Where(x => targetStudentIds.Contains(x.UserId))
            .ToListAsync();

        var adminRecordId = adminUserId.HasValue
            ? await dbContext.Admins.Where(x => x.UserId == adminUserId.Value).Select(x => x.Id).FirstOrDefaultAsync()
            : (int?)null;

        // Delete old enrollments from current semester before promoting
        var oldEnrollments = await dbContext.StudentEnrollments
            .Where(x => x.SemesterId == currentSemesterId && targetStudentIds.Contains(x.StudentId))
            .ToListAsync();
        
        dbContext.StudentEnrollments.RemoveRange(oldEnrollments);
        await dbContext.SaveChangesAsync();

        var promotedCount = 0;
        foreach (var student in students)
        {
            if (!stateLookup.TryGetValue(student.UserId, out var state) || state.ResolvedSemesterId != currentSemesterId)
            {
                continue;
            }

            dbContext.StudentEnrollments.Add(new StudentEnrollment
            {
                StudentId = student.UserId,
                SemesterId = nextSemesterId,
                IsApproved = true,
                ApprovedByAdminId = adminRecordId,
                ApprovedAtUtc = now,
                EnrolledAtUtc = now
            });

            student.CurrentSemesterId = nextSemesterId;
            student.PromotionStatus = PromotionStatus.Auto;
            promotedCount++;
        }

        dbContext.PromotionLogs.Add(new PromotionLog
        {
            FromSemesterId = currentSemesterId,
            ToSemesterId = nextSemesterId,
            TotalPromoted = promotedCount,
            TotalHeld = dashboard.StudentsOnHold,
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync();

        return (true, $"Promotion completed. Promoted {promotedCount} student(s). Held {dashboard.StudentsOnHold} student(s).");
    }

    private static Semester? DetermineCurrentSemester(IReadOnlyList<Semester> semesters)
    {
        var activeSemester = semesters
            .Where(x => x.IsActive)
            .OrderBy(GetSemesterOrder)
            .ThenBy(x => x.Id)
            .FirstOrDefault();

        return activeSemester ?? semesters.OrderBy(GetSemesterOrder).ThenBy(x => x.Id).FirstOrDefault();
    }

    private static int GetSemesterOrder(Semester semester)
    {
        var digits = new string(semester.Name.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var value) ? value : int.MaxValue;
    }

    private async Task<List<StudentPromotionState>> LoadStudentStatesAsync()
    {
        var students = await dbContext.Students
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.CurrentSemester)
            .ToListAsync();

        var fallbackByStudentId = await BuildFallbackEnrollmentLookupAsync();
        return students.Select(student => ResolveState(student, fallbackByStudentId)).ToList();
    }

    private async Task<Dictionary<int, StudentEnrollment>> BuildFallbackEnrollmentLookupAsync()
    {
        var fallbackEnrollments = await dbContext.StudentEnrollments
            .AsNoTracking()
            .Include(x => x.Semester)
            .Where(x => x.IsApproved)
            .OrderByDescending(x => x.EnrolledAtUtc)
            .ToListAsync();

        return fallbackEnrollments
            .GroupBy(x => x.StudentId)
            .ToDictionary(g => g.Key, g => g.First());
    }

    private static StudentPromotionState ResolveState(
        Student student,
        IReadOnlyDictionary<int, StudentEnrollment> fallbackByStudentId)
    {
        var fallbackSemester = fallbackByStudentId.TryGetValue(student.UserId, out var fallbackEnrollment)
            ? fallbackEnrollment
            : null;

        return new StudentPromotionState
        {
            StudentId = student.UserId,
            StudentName = student.User?.FullName ?? "Unknown",
            EnrollmentNumber = student.EnrollmentNumber,
            PhoneNumber = student.User?.PhoneNumber ?? "-",
            PromotionStatus = student.PromotionStatus,
            ResolvedSemesterId = student.CurrentSemesterId ?? fallbackSemester?.SemesterId,
            ResolvedSemesterName = student.CurrentSemester?.Name ?? fallbackSemester?.Semester?.Name ?? string.Empty
        };
    }

    private sealed class StudentPromotionState
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string EnrollmentNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public PromotionStatus PromotionStatus { get; set; }
        public int? ResolvedSemesterId { get; set; }
        public string ResolvedSemesterName { get; set; } = string.Empty;
    }
}
