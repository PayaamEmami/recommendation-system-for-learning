# Security Features

## Password Hashing

**Implementation:** Microsoft's `PasswordHasher<User>` from `Microsoft.AspNetCore.Identity`

- Passwords are hashed using **PBKDF2** with HMAC-SHA256
- Automatic salt generation (unique per password)
- Configurable iteration count (default: 10,000+)
- Hashes stored in `User.PasswordHash` field (max 500 chars)

**Usage:**
- Registration: Password hashed before saving to database
- Login: Entered password verified against stored hash
- No plain text passwords ever stored or logged

## Rate Limiting

**Implementation:** .NET's built-in rate limiting middleware (`.NET 7+`)

**Policies:**
- **Global:** 100 requests/minute per IP
- **Auth endpoints:** 10 requests/minute per IP (login, register, refresh)
- **API endpoints:** 60 requests/minute per IP (users, etc.)

**Behavior:**
- Returns `429 Too Many Requests` when limit exceeded
- Includes `Retry-After` header
- Per-IP address tracking
- Fixed window algorithm (resets every minute)

**Applied to:**
- `AuthController`: `[EnableRateLimiting("auth")]`
- `UsersController`: `[EnableRateLimiting("api")]`

## Database Security

**Migrations:**
- Initial migration includes `PasswordHash` field
- Run: `dotnet ef database update --startup-project ../Rsl.Api`

## Configuration

Secrets should never be committed. See README for configuration options.

