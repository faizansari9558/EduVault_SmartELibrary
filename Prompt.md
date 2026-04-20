# SmartELibrary Full Update Prompt (Step-by-Step)

Use this prompt to recreate all updates done in the last 3-4 days on the SmartELibrary project.

## Goal
Apply all Admin panel, Users, Subjects, Semester Results, Student Onboarding, data cleanup/seeding, and stability updates exactly as described below, then run and verify the app.

## Important Rules
1. Do not delete any existing file unless explicitly approved.
2. Preserve existing project style (simple ASP.NET MVC / Razor, plain UI).
3. Keep modals centered and consistent across Admin pages.
4. After code updates, run the app and confirm local URL.

---

## Step 1: Users Page Enhancements
Update the Admin Users page to support search and bulk delete by role sections.

1. Add a search bar with live filtering for Admin/Teacher/Student rows.
2. Add row checkboxes for selectable users.
3. Add section-wise controls:
   - Select All
   - Delete Selected
4. Add bulk delete confirmation modal (in-app modal, not browser confirm).
5. Add backend bulk delete endpoint in AdminController:
   - DeleteUsersBulk(List<int> userIds)
6. Reuse safe delete logic internally (single and bulk).
7. Keep System Admin and Admin-role delete restrictions.

---

## Step 2: Table Alignment Improvements
Improve Admin tables so columns stay aligned and avoid wrapping/misalignment.

1. Tighten table CSS.
2. Use nowrap where needed.
3. Remove extra spacing/gaps and keep one-line rows where expected.

---

## Step 3: Semesters Save (HTTP 405 Fix)
Fix Save action in Semesters page.

1. Ensure POST endpoint exists in AdminController:
   - UpdateSemester(int id, DateTime? startDate, DateTime? endDate, bool isActive)
2. Validate semester exists.
3. Update StartDate, EndDate, IsActive.
4. Redirect with success/error TempData.

---

## Step 4: Center All Admin Popups
Standardize popup placement across Admin pages.

1. Use Bootstrap centered modal dialog:
   - modal-dialog-centered
2. Ensure pages like Subjects and AssignTeachers use centered modals.
3. Keep shared admin CSS fallback so modals appear centered consistently.

---

## Step 5: Replace Browser Confirm with App Modal (Semester Results)
In Semester Result Detail page:

1. Replace inline confirm() for Publish/Unpublish with Bootstrap modal.
2. Intercept form submit using JS.
3. Show dynamic modal text for publish/unpublish action.
4. Submit actual form only on modal confirmation.

---

## Step 6: Publish Guard (No Approved Students)
Prevent publishing results when no approved students are present.

1. UI guard:
   - Disable Publish button if no students available.
2. Backend guard in PublishSemesterResult:
   - Check approved student count for semester.
   - Block publish and show error message if count = 0.

---

## Step 7: Student Onboarding Controls (Final State)
The final requested state for Student Onboarding table is **clean/read-only**.

1. Keep add manual student form.
2. Keep bulk upload form and import result summary.
3. Show onboarding records table.
4. Remove from final UI:
   - Row checkboxes
   - Select All button
   - Delete Selected button
   - Actions column
   - Locked labels
   - Delete modal and related JS

---

## Step 8: Subject Create/Delete Stability
Ensure Subjects page works without 405 and supports Subject Code.

1. Ensure Subjects form includes:
   - name
   - subjectCode
   - semesterId
2. Add/keep controller POST action:
   - CreateSubject(string name, string subjectCode, int semesterId)
3. Add/keep controller POST action:
   - DeleteSubject(int subjectId)
4. Add model field in Subject:
   - SubjectCode (required)
5. Add DB index/constraints via migration as needed.

---

## Step 9: Session and Layout Stability
Fix Razor session extension usage.

1. Ensure views import:
   - @using Microsoft.AspNetCore.Http
2. Keep layout using Context.Session.GetString("Role") and GetString("Name") safely.

---

## Step 10: Promotion System Wiring
If Semester Promotion features are present, wire all dependencies correctly.

1. Add/keep enum:
   - PromotionStatus (Auto, Hold)
2. Add/keep fields in Student model:
   - CurrentSemesterId
   - CurrentSemester
   - PromotionStatus
3. Add/keep model:
   - PromotionLog
4. Add/keep service and interface:
   - IStudentPromotionService
   - StudentPromotionService
5. Register DI in Program.cs:
   - AddScoped<IStudentPromotionService, StudentPromotionService>()
6. Ensure ApplicationDbContext includes DbSet<PromotionLog>.

---

## Step 11: Student Onboarding Data Model Wiring
If onboarding features are present, ensure all model/context/controller links exist.

1. Add/keep model:
   - StudentOnboardingRecord
2. Add/keep DbSet<StudentOnboardingRecord> in ApplicationDbContext.
3. Add/keep indexes and FK behavior:
   - EnrollmentNo unique
   - Semester FK
   - RegisteredUser FK
4. Keep User model field:
   - IsFirstLogin

---

## Step 12: Database Cleanup + Test Seed Data
Prepare clean teacher/student test accounts.

1. Remove old teacher/student generated records and dependent data.
2. Seed exactly:
   - 10 Teachers (TID2026001 to TID2026010)
   - 10 Students (ENR2026001 to ENR2026010)
3. Set all seeded users:
   - IsApproved = true
   - IsFirstLogin = false
4. Set password for seeded accounts:
   - 9898
5. Enroll all seeded students into Semester - 1.
6. Keep admin account intact.

---

## Step 13: Smoke Tests
After updates, verify:

1. Teacher login works.
2. Student login works.
3. Admin protected routes redirect to Login when unauthenticated.
4. Subject create/delete works.
5. Semesters Save works.
6. Semester publish/unpublish modal flow works.
7. Publish blocked when no approved students.
8. Student Onboarding page renders final read-only table state.

---

## Step 14: Run Application
1. Stop old SmartELibrary process.
2. Run:
   - dotnet run --project SmartELibrary.csproj
3. Confirm app URL:
   - http://localhost:5079
4. Keep app running.

---

## Expected Final Outcome
1. Admin UX improvements completed.
2. Modal behavior consistent and centered.
3. No HTTP 405 on Semesters/Subjects create flows.
4. Semester result publish safeguards active.
5. Student onboarding page is clean and read-only (final requested UI).
6. Test users seeded and login-ready.
7. App runs successfully on localhost:5079.

---

# SmartELibrary Master Consolidated Prompt (All Recent Updates)

Use this section as the final master prompt for the last 3-4 days of project updates.

## Core Project Vision
Build a stable College Management Admin Panel with strong data integrity, simple MVC patterns, clean modal UX (application popups), Excel-based bulk operations, and role-based flows for Admin, Teacher, and Student.

## Master Rules
1. Never delete this Prompt.md content. Only append new updates.
2. Keep UI plain and consistent with existing Razor + Bootstrap style.
3. Replace browser `confirm()` popups with in-app Bootstrap modal popups on admin actions.
4. Enforce DB-level integrity where business rules require uniqueness.
5. After every update, stop old run and run app again on localhost.

---

## A) Register -> Login Flow (Complete Logic)

### A1. Public Registration and OTP
1. User fills registration details.
2. OTP is generated and validated.
3. On OTP success:
   - Create `Users` record.
   - If role = Teacher: create `Teachers` record with generated TeacherId.
   - If role = Student: create `Students` record with EnrollmentNumber.
4. New users are created as pending approval where applicable.

### A2. Login (Unified)
1. User logs in by phone + password.
2. Password verified via hash.
3. Student has single-session guard.
4. Redirect by role:
   - Admin -> Admin Dashboard
   - Teacher -> Teacher Dashboard
   - Student -> Student Dashboard

### A3. Password Recovery
1. Forgot password -> OTP verify -> reset password.
2. Password is re-hashed and saved securely.

---

## B) Bulk Insertion via Excel (Students + Teachers)

### B1. Students Bulk Insert (Admin Student Onboarding)
1. Upload `.xlsx` with headers:
   - EnrollmentNo, Name, Email, Phone, DateOfBirth, Semester
2. Validate all fields row-by-row.
3. Validate email format.
4. Prevent duplicate EnrollmentNo:
   - inside file
   - in onboarding table
   - in existing student accounts
5. Auto-create semester if missing (as currently implemented).
6. Insert rows into `StudentOnboardingRecords`.

### B2. Teachers Bulk Insert
1. Upload `.xlsx` with teacher details.
2. Validate required fields and uniqueness checks.
3. Create `Users` + `Teachers` records safely.

### B3. Template Download
1. Keep downloadable sample Excel for Student Onboarding.
2. Keep downloadable sample Excel for Teachers.

---

## C) Subject Management + Subject Code Integrity

### C1. Subject Data Model
1. `Subject` includes:
   - Name
   - SubjectCode (required)
   - SemesterId

### C2. Subject Code Rule
1. SubjectCode must be unique globally.
2. Admin cannot create same SubjectCode in another semester.
3. Subject name duplicate in same semester is blocked.

### C3. Subjects UI
1. Create Subject form includes Subject Code input.
2. Subject list table shows:
   - SubjectCode
   - Subject
   - Semester

---

## D) Assign Teacher Rule (No Duplicate Assignment)

### D1. Business Rule
1. One Subject in one Semester must have only one assigned teacher.
2. Admin can change teacher anytime.

### D2. Technical Rule
1. Do not create multiple mappings for same Subject.
2. If mapping exists for Subject:
   - UPDATE TeacherId.
3. If mapping does not exist:
   - INSERT mapping.

### D3. DB Integrity
1. Unique index on TeacherSubjects SubjectId (effective one assignment per subject).
2. Migration handles deduplication before unique index creation.

### D4. AssignTeachers UI
1. Show SubjectCode in dropdown and table.
2. Button text behavior:
   - Assign (new mapping)
   - Update Assignment (existing mapping)

---

## E) Semester Students Page (Full Admin Control)

### E1. Data Display Rule
1. Show students by current semester assignment.
2. Avoid historical duplicate display across multiple semesters.

### E2. Columns + Search + Export
1. Columns:
   - Select control
   - Enrollment Number
   - Student Name
   - Phone
   - Email
   - Status
   - Enrolled
   - Decision
   - Action
2. Search bar at top with heading text.
3. Search over Name / Phone / Email / Enrollment / Semester.
4. Download per-semester student list as Excel-compatible CSV.

### E3. Single + Bulk Remove UI
1. Row checkbox selection.
2. Select All in header control.
3. Remove Selected action.
4. Remove single student action.

### E4. Critical Deletion Behavior (Final)
1. On remove action, delete student from database (not only semester mapping):
   - MaterialPageProgress
   - ProgressTrackings
   - QuizResults
   - StudentEnrollments
   - Students
   - Users
2. Keep archive trail in `DeletedUsers` before hard delete.

### E5. Popup Behavior
1. Use in-app Bootstrap modal for confirmation.
2. Message must clearly state:
   - student account deletion
   - semester record removal
3. Show status popup after redirect using app modal.

---

## F) Removed/Changed Flows

### F1. Removed Page
1. Remove Admin Enrollment Requests page from route/navigation/project artifacts.

### F2. Student EnrollSemester Removal
1. Remove student-side Enroll Semester page and links.
2. Admin controls semester assignment.

### F3. Student Library Access Rule
1. Promoted student can still access previous semester material/chapter.
2. Fix LINQ translation issue by sorting semesters in memory where needed.

---

## G) Application Popup Standardization

Convert all critical admin actions from browser `confirm()` to in-app modal popup.

### G1. Implemented Areas
1. Semester Students remove actions.
2. Teacher Deactivate action.
3. Users Delete action.
4. Semester Result publish/unpublish actions.

### G2. Modal UX Standard
1. Centered dialog (`modal-dialog-centered`).
2. Clear action wording.
3. Explicit confirm button labels.

---

## H) Users / Teachers / Security Logic

1. Keep System Admin protection (cannot be deleted).
2. Keep Admin-role delete restrictions.
3. Preserve role-based auth filters and session checks.
4. Keep safe cleanup logic for delete paths.

---

## I) Database & Migration Guidance

1. Create migration when model/index changes are introduced.
2. For existing live data:
   - Backfill new required columns (e.g., SubjectCode).
   - Deduplicate records before adding unique constraints.
3. Apply migration with:
   - `dotnet ef database update`

---

## J) Final Smoke-Test Checklist

1. Register + OTP + Login flows work.
2. Teacher and Student bulk Excel insertion flows work.
3. Subject create with SubjectCode works.
4. Duplicate SubjectCode across semesters is blocked.
5. AssignTeacher inserts first time and updates next time.
6. No duplicate teacher assignment for same subject/semester.
7. Semester Students search/filter/export works.
8. Remove student performs full DB deletion + archive entry.
9. Deactivate teacher uses application popup modal.
10. Delete user uses application popup modal.
11. App runs and loads on localhost without route errors.

---

## K) Run/Verify Command Flow
1. Stop existing app process if port is busy.
2. Build + migrate if needed:
   - `dotnet ef database update`
3. Run app:
   - `dotnet run --project SmartELibrary.csproj`
4. Verify URL:
   - `http://localhost:5079`

