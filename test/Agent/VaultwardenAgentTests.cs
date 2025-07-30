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
    public async Task CreateFolderAndItemAsync()
    {
        if (TestServer == null) Assert.Inconclusive();

        using var vaultwarden = await VaultwardenAgent.CreateAsync(TestServer, new(TestUser, TestPass));

        var folderName = $"folder-{DateTime.Now.Ticks:X16}";
        var folder = await vaultwarden.Affect.CreateFolderAsync(folderName);
        folder.Id.Should().NotBeEmpty();
        folder.Name.Should().Be(folderName);

        var loginName = $"login-{DateTime.Now.Ticks:X16}";
        var login = await vaultwarden.Affect.CreateCipherItemLoginAsync(new(loginName, FolderId: folder.Id), new("login-user", "login-pass"));
        login.Id.Should().NotBeEmpty();

        var notesName = $"notes-{DateTime.Now.Ticks:X16}";
        var notes = await vaultwarden.Affect.CreateCipherItemNotesAsync(new(notesName, FolderId: folder.Id, Notes: "note-text"));
        notes.Id.Should().NotBeEmpty();

    }

    [TestMethod()]
    public async Task CreateOrganizationAndCollectionAsync()
    {
        if (TestServer == null) Assert.Inconclusive();

        using var vaultwarden = await VaultwardenAgent.CreateAsync(TestServer, new(TestUser, TestPass));

        var orgName = $"org-{DateTime.Now.Ticks:X16}";
        var org = await vaultwarden.Affect.CreateOrganizationAsync(orgName, "DefaultCollection");
        org.Id.Should().NotBeEmpty();
        org.Name.Should().Be(orgName);

        var colName = $"{orgName}-col";
        var col = await vaultwarden.Affect.CreateCollectionAsync(org.Id, colName);
        col.Id.Should().NotBeEmpty();
        col.Name.Should().Be(colName);
    }

    [TestMethod()]
    public async Task GetItemAsync()
    {
        if (TestServer == null) Assert.Inconclusive();

        using var vaultwarden = await VaultwardenAgent.CreateAsync(TestServer, new(TestUser, TestPass));
        var items = await vaultwarden.GetItemsAsync();
    }

    [TestMethod()]
    public async Task InviteUserAsync()
    {
        if (TestServer == null) Assert.Inconclusive();

        using var vaultwarden = await VaultwardenAgent.CreateAsync(TestServer, new(TestUser, TestPass));

        var id = $"{DateTime.Now.Ticks:X16}";

        var orgName = $"TestOrg-{id}";
        var org = await vaultwarden.Affect.CreateOrganizationAsync(orgName, "default-collention");

        var userMail = $"user-{id}@myserver.home";
        var userPass = $"user-{id}-pass";
        await vaultwarden.Connector.Account.RegisterUserNoSmtpAsync(new(userMail, userPass));

        var inviteArgs = new InviteOrgMemberArgs(InviteMembershipType.User, emails: [userMail], groups: []);
        await vaultwarden.Connector.Organization.InviteUserAsync(vaultwarden.Token, org.Id, inviteArgs);
    }

    [TestMethod()]
    public async Task ConfirmMemberAsync()
    {
        if (TestServer == null) Assert.Inconclusive();

        using var vaultwarden = await VaultwardenAgent.CreateAsync(TestServer, new(TestUser, TestPass));

        var orgName = $"TestOrg";
        var org = vaultwarden.Profile.organizations.FirstOrDefault(o => o.name == orgName) ?? throw new Exception("Org not found");

        var members = await vaultwarden.Connector.Organization.GetMembersAsync(vaultwarden.Token, org.id);
        foreach (var member in members.data)
        {
            if (member.status != MembershipStatus.Accepted) continue;
            await vaultwarden.Affect.ConfirmMemberAsync(org.id, new(member.id, member.userId));
        }
    }
}
