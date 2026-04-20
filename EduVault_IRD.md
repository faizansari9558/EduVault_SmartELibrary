# EduVault Information Requirement Document (IRD)

Version: 1.2
Last Updated: 13/04/2026
Updated By: Requirements Alignment Update

## 1. Document Purpose
This IRD defines the information and process requirements for EduVault, ensuring implementation, operations, and reporting remain aligned across Admin, Teacher, and Student modules.

## 2. Product Overview
EduVault is a role-based learning platform that manages semester-structured resources, quizzes, enrollment approvals, and progress tracking.

## 3. Stakeholders and Actors
- System Admin
- Teacher
- Student

## 4. Information Domains
- User identity and access control data
- Academic hierarchy (semester, subject, topic)
- Learning resources (chapter pages, files, links)
- Quiz definitions, questions, and submissions
- Enrollment requests and approval history
- Progress, engagement, and result publication data

## 5. Functional Requirement Groups
### 5.1 Authentication and Access
- Role-based login and route authorization
- Student OTP verification workflow
- OTP-first student access from unified login route before dashboard entry
- Password recovery and password change workflow
- Cross-role session integrity handling for invalid/stale sessions

### 5.2 Admin Operations
- User approval and lifecycle management
- Semester and subject administration
- Teacher assignment to subjects
- Enrollment request decisions
- Semester result publish/unpublish control

### 5.3 Teacher Operations
- Chapter creation with multi-page rich content
- File/link resource upload
- Quiz creation (chapter-linked and standalone)
- Direct question-entry flow after Add Quiz in chapter authoring UI
- Quiz identity continuity during edit/update operations
- Student analytics review

### 5.4 Student Operations
- Semester enrollment request
- Approved content consumption
- Quiz participation
- Pause reading and resume later from Library
- Continue chapter from correct pending page in resume flow
- In-page quiz popup sequence before forward navigation when pending quiz exists
- Published semester result review and print

## 6. Non-Functional Requirements
- Usability: simple, workflow-oriented UI for education users
- Security: role guards, hashed passwords, OTP validation
- Reliability: transactional saves and consistent data integrity
- Performance: responsive dashboard/report queries for classroom-scale usage
- Maintainability: controller-service-data layering with clear entity boundaries

## 7. Data Requirements
### 7.1 Core Identity
- User role, approval state, contact identity, and credentials

### 7.2 Academic Structure
- Semester and subject catalog
- Topic grouping (optional)
- Teacher-subject mapping

### 7.3 Learning Content
- Material metadata and ownership
- Material pages with sequence order
- Resource type support (chapter/file/link)

### 7.4 Assessment and Progress
- Quiz metadata and question bank
- Quiz attempt outcomes
- Page progress, reading-time and scroll-depth metrics
- Aggregated progress percentages
- Anti-cheat audit flags and violation reason/time metadata for quiz submissions

### 7.5 Outcome Publication
- Semester result publishing state per semester
- Published timestamp and publishing admin reference

## 8. Entity Inventory
| Entity | Purpose |
| --- | --- |
| Users | Base identity and role information |
| Admins | Admin profile linked to Users |
| Teachers | Teacher profile linked to Users |
| Students | Student profile linked to Users |
| Semesters | Academic semester definitions |
| Subjects | Semester-bound subject definitions |
| Topics | Optional subject sub-grouping |
| TeacherSubjects | Teacher-subject assignment map |
| StudentEnrollments | Enrollment and approval workflow records |
| Materials | Learning content master records |
| MaterialPages | Rich-text pages under chapter materials |
| MaterialPageProgress | Student chapter page engagement records |
| Quizzes | Quiz master records |
| QuizQuestions | Objective question bank |
| QuizResults | Student quiz submissions and scores |
| ProgressTrackings | Aggregated performance metrics |
| OtpVerifications | OTP generation/verification records |
| DeletedUsers | Soft-audit archive for removed users |
| SemesterResultPublishes | Semester result visibility control |

## 9. Relationship Summary
- One Semester to many Subjects
- One Subject to many Topics
- Many Teachers to many Subjects via TeacherSubjects
- Many Students to many Semesters via StudentEnrollments
- One Material to many MaterialPages
- One MaterialPage to many MaterialPageProgress rows
- One Quiz to many QuizQuestions
- One Quiz to many QuizResults
- One Semester to one publish-state record in SemesterResultPublishes

## 10. Information Lifecycle
1. Admin initializes semesters and subjects.
2. Admin assigns teachers to subjects.
3. Teachers publish learning content and quizzes.
4. Students request semester enrollment.
5. Admin approves enrollments.
6. Students consume content with pause/resume continuity and submit quizzes.
7. System updates progress metrics and report views.
8. Admin publishes semester results for student visibility.

## 11. Security Requirements
- Hashed password storage for all users
- Server-side authorization checks by role
- OTP expiration and one-time use behavior
- Input validation on all core forms
- Session timeout and session integrity controls
- Invalid-session safe handling with controlled redirect/logout behavior

## 12. Reporting Requirements
- Student progress distribution by status bands
- Engagement alerts for low activity
- Subject and semester level summaries
- Semester result breakdown per student

## 13. Implementation Notes
- Primary stack: ASP.NET Core MVC + EF Core + MySQL
- Supports local MySQL (for example XAMPP) and managed MySQL via config
- Database target can be overridden through runtime configuration

## 14. Future Extension Considerations
- External SMS gateway integration
- Advanced reporting filters and exports
- Notification center for approvals and publication events
- Institution-level multi-tenant isolation

## 15. Implemented Requirement Updates (April 2026)
### 15.1 Student Login and Session Enforcement
- Student login from main login path now enforces OTP verification prior to dashboard access.
- Session checks were aligned to reduce inconsistent states across Admin, Teacher, and Student modules.

### 15.2 Student Reading Continuity Requirements
- Reading flow now supports pause, leave, and later continuation without forced one-session completion.
- Library must present resume-aware actions for partially completed chapter materials.
- ReadChapter next-step behavior must respect pending page quiz state before moving ahead.

### 15.3 Teacher Authoring Requirements
- Public Material option was removed from teacher-facing upload flow.
- Quiz title manual entry is no longer mandatory in chapter authoring UI.
- Add Quiz action should directly support immediate question authoring.

### 15.4 Reporting and Compliance Requirements
- Quiz violation reporting must map to persisted anti-cheat audit attributes in quiz result records.
- Teacher progress analytics should continue presenting violation indicators based on stored audit evidence.
