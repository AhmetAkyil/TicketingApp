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





## 🏗 Architecture

- **Program/Startup** → auth cookies, rate limiting, DI  
  [Program.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Program.cs)

- **Data** → EF Core DbContext, relationships  
  [AppDbContext.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Data/AppDbContext.cs)


---

## 📂 Entities

- **User** — Minimal user (Email, Password, Role). 
  [User.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Models/User.cs)

- **Ticket** — Title, Description, Status; `CreatedByUser`, `AssignedToUser`.  
  [Ticket.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Models/Ticket.cs)

- **Comment** — Ticket comments; author + timestamp.  
  [Comment.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Models/Comment.cs)

---

## ⚙ Controllers & Endpoints

- **[AuthController.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Controllers/AuthController.cs)**
  - `/auth/login` → **secure** login (RateLimiter + reCAPTCHA + EF LINQ)  
  - `/auth/login-open` → **weak demo** (no CAPTCHA, no rate limit → brute-force risk)  
  - `/auth/login-insecure` → **SQL Injection demo** using raw SQL 
  - `/auth/logout`, `/auth/access-denied`

- **[UsersController.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Controllers/UsersController.cs)**
  - Restricted with `[Authorize(Roles="Admin")]`  
  - CRUD actions for users (CSRF-protected)  
  - `create-auto` 

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


---
