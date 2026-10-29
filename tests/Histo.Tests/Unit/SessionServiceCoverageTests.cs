using System.Security.Claims;
using Histo.Administration.Models;
using Histo.Web.Auth;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;

namespace Histo.Tests.Unit;

public class SessionServiceCoverageTests
{
    private sealed class DictSession : ISession
    {
        private readonly Dictionary<string, byte[]> _data = [];
        public bool IsAvailable => true;
        public string Id => "test";
        public IEnumerable<string> Keys => _data.Keys;
        public void Clear() => _data.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _data.Remove(key);
        public void Set(string key, byte[] value) => _data[key] = value;
        public bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out byte[]? value) => _data.TryGetValue(key, out value);
    }

    private static SessionService Sut() =>
        new(new HttpContextAccessor { HttpContext = new DefaultHttpContext { Session = new DictSession() } });

    private static ClaimsPrincipal Principal(params Claim[] claims) => new(new ClaimsIdentity(claims, "test"));

    [Fact]
    public void Constructor_NoHttpContext_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new SessionService(new HttpContextAccessor()));
    }

    [Fact]
    public void FreshSession_ExposesDefaults()
    {
        var sut = Sut();

        Assert.Equal((0, string.Empty, string.Empty, 0, string.Empty, string.Empty, 0),
            (sut.UserID, sut.UserName, sut.GroupName, sut.GroupID, sut.UserEmail, sut.UserArea, sut.UserAreaID));
        Assert.Equal((null, null, null, null), (sut.BatchID, sut.BatchSubmissionID, sut.AnimalID, sut.BlockID));
        Assert.Null(sut.CassettedBatchTypeDraft);
        Assert.Equal(0, sut.BatchType);
        Assert.False(sut.IsViewSubmissionMode);
        Assert.Equal(string.Empty, sut.ReturnPage);
    }

    [Fact]
    public void NullableWorkflowIds_RoundTripAndClearWhenSetToNull()
    {
        var sut = Sut();

        sut.BatchID = 1;
        sut.BatchSubmissionID = 2;
        sut.AnimalID = 3;
        sut.BlockID = 4;
        sut.CassettedBatchTypeDraft = 5;
        Assert.Equal((1, 2, 3, 4, 5), (sut.BatchID, sut.BatchSubmissionID, sut.AnimalID, sut.BlockID, sut.CassettedBatchTypeDraft));

        sut.BatchID = null;
        sut.BatchSubmissionID = null;
        sut.AnimalID = null;
        sut.BlockID = null;
        sut.CassettedBatchTypeDraft = null;
        Assert.Equal((null, null, null, null, null), (sut.BatchID, sut.BatchSubmissionID, sut.AnimalID, sut.BlockID, sut.CassettedBatchTypeDraft));
    }

    [Fact]
    public void BatchType_RoundTrips()
    {
        var sut = Sut();

        sut.BatchType = 3;

        Assert.Equal(3, sut.BatchType);
    }

    [Fact]
    public void ReturnPages_RoundTripAndNullBecomesEmpty()
    {
        var sut = Sut();

        sut.ReturnPage = "/A";
        sut.ReturnPageQuery = "?x=1";
        sut.SampleSummaryReturnPage = "/B";
        sut.SampleDetailReturnPage = "/C";
        sut.EditBatchReturnPage = "/D";
        Assert.Equal(("/A", "?x=1", "/B", "/C", "/D"),
            (sut.ReturnPage, sut.ReturnPageQuery, sut.SampleSummaryReturnPage, sut.SampleDetailReturnPage, sut.EditBatchReturnPage));

        sut.ReturnPageQuery = null;
        sut.SampleSummaryReturnPage = null;
        sut.SampleDetailReturnPage = null;
        sut.EditBatchReturnPage = null;
        Assert.Equal((string.Empty, string.Empty, string.Empty, string.Empty),
            (sut.ReturnPageQuery, sut.SampleSummaryReturnPage, sut.SampleDetailReturnPage, sut.EditBatchReturnPage));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsViewSubmissionMode_RoundTrips(bool value)
    {
        var sut = Sut();

        sut.IsViewSubmissionMode = value;

        Assert.Equal(value, sut.IsViewSubmissionMode);
    }

    [Theory]
    [InlineData("Customer", true, false, false)]
    [InlineData("Histopathology User", false, true, false)]
    [InlineData("Maintenance", false, false, true)]
    [InlineData("Other", false, false, false)]
    public void RoleHelpers_ReflectTheGroupName(string group, bool customer, bool histo, bool maintenance)
    {
        var sut = Sut();
        sut.PopulateFromUser(new User { GroupName = group });

        Assert.Equal((customer, histo, maintenance), (sut.IsCustomer, sut.IsHistoUser, sut.IsMaintenance));
    }

    [Fact]
    public void Populate_StoresIdentityName_OrEmptyWhenNoIdentityName()
    {
        var sut = Sut();

        sut.Populate(Principal(new Claim(ClaimTypes.Name, "Jane")));
        Assert.Equal("Jane", sut.UserName);

        sut.Populate(new ClaimsPrincipal());
        Assert.Equal(string.Empty, sut.UserName);
    }

    [Fact]
    public void PopulateFromUser_WritesEveryIdentityField()
    {
        var sut = Sut();

        sut.PopulateFromUser(new User
        {
            UserID = 9, Name = "Jane", GroupCode = 2, GroupName = "Maintenance", Email = "j@d.c", AreaCode = 4, AreaName = "Weybridge",
        });

        Assert.Equal((9, "Jane", 2, "Maintenance", "j@d.c", 4, "Weybridge"),
            (sut.UserID, sut.UserName, sut.GroupID, sut.GroupName, sut.UserEmail, sut.UserAreaID, sut.UserArea));
    }

    [Fact]
    public void PopulateFromClaims_MapsAppClaimsAndParsesNumericIds()
    {
        var sut = Sut();

        sut.PopulateFromClaims(Principal(
            new Claim(AppClaimTypes.UserName, "Jane"),
            new Claim(AppClaimTypes.GroupName, "Customer"),
            new Claim(ClaimTypes.Email, "j@d.c"),
            new Claim(AppClaimTypes.UserArea, "Weybridge"),
            new Claim(AppClaimTypes.UserDbId, "7"),
            new Claim(AppClaimTypes.GroupId, "3"),
            new Claim(AppClaimTypes.UserAreaId, "5")));

        Assert.Equal(("Jane", "Customer", "j@d.c", "Weybridge", 7, 3, 5),
            (sut.UserName, sut.GroupName, sut.UserEmail, sut.UserArea, sut.UserID, sut.GroupID, sut.UserAreaID));
    }

    [Fact]
    public void PopulateFromClaims_FallsBackToLegacyEmailClaimAndIgnoresNonNumericIds()
    {
        var sut = Sut();

        sut.PopulateFromClaims(Principal(
            new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", "legacy@d.c"),
            new Claim(AppClaimTypes.UserDbId, "abc"),
            new Claim(AppClaimTypes.GroupId, ""),
            new Claim(AppClaimTypes.UserAreaId, "x")));

        Assert.Equal(("legacy@d.c", 0, 0, 0), (sut.UserEmail, sut.UserID, sut.GroupID, sut.UserAreaID));
    }

    [Fact]
    public void PopulateFromClaims_NoClaims_LeavesEverythingBlank()
    {
        var sut = Sut();

        sut.PopulateFromClaims(new ClaimsPrincipal());

        Assert.Equal((string.Empty, string.Empty, string.Empty, string.Empty), (sut.UserName, sut.GroupName, sut.UserEmail, sut.UserArea));
    }
}
