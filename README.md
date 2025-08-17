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
- [Demo Scenarios](#demo-scenarios)
- [Vulnerable vs Secure Endpoints](#vulnerable-vs-secure-endpoints)
- [OWASP Mapping](#owasp-mapping)
- [Demo Flow](#demo-flow)
- [Presentation Narrative](#presentation-narrative)

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

## Architecture

- **Program/Startup** → auth cookies, rate limiting, DI  
  [Program.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Program.cs)

- **Data** → EF Core DbContext, relationships  
  [AppDbContext.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Data/AppDbContext.cs)

---

## Entities

- **User** — Minimal user (Email, Password, Role).  
  [User.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Models/User.cs)

- **Ticket** — Title, Description, Status; `CreatedByUser`, `AssignedToUser`.  
  [Ticket.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Models/Ticket.cs)

- **Comment** — Ticket comments; author + timestamp.  
  [Comment.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Models/Comment.cs)

---

## Controllers & Endpoints

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

## Services

- [RecaptchaService.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Services/RecaptchaService.cs) → Validates Google reCAPTCHA tokens  
- [AccountCreationService.cs](https://github.com/AhmetAkyil/TicketingApp/blob/main/TicketingSystem/TicketSystem/Services/AccountCreationService.cs) → Generates demo users  

---

## Demo Scenarios

1. **SQL Injection Attack**  
   - Insecure endpoint: `/auth/login-insecure`  
   - Demo: Postman payload `' OR 1=1 --`  
   - Result: Unauthorized access granted.  
   - Secure endpoint: `/auth/login` → EF LINQ blocks the attack.

2. **Brute Force Attack**  
   - Insecure endpoint: `/auth/login-open` (no CAPTCHA, no rate limit).  
   - Demo: Python brute force script.  
   - Result: Unlimited login attempts succeed eventually.  
   - Secure endpoint: `/auth/login` → reCAPTCHA + rate limiting stop the attack.  

---

## 🆚 Vulnerable vs Secure Endpoints

| Endpoint               | Vulnerability     | Demo Tool           | Secure Counterpart |
|------------------------|------------------|---------------------|--------------------|
| `/auth/login-insecure` | SQL Injection    | Postman (payload `' OR 1=1 --`) | `/auth/login` |
| `/auth/login-open`     | Brute Force      | Python script       | `/auth/login` |

---

## 🛡 OWASP Mapping

- **A03: SQL Injection** → `/auth/login-insecure`  
- **A07: Identification & Authentication Failures** → `/auth/login-open`  

**Mitigations:**  
- EF Core parameterized queries (LINQ)  
- reCAPTCHA integration  
- Rate limiting  
- `[ValidateAntiForgeryToken]` CSRF protection  
- Role-based + ownership-based authorization  

---

## Demo Flow (SQL Injection)

```mermaid
sequenceDiagram
    participant Attacker
    participant App
    participant DB

    Attacker->>App: POST /auth/login-insecure ("' OR 1=1 --")
    App->>DB: Raw SQL
    DB-->>App: Returns all users
    App-->>Attacker: Access Granted (vulnerable)

    Attacker->>App: POST /auth/login (same payload)
    App->>DB: EF LINQ with parameters
    DB-->>App: No match
    App-->>Attacker: Access Denied (secure)
