# Ticket System – OWASP Security Demo (ASP.NET Core MVC)

This repository demonstrates **OWASP Top 10** concepts — focusing on:

- **A03: SQL Injection**
- **A07: Identification & Authentication Failures**

The project is a simple **ASP.NET Core MVC** ticketing app.  
It contains both **secure implementations** and **deliberately vulnerable endpoints** for demonstration.

---

## 📑 Table of Contents

- [General Flow](#general-flow)
- [What Changed vs Previous Version](#what-changed-vs-previous-version)
- [Architecture](#architecture)
- [Entities](#entities)
- [Controllers & Endpoints](#controllers--endpoints)
- [Services](#services)
- [Security Highlights (OWASP)](#security-highlights-owasp)
- [How to Run Locally](#how-to-run-locally)
- [Demo Playbook](#demo-playbook)
- [Hardening Checklist](#hardening-checklist)
- [License](#license)

---

## 🔄 General Flow

1. **Login**
   - ✅ Secure: `/auth/login` → reCAPTCHA + Rate Limiter + EF LINQ (parameterized)
   - ⚠️ Weak: `/auth/login-open` → EF LINQ but **no CAPTCHA** / **no rate limit**
   - ❌ Vulnerable: `/auth/login-insecure` → **Raw SQL** (`FromSqlRaw`) → SQL Injection

2. **Ticket Operations**
   - Create / Edit / Delete tickets with **CSRF protection**
   - Only **Creator**, **Assignee**, or **Admin** may edit/view

3. **Comment Operations**
   - Add / Edit / Delete with CSRF and ownership checks

4. **Admin Operations**
   - List & manage users (Admin only)
   - **Auto-create user endpoint** included for demo

---

## 📌 What Changed vs Previous Version

- Adopted **Cookie Authentication + Claims** with `[Authorize]` attributes  
- Added **ASP.NET Rate Limiter** (`LoginPolicy`)  
- Integrated **Google reCAPTCHA** service  
- Added a separate **`UserInsecure`** table for SQLi demo  
- Hardened controllers with `[ValidateAntiForgeryToken]` and ownership checks  

---

## 🏗 Architecture

- **Program & Startup** — Auth cookies, rate limiter, DI registrations  
  [Program.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Program.cs)

- **Data Layer** — EF Core context, DbSets, relationships  
  [AppDbContext.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Data/AppDbContext.cs)

- **MVC** — Controllers, Views, Models  

---

## 📂 Entities

- **User** — Minimal user record (Email, Password, Role). ⚠️ Plain-text for demo  
  [User.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Models/User.cs)

- **UserInsecure** — Separate table used for SQL Injection demo  
  [UserInsecure.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Models/UserInsecure.cs)

- **Ticket** — Title, Description, Status + Creator/Assignee  
  [Ticket.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Models/Ticket.cs)

- **Comment** — Ticket comments with author and timestamps  
  [Comment.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Models/Comment.cs)

---

## ⚙ Controllers & Endpoints

### [AuthController.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Controllers/AuthController.cs)
- Secure login: reCAPTCHA + RateLimiter + EF LINQ (parameterized)  
- Weak login: no CAPTCHA / no rate limit (brute-force demo)  
- Vulnerable login: `FromSqlRaw` with string concat (SQL Injection demo)  
- Logout & AccessDenied endpoints  

### [UsersController.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Controllers/UsersController.cs)
- Admin-only (`[Authorize(Roles="Admin")]`)  
- List/Details/Create/Edit (CSRF-protected)  
- Demo misconfig: `create-auto` can be `[AllowAnonymous]`  

### [TicketsController.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Controllers/TicketsController.cs)
- `[Authorize]` class-level  
- Ownership checks: only Creator or Admin may modify  
- CSRF protection on create/edit/delete  

### [CommentsController.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Controllers/CommentsController.cs)
- `[Authorize]` class-level  
- Add/Edit/Delete with CSRF + ownership checks  

### [KanbanController.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Controllers/KanbanController.cs)  
- Board/pin helpers (UI feature, not central to security demo)  

---

## 🛠 Services

- [RecaptchaService.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Services/RecaptchaService.cs) — Verifies Google reCAPTCHA  
- [AccountCreationService.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Services/AccountCreationService.cs) — Generates demo users  
- [LoginAttemptService.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Services/LoginAttemptService.cs) — Tracks failed attempts  

---

## 🔐 Security Highlights (OWASP)

### A03: Injection (SQLi)
```csharp
// /auth/login-insecure
var sql = $"SELECT * FROM UsersInsecure WHERE Email = '{email}' AND Password = '{password}'";
var rows = await _context.UsersInsecure.FromSqlRaw(sql).ToListAsync();
