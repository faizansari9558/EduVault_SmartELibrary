using SmartELibrary.ViewModels;

namespace SmartELibrary.Services;

public interface IStudentPromotionService
{
    Task<SemesterPromotionDashboardViewModel> BuildDashboardAsync(int? semesterId = null);

    Task UpdateHoldStatusesAsync(int semesterId, IReadOnlyCollection<int> holdStudentIds);

    Task<(bool Success, string Message)> PromoteStudentsAsync(int semesterId, int? adminUserId);
}