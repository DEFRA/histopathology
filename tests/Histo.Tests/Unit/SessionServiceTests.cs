using Histo.Web.Pages;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;

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
