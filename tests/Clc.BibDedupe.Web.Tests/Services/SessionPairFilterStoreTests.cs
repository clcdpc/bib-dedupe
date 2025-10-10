using System.Text.Json;
using System.Threading.Tasks;
using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Services;
using Clc.BibDedupe.Web.Tests.TestUtilities;

namespace Clc.BibDedupe.Web.Tests.Services;

[TestClass]
public class SessionPairFilterStoreTests
{
    private const string UserId = "user@example.com";
    private const string SessionKey = "PairFilters:" + UserId;

    [TestMethod]
    public async Task Getting_Filters_With_A_Missing_User_Id_Returns_Null()
    {
        var store = new SessionPairFilterStore(TestHttpContextAccessor.WithSession(new TestSession()));

        var result = await store.GetAsync(string.Empty);

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task Getting_Filters_From_Session_Returns_Normalized_Options()
    {
        var session = new TestSession();
        var stored = new PairFilterOptions
        {
            TomId = 123,
            MatchType = "  exact  ",
            HasHolds = true
        };
        session.SetString(SessionKey, JsonSerializer.Serialize(stored));

        var store = new SessionPairFilterStore(TestHttpContextAccessor.WithSession(session));

        var result = await store.GetAsync(UserId);

        result.Should().NotBeNull();
        result!.TomId.Should().Be(123);
        result.MatchType.Should().Be("exact");
        result.HasHolds.Should().BeTrue();
    }

    [TestMethod]
    public async Task Getting_Filters_With_Invalid_Json_Removes_The_Session_Key()
    {
        var session = new TestSession();
        session.SetString(SessionKey, "{ invalid");
        var store = new SessionPairFilterStore(TestHttpContextAccessor.WithSession(session));

        var result = await store.GetAsync(UserId);

        result.Should().BeNull();
        session.GetString(SessionKey).Should().BeNull();
    }

    [TestMethod]
    public async Task Setting_Empty_Filters_Removes_The_Session_Entry()
    {
        var session = new TestSession();
        session.SetString(SessionKey, "{\"TomId\":1}");
        var store = new SessionPairFilterStore(TestHttpContextAccessor.WithSession(session));

        await store.SetAsync(UserId, new PairFilterOptions());

        session.GetString(SessionKey).Should().BeNull();
    }

    [TestMethod]
    public async Task Setting_Filters_Serializes_Normalized_Values()
    {
        var session = new TestSession();
        var store = new SessionPairFilterStore(TestHttpContextAccessor.WithSession(session));
        var filters = new PairFilterOptions
        {
            TomId = 12,
            MatchType = "  fuzzy  ",
            HasHolds = false
        };

        await store.SetAsync(UserId, filters);

        var serialized = session.GetString(SessionKey);
        serialized.Should().NotBeNull();

        var roundTrip = JsonSerializer.Deserialize<PairFilterOptions>(serialized!);
        roundTrip.Should().NotBeNull();
        roundTrip!.TomId.Should().Be(12);
        roundTrip.MatchType.Should().Be("fuzzy");
        roundTrip.HasHolds.Should().BeFalse();
    }
}
