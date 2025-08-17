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
   - Create / Edit / Delete with **CSRF protection**
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

- **Program & Startup**
  - Auth cookies, rate limiter, DI registrations  
  - **File:** [`TicketingSystem/Program.cs`](TicketingApp/Program.cs)

- **Data Layer**
  - EF Core context, DbSets, relationships  
  - **File:** [`TicketingSystem/Data/AppDbContext.cs`](TicketingSystem/Data/AppDbContext.cs)

- **MVC**
  - Controllers → HTTP endpoints  
  - Views → Razor UI  
  - Models → Entities & ViewModels  

---

## 📂 Entities

- **User** — Minimal user record (Email, Password, Role). _Plain-text for demo ( intentionally insecure )._  
  **File:** [`TicketingSystem/Models/User.cs`](TicketingSystem/Models/User.cs)

- **UserInsecure** — Separate table **only** for SQL Injection demo.  
  **File:** [`TicketingSystem/Models/UserInsecure.cs`](TicketingSystem/Models/UserInsecure.cs)

- **Ticket** — Title, Description, Status + Creator/Assignee relations.  
  **File:** [`TicketingSystem/Models/Ticket.cs`](TicketingSystem/Models/Ticket.cs)

- **Comment** — Per-ticket comments with author and timestamps.  
  **File:** [`TicketingSystem/Models/Comment.cs`](TicketingSystem/Models/Comment.cs)

---

## ⚙ Controllers & Endpoints

### AuthController
- Secure login: reCAPTCHA + RateLimiter + EF LINQ (parameterized)  
- Weak login: no CAPTCHA / no rate limit (brute-force demo)  
- Vulnerable login: **string-concat SQL** with `FromSqlRaw` (SQLi demo)  
- Logout & AccessDenied endpoints  
**File:** [`TicketingSystem/Controllers/AuthController.cs`](TicketingSystem/Controllers/AuthController.cs)

### UsersController
- Admin-only (class-level `[Authorize(Roles="Admin")]`)  
- List/Details/Create/Edit users (CSRF-protected)  
- **Demo misconfig:** `create-auto` action may use `[AllowAnonymous]` to show risk  
**File:** [`TicketingSystem/Controllers/UsersController.cs`](TicketingSystem/Controllers/UsersController.cs)

### TicketsController
- Auth-required (class-level `[Authorize]`)  
- Ownership checks: **only Creator or Admin** can modify; Creator/Assignee/Admin can view  
- CSRF on mutating actions  
**File:** [`TicketingSystem/Controllers/TicketsController.cs`](TicketingSystem/Controllers/TicketsController.cs)

### CommentsController
- Auth-required; CSRF-protected add/edit/delete  
- Simple ownership checks for comment actions  
**File:** [`TicketingSystem/Controllers/CommentsController.cs`](TicketingSystem/Controllers/CommentsController.cs)

### (Optional) KanbanController
- Board/pin UI helpers; not central to security demo  
**File:** [`TicketingSystem/Controllers/KanbanController.cs`](TicketingSystem/Controllers/KanbanController.cs)

---

## 🛠 Services

- **RecaptchaService** — Verifies Google reCAPTCHA tokens via HTTP  
  **File:** [`TicketingSystem/Services/RecaptchaService.cs`](TicketingSystem/Services/RecaptchaService.cs)

- **AccountCreationService** — Generates random demo users/passwords  
  **File:** [`TicketingSystem/Services/AccountCreationService.cs`](TicketingSystem/Services/AccountCreationService.cs)

- **LoginAttemptService** — Illustrative throttling/attempt tracking  
  **File:** [`TicketingSystem/Services/LoginAttemptService.cs`](TicketingSystem/Services/LoginAttemptService.cs)

---

## 🔐 Security Highlights (OWASP)

### A03: Injection (SQLi)
**Vulnerable (demo):**
```csharp
// /auth/login-insecure
var sql = $"SELECT * FROM UsersInsecure WHERE Email = '{email}' AND Password = '{password}'";
var rows = await _context.UsersInsecure.FromSqlRaw(sql).ToListAsync();
