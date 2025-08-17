# Ticket System – OWASP Security Demo (ASP.NET Core MVC)

This repository demonstrates **OWASP Top 10** concepts with a focus on:

- **A03: SQL Injection**
- **A07: Identification & Authentication Failures**

A simple **ASP.NET Core MVC** ticketing app that includes both **secure** and **deliberately vulnerable** endpoints for demonstration purposes.

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
   - ⚠️ Weak (demo): `/auth/login-open` → EF LINQ but **no CAPTCHA** / **no rate limit** → brute-force possible
   - ❌ Vulnerable (demo): `/auth/login-insecure` → **Raw SQL** (`FromSqlRaw`) → SQL Injection

2. **Tickets**
   - Create / Edit / Delete with **CSRF protection**
   - Authorization: only **Creator**, **Assignee**, or **Admin**

3. **Comments**
   - Add / Edit / Delete with CSRF + ownership checks

4. **Admin**
   - Manage users (Admin-only)
   - Auto user creation helper (intentionally exposed for testing misconfiguration)

---

## 📌 What Changed vs Previous Version

- Cookie Authentication with **Claims** (`Email`, `Role`)  
- Attribute-based authorization (`[Authorize]`, `[Authorize(Roles="Admin")]`)  
- **ASP.NET Rate Limiter** (`LoginPolicy`) for secure login  
- **Google reCAPTCHA** integration for bot protection  
- Dedicated **`UserInsecure`** table for SQL Injection demo  
- Consistent use of `[ValidateAntiForgeryToken]` on state-changing actions  

---

## 🏗 Architecture

- **Program/Startup** → auth cookies, rate limiting, DI  
  [Program.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Program.cs)

- **Data** → EF Core DbContext, relationships  
  [AppDbContext.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Data/AppDbContext.cs)

- **Config** → Application settings  
  [appsettings.json](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/appsettings.json)

---

## 📂 Entities

- **User** — Minimal user (Email, Password, Role). ⚠️ Passwords stored in **plain text** for demo.  
  [User.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Models/User.cs)

- **UserInsecure** — Used exclusively for SQL Injection demo.  
  [UserInsecure.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Models/UserInsecure.cs)

- **Ticket** — Title, Description, Status; `CreatedByUser`, `AssignedToUser`.  
  [Ticket.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Models/Ticket.cs)

- **Comment** — Ticket comments; author + timestamp.  
  [Comment.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Models/Comment.cs)

---

## ⚙ Controllers & Endpoints

- **[AuthController.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Controllers/AuthController.cs)**
  - `/auth/login` → **secure** login (RateLimiter + reCAPTCHA + EF LINQ)  
  - `/auth/login-open` → **weak demo** (no CAPTCHA, no rate limit → brute-force risk)  
  - `/auth/login-insecure` → **SQL Injection demo** using raw SQL (see [line ~45](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Controllers/AuthController.cs#L45))  
  - `/auth/logout`, `/auth/access-denied`

- **[UsersController.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Controllers/UsersController.cs)**
  - Restricted with `[Authorize(Roles="Admin")]`  
  - CRUD actions for users (CSRF-protected)  
  - `create-auto` → intentionally misconfigured (`[AllowAnonymous]` + no CSRF)  

- **[TicketsController.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Controllers/TicketsController.cs)**
  - `[Authorize]` class-level  
  - Ownership checks → only **Creator** or **Admin** can modify  
  - `[ValidateAntiForgeryToken]` on Create/Edit/Delete  

- **[CommentsController.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Controllers/CommentsController.cs)**
  - `[Authorize]`  
  - Add/Edit/Delete comments with CSRF + ownership checks  

- **[KanbanController.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Controllers/KanbanController.cs)**
  - Board/pin UI helpers (not security-critical)  

---

## 🛠 Services

- [RecaptchaService.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Services/RecaptchaService.cs) → Validates Google reCAPTCHA tokens  
- [AccountCreationService.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Services/AccountCreationService.cs) → Generates demo users  
- [LoginAttemptService.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Services/LoginAttemptService.cs) → Tracks failed login attempts  

---

## 🔐 Security Highlights (OWASP)

### A03: SQL Injection (demo)

**Vulnerable code ([AuthController.cs#L45](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Controllers/AuthController.cs#L45)):**
```csharp
var sql = $"SELECT * FROM UsersInsecure WHERE Email = '{email}' AND Password = '{password}'";
var rows = await _context.UsersInsecure.FromSqlRaw(sql).ToListAsync();
