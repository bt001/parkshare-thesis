using System.Data.Common;
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Application.Queries;
using ParkShare.Application.Commands;
using ParkShare.Application.Services;
using ParkShare.Common.Enums;
using ParkShare.Domain.Entities;
using ParkShare.Infrastructure.Configuration;
using ParkShare.Infrastructure.Persistence;
using ParkShare.Infrastructure.Repositories;
using ParkShare.Infrastructure.Services;

/// <summary>Credential conversion, authentication, isolation and safe failure regressions.</summary>
internal static partial class Program
{
    /// <summary>Creates authentication with real repositories and test-only token settings.</summary>
    private static AuthenticationService Authentication(ParkShareDbContext db, IRepositoryTransactionCoordinator? transactions = null)
        => new(new UserRepository(db), new UserRoleRepository(db), transactions ?? new RepositoryTransactionCoordinator(db),
            new JwtTokenService(Options.Create(new JwtConfig { Issuer = "tests", Audience = "tests", SigningKey = new string('t', 64) })),
            new PasswordService(), NullLogger<AuthenticationService>.Instance);

    /// <summary>Creates the same credential initializer used by API startup.</summary>
    private static PasswordMigrationInitializer Credentials(ParkShareDbContext db)
        => new(db, new PasswordService(), NullLogger<PasswordMigrationInitializer>.Instance);

    /// <summary>Checks legacy conversion, rollback, repeat startup and simultaneous startup.</summary>
    private static async Task CredentialConversion()
    {
        await using (var failing = Context(new FailCredentialWrite()))
        {
            try { await Credentials(failing).InitializeAsync(Ct); throw new Exception("Expected conversion failure"); }
            catch (InvalidOperationException ex) { Check(!ex.Message.Contains("private-fixture"), "conversion error is safe"); }
        }
        await using (var check = Context())
            Check(await check.Users.AllAsync(u => !u.IsPasswordHashed && u.PasswordHash == "fixture"), "failed conversion rolls back credentials and markers");
        await using (var first = Context())
        await using (var second = Context())
            await Task.WhenAll(Credentials(first).InitializeAsync(Ct), Credentials(second).InitializeAsync(Ct));
        await using var db = Context();
        var users = await db.Users.AsNoTracking().ToListAsync();
        var passwords = new PasswordService();
        Check(users.All(u => u.IsPasswordHashed && passwords.Verify(u, "fixture", out _)), "all legacy credentials preserved");
        Check(users.Select(u => u.PasswordHash).Distinct().Count() == users.Count, "equal passwords have distinct salts");
        var legacy = users.Single(u => u.UserName == "legacy");
        Check(await db.Listings.AnyAsync(l => l.OwnerId == legacy.Id) && await db.Notifications.AnyAsync(n => n.UserId == legacy.Id), "related legacy data retained");
        Check((await Authentication(db).AuthenticateUser(legacy.Email, "fixture", Ct)).IsSuccessful, "converted account signs in");
        await Credentials(db).InitializeAsync(Ct);
        Check((await db.Users.AsNoTracking().ToListAsync()).All(u => users.Single(v => v.Id == u.Id).PasswordHash == u.PasswordHash), "repeat startup does not rehash");
    }

    /// <summary>Checks registration, login, malformed credentials and hash upgrades.</summary>
    private static async Task AuthenticationWorkflow()
    {
        await using var db = Context();
        var auth = Authentication(db);
        var registered = await auth.RegisterUser("auth-new", "Auth", "Test", "auth-new@test.invalid", "Password-123", Ct);
        Check(registered.IsSuccessful && registered.Payload.Roles.Contains("Renter"), "registration returns renter session");
        var user = await db.Users.SingleAsync(u => u.Id == registered.Payload.UserId);
        Check(user.IsPasswordHashed && user.PasswordHash != "Password-123", "registration hashes credentials");
        Check((await auth.AuthenticateUser(user.Email, "Password-123", Ct)).IsSuccessful, "valid login");
        Check(!(await auth.AuthenticateUser(user.Email, "wrong", Ct)).IsSuccessful, "wrong password rejected");
        Check(!(await auth.RegisterUser("auth-new", "Auth", "Test", user.Email, "Password-123", Ct)).IsSuccessful, "duplicate rejected");
        user.IsActive = false; await db.SaveChangesAsync();
        Check(!(await auth.AuthenticateUser(user.Email, "Password-123", Ct)).IsSuccessful, "inactive login rejected");
        Check((await auth.RefreshTokens(registered.Payload.RefreshToken, Ct)).ErrorsSummary == "Invalid refresh token.", "inactive refresh rejected with stable contract");
        user.IsActive = true;
        foreach (var malformed in new[] { "not-base64", "", "AQ==", "AAAA" })
        {
            user.PasswordHash = malformed; await db.SaveChangesAsync();
            Check((await auth.AuthenticateUser(user.Email, "Password-123", Ct)).ErrorsSummary == "Invalid email or password.", "malformed hash rejected safely");
        }
        user.IsPasswordHashed = false; user.PasswordHash = "Password-123"; await db.SaveChangesAsync();
        Check(!(await auth.AuthenticateUser(user.Email, "Password-123", Ct)).IsSuccessful, "no legacy plaintext login fallback");
        user.IsPasswordHashed = true;
        user.PasswordHash = new PasswordHasher<User>(Options.Create(new PasswordHasherOptions { IterationCount = 1000 })).HashPassword(user, "Password-123");
        await db.SaveChangesAsync();
        var old = user.PasswordHash;
        Check((await auth.AuthenticateUser(user.Email, "Password-123", Ct)).IsSuccessful, "old hash accepted");
        await db.Entry(user).ReloadAsync();
        Check(user.PasswordHash != old && new PasswordService().Verify(user, "Password-123", out var needsRehash) && !needsRehash, "rehash persisted");
        Check((await auth.RefreshTokens("invalid", Ct)).ErrorsSummary == "Invalid refresh token.", "invalid refresh contract preserved");
    }

    /// <summary>Checks safe operational errors and rollback when registration is interrupted.</summary>
    private static async Task AuthenticationFailures()
    {
        foreach (var stage in new[] { "precheck", "role" })
        {
            await using var db = Context(new FailAuthenticationCommand(stage));
            var result = await Authentication(db).RegisterUser("failed-" + stage, "Test", "Failure", "failed-" + stage + "@test.invalid", "Password-123", Ct);
            Check(!result.IsSuccessful && result.ErrorsSummary == "Registration could not be completed. Please try again.", "safe " + stage + " failure");
        }
        await using (var db = Context())
        {
            var result = await Authentication(db, new FailedTransactionStart()).RegisterUser("failed-begin", "Test", "Begin", "failed-begin@test.invalid", "Password-123", Ct);
            Check(result.ErrorsSummary == "Registration could not be completed. Please try again.", "transaction start failure is safe");
        }
        await using (var db = Context(new FailAuthenticationCommand("precheck")))
        {
            Check((await Authentication(db).AuthenticateUser("owner@test.invalid", "fixture", Ct)).ErrorsSummary == "Sign in could not be completed. Please try again.", "login lookup failure is safe");
        }
        using var cancelled = new CancellationTokenSource();
        await using (var db = Context(new FailAuthenticationCommand("role", cancelled)))
        {
            try { await Authentication(db).RegisterUser("failed-cancel", "Test", "Cancel", "failed-cancel@test.invalid", "Password-123", cancelled.Token); throw new Exception("Expected cancellation"); }
            catch (OperationCanceledException) { Check(cancelled.IsCancellationRequested, "caller cancellation propagated"); }
        }
        await using var check = Context();
        Check(!await check.Users.AnyAsync(u => u.UserName.StartsWith("failed-")), "failed registration leaves no account or roles");
    }

    /// <summary>Checks self-only lookup without revealing whether another email exists.</summary>
    private static async Task UserLookupIsolation()
    {
        await using var db = Context();
        var user = await db.Users.SingleAsync(u => u.Id == _owner);
        var service = new UserService(new UserRepository(db), new TestCurrentUser(_owner), NullLogger<UserService>.Instance);
        Check((await service.GetUserByEmail(user.Email.ToUpperInvariant(), Ct)).Payload.Id == _owner, "self lookup ignores email case");
        var denied = await service.GetUserByEmail("renter@test.invalid", Ct);
        Check(!denied.IsSuccessful && denied.ErrorsSummary == (await service.GetUserByEmail("missing@test.invalid", Ct)).ErrorsSummary, "other and nonexistent accounts indistinguishable");
        var anonymous = new UserService(new UserRepository(db), new TestCurrentUser(null), NullLogger<UserService>.Instance);
        Check(!(await anonymous.GetUserByEmail(user.Email, Ct)).IsSuccessful, "missing identity rejected");
        user.IsActive = false; await db.SaveChangesAsync();
        Check(!(await service.GetUserByEmail(user.Email, Ct)).IsSuccessful, "inactive self rejected");
        user.IsActive = true; await db.SaveChangesAsync();
    }

    /// <summary>Checks authentication handler forwarding retains conflict classification.</summary>
    private static async Task AuthenticationHandlerErrors()
    {
        var service = new ConflictingAuthentication();
        Check((await new LoginHandler(service).Handle(new(), Ct)).IsConflict, "login conflict retained");
        Check((await new RefreshTokenHandler(service).Handle(new(), Ct)).IsConflict, "refresh conflict retained");
        Check((await new RegisterHandler(service).Handle(new(), Ct)).IsConflict, "registration conflict retained");
        var bookings = DispatchProxy.Create<IBookingService, ConflictServiceProxy>();
        var availability = DispatchProxy.Create<IAvailabilityService, ConflictServiceProxy>();
        var listings = DispatchProxy.Create<IListingService, ConflictServiceProxy>();
        var reads = DispatchProxy.Create<IListingReadService, ConflictServiceProxy>();
        var users = DispatchProxy.Create<IUserService, ConflictServiceProxy>();
        Check((await new GetBookingByIdHandler(bookings).Handle(new(), Ct)).IsConflict, "booking read conflict retained");
        Check((await new GetRenterBookingsHandler(bookings).Handle(new(), Ct)).IsConflict, "renter history conflict retained");
        Check((await new GetOwnerListingsHandler(listings).Handle(new(), Ct)).IsConflict, "owner listing conflict retained");
        Check((await new GetListingAvailabilityHandler(availability).Handle(new(), Ct)).IsConflict, "listing availability conflict retained");
        Check((await new GetUserByEmailHandler(users).Handle(new(), Ct)).IsConflict, "user lookup conflict retained");
        foreach (var listingId in new long?[] { null, 1 })
        {
            Check((await new GetOwnerBookingsHandler(bookings, reads).Handle(new() { ListingId = listingId }, Ct)).IsConflict, "owner bookings conflict retained");
            Check((await new GetOwnerAvailabilityHandler(availability, reads).Handle(new() { ListingId = listingId }, Ct)).IsConflict, "owner availability conflict retained");
        }
    }

    /// <summary>Supplies an isolated authenticated identity.</summary>
    private sealed class TestCurrentUser(long? id) : ICurrentUserService
    {
        /// <inheritdoc/>
        public long? UserId => id;
        /// <inheritdoc/>
        public string? Email => null;
        /// <inheritdoc/>
        public Roles Capabilities => Roles.Renter;
        /// <inheritdoc/>
        public IReadOnlyCollection<string> RoleNames => ["Renter"];
        /// <inheritdoc/>
        public bool IsAuthenticated => id.HasValue;
    }

    /// <summary>Fails conversion after SQL updates, before the transaction commits.</summary>
    private sealed class FailCredentialWrite : SaveChangesInterceptor
    {
        /// <inheritdoc/>
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("private-fixture");
    }

    /// <summary>Fails before a registration transaction can be created.</summary>
    private sealed class FailedTransactionStart : IRepositoryTransactionCoordinator
    {
        /// <inheritdoc/>
        public Task<IRepositoryTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
            => throw new InvalidOperationException("private-fixture");
    }

    /// <summary>Injects registration lookup or role-write failures and caller cancellation.</summary>
    private sealed class FailAuthenticationCommand(string stage, CancellationTokenSource? cancellation = null) : DbCommandInterceptor
    {
        /// <inheritdoc/>
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (stage == "precheck" || (command.CommandText.Contains("INSERT") && command.CommandText.Contains("UserRoles")))
            {
                if (cancellation != null) { cancellation.Cancel(); cancellation.Token.ThrowIfCancellationRequested(); }
                throw new InvalidOperationException("private-fixture");
            }
            return ValueTask.FromResult(result);
        }
    }

    /// <summary>Returns a classified service failure for handler boundary checks.</summary>
    public class ConflictServiceProxy : DispatchProxy
    {
        /// <inheritdoc/>
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            var resultType = targetMethod!.ReturnType.GetGenericArguments()[0];
            var failure = resultType.GetMethod("Conflict")!.Invoke(null, ["Conflict"]);
            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType).Invoke(null, [failure]);
        }
    }

    /// <summary>Returns classified failures for authentication handler tests.</summary>
    private sealed class ConflictingAuthentication : IAuthenticationService
    {
        /// <inheritdoc/>
        public Task<ServiceResult<LoginResponse>> AuthenticateUser(string email, string password, CancellationToken cancellationToken)
            => Task.FromResult(ServiceResult<LoginResponse>.Conflict("Conflict"));
        /// <inheritdoc/>
        public Task<ServiceResult<LoginResponse>> RefreshTokens(string refreshToken, CancellationToken cancellationToken)
            => AuthenticateUser("", "", cancellationToken);
        /// <inheritdoc/>
        public Task<ServiceResult<LoginResponse>> RegisterUser(string userName, string firstName, string lastName, string email, string password, CancellationToken cancellationToken)
            => AuthenticateUser("", "", cancellationToken);
    }
}
