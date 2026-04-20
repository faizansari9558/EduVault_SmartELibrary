namespace SmartELibrary.ViewModels;

public class AdminUsersViewModel
{
    public string RoleFilter { get; set; } = "All";

    public string SearchTerm { get; set; } = string.Empty;

    public IReadOnlyList<AdminUserRowViewModel> Users { get; set; } = Array.Empty<AdminUserRowViewModel>();
}
