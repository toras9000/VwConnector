using System.Diagnostics.CodeAnalysis;
using VwConnector.Agent;

namespace VwConnector.Tests.Agent;

[TestClass]
public class VaultwardenAgentTests
{
    public static Uri? TestServer;
    public const string ServiceUrl = "http://localhost:8190";
    public const string TestAdminPass = "admin-pass";
    public const string TestUser = "tester@myserver.home";
    public const string TestPass = "tester-password";

    [ClassInitialize]
    public static void ClassInitialize(TestContext testContext)
    {
        try
        {
            using var http = new HttpClient();
            Task.Run(async () => await http.GetAsync(ServiceUrl)).Wait();
            TestServer = new(ServiceUrl);
        }
        catch { }
    }

    [TestMethod()]
    public async Task GetItemAsync()
    {
        if (TestServer == null) Assert.Inconclusive();

        using var vaultwarden = await VaultwardenAgent.CreateAsync(TestServer, new(TestUser, TestPass));
        var items = await vaultwarden.GetItemsAsync();
        

    }

}
