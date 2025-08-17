# Ticket System – OWASP Security Demo (ASP.NET Core MVC)

This repository demonstrates **OWASP Top 10** concepts with a focus on:

- **A03: SQL Injection**
- **A07: Identification & Authentication Failures**

A simple **ASP.NET Core MVC** ticketing app that includes both **secure** and **deliberately vulnerable** endpoints for demos.

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

2. **Tickets**
   - Create / Edit / Delete with **CSRF protection**
   - View/Edit rights: **Creator**, **Assignee**, **Admin**

3. **Comments**
   - Add / Edit / Delete with CSRF + ownership checks

4. **Admin**
   - Manage users (Admin-only)
   - Demo helper: auto user create

---

## 📌 What Changed vs Previous Version

- Cookie Auth + **Claims**; attribute-based security (`[Authorize]`, `[Authorize(Roles="Admin")]`)
- **ASP.NET Rate Limiter** (`LoginPolicy`) on secure login
- **Google reCAPTCHA** integration
- Dedicated **`UserInsecure`** table for SQLi demo
- Widespread `[ValidateAntiForgeryToken]` + ownership checks

---

## 🏗 Architecture

- **Program/Startup** – auth cookies, rate limiting, DI  
  **`Program.cs`** →  
  https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/TicketSystem/Program.cs

- **Data** – EF Core DbContext, relationships  
  **`Data/AppDbContext.cs`** →  
  https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/TicketSystem/Data/AppDbContext.cs

- **Config** – app settings, keys  
  **`appsettings.json`** →  
  https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/TicketSystem/appsettings.json

- **MVC** – Controllers, Views, Models (links aşağıda)

---

## 📂 Entities

- **User** — Minimal user (Email, Password, Role). _Plain-text for demo (bilerek)._  
  https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/TicketSystem/Models/User.cs

- **UserInsecure** — SQLi gösterimi için ayrılmış tablo.  
  https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/TicketSystem/Models/UserInsecure.cs

- **Ticket** — Title, Description, Status; `CreatedByUser`, `AssignedToUser`.  
  https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/TicketSystem/Models/Ticket.cs

- **Comment** — Ticket yorumları; yazar + timestamp.  
  https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/TicketSystem/Models/Comment.cs

---

## ⚙ Controllers & Endpoints

- **AuthController**
  - `/auth/login` → **secure** (RateLimiter + reCAPTCHA + EF LINQ)
  - `/auth/login-open` → **weak** (brute force demoları)
  - `/auth/login-insecure` → **vulnerable** (`FromSqlRaw` string concat → **SQLi**)
  - `/auth/logout`, `/auth/access-denied`
  https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/TicketSystem/Controllers/AuthController.cs

- **UsersController**
  - `[Authorize(Roles="Admin")]` (Admin-only)
  - List/Details/Create/Edit (CSRF)
  - _Demo misconfig_: `create-auto` örneği (AllowAnonymous + no CSRF)
  https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/TicketSystem/Controllers/UsersController.cs

- **TicketsController**
  - `[Authorize]` (login zorunlu)
  - **Ownership checks**: sadece **Creator** veya **Admin** modifiye edebilir
  - Create/Edit/Delete → **[ValidateAntiForgeryToken]**
  https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/TicketSystem/Controllers/TicketsController.cs

- **CommentsController**
  - `[Authorize]`
  - Add/Edit/Delete with CSRF + ownership checks
  https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/TicketSystem/Controllers/CommentsController.cs

- **KanbanController** (opsiyonel UI)
  - Basit board/pin özellikleri (güvenlik demosunun parçası değil)
  https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/TicketSystem/Controllers/KanbanController.cs

---

## 🛠 Services

- **RecaptchaService** — Google reCAPTCHA doğrulaması  
  https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/TicketSystem/Services/RecaptchaService.cs

- **AccountCreationService** — Demo kullanıcı üretir  
  https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/TicketSystem/Services/AccountCreationService.cs

- **LoginAttemptService** — (İllüstratif) login denemesi takibi  
  https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/TicketSystem/Services/LoginAttemptService.cs

---

## 🔐 Security Highlights (OWASP)

### A03: Injection (SQLi)
**Vulnerable (demo):**
```csharp
// /auth/login-insecure
var sql = $"SELECT * FROM UsersInsecure WHERE Email = '{email}' AND Password = '{password}'";
var rows = await _context.UsersInsecure.FromSqlRaw(sql).ToListAsync();
