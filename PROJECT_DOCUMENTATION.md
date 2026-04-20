Version: 2.5
Last Updated: 13/04/2026
Updated By: Product and Flow Update Sync

# EduVault (Smart E-Library)

## 1. Project Title
EduVault (Smart E-Library)

## 2. Project Summary
EduVault is an ASP.NET Core MVC web application for academic content delivery, quiz assessment, and progress analytics.

It supports three roles:
- Admin: governance, approvals, academic setup, reporting, and result publication
- Teacher: chapter creation, file upload, quiz creation, and analytics review
- Student: semester enrollment, learning, quiz attempts, and result viewing

## 3. Business Goals
- Create one centralized digital learning platform
- Keep semester and subject structure clear and controlled
- Track real learner behavior (time, page completion, quiz score)
- Give admin and teachers measurable analytics
- Publish semester results in a controlled workflow

## 4. Scope
### In Scope (Implemented)
- Role-based authentication and authorization (Admin, Teacher, Student)
- OTP flow for student login and account recovery
- Admin approval workflows for users and semester enrollments
- Semester, subject, and teacher assignment management
- Rich-text chapter materials with multi-page sequencing
- Standalone and chapter-linked quizzes
- Student progress tracking and weighted progress score
- Semester result computation and publish/unpublish control
- Student result viewing with print-friendly layout

### Out of Scope (Current Release)
- Payment gateway integration
- Live classes and video streaming
- Production SMS gateway integration (OTP provider abstraction is present)
- Multi-tenant institution management

## 5. Technology Stack
### Application Layer
- ASP.NET Core MVC (.NET 10, target framework net10.0)
- Razor Views + Bootstrap-based UI
- Session-based user state

### Data Layer
- MySQL database
- Entity Framework Core with Pomelo provider
- Code-first migrations

### Runtime and Tools
- dotnet CLI
- Visual Studio Code / Visual Studio
- XAMPP or managed MySQL (configurable)

## 6. High-Level Architecture
1. Browser sends request to MVC controllers.
2. Controllers validate role/session and request state.
3. Controllers call EF Core and domain services.
4. Data is read/written in MySQL.
5. Razor view is returned to the client.

Core implementation units:
- Controllers: role modules and user workflows
- Filters: RoleAuthorizeAttribute, student approval checks
- Services: OTP, progress calculation, result backfill/helper services
- Data: ApplicationDbContext with relational entities

## 7. Role-Wise Functional Modules
### Admin
- User management (approve, edit, delete with safety checks)
- Semester and subject management
- Teacher-subject assignment
- Enrollment request approval/rejection
- Semester-student grouped view
- Platform analytics and reports
- Semester result publication controls

### Teacher
- Upload chapter materials with at least 2 pages
- Upload file/link-based resources
- Create quizzes with optional availability and time limits
- Create per-page quizzes directly from chapter authoring flow
- Manage quiz content through stable quiz identity in edit flow
- Review student engagement and performance analytics

### Student
- OTP-based login and secure session handling
- Semester enrollment request submission
- Access approved learning resources
- Complete chapter pages and submit quizzes
- Pause reading and continue later from Library resume flow
- Resume chapter from the correct next-pending page
- View published semester results and print output

## 8. Progress and Result Logic
### Material Progress Formula
Final Progress (%) =
- Screen Time % x 50%
- Quiz Score % x 40%
- Completion % x 10%

### Notes
- Page completion is captured once per page attempt lifecycle.
- Scroll depth may reduce effective reading-time contribution.
- Chapter-linked quizzes are used in per-material progress logic.
- Standalone quizzes are tracked separately for quiz reporting.

### Semester Result Logic
- Chapter Result: from stored material progress percent
- Subject Average: average of chapter results within a subject
- Semester Final Result: average of subject averages

## 9. Data Model Snapshot
Primary entities:
- Users, Admins, Teachers, Students
- Semesters, Subjects, Topics
- TeacherSubjects
- StudentEnrollments
- Materials, MaterialPages, MaterialPageProgress
- Quizzes, QuizQuestions, QuizResults
- ProgressTrackings
- OtpVerifications
- DeletedUsers
- SemesterResultPublishes

## 10. Security and Compliance Notes
- Passwords are hashed (not plaintext)
- Server-side role guard on secured actions
- Session-based authentication flow with invalid-session handling
- OTP validity and one-time verification controls
- Validation on all key input workflows

## 10.1 April 2026 Implemented Updates
This section records all major workflow updates implemented in the latest cycle.

### 10.1.1 Authentication, Session, and Access Control
- Unified login was tightened so student credentials on the main login route are routed through OTP verification before dashboard access.
- Session enforcement was strengthened to handle invalid or stale session states and redirect users to safe logout/login behavior.
- Role/session guard consistency was improved across Admin, Teacher, and Student protected areas.

### 10.1.2 Student Reading Experience (Pause, Resume, Flexible Completion)
- Chapter reading was updated from single uninterrupted flow to a flexible flow where students can pause and continue later.
- Pause operation stores reading state and allows return to Library without losing progress.
- Library now supports resume-aware actions so students continue from the correct page instead of always restarting.
- Page completion and chapter navigation were adjusted to support progressive reading sessions across multiple visits.

### 10.1.3 Student ReadChapter UX and Navigation Logic
- Student-visible chapter progress/time strip was removed from ReadChapter UI to reduce clutter.
- Next-page navigation logic was corrected to ensure page transitions work reliably.
- Quiz gate behavior was aligned so when the current page has a pending quiz, the quiz popup appears in flow before moving forward.
- After quiz submission, navigation proceeds correctly to the next page/chapter path according to completion state.

### 10.1.4 Teacher Chapter Upload and Quiz Authoring
- Public Material checkbox was removed from the teacher upload UI.
- Quiz authoring in chapter flow was simplified so Add Quiz leads directly into adding questions.
- Dedicated quiz title input in upload flow was removed from teacher-facing UI.
- Backend quiz handling retains stable quiz identity and uses safe title fallback generation where required for persistence.
- Quiz requirement handling was adjusted to support optional-per-page quiz usage while preserving validation for actual question payloads.

### 10.1.5 Analytics and Anti-Cheat Data Capture
- Quiz violation analytics are sourced from frontend anti-cheat trigger events (such as focus-loss/visibility changes) and persisted in quiz result audit flags.
- Teacher progress views continue to aggregate these stored flags for reporting and review.

### 10.1.6 Operational Stability During Development
- Startup/run process handling was improved in practice to resolve locked executable scenarios during repeated run/rebuild cycles.


## 11. Environment and Configuration
Configuration can be supplied via:
- appsettings.json
- appsettings.Development.json
- environment variables
- command-line arguments

Common database configuration keys:
- ConnectionStrings:DefaultConnection
- Database:Host
- Database:Port
- Database:Name
- Database:User
- Database:Password

## 12. Local Run Guide
1. Ensure MySQL server is running.
2. Ensure schema exists (example: smartelibrary_db).
3. Start the app:
   - dotnet run
   - optional DB override via command-line arguments
4. Open localhost URL from runtime output.

## 13. Quality Checklist
- Build passes with no blocking errors
- Role guards validated on protected routes
- Enrollment and approval workflows verified
- Progress aggregation outputs expected values
- Published/unpublished result visibility validated
- Mobile responsiveness validated on shared header and critical pages

## 14. Known Limitations
- SMS delivery is environment-dependent
- High-volume analytics may need future query optimization
- UI currently prioritizes clarity over complex interaction patterns

## 15. Change Log
- 2.5: Added complete April 2026 implementation updates for student OTP-first login, session handling, pause/resume reading flow, teacher quiz authoring simplification, quiz-popup navigation fixes, and anti-cheat analytics traceability
- 2.4: Documentation refresh, terminology alignment to EduVault, feature and flow consistency updates
- 2.3: Semester result publication and analytics details expanded
