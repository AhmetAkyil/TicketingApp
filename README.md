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


- **Controllers** → Handle HTTP requests  
- **Models/Entities** → EF Core classes  
- **Data** → AppDbContext (DbSets, relations)  
- **Services** → reCAPTCHA, Account generator, Login throttling  
- **Views** → Razor UI  
- **Program.cs** → DI setup, auth, rate limiting  

Target framework: **.NET 8** + **EF Core 9.x**

---

## 📂 Entities

- **User** → Email, Password, Role (⚠️ stored in plain text for demo)  
- **UserInsecure** → Demo-only table used for SQL Injection  
- **Ticket** → Title, Description, Status, Creator/Assignee relations  
- **Comment** → Ticket comments with author + timestamps  
- **AppDbContext** → EF Core context with DbSets  

---

## ⚙ Controllers & Endpoints

### AuthController
- `GET /auth/login` → login form (injects reCAPTCHA key)  
- `POST /auth/login` → **SECURE** (RateLimiter + reCAPTCHA + EF LINQ)  
- `POST /auth/login-open` → **WEAK** (no CAPTCHA, no rate limit → brute force risk)  
- `POST /auth/login-insecure` → **VULNERABLE** raw SQL → SQL Injection  
- `GET /auth/logout` → clears cookie auth  
- `GET /auth/access-denied` → shown on forbidden access  

### UsersController
- `[Authorize(Roles="Admin")]` class-level  
- List, details, create, edit (all CSRF-protected)  
- `POST /users/create-auto` → demo helper (⚠️ AllowAnonymous + no CSRF → misconfiguration example)  

### TicketsController
- `[Authorize]` class-level  
- Index, Details, Create (with CSRF), Edit, Delete  
- Authorization: only **Creator** or **Admin** can modify  

### CommentsController
- `[Authorize]` class-level  
- Add, Edit, Delete with CSRF + ownership checks  

---

## 🛠 Services

- **RecaptchaService** → Verifies Google reCAPTCHA  
- **AccountCreationService** → Generates demo users  
- **LoginAttemptService** → Tracks failed login attempts (illustration only)  

---

## 🔐 Security Highlights (OWASP)

### A03: Injection (SQLi)

**Vulnerable code:**
```csharp
var sql = $"SELECT * FROM UsersInsecure WHERE Email = '{email}' AND Password = '{password}'";
var rows = await _context.UsersInsecure.FromSqlRaw(sql).ToListAsync();
