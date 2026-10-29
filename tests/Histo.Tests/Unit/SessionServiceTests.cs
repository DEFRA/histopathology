using Histo.Web.Pages;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Histo.Tests.Unit;

public class SessionServiceTests
{
    private static SessionService CreateSut() =>
        new(new HttpContextAccessor { HttpContext = new DefaultHttpContext { Session = new FakeSession() } });

    [Fact]
    public void UseSenderRefValidation_Unset_IsNull()
    {
        Assert.Null(CreateSut().UseSenderRefValidation);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UseSenderRefValidation_RoundTripsTheStoredFlag(bool value)
    {
        var sut = CreateSut();

        sut.UseSenderRefValidation = value;

        Assert.Equal(value, sut.UseSenderRefValidation);
    }

    [Fact]
    public void UseSenderRefValidation_SetToNull_ClearsTheFlag()
    {
        var sut = CreateSut();
        sut.UseSenderRefValidation = true;

        sut.UseSenderRefValidation = null;

        Assert.Null(sut.UseSenderRefValidation);
    }

    [Fact]
    public void SessionErrorModel_OnGet_DoesNothing()
    {
        Assert.Null(Record.Exception(() => new SessionErrorModel().OnGet()));
    }

    [Fact]
    public void PopulateFromClaims_UsesTheTblUserNameRatherThanTheEmailNameClaim()
    {
        var sut = CreateSut();
        var principal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
        [
            new(System.Security.Claims.ClaimTypes.Name, "jane.smith@example.gov.uk"),
            new(Histo.Web.Auth.AppClaimTypes.UserName, "Jane Smith"),
        ], "test"));

        sut.PopulateFromClaims(principal);

        Assert.Equal("Jane Smith", sut.UserName);
    }

    [Fact]
    public void PopulateFromClaims_WithoutTheAppNameClaim_FallsBackToTheNameClaim()
    {
        var sut = CreateSut();
        var principal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
            [new(System.Security.Claims.ClaimTypes.Name, "Dev User")], "test"));

        sut.PopulateFromClaims(principal);

        Assert.Equal("Dev User", sut.UserName);
    }

    [Fact]
    public async Task ClaimsTransformation_IssuesTheTblUserNameAndReplacesAStaleOne()
    {
        var users = new Mock<Histo.Administration.Interfaces.IUserService>();
        users.Setup(u => u.ResolveUserByEmailAsync("jane.smith@example.gov.uk", It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Histo.Administration.Models.User { UserID = 7, Name = "Jane Smith", GroupName = "Maintenance", AreaName = "Neuropath" });
        var transform = new Histo.Web.Auth.HistopathologyClaimsTransformation(
            users.Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<Histo.Web.Auth.HistopathologyClaimsTransformation>.Instance);
        var principal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
        [
            new(System.Security.Claims.ClaimTypes.Email, "jane.smith@example.gov.uk"),
            new(Histo.Web.Auth.AppClaimTypes.UserName, "Old Name"),
        ], "test"));

        var result = await transform.TransformAsync(principal);

        Assert.Equal(["Jane Smith"], result.FindAll(Histo.Web.Auth.AppClaimTypes.UserName).Select(c => c.Value));
    }

    private sealed class FakeSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = [];

        public bool IsAvailable => true;
        public string Id => "test";
        public IEnumerable<string> Keys => _store.Keys;
        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out byte[]? value) => _store.TryGetValue(key, out value);
    }
}
