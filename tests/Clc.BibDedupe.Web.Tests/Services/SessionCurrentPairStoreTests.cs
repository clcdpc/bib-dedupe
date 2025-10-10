using System.Text.Json;
using System.Threading.Tasks;
using Clc.BibDedupe.Web.Models;
using Clc.BibDedupe.Web.Services;
using Clc.BibDedupe.Web.Tests.TestUtilities;

namespace Clc.BibDedupe.Web.Tests.Services;

[TestClass]
public class SessionCurrentPairStoreTests
{
    private const string UserId = "user";
    private const string SessionKey = "CurrentPair:" + UserId;

    [TestMethod]
    public async Task Getting_A_Current_Pair_When_None_Exists_Returns_Null()
    {
        var store = new SessionCurrentPairStore(TestHttpContextAccessor.WithSession(new TestSession()));

        var result = await store.GetAsync(UserId);

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task Getting_A_Stored_Current_Pair_Returns_The_Value()
    {
        var session = new TestSession();
        var expected = new CurrentPair { LeftBibId = 1, RightBibId = 2 };
        session.SetString(SessionKey, JsonSerializer.Serialize(expected));
        var store = new SessionCurrentPairStore(TestHttpContextAccessor.WithSession(session));

        var result = await store.GetAsync(UserId);

        result.Should().NotBeNull();
        result!.LeftBibId.Should().Be(1);
        result.RightBibId.Should().Be(2);
    }

    [TestMethod]
    public async Task Getting_A_Current_Pair_With_Invalid_Json_Removes_The_Entry()
    {
        var session = new TestSession();
        session.SetString(SessionKey, "not json");
        var store = new SessionCurrentPairStore(TestHttpContextAccessor.WithSession(session));

        var result = await store.GetAsync(UserId);

        result.Should().BeNull();
        session.GetString(SessionKey).Should().BeNull();
    }

    [TestMethod]
    public async Task Setting_A_Current_Pair_Persists_It_In_The_Session()
    {
        var session = new TestSession();
        var store = new SessionCurrentPairStore(TestHttpContextAccessor.WithSession(session));
        var pair = new CurrentPair { LeftBibId = 10, RightBibId = 20 };

        await store.SetAsync(UserId, pair);

        var serialized = session.GetString(SessionKey);
        serialized.Should().NotBeNull();
        var roundTrip = JsonSerializer.Deserialize<CurrentPair>(serialized!);
        roundTrip.Should().NotBeNull();
        roundTrip!.LeftBibId.Should().Be(10);
        roundTrip.RightBibId.Should().Be(20);
    }

    [TestMethod]
    public async Task Clearing_A_Current_Pair_Removes_The_Session_Entry()
    {
        var session = new TestSession();
        session.SetString(SessionKey, "{\"LeftBibId\":1}");
        var store = new SessionCurrentPairStore(TestHttpContextAccessor.WithSession(session));

        await store.ClearAsync(UserId);

        session.GetString(SessionKey).Should().BeNull();
    }
}
