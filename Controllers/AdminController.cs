using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartELibrary.Data;
using SmartELibrary.Filters;
using SmartELibrary.Enums;
using SmartELibrary.Models;
using SmartELibrary.Services;
using SmartELibrary.ViewModels;
using ClosedXML.Excel;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace SmartELibrary.Controllers;

[RoleAuthorize(UserRole.Admin)]
public class AdminController(ApplicationDbContext dbContext, IProgressService progressService, IStudentPromotionService studentPromotionService) : Controller
{
    public async Task<IActionResult> Dashboard()
    {
        ViewBag.TotalUsers = await dbContext.Users.CountAsync();
        ViewBag.TotalStudents = await dbContext.Users.CountAsync(x => x.Role == UserRole.Student);
        ViewBag.TotalTeachers = await dbContext.Users.CountAsync(x => x.Role == UserRole.Teacher);
        ViewBag.TotalMaterials = await dbContext.Materials.CountAsync();
        ViewBag.TotalQuizzes = await dbContext.Quizzes.CountAsync();
        ViewBag.PendingStudentRegistrations = await dbContext.StudentOnboardingRecords.CountAsync(x => !x.IsRegistered);
        ViewBag.PendingTeacherFirstLogin = await dbContext.Users.CountAsync(x => x.Role == UserRole.Teacher && x.IsFirstLogin && x.IsApproved);
        return View();
    }

    [HttpGet]
    public IActionResult TeachersOnboarding() => RedirectToAction(nameof(Teachers));

    [HttpGet]
    public IActionResult StudentsOnboarding() => RedirectToAction(nameof(StudentOnboarding));

    [HttpGet]
    public async Task<IActionResult> StudentOnboarding()
    {
        var rows = await LoadAllStudentRowsAsync();

        var model = new AdminStudentOnboardingPageViewModel
        {
            Rows = rows
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult DownloadStudentOnboardingTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Students");

        var headers = new[] { "EnrollmentNo", "Name", "Email", "Phone", "DateOfBirth", "Semester" };
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }

        var samples = new[]
        {
            new[] { "ENR2026001", "Aarav Sharma", "aarav.sharma@example.edu", "9876543210", "2006-05-14", "Semester - 1" },
            new[] { "ENR2026002", "Diya Patel", "diya.patel@example.edu", "9876543211", "2006-07-22", "Semester - 1" },
            new[] { "ENR2026003", "Kabir Mehta", "kabir.mehta@example.edu", "9876543212", "2005-12-03", "Semester - 2" }
        };

        for (var row = 0; row < samples.Length; row++)
        {
            for (var col = 0; col < samples[row].Length; col++)
            {
                sheet.Cell(row + 2, col + 1).Value = samples[row][col];
            }
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "student_onboarding_sample.xlsx");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddStudentManually(string enrollmentNo, string name, string email, string phone, DateTime? dateOfBirth, int semesterId)
    {
        if (string.IsNullOrWhiteSpace(enrollmentNo)
            || string.IsNullOrWhiteSpace(name)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(phone)
            || !dateOfBirth.HasValue
            || semesterId <= 0)
        {
            TempData["Error"] = "Enrollment No, name, phone, email, date of birth, and semester are required.";
            return RedirectToAction(nameof(StudentOnboarding));
        }

        var normalizedEnrollmentNo = enrollmentNo.Trim();
        var normalizedName = name.Trim();
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var normalizedPhone = phone.Trim();

        var enrollmentInOnboarding = await dbContext.StudentOnboardingRecords.AnyAsync(x => x.EnrollmentNo == normalizedEnrollmentNo);
        var enrollmentInStudents = await dbContext.Students.AnyAsync(x => x.EnrollmentNumber == normalizedEnrollmentNo);
        var phoneInUse = await dbContext.Users.AnyAsync(x => x.PhoneNumber == normalizedPhone);
        var emailInUse = await dbContext.Users.AnyAsync(x => x.Email != null && x.Email.ToLower() == normalizedEmail);

        if (enrollmentInOnboarding || enrollmentInStudents)
        {
            TempData["Error"] = "Enrollment number already exists.";
            return RedirectToAction(nameof(StudentOnboarding));
        }

        if (phoneInUse)
        {
            TempData["Error"] = "Phone number already exists.";
            return RedirectToAction(nameof(StudentOnboarding));
        }

        if (emailInUse)
        {
            TempData["Error"] = "Email already exists.";
            return RedirectToAction(nameof(StudentOnboarding));
        }

        await CreateStudentAccountAsync(normalizedEnrollmentNo, normalizedName, normalizedEmail, normalizedPhone, dateOfBirth.Value.Date, semesterId);
        TempData["Success"] = "Student added successfully.";
        return RedirectToAction(nameof(StudentOnboarding));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadStudentOnboardingExcel(StudentOnboardingUploadViewModel upload)
    {
        var result = new StudentOnboardingImportResultViewModel();

        if (upload.ExcelFile is null || upload.ExcelFile.Length == 0)
        {
            result.Errors.Add("Please select an Excel file (.xlsx).");
            return await BuildStudentOnboardingViewAsync(result);
        }

        if (!Path.GetExtension(upload.ExcelFile.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            result.Errors.Add("Only .xlsx files are allowed.");
            return await BuildStudentOnboardingViewAsync(result);
        }

        var semesters = await dbContext.Semesters.ToListAsync();
        var semesterLookup = semesters.ToDictionary(x => x.Name.Trim().ToLowerInvariant(), x => x.Id);
        var emailValidator = new EmailAddressAttribute();

        var rowsToInsert = new List<(string EnrollmentNo, string Name, string Email, string Phone, DateTime DateOfBirth, int SemesterId)>();
        var seenEnrollmentNos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var stream = upload.ExcelFile.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet is null)
        {
            result.Errors.Add("The workbook does not contain any worksheet.");
            return await BuildStudentOnboardingViewAsync(result);
        }

        var usedRange = worksheet.RangeUsed();
        if (usedRange is null)
        {
            result.Errors.Add("The worksheet is empty.");
            return await BuildStudentOnboardingViewAsync(result);
        }

        var headerRow = worksheet.Row(1);
        var headers = Enumerable.Range(1, 6)
            .Select(i => headerRow.Cell(i).GetString().Trim())
            .ToList();

        var expectedHeaders = new[] { "EnrollmentNo", "Name", "Email", "Phone", "DateOfBirth", "Semester" };
        for (var i = 0; i < expectedHeaders.Length; i++)
        {
            if (!string.Equals(headers[i], expectedHeaders[i], StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add($"Header mismatch at column {i + 1}. Expected '{expectedHeaders[i]}'.");
            }
        }

        if (result.Errors.Count > 0)
        {
            return await BuildStudentOnboardingViewAsync(result);
        }

        var lastRow = usedRange.LastRow().RowNumber();
        for (var rowIndex = 2; rowIndex <= lastRow; rowIndex++)
        {
            result.TotalRows++;
            var row = worksheet.Row(rowIndex);

            var enrollmentNo = row.Cell(1).GetString().Trim();
            var name = row.Cell(2).GetString().Trim();
            var email = row.Cell(3).GetString().Trim();
            var phone = row.Cell(4).GetString().Trim();
            var dobRaw = row.Cell(5).GetString().Trim();
            var semesterRaw = row.Cell(6).GetString().Trim();

            if (string.IsNullOrWhiteSpace(enrollmentNo)
                || string.IsNullOrWhiteSpace(name)
                || string.IsNullOrWhiteSpace(email)
                || string.IsNullOrWhiteSpace(phone)
                || string.IsNullOrWhiteSpace(dobRaw)
                || string.IsNullOrWhiteSpace(semesterRaw))
            {
                result.Errors.Add($"Row {rowIndex}: all fields are required.");
                continue;
            }

            if (!emailValidator.IsValid(email))
            {
                result.Errors.Add($"Row {rowIndex}: invalid email format.");
                continue;
            }

            if (!DateTime.TryParse(dobRaw, out var dob))
            {
                result.Errors.Add($"Row {rowIndex}: invalid DateOfBirth.");
                continue;
            }

            if (!seenEnrollmentNos.Add(enrollmentNo))
            {
                result.Errors.Add($"Row {rowIndex}: duplicate EnrollmentNo in the file.");
                continue;
            }

            if (!semesterLookup.TryGetValue(semesterRaw.ToLowerInvariant(), out var semesterId))
            {
                var newSemester = new Semester
                {
                    Name = semesterRaw,
                    IsActive = true
                };

                dbContext.Semesters.Add(newSemester);
                await dbContext.SaveChangesAsync();
                semesterId = newSemester.Id;
                semesterLookup[semesterRaw.ToLowerInvariant()] = semesterId;
            }

            var enrollmentExistsInOnboarding = await dbContext.StudentOnboardingRecords
                .AnyAsync(x => x.EnrollmentNo == enrollmentNo);

            if (enrollmentExistsInOnboarding)
            {
                result.Errors.Add($"Row {rowIndex}: EnrollmentNo already imported.");
                continue;
            }

            var enrollmentExistsInRegisteredStudents = await dbContext.Students
                .AnyAsync(x => x.EnrollmentNumber == enrollmentNo);

            if (enrollmentExistsInRegisteredStudents)
            {
                result.Errors.Add($"Row {rowIndex}: EnrollmentNo already registered in student accounts.");
                continue;
            }

            rowsToInsert.Add((
                EnrollmentNo: enrollmentNo,
                Name: name,
                Email: email,
                Phone: phone,
                DateOfBirth: dob.Date,
                SemesterId: semesterId));
        }

        if (rowsToInsert.Count > 0)
        {
            foreach (var rowData in rowsToInsert)
            {
                await CreateStudentAccountAsync(rowData.EnrollmentNo, rowData.Name, rowData.Email, rowData.Phone, rowData.DateOfBirth, rowData.SemesterId);
                result.ImportedRows++;
            }
        }

        TempData["Success"] = $"Import finished. Added {result.ImportedRows} student account(s).";

        return await BuildStudentOnboardingViewAsync(result);
    }

    private async Task<IActionResult> BuildStudentOnboardingViewAsync(StudentOnboardingImportResultViewModel result)
    {
        var rows = await LoadAllStudentRowsAsync();

        var model = new AdminStudentOnboardingPageViewModel
        {
            LastImportResult = result,
            Rows = rows
        };

        return View("StudentOnboarding", model);
    }

    private async Task<List<StudentOnboardingRowViewModel>> LoadAllStudentRowsAsync()
    {
        return await dbContext.Students
            .Include(x => x.User)
            .Include(x => x.CurrentSemester)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new StudentOnboardingRowViewModel
            {
                EnrollmentNo = x.EnrollmentNumber,
                Name = x.User != null ? x.User.FullName : string.Empty,
                Email = x.User != null && x.User.Email != null ? x.User.Email : string.Empty,
                Phone = x.User != null ? x.User.PhoneNumber : string.Empty,
                DateOfBirth = dbContext.StudentOnboardingRecords
                    .Where(r => r.EnrollmentNo == x.EnrollmentNumber)
                    .Select(r => (DateTime?)r.DateOfBirth)
                    .FirstOrDefault() ?? x.CreatedAtUtc.Date,
                SemesterName = x.CurrentSemester != null ? x.CurrentSemester.Name : "-",
                IsRegistered = true,
                ImportedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();
    }

    private async Task CreateStudentAccountAsync(string enrollmentNo, string name, string email, string phone, DateTime dateOfBirth, int semesterId)
    {
        var adminUserId = HttpContext.Session.GetInt32("UserId");
        var adminId = adminUserId.HasValue
            ? await dbContext.Admins.Where(x => x.UserId == adminUserId.Value).Select(x => x.Id).FirstOrDefaultAsync()
            : (int?)null;

        var user = new User
        {
            FullName = name,
            PhoneNumber = phone,
            Email = email,
            PasswordHash = PasswordService.HashPassword(phone),
            Role = UserRole.Student,
            IsApproved = true,
            IsFirstLogin = true,
            EnrollmentNo = enrollmentNo,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        dbContext.Students.Add(new Student
        {
            UserId = user.Id,
            EnrollmentNumber = enrollmentNo,
            CurrentSemesterId = semesterId,
            PromotionStatus = PromotionStatus.Auto,
            CreatedAtUtc = DateTime.UtcNow
        });

        dbContext.StudentEnrollments.Add(new StudentEnrollment
        {
            StudentId = user.Id,
            SemesterId = semesterId,
            IsApproved = true,
            ApprovedByAdminId = adminId,
            ApprovedAtUtc = DateTime.UtcNow,
            EnrolledAtUtc = DateTime.UtcNow
        });

        dbContext.StudentOnboardingRecords.Add(new StudentOnboardingRecord
        {
            EnrollmentNo = enrollmentNo,
            Name = name,
            Email = email,
            Phone = phone,
            DateOfBirth = dateOfBirth,
            SemesterId = semesterId,
            IsRegistered = true,
            RegisteredUserId = user.Id,
            RegisteredAtUtc = DateTime.UtcNow,
            ImportedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Teachers()
    {
        var model = new TeacherManagementPageViewModel
        {
            Teachers = await GetTeacherRowsAsync()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult DownloadTeacherTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Teachers");

        var headers = new[] { "TeacherId", "Name", "CodeName", "Email", "Phone" };
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }

        var samples = new[]
        {
            new[] { "TID1001", "Riya Sharma", "riya.sharma", "riya.sharma@college.edu", "9876500011" },
            new[] { "TID1002", "Arjun Mehta", "arjun.mehta", "arjun.mehta@college.edu", "9876500012" },
            new[] { "TID1003", "Neha Patel", "neha.patel", "neha.patel@college.edu", "9876500013" }
        };

        for (var row = 0; row < samples.Length; row++)
        {
            for (var col = 0; col < samples[row].Length; col++)
            {
                sheet.Cell(row + 2, col + 1).Value = samples[row][col];
            }
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "teachers_sample.xlsx");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTeacher(TeacherManualCreateViewModel manual)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please fix validation errors before adding teacher.";
            return await BuildTeacherViewAsync();
        }

        var teacherId = string.IsNullOrWhiteSpace(manual.TeacherId)
            ? await GenerateTeacherIdAsync()
            : manual.TeacherId.Trim();

        if (await dbContext.Teachers.AnyAsync(x => x.TeacherId == teacherId))
        {
            TempData["Error"] = "Teacher ID already exists.";
            return await BuildTeacherViewAsync();
        }

        var normalizedEmail = manual.Email.Trim().ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(x => x.Email != null && x.Email.ToLower() == normalizedEmail))
        {
            TempData["Error"] = "Email already exists.";
            return await BuildTeacherViewAsync();
        }

        var normalizedPhone = manual.Phone.Trim();
        if (await dbContext.Users.AnyAsync(x => x.PhoneNumber == normalizedPhone))
        {
            TempData["Error"] = "Phone number already exists.";
            return await BuildTeacherViewAsync();
        }

        var codeName = await ResolveUniqueCodeNameAsync(manual.CodeName, manual.Name);
        if (await IsTeacherCodeNameInUseAsync(codeName))
        {
            TempData["Error"] = "Code Name already exists.";
            return await BuildTeacherViewAsync();
        }

        var bootstrapPassword = normalizedPhone;

        var user = new User
        {
            FullName = manual.Name.Trim(),
            PhoneNumber = normalizedPhone,
            Email = normalizedEmail,
            PasswordHash = PasswordService.HashPassword(bootstrapPassword),
            Role = UserRole.Teacher,
            IsApproved = true,
            IsFirstLogin = true,
            EnrollmentNo = codeName,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        dbContext.Teachers.Add(new Teacher
        {
            UserId = user.Id,
            TeacherId = teacherId,
            AssignedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        TempData["Success"] = "Teacher added. First login password is the phone number until they change it.";
        return RedirectToAction(nameof(Teachers));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadTeachersExcel(TeacherUploadViewModel upload)
    {
        var result = new TeacherImportResultViewModel();
        var emailValidator = new EmailAddressAttribute();
        var phoneValidator = new PhoneAttribute();

        if (upload.ExcelFile is null || upload.ExcelFile.Length == 0)
        {
            result.Errors.Add("Please select an Excel file (.xlsx).");
            return await BuildTeacherViewAsync(result);
        }

        if (!Path.GetExtension(upload.ExcelFile.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            result.Errors.Add("Only .xlsx files are allowed.");
            return await BuildTeacherViewAsync(result);
        }

        using var stream = upload.ExcelFile.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.FirstOrDefault();
        if (sheet is null || sheet.RangeUsed() is null)
        {
            result.Errors.Add("Worksheet is empty.");
            return await BuildTeacherViewAsync(result);
        }

        var headerValues = Enumerable.Range(1, 5)
            .Select(i => sheet.Cell(1, i).GetString().Trim())
            .ToList();

        var usesCodeNameColumn = headerValues.Count >= 5
            && string.Equals(headerValues[2], "CodeName", StringComparison.OrdinalIgnoreCase);

        var expectedHeaders = usesCodeNameColumn
            ? new[] { "TeacherId", "Name", "CodeName", "Email", "Phone" }
            : new[] { "TeacherId", "Name", "Phone", "Email" };

        for (var i = 0; i < expectedHeaders.Length; i++)
        {
            var value = headerValues[i];
            if (!string.Equals(value, expectedHeaders[i], StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add($"Header mismatch at column {i + 1}. Expected '{expectedHeaders[i]}'.");
            }
        }

        if (result.Errors.Count > 0)
        {
            return await BuildTeacherViewAsync(result);
        }

        var seenTeacherIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenCodeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var rowsToCreate = new List<(User user, Teacher teacher)>();
        var lastRow = sheet.RangeUsed()!.LastRow().RowNumber();

        for (var rowIndex = 2; rowIndex <= lastRow; rowIndex++)
        {
            result.TotalRows++;

            var teacherIdRaw = sheet.Cell(rowIndex, 1).GetString().Trim();
            var nameRaw = sheet.Cell(rowIndex, 2).GetString().Trim();
            var codeNameRaw = usesCodeNameColumn ? sheet.Cell(rowIndex, 3).GetString().Trim() : string.Empty;
            var emailRaw = usesCodeNameColumn
                ? sheet.Cell(rowIndex, 4).GetString().Trim().ToLowerInvariant()
                : sheet.Cell(rowIndex, 4).GetString().Trim().ToLowerInvariant();
            var phoneRaw = usesCodeNameColumn
                ? sheet.Cell(rowIndex, 5).GetString().Trim()
                : sheet.Cell(rowIndex, 3).GetString().Trim();

            if (string.IsNullOrWhiteSpace(teacherIdRaw)
                || string.IsNullOrWhiteSpace(nameRaw)
                || string.IsNullOrWhiteSpace(emailRaw)
                || string.IsNullOrWhiteSpace(phoneRaw))
            {
                result.Errors.Add($"Row {rowIndex}: required field missing.");
                continue;
            }

            if (!emailValidator.IsValid(emailRaw))
            {
                result.Errors.Add($"Row {rowIndex}: invalid email format.");
                continue;
            }

            if (!phoneValidator.IsValid(phoneRaw))
            {
                result.Errors.Add($"Row {rowIndex}: invalid phone number.");
                continue;
            }

            if (!seenTeacherIds.Add(teacherIdRaw))
            {
                result.Errors.Add($"Row {rowIndex}: duplicate TeacherId in file.");
                continue;
            }

            if (!seenEmails.Add(emailRaw))
            {
                result.Errors.Add($"Row {rowIndex}: duplicate Email in file.");
                continue;
            }

            var finalCodeName = await ResolveUniqueCodeNameAsync(codeNameRaw, nameRaw, rowsToCreate.Select(x => x.user.EnrollmentNo).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>());
            if (!seenCodeNames.Add(finalCodeName))
            {
                result.Errors.Add($"Row {rowIndex}: duplicate CodeName in file.");
                continue;
            }

            if (await dbContext.Teachers.AnyAsync(x => x.TeacherId == teacherIdRaw))
            {
                result.Errors.Add($"Row {rowIndex}: TeacherId already exists.");
                continue;
            }

            if (await dbContext.Users.AnyAsync(x => x.Email != null && x.Email.ToLower() == emailRaw))
            {
                result.Errors.Add($"Row {rowIndex}: Email already exists.");
                continue;
            }

            if (await dbContext.Users.AnyAsync(x => x.PhoneNumber == phoneRaw))
            {
                result.Errors.Add($"Row {rowIndex}: Phone already exists.");
                continue;
            }

            if (await IsTeacherCodeNameInUseAsync(finalCodeName))
            {
                result.Errors.Add($"Row {rowIndex}: CodeName already exists.");
                continue;
            }

            var bootstrapPassword = phoneRaw;
            var user = new User
            {
                FullName = nameRaw,
                PhoneNumber = phoneRaw,
                Email = emailRaw,
                PasswordHash = PasswordService.HashPassword(bootstrapPassword),
                Role = UserRole.Teacher,
                IsApproved = true,
                IsFirstLogin = true,
                EnrollmentNo = finalCodeName,
                CreatedAtUtc = DateTime.UtcNow
            };

            var teacher = new Teacher
            {
                TeacherId = teacherIdRaw,
                AssignedAtUtc = DateTime.UtcNow
            };

            rowsToCreate.Add((user, teacher));
        }

        foreach (var row in rowsToCreate)
        {
            dbContext.Users.Add(row.user);
            await dbContext.SaveChangesAsync();

            row.teacher.UserId = row.user.Id;
            dbContext.Teachers.Add(row.teacher);
            await dbContext.SaveChangesAsync();
            result.ImportedRows++;
        }

        if (result.ImportedRows > 0)
        {
            TempData["Success"] = $"Teacher import completed. Added {result.ImportedRows} teachers.";
        }

        return await BuildTeacherViewAsync(result);
    }

    [HttpGet]
    public async Task<IActionResult> EditTeacher(int userId)
    {
        var teacherRecord = await dbContext.Teachers
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (teacherRecord?.User is null)
        {
            return NotFound();
        }

        var model = new EditTeacherViewModel
        {
            UserId = teacherRecord.UserId,
            TeacherId = teacherRecord.TeacherId,
            Name = teacherRecord.User.FullName,
            CodeName = teacherRecord.User.EnrollmentNo ?? string.Empty,
            Email = teacherRecord.User.Email ?? string.Empty,
            Phone = teacherRecord.User.PhoneNumber,
            IsActive = teacherRecord.User.IsApproved
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTeacher(EditTeacherViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var teacherRecord = await dbContext.Teachers
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.UserId == model.UserId);

        if (teacherRecord?.User is null)
        {
            return NotFound();
        }

        var codeName = model.CodeName.Trim();
        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var phone = model.Phone.Trim();
        var teacherId = model.TeacherId.Trim();

        if (await dbContext.Teachers.AnyAsync(x => x.TeacherId == teacherId && x.UserId != model.UserId))
        {
            ModelState.AddModelError(nameof(model.TeacherId), "Teacher ID already exists.");
            return View(model);
        }

        if (await dbContext.Users.AnyAsync(x => x.Id != model.UserId && x.Email != null && x.Email.ToLower() == normalizedEmail))
        {
            ModelState.AddModelError(nameof(model.Email), "Email already exists.");
            return View(model);
        }

        if (await dbContext.Users.AnyAsync(x => x.Id != model.UserId && x.PhoneNumber == phone))
        {
            ModelState.AddModelError(nameof(model.Phone), "Phone already exists.");
            return View(model);
        }

        if (await dbContext.Users.AnyAsync(x => x.Id != model.UserId && x.Role == UserRole.Teacher && x.EnrollmentNo == codeName))
        {
            ModelState.AddModelError(nameof(model.CodeName), "Code Name already exists.");
            return View(model);
        }

        teacherRecord.TeacherId = teacherId;
        teacherRecord.User.FullName = model.Name.Trim();
        teacherRecord.User.Email = normalizedEmail;
        teacherRecord.User.PhoneNumber = phone;
        teacherRecord.User.EnrollmentNo = codeName;
        teacherRecord.User.IsApproved = model.IsActive;

        await dbContext.SaveChangesAsync();

        TempData["Success"] = "Teacher updated successfully.";
        return RedirectToAction(nameof(Teachers));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateTeacher(int userId)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId && x.Role == UserRole.Teacher);
        if (user is null)
        {
            TempData["Error"] = "Teacher not found.";
            return RedirectToAction(nameof(Teachers));
        }

        user.IsApproved = false;
        await dbContext.SaveChangesAsync();

        TempData["Success"] = "Teacher deactivated successfully.";
        return RedirectToAction(nameof(Teachers));
    }

    private async Task<TeacherManagementPageViewModel> BuildTeacherModelAsync(TeacherImportResultViewModel? importResult = null)
    {
        return new TeacherManagementPageViewModel
        {
            LastImportResult = importResult,
            Teachers = await GetTeacherRowsAsync()
        };
    }

    private async Task<IActionResult> BuildTeacherViewAsync(TeacherImportResultViewModel? importResult = null)
    {
        var model = await BuildTeacherModelAsync(importResult);
        return View("Teachers", model);
    }

    private async Task<IReadOnlyList<TeacherRowViewModel>> GetTeacherRowsAsync()
    {
        return await dbContext.Teachers
            .Include(x => x.User)
            .Where(x => x.User != null)
            .OrderBy(x => x.TeacherId)
            .Select(x => new TeacherRowViewModel
            {
                UserId = x.UserId,
                TeacherId = x.TeacherId,
                Name = x.User!.FullName,
                CodeName = x.User.EnrollmentNo ?? string.Empty,
                Email = x.User.Email ?? string.Empty,
                Phone = x.User.PhoneNumber,
                IsActive = x.User.IsApproved,
                IsFirstLogin = x.User.IsFirstLogin
            })
            .ToListAsync();
    }

    private async Task<string> GenerateTeacherIdAsync()
    {
        var lastTeacherId = await dbContext.Teachers
            .OrderByDescending(x => x.Id)
            .Select(x => x.TeacherId)
            .FirstOrDefaultAsync();

        if (!string.IsNullOrWhiteSpace(lastTeacherId)
            && lastTeacherId.StartsWith("TID", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(lastTeacherId[3..], out var parsed))
        {
            return $"TID{parsed + 1:0000}";
        }

        return "TID0001";
    }

    private async Task<string> ResolveUniqueCodeNameAsync(string? requestedCodeName, string fullName, IEnumerable<string>? reservedCodeNames = null)
    {
        var baseCodeName = string.IsNullOrWhiteSpace(requestedCodeName)
            ? BuildBaseCodeNameFromName(fullName)
            : NormalizeCodeName(requestedCodeName);

        if (string.IsNullOrWhiteSpace(baseCodeName))
        {
            baseCodeName = "teacher";
        }

        var candidate = baseCodeName;
        var suffix = 1;
        var reserved = new HashSet<string>((reservedCodeNames ?? Array.Empty<string>()).Select(NormalizeCodeName), StringComparer.OrdinalIgnoreCase);

        while (reserved.Contains(candidate) || await IsTeacherCodeNameInUseAsync(candidate))
        {
            suffix++;
            candidate = $"{baseCodeName}{suffix}";
        }

        return candidate;
    }

    private async Task<bool> IsTeacherCodeNameInUseAsync(string codeName)
    {
        return await dbContext.Users
            .AnyAsync(x => x.Role == UserRole.Teacher && x.EnrollmentNo == codeName);
    }

    private static string BuildBaseCodeNameFromName(string fullName)
    {
        var normalized = NormalizeCodeName(fullName);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "teacher";
        }

        return normalized.Length > 18 ? normalized[..18] : normalized;
    }

    private static string NormalizeCodeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var allowed = value.Trim().ToLowerInvariant().Where(ch => char.IsLetterOrDigit(ch) || ch == '.' || ch == '_' || ch == '-');
        return new string(allowed.ToArray());
    }

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
        var bytes = RandomNumberGenerator.GetBytes(12);
        var builder = new StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(chars[b % chars.Length]);
        }

        return builder.ToString();
    }

    public async Task<IActionResult> Users(string? role, string? search)
    {
        var roleFilter = string.IsNullOrWhiteSpace(role) ? "All" : role.Trim();
        roleFilter = roleFilter.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                     || roleFilter.Equals("Teacher", StringComparison.OrdinalIgnoreCase)
                     || roleFilter.Equals("Student", StringComparison.OrdinalIgnoreCase)
                     || roleFilter.Equals("All", StringComparison.OrdinalIgnoreCase)
            ? roleFilter
            : "All";

        var usersQuery = dbContext.Users.AsQueryable();
        if (!roleFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (Enum.TryParse<UserRole>(roleFilter, out var parsedRole))
            {
                usersQuery = usersQuery.Where(x => x.Role == parsedRole);
            }
        }

        var users = await usersQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

        var userIds = users.Select(x => x.Id).ToList();

        var teacherCodes = await dbContext.Teachers
            .Where(x => userIds.Contains(x.UserId))
            .Select(x => new { x.UserId, x.TeacherId })
            .ToDictionaryAsync(x => x.UserId, x => x.TeacherId);

        var teacherSubjectCounts = await dbContext.TeacherSubjects
            .Where(x => userIds.Contains(x.TeacherId))
            .GroupBy(x => x.TeacherId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var studentEnrollmentNumbers = await dbContext.Students
            .Where(x => userIds.Contains(x.UserId))
            .Select(x => new { x.UserId, x.EnrollmentNumber })
            .ToDictionaryAsync(x => x.UserId, x => x.EnrollmentNumber);

        var studentCurrentSemesterNames = await dbContext.Students
            .Include(x => x.CurrentSemester)
            .Where(x => userIds.Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId, x => x.CurrentSemester != null ? x.CurrentSemester.Name : string.Empty);

        var fallbackSemesterNames = await dbContext.StudentEnrollments
            .Include(x => x.Semester)
            .Where(x => userIds.Contains(x.StudentId) && x.IsApproved)
            .OrderByDescending(x => x.EnrolledAtUtc)
            .ToListAsync();

        var fallbackSemesterByStudent = fallbackSemesterNames
            .GroupBy(x => x.StudentId)
            .ToDictionary(g => g.Key, g => g.First().Semester?.Name ?? string.Empty);

        var mappedUsers = users.Select(user => new AdminUserRowViewModel
        {
            UserId = user.Id,
            Name = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.ToString(),
            IsApproved = user.IsApproved,
            CreatedAtUtc = user.CreatedAtUtc,
            TeacherId = teacherCodes.TryGetValue(user.Id, out var code) ? code : null,
            AssignedSubjectCount = teacherSubjectCounts.TryGetValue(user.Id, out var count) ? count : null,
            EnrollmentNumber = studentEnrollmentNumbers.TryGetValue(user.Id, out var enrollment) ? enrollment : null,
            CurrentSemesterName = studentCurrentSemesterNames.TryGetValue(user.Id, out var currentSemesterName) && !string.IsNullOrWhiteSpace(currentSemesterName)
                ? currentSemesterName
                : (fallbackSemesterByStudent.TryGetValue(user.Id, out var fallbackSemesterName) ? fallbackSemesterName : string.Empty)
        }).ToList();

        var searchTerm = string.IsNullOrWhiteSpace(search) ? string.Empty : search.Trim();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            mappedUsers = mappedUsers
                .Where(user =>
                    user.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    || user.PhoneNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    || user.Role.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrWhiteSpace(user.TeacherId) && user.TeacherId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(user.EnrollmentNumber) && user.EnrollmentNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(user.CurrentSemesterName) && user.CurrentSemesterName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        var model = new AdminUsersViewModel
        {
            RoleFilter = roleFilter,
            SearchTerm = searchTerm,
            Users = mappedUsers
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ApproveUser(int userId, string? role, string? search)
    {
        var user = await dbContext.Users.FindAsync(userId);
        if (user is not null)
        {
            user.IsApproved = true;
            await dbContext.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Users), new { role, search });
    }

    private async Task<(bool Success, string Message)> DeleteUserWithCleanupAsync(int userId, int? adminUserId)
    {
        if (userId == 1)
        {
            return (false, "System Admin user cannot be deleted.");
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user is null)
        {
            return (false, "User not found.");
        }

        if (user.Role == UserRole.Admin)
        {
            return (false, "Admin users cannot be deleted.");
        }

        await using var tx = await dbContext.Database.BeginTransactionAsync();

        string? enrollmentNumber = null;
        if (user.Role == UserRole.Student)
        {
            enrollmentNumber = await dbContext.Students
                .Where(x => x.UserId == userId)
                .Select(x => x.EnrollmentNumber)
                .FirstOrDefaultAsync();
        }

        var deletedUser = new DeletedUser
        {
            OriginalUserId = user.Id,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            EnrollmentNumber = enrollmentNumber ?? user.EnrollmentNo,
            IsApproved = user.IsApproved,
            CreatedAtUtc = user.CreatedAtUtc,
            DeletedAtUtc = DateTime.UtcNow,
            DeletedByAdminUserId = adminUserId
        };

        dbContext.DeletedUsers.Add(deletedUser);
        await dbContext.SaveChangesAsync();

        if (user.Role == UserRole.Student)
        {
            await dbContext.MaterialPageProgress
                .Where(x => x.StudentId == userId)
                .ExecuteDeleteAsync();

            await dbContext.ProgressTrackings
                .Where(x => x.StudentId == userId)
                .ExecuteDeleteAsync();

            await dbContext.QuizResults
                .Where(x => x.StudentId == userId)
                .ExecuteDeleteAsync();

            await dbContext.StudentEnrollments
                .Where(x => x.StudentId == userId)
                .ExecuteDeleteAsync();

            await dbContext.Students
                .Where(x => x.UserId == userId)
                .ExecuteDeleteAsync();
        }
        else if (user.Role == UserRole.Teacher)
        {
            await dbContext.TeacherSubjects
                .Where(x => x.TeacherId == userId)
                .ExecuteDeleteAsync();

            var teacherQuizIds = dbContext.Quizzes
                .Where(x => x.TeacherId == userId)
                .Select(x => x.Id);

            await dbContext.QuizQuestions
                .Where(x => teacherQuizIds.Contains(x.QuizId))
                .ExecuteDeleteAsync();

            await dbContext.QuizResults
                .Where(x => teacherQuizIds.Contains(x.QuizId))
                .ExecuteDeleteAsync();

            await dbContext.Quizzes
                .Where(x => x.TeacherId == userId)
                .ExecuteDeleteAsync();

            var teacherMaterialIds = dbContext.Materials
                .Where(x => x.TeacherId == userId)
                .Select(x => x.Id);

            await dbContext.ProgressTrackings
                .Where(x => x.MaterialId != null && teacherMaterialIds.Contains(x.MaterialId.Value))
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.MaterialId, (int?)null));

            await dbContext.Materials
                .Where(x => x.TeacherId == userId)
                .ExecuteDeleteAsync();

            await dbContext.Teachers
                .Where(x => x.UserId == userId)
                .ExecuteDeleteAsync();
        }

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync();
        await tx.CommitAsync();

        return (true, "User deleted.");
    }

    public async Task<IActionResult> EditUser(int userId)
    {
        var user = await dbContext.Users.FindAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        if (user.Id == 1)
        {
            TempData["Error"] = "System Admin user cannot be edited.";
            return RedirectToAction(nameof(Users));
        }

        var student = await dbContext.Students.FirstOrDefaultAsync(x => x.UserId == user.Id);

        var model = new EditUserViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            EnrollmentNo = student?.EnrollmentNumber,
            Role = user.Role,
            IsApproved = user.IsApproved
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> EditUser(EditUserViewModel model)
    {
        if (model.Id == 1)
        {
            TempData["Error"] = "System Admin user cannot be edited.";
            return RedirectToAction(nameof(Users));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await dbContext.Users.FindAsync(model.Id);
        if (user is null)
        {
            return NotFound();
        }

        var phoneInUse = await dbContext.Users.AnyAsync(x => x.PhoneNumber == model.PhoneNumber && x.Id != model.Id);
        if (phoneInUse)
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), "Phone number is already in use.");
            return View(model);
        }

        user.FullName = model.FullName.Trim();
        user.PhoneNumber = model.PhoneNumber.Trim();
        user.Role = model.Role;
        user.IsApproved = model.IsApproved;

        if (model.Role == UserRole.Student)
        {
            var enrollmentNo = model.EnrollmentNo?.Trim() ?? string.Empty;
            var student = await dbContext.Students.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (student is null)
            {
                dbContext.Students.Add(new Student
                {
                    UserId = user.Id,
                    EnrollmentNumber = enrollmentNo,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                student.EnrollmentNumber = enrollmentNo;
            }
        }

        await dbContext.SaveChangesAsync();

        TempData["Success"] = "User updated.";
        return RedirectToAction(nameof(Users), new { role = "All" });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(int userId, string? role, string? search)
    {
        try
        {
            var result = await DeleteUserWithCleanupAsync(userId, HttpContext.Session.GetInt32("UserId"));
            TempData[result.Success ? "Success" : "Error"] = result.Message;
        }
        catch
        {
            TempData["Error"] = "Failed to delete user.";
        }

        return RedirectToAction(nameof(Users), new { role, search });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUsersBulk(List<int> userIds, string? role, string? search)
    {
        if (userIds == null || userIds.Count == 0)
        {
            TempData["Error"] = "No users selected for deletion.";
            return RedirectToAction(nameof(Users), new { role, search });
        }

        var deletedCount = 0;
        var blockedMessages = new List<string>();

        foreach (var userId in userIds.Distinct())
        {
            try
            {
                var result = await DeleteUserWithCleanupAsync(userId, HttpContext.Session.GetInt32("UserId"));
                if (result.Success)
                {
                    deletedCount++;
                }
                else
                {
                    blockedMessages.Add(result.Message);
                }
            }
            catch
            {
                blockedMessages.Add($"Failed to delete user with id {userId}.");
            }
        }

        if (deletedCount > 0)
        {
            TempData["Success"] = blockedMessages.Count == 0
                ? $"Deleted {deletedCount} user(s)."
                : $"Deleted {deletedCount} user(s). Some deletions were skipped.";
        }
        else
        {
            TempData["Error"] = "No users were deleted.";
        }

        if (blockedMessages.Count > 0)
        {
            TempData["Error"] = string.Join(" ", blockedMessages.Distinct().Take(3));
        }

        return RedirectToAction(nameof(Users), new { role, search });
    }

    public async Task<IActionResult> Semesters()
    {
        await RemoveDuplicateSemestersByNameAsync();
        var semesters = await dbContext.Semesters.OrderBy(x => x.Id).ToListAsync();
        return View(semesters);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSemester(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            var normalizedName = name.Trim();
            var exists = await dbContext.Semesters.AnyAsync(x => x.Name.ToLower() == normalizedName.ToLower());
            if (!exists)
            {
                dbContext.Semesters.Add(new Semester { Name = normalizedName, IsActive = false });
                await dbContext.SaveChangesAsync();
            }
        }

        return RedirectToAction(nameof(Semesters));
    }

    private async Task RemoveDuplicateSemestersByNameAsync()
    {
        var semesters = await dbContext.Semesters
            .OrderBy(x => x.Id)
            .ToListAsync();

        var duplicateGroups = semesters
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .ToList();

        if (!duplicateGroups.Any())
        {
            return;
        }

        foreach (var group in duplicateGroups)
        {
            var keepSemester = group.OrderBy(x => x.Id).First();
            var duplicateIds = group
                .Where(x => x.Id != keepSemester.Id)
                .Select(x => x.Id)
                .ToHashSet();

            if (duplicateIds.Count == 0)
            {
                continue;
            }

            var students = await dbContext.Students
                .Where(x => x.CurrentSemesterId.HasValue && duplicateIds.Contains(x.CurrentSemesterId.Value))
                .ToListAsync();
            foreach (var student in students)
            {
                student.CurrentSemesterId = keepSemester.Id;
            }

            var subjects = await dbContext.Subjects
                .Where(x => duplicateIds.Contains(x.SemesterId))
                .ToListAsync();
            foreach (var subject in subjects)
            {
                subject.SemesterId = keepSemester.Id;
            }

            var onboardingRecords = await dbContext.StudentOnboardingRecords
                .Where(x => duplicateIds.Contains(x.SemesterId))
                .ToListAsync();
            foreach (var record in onboardingRecords)
            {
                record.SemesterId = keepSemester.Id;
            }

            var enrollments = await dbContext.StudentEnrollments
                .Where(x => duplicateIds.Contains(x.SemesterId) || x.SemesterId == keepSemester.Id)
                .OrderBy(x => x.Id)
                .ToListAsync();

            var enrollmentKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var enrollment in enrollments)
            {
                var targetSemesterId = duplicateIds.Contains(enrollment.SemesterId) ? keepSemester.Id : enrollment.SemesterId;
                var key = $"{enrollment.StudentId}:{targetSemesterId}";
                if (!enrollmentKeys.Add(key))
                {
                    dbContext.StudentEnrollments.Remove(enrollment);
                    continue;
                }

                enrollment.SemesterId = targetSemesterId;
            }

            var publishes = await dbContext.SemesterResultPublishes
                .Where(x => duplicateIds.Contains(x.SemesterId) || x.SemesterId == keepSemester.Id)
                .OrderBy(x => x.Id)
                .ToListAsync();
            var keepPublish = publishes.FirstOrDefault(x => x.SemesterId == keepSemester.Id);
            foreach (var publish in publishes.Where(x => duplicateIds.Contains(x.SemesterId)))
            {
                if (keepPublish is null)
                {
                    publish.SemesterId = keepSemester.Id;
                    keepPublish = publish;
                }
                else
                {
                    dbContext.SemesterResultPublishes.Remove(publish);
                }
            }

            var promotionLogs = await dbContext.PromotionLogs
                .Where(x => duplicateIds.Contains(x.FromSemesterId) || duplicateIds.Contains(x.ToSemesterId))
                .ToListAsync();
            foreach (var log in promotionLogs)
            {
                if (duplicateIds.Contains(log.FromSemesterId))
                {
                    log.FromSemesterId = keepSemester.Id;
                }

                if (duplicateIds.Contains(log.ToSemesterId))
                {
                    log.ToSemesterId = keepSemester.Id;
                }
            }

            var duplicates = await dbContext.Semesters.Where(x => duplicateIds.Contains(x.Id)).ToListAsync();
            dbContext.Semesters.RemoveRange(duplicates);
        }

        await dbContext.SaveChangesAsync();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSemester(int id, DateTime? startDate, DateTime? endDate, bool isActive)
    {
        var semester = await dbContext.Semesters.FirstOrDefaultAsync(x => x.Id == id);
        if (semester is null)
        {
            TempData["Error"] = "Semester not found.";
            return RedirectToAction(nameof(Semesters));
        }

        if (startDate.HasValue && endDate.HasValue && endDate.Value.Date < startDate.Value.Date)
        {
            TempData["Error"] = "End date cannot be before start date.";
            return RedirectToAction(nameof(Semesters));
        }

        semester.StartDate = startDate?.Date;
        semester.EndDate = endDate?.Date;
        semester.IsActive = isActive;

        await dbContext.SaveChangesAsync();
        TempData["Success"] = "Semester updated.";
        return RedirectToAction(nameof(Semesters));
    }

    [HttpGet]
    public async Task<IActionResult> SemesterPromotion(int? semesterId = null)
    {
        var model = await studentPromotionService.BuildDashboardAsync(semesterId);

        if (model.CurrentSemesterId == 0)
        {
            TempData["Error"] = "No semester found for promotion.";
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePromotionHold(int semesterId, List<int> holdStudentIds)
    {
        await studentPromotionService.UpdateHoldStatusesAsync(semesterId, holdStudentIds);
        TempData["Success"] = "Hold status updated.";
        return RedirectToAction(nameof(SemesterPromotion), new { semesterId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteStudents(int semesterId)
    {
        var adminUserId = HttpContext.Session.GetInt32("UserId");
        var result = await studentPromotionService.PromoteStudentsAsync(semesterId, adminUserId);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(SemesterPromotion), new { semesterId });
    }

    public async Task<IActionResult> Subjects()
    {
        ViewBag.Semesters = await dbContext.Semesters.OrderBy(x => x.Id).ToListAsync();
        var subjects = await dbContext.Subjects
            .Include(x => x.Semester)
            .OrderBy(x => x.SemesterId)
            .ThenBy(x => x.SubjectCode)
            .ToListAsync();
        return View(subjects);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSubject(string name, string subjectCode, int semesterId)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(subjectCode) || semesterId <= 0)
        {
            TempData["Error"] = "Subject name, subject code, and semester are required.";
            return RedirectToAction(nameof(Subjects));
        }

        var normalizedCode = subjectCode.Trim().ToUpperInvariant();
        var normalizedName = name.Trim();

        var duplicateCodeExists = await dbContext.Subjects
            .AnyAsync(x => x.SubjectCode != null && x.SubjectCode.ToUpper() == normalizedCode);

        if (duplicateCodeExists)
        {
            TempData["Error"] = "This Subject Code already exists in another semester. Use a unique Subject Code.";
            return RedirectToAction(nameof(Subjects));
        }

        var duplicateNameInSemester = await dbContext.Subjects
            .AnyAsync(x => x.SemesterId == semesterId && x.Name.ToUpper() == normalizedName.ToUpper());

        if (duplicateNameInSemester)
        {
            TempData["Error"] = "This subject already exists in the selected semester.";
            return RedirectToAction(nameof(Subjects));
        }

        dbContext.Subjects.Add(new Subject
        {
            Name = normalizedName,
            SubjectCode = normalizedCode,
            SemesterId = semesterId
        });

        await dbContext.SaveChangesAsync();
        TempData["Success"] = "Subject created successfully.";

        return RedirectToAction(nameof(Subjects));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSubject(int subjectId)
    {
        var subject = await dbContext.Subjects
            .Include(x => x.Semester)
            .FirstOrDefaultAsync(x => x.Id == subjectId);

        if (subject is null)
        {
            TempData["Error"] = "Subject not found.";
            return RedirectToAction(nameof(Subjects));
        }

        var hasAssignments = await dbContext.TeacherSubjects.AnyAsync(x => x.SubjectId == subjectId);
        var hasTopics = await dbContext.Topics.AnyAsync(x => x.SubjectId == subjectId);
        var hasMaterials = await dbContext.Materials.AnyAsync(x => x.SubjectId == subjectId);
        var hasQuizzes = await dbContext.Quizzes.AnyAsync(x => x.SubjectId == subjectId);

        if (hasAssignments || hasTopics || hasMaterials || hasQuizzes)
        {
            TempData["Error"] = "Cannot delete subject because it is already in use.";
            return RedirectToAction(nameof(Subjects));
        }

        dbContext.Subjects.Remove(subject);
        await dbContext.SaveChangesAsync();

        TempData["Success"] = "Subject deleted successfully.";
        return RedirectToAction(nameof(Subjects));
    }

    public async Task<IActionResult> AssignTeachers()
    {
        var teachers = await dbContext.Users
            .Where(x => x.Role == UserRole.Teacher && x.IsApproved)
            .OrderBy(x => x.FullName)
            .ToListAsync();

        var teacherUserIds = teachers.Select(x => x.Id).ToList();
        var teacherIdsByUserId = await dbContext.Teachers
            .Where(x => teacherUserIds.Contains(x.UserId))
            .Select(x => new { x.UserId, x.TeacherId })
            .ToDictionaryAsync(x => x.UserId, x => x.TeacherId);

        ViewBag.Teachers = teachers;
        ViewBag.TeacherIdsByUserId = teacherIdsByUserId;
        ViewBag.Subjects = await dbContext.Subjects.Include(x => x.Semester).ToListAsync();
        var mappings = await dbContext.TeacherSubjects
            .Include(x => x.Teacher)
            .Include(x => x.Subject)
            .ThenInclude(x => x!.Semester)
            .OrderBy(x => x.Teacher!.FullName)
            .ToListAsync();

        return View(mappings);
    }

    [HttpPost]
    public async Task<IActionResult> AssignTeacher(int teacherId, int subjectId)
    {
        var subject = await dbContext.Subjects.FirstOrDefaultAsync(x => x.Id == subjectId);
        if (subject is null)
        {
            TempData["Error"] = "Subject not found.";
            return RedirectToAction(nameof(AssignTeachers));
        }

        var teacher = await dbContext.Users
            .AnyAsync(x => x.Id == teacherId && x.Role == UserRole.Teacher && x.IsApproved);

        if (!teacher)
        {
            TempData["Error"] = "Teacher not found or not active.";
            return RedirectToAction(nameof(AssignTeachers));
        }

        var mapping = await dbContext.TeacherSubjects.FirstOrDefaultAsync(x => x.SubjectId == subjectId);
        if (mapping is null)
        {
            dbContext.TeacherSubjects.Add(new TeacherSubject { TeacherId = teacherId, SubjectId = subjectId });
            TempData["Success"] = "Teacher assigned successfully.";
        }
        else
        {
            mapping.TeacherId = teacherId;
            TempData["Success"] = "Teacher assignment updated successfully.";
        }

        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(AssignTeachers));
    }

    public async Task<IActionResult> SemesterStudents()
    {
        var students = await dbContext.Students
            .Include(x => x.User)
            .Include(x => x.CurrentSemester)
            .Where(x => x.CurrentSemesterId.HasValue)
            .ToListAsync();

        var studentIds = students.Select(x => x.UserId).ToList();
        var semesterIds = students
            .Where(x => x.CurrentSemesterId.HasValue)
            .Select(x => x.CurrentSemesterId!.Value)
            .Distinct()
            .ToList();

        var enrollments = await dbContext.StudentEnrollments
            .Where(x => studentIds.Contains(x.StudentId) && semesterIds.Contains(x.SemesterId))
            .OrderByDescending(x => x.EnrolledAtUtc)
            .ToListAsync();

        var latestByStudentSemester = enrollments
            .GroupBy(x => new { x.StudentId, x.SemesterId })
            .ToDictionary(g => (g.Key.StudentId, g.Key.SemesterId), g => g.First());

        var rows = new List<(int SemesterId, string SemesterName, SemesterStudentRowViewModel Row)>();

        foreach (var student in students)
        {
            var resolvedSemesterId = student.CurrentSemesterId!.Value;
            var resolvedSemesterName = student.CurrentSemester?.Name;

            latestByStudentSemester.TryGetValue((student.UserId, resolvedSemesterId), out var enrollment);
            var status = enrollment is null
                ? "Approved"
                : (enrollment.ApprovedAtUtc is null ? "Pending" : (enrollment.IsApproved ? "Approved" : "Rejected"));

            rows.Add((
                resolvedSemesterId,
                string.IsNullOrWhiteSpace(resolvedSemesterName) ? "-" : resolvedSemesterName,
                new SemesterStudentRowViewModel
                {
                    StudentId = student.UserId,
                    EnrollmentId = enrollment?.Id ?? 0,
                    StudentName = student.User?.FullName ?? "Unknown",
                    PhoneNumber = student.User?.PhoneNumber ?? "-",
                    Email = string.IsNullOrWhiteSpace(student.User?.Email) ? "-" : student.User!.Email!,
                    EnrollmentNumber = string.IsNullOrWhiteSpace(student.EnrollmentNumber) ? "-" : student.EnrollmentNumber,
                    Status = status,
                    EnrolledAtUtc = enrollment?.EnrolledAtUtc,
                    ApprovedAtUtc = enrollment?.ApprovedAtUtc
                }));
        }

        var groups = rows
            .GroupBy(x => new { x.SemesterId, x.SemesterName })
            .Select(group => new SemesterStudentsGroupViewModel
            {
                SemesterId = group.Key.SemesterId,
                SemesterName = group.Key.SemesterName,
                Students = group
                    .Select(x => x.Row)
                    .OrderBy(x => x.StudentName)
                    .ToList()
            })
            .OrderBy(x => x.SemesterId)
            .ToList();

        return View(groups);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveStudentFromSemester(int semesterId, int studentId)
    {
        var student = await dbContext.Students.FirstOrDefaultAsync(x => x.UserId == studentId);
        if (student is null)
        {
            TempData["Error"] = "Student not found.";
            return RedirectToAction(nameof(SemesterStudents));
        }

        if (student.CurrentSemesterId != semesterId)
        {
            TempData["Error"] = "Student is not in the selected semester.";
            return RedirectToAction(nameof(SemesterStudents));
        }

        try
        {
            var deletedCount = await DeleteStudentUsersAsync(new List<int> { studentId }, HttpContext.Session.GetInt32("UserId"));
            if (deletedCount == 0)
            {
                TempData["Error"] = "Student could not be deleted.";
                return RedirectToAction(nameof(SemesterStudents));
            }

            TempData["Success"] = "Student deleted from database and semester records successfully.";
        }
        catch
        {
            TempData["Error"] = "Failed to delete student.";
        }

        return RedirectToAction(nameof(SemesterStudents));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveStudentsFromSemester(int semesterId, List<int>? studentIds)
    {
        var selectedIds = (studentIds ?? []).Distinct().ToList();
        if (selectedIds.Count == 0)
        {
            TempData["Error"] = "Please select at least one student.";
            return RedirectToAction(nameof(SemesterStudents));
        }

        var students = await dbContext.Students
            .Where(x => selectedIds.Contains(x.UserId) && x.CurrentSemesterId == semesterId)
            .ToListAsync();

        if (students.Count == 0)
        {
            TempData["Error"] = "No matching students found in selected semester.";
            return RedirectToAction(nameof(SemesterStudents));
        }

        try
        {
            var targetStudentIds = students.Select(x => x.UserId).ToList();
            var deletedCount = await DeleteStudentUsersAsync(targetStudentIds, HttpContext.Session.GetInt32("UserId"));
            TempData["Success"] = $"Deleted {deletedCount} student(s) from database and semester records successfully.";
        }
        catch
        {
            TempData["Error"] = "Failed to delete selected students.";
        }

        return RedirectToAction(nameof(SemesterStudents));
    }

    private async Task<int> DeleteStudentUsersAsync(IReadOnlyCollection<int> userIds, int? deletedByAdminUserId)
    {
        var targetIds = userIds.Distinct().ToList();
        if (targetIds.Count == 0)
        {
            return 0;
        }

        var users = await dbContext.Users
            .Where(x => targetIds.Contains(x.Id) && x.Role == UserRole.Student)
            .ToListAsync();

        if (users.Count == 0)
        {
            return 0;
        }

        var userIdSet = users.Select(x => x.Id).ToList();
        var enrollmentNumbersByUserId = await dbContext.Students
            .Where(x => userIdSet.Contains(x.UserId))
            .Select(x => new { x.UserId, x.EnrollmentNumber })
            .ToDictionaryAsync(x => x.UserId, x => x.EnrollmentNumber);

        await using var tx = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var deletedUsers = users.Select(user => new DeletedUser
            {
                OriginalUserId = user.Id,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                EnrollmentNumber = enrollmentNumbersByUserId.TryGetValue(user.Id, out var enrollmentNo)
                    ? enrollmentNo
                    : user.EnrollmentNo,
                IsApproved = user.IsApproved,
                CreatedAtUtc = user.CreatedAtUtc,
                DeletedAtUtc = DateTime.UtcNow,
                DeletedByAdminUserId = deletedByAdminUserId
            }).ToList();

            dbContext.DeletedUsers.AddRange(deletedUsers);
            await dbContext.SaveChangesAsync();

            await dbContext.MaterialPageProgress
                .Where(x => userIdSet.Contains(x.StudentId))
                .ExecuteDeleteAsync();

            await dbContext.ProgressTrackings
                .Where(x => userIdSet.Contains(x.StudentId))
                .ExecuteDeleteAsync();

            await dbContext.QuizResults
                .Where(x => userIdSet.Contains(x.StudentId))
                .ExecuteDeleteAsync();

            await dbContext.StudentEnrollments
                .Where(x => userIdSet.Contains(x.StudentId))
                .ExecuteDeleteAsync();

            await dbContext.Students
                .Where(x => userIdSet.Contains(x.UserId))
                .ExecuteDeleteAsync();

            await dbContext.Users
                .Where(x => userIdSet.Contains(x.Id))
                .ExecuteDeleteAsync();

            await tx.CommitAsync();
            return users.Count;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    [HttpPost]
    public async Task<IActionResult> ApproveEnrollment(int enrollmentId, string? returnTo)
    {
        var enrollment = await dbContext.StudentEnrollments.FirstOrDefaultAsync(x => x.Id == enrollmentId);
        if (enrollment is null)
        {
            TempData["Error"] = "Enrollment request not found.";
            return RedirectToAction(nameof(SemesterStudents));
        }

        var adminUserId = HttpContext.Session.GetInt32("UserId");
        var admin = adminUserId.HasValue
            ? await dbContext.Admins.FirstOrDefaultAsync(x => x.UserId == adminUserId.Value)
            : null;

        enrollment.IsApproved = true;
        enrollment.ApprovedAtUtc = DateTime.UtcNow;
        enrollment.ApprovedByAdminId = admin?.Id;

        await dbContext.SaveChangesAsync();
        TempData["Success"] = "Enrollment approved.";
        return RedirectToAction(nameof(SemesterStudents));
    }

    [HttpPost]
    public async Task<IActionResult> RejectEnrollment(int enrollmentId, string? returnTo)
    {
        var enrollment = await dbContext.StudentEnrollments.FirstOrDefaultAsync(x => x.Id == enrollmentId);
        if (enrollment is null)
        {
            TempData["Error"] = "Enrollment request not found.";
            return RedirectToAction(nameof(SemesterStudents));
        }

        var adminUserId = HttpContext.Session.GetInt32("UserId");
        var admin = adminUserId.HasValue
            ? await dbContext.Admins.FirstOrDefaultAsync(x => x.UserId == adminUserId.Value)
            : null;

        enrollment.IsApproved = false;
        enrollment.ApprovedAtUtc = DateTime.UtcNow;
        enrollment.ApprovedByAdminId = admin?.Id;

        await dbContext.SaveChangesAsync();
        TempData["Success"] = "Enrollment rejected.";
        return RedirectToAction(nameof(SemesterStudents));
    }

    public async Task<IActionResult> Reports()
    {
        var totalPagesBySemester = await dbContext.MaterialPages
            .Include(x => x.Material)
            .GroupBy(x => x.Material!.SemesterId)
            .Select(g => new { SemesterId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SemesterId, x => x.Count);

        var pageQuizIds = await dbContext.Quizzes
            .Where(x => x.MaterialPageId != null)
            .Select(x => x.Id)
            .ToListAsync();

        var quizQuestionCounts = pageQuizIds.Count == 0
            ? new Dictionary<int, int>()
            : await dbContext.QuizQuestions
                .Where(x => pageQuizIds.Contains(x.QuizId))
                .GroupBy(x => x.QuizId)
                .Select(g => new { QuizId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.QuizId, x => x.Count);

        var totalQuizQuestionsBySemester = await dbContext.QuizQuestions
            .Include(x => x.Quiz)
            .ThenInclude(x => x!.Material)
            .Where(x => x.Quiz!.MaterialPageId != null)
            .GroupBy(x => x.Quiz!.Material!.SemesterId)
            .Select(g => new { SemesterId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SemesterId, x => x.Count);

        var includedSemesterIds = await dbContext.Semesters.Select(x => x.Id).ToListAsync();
        var includedSemesters = await dbContext.Semesters.ToDictionaryAsync(x => x.Id, x => x.Name);

        var pageProgress = await dbContext.MaterialPageProgress
            .Include(x => x.Student)
            .Include(x => x.MaterialPage)
            .ThenInclude(x => x!.Material)
            .ToListAsync();

        var quizResults = await dbContext.QuizResults
            .Include(x => x.Quiz)
            .Where(x => x.Quiz!.MaterialPageId != null)
            .ToListAsync();

        var studentIds = pageProgress.Select(x => x.StudentId)
            .Concat(quizResults.Select(x => x.StudentId))
            .Distinct()
            .ToList();

        var studentEnrollmentNumbers = await dbContext.Students
            .Where(x => studentIds.Contains(x.UserId))
            .Select(x => new { x.UserId, x.EnrollmentNumber })
            .ToDictionaryAsync(x => x.UserId, x => x.EnrollmentNumber);

        var rows = new List<ProgressAnalyticsRowViewModel>();

        foreach (var studentId in studentIds)
        {
            var studentName = pageProgress.FirstOrDefault(x => x.StudentId == studentId)?.Student?.FullName
                              ?? (await dbContext.Users.Where(x => x.Id == studentId).Select(x => x.FullName).FirstOrDefaultAsync())
                              ?? "Unknown";

            var studentEnrollmentNo = studentEnrollmentNumbers.TryGetValue(studentId, out var enrollment)
                ? enrollment
                : null;

            var studentEnrollments = await dbContext.StudentEnrollments
                .Include(x => x.Semester)
                .Where(x => x.StudentId == studentId)
                .OrderBy(x => x.SemesterId)
                .ToListAsync();

            var enrollmentNo = string.IsNullOrWhiteSpace(studentEnrollmentNo) ? "-" : studentEnrollmentNo;

            var semester = studentEnrollments.Count switch
            {
                0 => "-",
                1 => studentEnrollments[0].Semester?.Name ?? (includedSemesters.TryGetValue(studentEnrollments[0].SemesterId, out var name) ? name : "-"),
                _ => "Multiple"
            };

            var studentSemesterIds = studentEnrollments.Select(x => x.SemesterId).ToList();
            var totalPages = studentSemesterIds.Count > 0 
                ? studentSemesterIds.Sum(sId => totalPagesBySemester.TryGetValue(sId, out var p) ? p : 0) 
                : totalPagesBySemester.Values.Sum();
            var totalQuizQuestions = studentSemesterIds.Count > 0
                ? studentSemesterIds.Sum(sId => totalQuizQuestionsBySemester.TryGetValue(sId, out var q) ? q : 0)
                : totalQuizQuestionsBySemester.Values.Sum();

            var studentPageProgress = pageProgress.Where(x => x.StudentId == studentId).ToList();
            var totalTimeSeconds = studentPageProgress.Sum(x => x.TimeSpentSeconds);
            var screenTimeMinutes = totalTimeSeconds / 60d;

            var effectiveTimeSecondsForFormula = studentPageProgress.Sum(p =>
            {
                var depth = Math.Clamp(p.MaxScrollDepthPercent, 0d, 100d);
                var factor = depth < 30d ? (depth / 30d) : 1d;
                return Math.Max(0d, p.TimeSpentSeconds) * Math.Clamp(factor, 0d, 1d);
            });

            var visitedPages = studentPageProgress
                .Select(x => x.MaterialPageId)
                .Distinct()
                .Count();

            var completedPages = studentPageProgress.Count(x => x.IsCompleted);
            var completionPercent = totalPages == 0 ? 0d : Math.Clamp((double)completedPages / totalPages * 100d, 0, 100);

            var quizCorrectAnswers = quizResults
                .Where(x => x.StudentId == studentId)
                .GroupBy(x => x.QuizId)
                .Select(g => g.OrderByDescending(r => r.SubmittedAtUtc).First())
                .Select(attempt =>
                {
                    var perQuizTotal = attempt.TotalQuestions > 0
                        ? attempt.TotalQuestions
                        : (quizQuestionCounts.TryGetValue(attempt.QuizId, out var count) ? count : 0);

                    var perQuizCorrect = attempt.CorrectAnswers;
                    if (perQuizCorrect <= 0 && perQuizTotal > 0)
                    {
                        perQuizCorrect = (int)Math.Round((double)attempt.ScorePercent / 100d * perQuizTotal);
                    }

                    return Math.Max(0, perQuizCorrect);
                })
                .Sum();

            var quizScorePercent = totalQuizQuestions <= 0
                ? 0d
                : Math.Clamp(Math.Round((double)quizCorrectAnswers / totalQuizQuestions * 100d, 2), 0, 100);

            var studentViolations = quizResults
                .Where(x => x.StudentId == studentId && x.IsAutoSubmitted)
                .OrderByDescending(x => x.AntiCheatDetectedAtUtc ?? x.SubmittedAtUtc)
                .ToList();

            var latestViolation = studentViolations.FirstOrDefault();

            var averageScrollDepth = studentPageProgress.Count == 0
                ? 0d
                : studentPageProgress.Average(x => Math.Clamp(x.MaxScrollDepthPercent, 0d, 100d));

            var breakdown = progressService.CalculateBreakdown(
                totalActiveReadingSeconds: totalTimeSeconds,
                effectiveActiveReadingSecondsForFormula: effectiveTimeSecondsForFormula,
                totalPages: totalPages,
                completedPages: completedPages,
                averageQuizScorePercent: quizScorePercent,
                averageScrollDepthPercent: averageScrollDepth,
                idealMinutesPerPage: 6d);

            var finalProgress = breakdown.FinalProgressPercent;

            var status = progressService.GetProgressStatus(finalProgress);
            var barClass = status switch
            {
                "Skimmer" => "bg-danger",
                "NeedsImprovement" => "bg-warning",
                "Learning" => "bg-warning",
                "Progressing" => "bg-primary",
                "ActiveLearner" => "bg-success",
                "Mastered" => "bg-success",
                _ => "bg-secondary"
            };

            rows.Add(new ProgressAnalyticsRowViewModel
            {
                EnrollmentNo = enrollmentNo,
                StudentName = studentName,
                Semester = semester,
                ScreenTimeMinutes = Math.Round(screenTimeMinutes, 2),
                ScreenTimeHms = DurationFormatter.ToHms(totalTimeSeconds),
                QuizScorePercent = Math.Round(quizScorePercent, 2),
                QuizMarks = $"{quizCorrectAnswers}/{totalQuizQuestions}",
                QuizViolationCount = studentViolations.Count,
                LatestQuizViolationReason = latestViolation?.AntiCheatReason,
                LatestQuizViolationAtUtc = latestViolation?.AntiCheatDetectedAtUtc ?? latestViolation?.SubmittedAtUtc,
                CompletionPercent = Math.Round(completionPercent, 2),
                FinalProgressPercent = Math.Round(finalProgress, 2),
                Status = status,
                ProgressBarClass = barClass
            });
        }

        rows = rows
            .OrderByDescending(x => x.FinalProgressPercent)
            .ThenBy(x => x.StudentName)
            .ToList();

        var visitedPairsCount = pageProgress
            .Select(x => $"{x.StudentId}:{x.MaterialPageId}")
            .Distinct()
            .Count();

        var model = new ProgressAnalyticsDashboardViewModel
        {
            Rows = rows,
            AverageScreenTimeMinutes = rows.Count == 0 ? 0 : Math.Round(rows.Average(x => x.ScreenTimeMinutes), 2),
            AverageScreenTimePercentForFormula = rows.Count == 0
                ? 0
                : Math.Round(Math.Clamp(pageProgress.Sum(x => x.TimeSpentSeconds) / (Math.Max(visitedPairsCount, 1) * 360d) * 100d, 0d, 100d), 2),
            AverageQuizScorePercent = rows.Count == 0 ? 0 : Math.Round(rows.Average(x => x.QuizScorePercent), 2),
            AverageCompletionPercent = rows.Count == 0 ? 0 : Math.Round(rows.Average(x => x.CompletionPercent), 2)
        };

        return View(model);
    }

    private static (string status, string barClass) GetStatus(double finalProgress)
    {
        if (finalProgress >= 80)
        {
            return ("Excellent", "bg-success");
        }

        if (finalProgress >= 60)
        {
            return ("Good", "bg-primary");
        }

        if (finalProgress >= 40)
        {
            return ("Average", "bg-warning");
        }

        return ("Skimmer", "bg-danger");
    }

    // =====================================================================
    // Semester Result Management
    // =====================================================================

    public async Task<IActionResult> SemesterResults()
    {
        var semesters = await dbContext.Semesters.OrderBy(x => x.Id).ToListAsync();
        var publications = await dbContext.SemesterResultPublishes.ToListAsync();
        var pubDict = publications.ToDictionary(x => x.SemesterId);

        var enrollmentCounts = await dbContext.StudentEnrollments
            .Where(x => x.IsApproved)
            .GroupBy(x => x.SemesterId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var model = new SemesterResultIndexViewModel
        {
            Semesters = semesters.Select(s => new SemesterResultRowViewModel
            {
                SemesterId = s.Id,
                SemesterName = s.Name,
                ApprovedStudentCount = enrollmentCounts.TryGetValue(s.Id, out var c) ? c : 0,
                IsPublished = pubDict.TryGetValue(s.Id, out var pub) && pub.IsPublished,
                PublishedAtUtc = pubDict.TryGetValue(s.Id, out var pub2) ? pub2.PublishedAtUtc : null
            }).ToList()
        };

        return View(model);
    }

    public async Task<IActionResult> SemesterResultDetail(int semesterId)
    {
        var semester = await dbContext.Semesters.FindAsync(semesterId);
        if (semester is null) return NotFound();

        var publication = await dbContext.SemesterResultPublishes
            .FirstOrDefaultAsync(x => x.SemesterId == semesterId);

        var enrollments = await dbContext.StudentEnrollments
            .Include(x => x.Student)
            .Where(x => x.SemesterId == semesterId && x.IsApproved)
            .ToListAsync();

        var studentIds = enrollments.Select(x => x.StudentId).ToList();

        var enrollmentNumbers = await dbContext.Students
            .Where(x => studentIds.Contains(x.UserId))
            .Select(x => new { x.UserId, x.EnrollmentNumber })
            .ToDictionaryAsync(x => x.UserId, x => x.EnrollmentNumber);

        var subjectIds = await dbContext.Subjects
            .Where(x => x.SemesterId == semesterId)
            .Select(x => x.Id)
            .ToListAsync();

        // Only chapters that have at least one page participate in scoring
        var materials = await dbContext.Materials
            .Where(x => subjectIds.Contains(x.SubjectId)
                     && x.MaterialType == MaterialType.Notes
                     && dbContext.MaterialPages.Any(p => p.MaterialId == x.Id))
            .Select(x => new { x.Id, x.SubjectId })
            .ToListAsync();

        var materialIds = materials.Select(x => x.Id).ToList();

        var progressTrackings = materialIds.Count > 0
            ? await dbContext.ProgressTrackings
                .Where(x => studentIds.Contains(x.StudentId)
                         && x.MaterialId != null
                         && materialIds.Contains(x.MaterialId.Value))
                .ToListAsync()
            : new List<ProgressTracking>();

        var studentRows = new List<StudentResultSummaryRow>();

        foreach (var enrollment in enrollments)
        {
            var sid = enrollment.StudentId;
            var finalResult = CalculateSemesterResult(sid, subjectIds, materials.Select(m => (m.Id, m.SubjectId)).ToList(), progressTrackings);

            var (status, statusClass) = SemesterResultHelper.GetStatus(finalResult);

            studentRows.Add(new StudentResultSummaryRow
            {
                StudentId = sid,
                StudentName = enrollment.Student?.FullName ?? "Unknown",
                EnrollmentNumber = enrollmentNumbers.TryGetValue(sid, out var en) ? en : "-",
                FinalResult = finalResult,
                Status = status,
                StatusClass = statusClass
            });
        }

        var model = new SemesterResultDetailViewModel
        {
            SemesterId = semesterId,
            SemesterName = semester.Name,
            IsPublished = publication?.IsPublished ?? false,
            PublishedAtUtc = publication?.PublishedAtUtc,
            Students = studentRows.OrderByDescending(x => x.FinalResult).ToList()
        };

        return View(model);
    }

    public async Task<IActionResult> AdminStudentResult(int studentId, int semesterId)
    {
        var semester = await dbContext.Semesters.FindAsync(semesterId);
        if (semester is null) return NotFound();

        var user = await dbContext.Users.FindAsync(studentId);
        if (user is null) return NotFound();

        var publication = await dbContext.SemesterResultPublishes
            .FirstOrDefaultAsync(x => x.SemesterId == semesterId);

        var student = await dbContext.Students.FirstOrDefaultAsync(x => x.UserId == studentId);

        var subjects = await dbContext.Subjects
            .Where(x => x.SemesterId == semesterId)
            .OrderBy(x => x.Name)
            .ToListAsync();

        var subjectIds = subjects.Select(x => x.Id).ToList();

        var materials = await dbContext.Materials
            .Where(x => subjectIds.Contains(x.SubjectId)
                     && x.MaterialType == MaterialType.Notes
                     && dbContext.MaterialPages.Any(p => p.MaterialId == x.Id))
            .OrderBy(x => x.Title)
            .ToListAsync();

        var materialIds = materials.Select(x => x.Id).ToList();

        var progressTrackings = materialIds.Count > 0
            ? await dbContext.ProgressTrackings
                .Where(x => x.StudentId == studentId
                         && x.MaterialId != null
                         && materialIds.Contains(x.MaterialId.Value))
                .ToListAsync()
            : new List<ProgressTracking>();

        var progressDict = progressTrackings
            .Where(x => x.MaterialId.HasValue)
            .ToDictionary(x => x.MaterialId!.Value);

        var subjectResults = new List<SubjectResultViewModel>();
        var allSubjectAverages = new List<double>();

        foreach (var subject in subjects)
        {
            var subMaterials = materials.Where(x => x.SubjectId == subject.Id).ToList();
            if (subMaterials.Count == 0) continue;

            var matRows = subMaterials.Select(m =>
            {
                var p = progressDict.TryGetValue(m.Id, out var pt) ? pt : null;
                return new MaterialResultRowViewModel
                {
                    Title = m.Title,
                    CompletionPercent = Math.Round(p?.CompletionPercent ?? 0d, 1),
                    QuizScorePercent = Math.Round(p?.QuizScorePercent ?? 0d, 1),
                    FinalProgress = Math.Round(p?.ProgressPercent ?? 0d, 1)
                };
            }).ToList();

            var subAvg = Math.Round(matRows.Average(x => x.FinalProgress), 1);
            allSubjectAverages.Add(subAvg);

            subjectResults.Add(new SubjectResultViewModel
            {
                SubjectName = subject.Name,
                Materials = matRows,
                SubjectAverage = subAvg
            });
        }

        var finalResult = allSubjectAverages.Count == 0 ? 0d : Math.Round(allSubjectAverages.Average(), 2);
        var (status, statusClass) = SemesterResultHelper.GetStatus(finalResult);

        var model = new StudentSemesterResultViewModel
        {
            StudentId = studentId,
            StudentName = user.FullName,
            EnrollmentNumber = student?.EnrollmentNumber ?? "-",
            SemesterId = semesterId,
            SemesterName = semester.Name,
            IsPublished = publication?.IsPublished ?? false,
            Subjects = subjectResults,
            FinalResult = finalResult,
            Status = status,
            StatusClass = statusClass,
            IsOwnResult = false
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> PublishSemesterResult(int semesterId)
    {
        var approvedStudentCount = await dbContext.StudentEnrollments
            .CountAsync(x => x.SemesterId == semesterId && x.IsApproved);

        if (approvedStudentCount == 0)
        {
            TempData["Error"] = "Cannot publish results. No approved students are enrolled in this semester.";
            return RedirectToAction(nameof(SemesterResultDetail), new { semesterId });
        }

        var adminUserId = HttpContext.Session.GetInt32("UserId");
        var admin = adminUserId.HasValue
            ? await dbContext.Admins.FirstOrDefaultAsync(x => x.UserId == adminUserId.Value)
            : null;

        var publication = await dbContext.SemesterResultPublishes
            .FirstOrDefaultAsync(x => x.SemesterId == semesterId);

        if (publication is null)
        {
            publication = new SemesterResultPublish
            {
                SemesterId = semesterId,
                CreatedAtUtc = DateTime.UtcNow
            };
            dbContext.SemesterResultPublishes.Add(publication);
        }

        publication.IsPublished = true;
        publication.PublishedAtUtc = DateTime.UtcNow;
        publication.PublishedByAdminId = admin?.Id;

        await dbContext.SaveChangesAsync();

        TempData["Success"] = "Semester results published. Students can now view their results.";
        return RedirectToAction(nameof(SemesterResultDetail), new { semesterId });
    }

    [HttpPost]
    public async Task<IActionResult> UnpublishSemesterResult(int semesterId)
    {
        var publication = await dbContext.SemesterResultPublishes
            .FirstOrDefaultAsync(x => x.SemesterId == semesterId);

        if (publication is not null)
        {
            publication.IsPublished = false;
            publication.PublishedAtUtc = null;
            await dbContext.SaveChangesAsync();
        }

        TempData["Success"] = "Semester results unpublished.";
        return RedirectToAction(nameof(SemesterResultDetail), new { semesterId });
    }

    private static double CalculateSemesterResult(
        int studentId,
        List<int> subjectIds,
        List<(int Id, int SubjectId)> materials,
        List<ProgressTracking> progressTrackings)
    {
        var progressDict = progressTrackings
            .Where(x => x.StudentId == studentId && x.MaterialId.HasValue)
            .ToDictionary(x => x.MaterialId!.Value);

        var subjectAverages = new List<double>();

        foreach (var subId in subjectIds)
        {
            var subMatIds = materials.Where(x => x.SubjectId == subId).Select(x => x.Id).ToList();
            if (subMatIds.Count == 0) continue;

            var avg = subMatIds.Sum(mid =>
                progressDict.TryGetValue(mid, out var pt) ? pt.ProgressPercent : 0d
            ) / subMatIds.Count;

            subjectAverages.Add(Math.Clamp(avg, 0d, 100d));
        }

        return subjectAverages.Count == 0 ? 0d : Math.Round(subjectAverages.Average(), 2);
    }
}
