using Microsoft.EntityFrameworkCore;
using SmartELibrary.Data;

namespace SmartELibrary.Services;

public interface IStudentSemesterApprovalService
{
    Task<bool> HasApprovedEnrollmentAsync(int studentUserId);
}

public class StudentSemesterApprovalService(ApplicationDbContext dbContext) : IStudentSemesterApprovalService
{
    public Task<bool> HasApprovedEnrollmentAsync(int studentUserId)
    {
        return dbContext.StudentEnrollments.AnyAsync(x => x.StudentId == studentUserId && x.IsApproved);
    }
}
