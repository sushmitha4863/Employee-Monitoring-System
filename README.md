# Employee Monitoring System

A full-stack Employee Monitoring System designed to manage employees, attendance, leave requests, projects, tasks, timesheets, recruitment, notifications, reports, and role-based organizational workflows.

## Tech Stack

### Frontend
- React.js
- JavaScript
- CSS
- Axios
- React Router

### Backend
- ASP.NET Core Web API
- C#
- Entity Framework Core
- JWT Authentication

### Database
- Microsoft SQL Server

## User Roles

- Admin
- HR
- Manager
- Employee

## Main Features

- Secure login with JWT authentication
- Multi-factor authentication
- Role-based access control
- Employee and department management
- Attendance and break tracking
- Leave request and approval workflow
- Project and task management
- Weekly timesheets
- Recruitment and onboarding workflows
- Performance tracking
- Notifications
- Reports and audit logs
- Profile and system settings

## Project Structure

```text
Employee-Monitoring-System/
├── backend/
│   ├── Controllers/
│   ├── DTOs/
│   ├── Data/
│   ├── Models/
│   ├── Services/
│   ├── Migrations/
│   └── Program.cs
│
└── frontend/
    ├── public/
    ├── src/
    │   ├── components/
    │   ├── context/
    │   ├── pages/
    │   ├── routes/
    │   ├── services/
    │   └── utils/
    └── package.json