using Clc.BibDedupe.Web;
using Clc.BibDedupe.Web.Tests.TestUtilities;

namespace Clc.BibDedupe.Web.Tests.Extensions;

[TestClass]
public class SessionExtensionsTests
{
    [TestMethod]
    public void Taking_An_Auth_Message_Returns_Value_And_Clears_The_Session()
    {
        var session = new TestSession();

        session.SetAuthMessage("Hello", "Alice");

        var result = session.TakeAuthMessage();

        result.Should().NotBeNull();
        result.Value.Message.Should().Be("Hello");
        result.Value.UserName.Should().Be("Alice");
        session.GetString("AuthMessage").Should().BeNull();
        session.GetString("AuthUserName").Should().BeNull();
    }

    [TestMethod]
    public void Taking_An_Auth_Message_When_None_Exists_Returns_Null()
    {
        var session = new TestSession();
        session.SetString("AuthUserName", "Bob");
        session.GetString("AuthMessage").Should().BeNull();

        var result = session.TakeAuthMessage();

        result.Should().BeNull();
        session.GetString("AuthUserName").Should().BeNull();
    }

    [TestMethod]
    public void Setting_An_Empty_User_Name_Removes_The_Session_Value()
    {
        var session = new TestSession();
        session.SetString("AuthUserName", "Bob");

        session.SetAuthMessage("Hello", null);

        session.GetString("AuthUserName").Should().BeNull();
    }
}
