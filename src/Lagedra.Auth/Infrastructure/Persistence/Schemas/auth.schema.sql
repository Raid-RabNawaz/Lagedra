-- Auth schema — managed by EF Core migrations (this file is for reference only)
-- Run: dotnet ef migrations add InitAuth --project src/Lagedra.Auth --startup-project src/Lagedra.ApiGateway --context AuthDbContext

CREATE SCHEMA IF NOT EXISTS auth;

-- ASP.NET Identity tables live in the "auth" schema (configured via HasDefaultSchema)
-- EF Core migrations generate the actual DDL; this file documents the intended structure.

-- auth.AspNetUsers       -> ApplicationUser (extends IdentityUser<Guid>)
-- auth.AspNetRoles       -> IdentityRole<Guid>
-- auth.AspNetUserRoles   -> pivot
-- auth.AspNetUserClaims
-- auth.AspNetUserLogins
-- auth.AspNetUserTokens
-- auth.AspNetRoleClaims
-- auth.refresh_tokens    -> RefreshToken entity
