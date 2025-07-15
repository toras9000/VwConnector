using System.Diagnostics.CodeAnalysis;

namespace VwConnector.Tests;

[TestClass]
public class VaultwardenConnectorTests
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

    private record TestUserInfo(PasswordConnectTokenResult Token, string Mail, string Password);

    [MemberNotNull(nameof(TestServer))]
    private async Task<TestUserInfo> ensureTestUserAsync(CancellationToken cancelToken = default)
    {
        if (TestServer == null) Assert.Inconclusive();

        using var vaultwarden = new VaultwardenConnector(TestServer);
        var userPrelogin = await vaultwarden.Identity.PreloginAsync(new(TestUser), cancelToken);
        var userPassHsah = vaultwarden.Utility.CreatePasswordHash(TestUser, TestPass, userPrelogin);
        var userPassHashB64 = userPassHsah.EncodeBase64();
        var userCredential = new PasswordConnectTokenModel(
            scope: "api offline_access",
            client_id: "web",
            device_type: ClientDeviceType.UnknownBrowser,
            device_name: Environment.MachineName,
            device_identifier: Environment.MachineName,
            username: TestUser,
            password: userPassHashB64
        );
        var userToken = await vaultwarden.Identity.ConnectTokenAsync(userCredential, cancelToken);
        return new(userToken, TestUser, TestPass);
    }

    [TestMethod()]
    public async Task User_Register()
    {
        if (TestServer == null) Assert.Inconclusive();

        var id = $"{DateTime.Now.Ticks:X16}";
        var mail = $"user-{id}@myserver.home";
        var pass = $"user-{id}-pass";

        using var vaultwarden = new VaultwardenConnector(TestServer);
        var validationToken = await vaultwarden.Identity.SendRegisterVerificationMailAsync(new(mail));

        var kdf = new KdfConfig(KdfType.Pbkdf2, 600000);
        var masterKey = vaultwarden.Utility.CreateMasterKey(mail, pass, kdf);
        var serverHsah = vaultwarden.Utility.CreateMasterKeyHash(masterKey, pass, 1);
        var localHsah = vaultwarden.Utility.CreateMasterKeyHash(masterKey, pass, 2);

        var stretchKey = vaultwarden.Utility.CreateStretchKey(masterKey);
        var newUserKey = SymmetricCryptoKey.From(vaultwarden.Utility.GenerateKeyData());
        var keyPair = vaultwarden.Utility.GenerateRsaKeyPair();
        var userKeyEnc = vaultwarden.Utility.EncryptAes(stretchKey, newUserKey.ToBytes(), hmac: true);
        var prvKeyEnc = vaultwarden.Utility.EncryptAes(newUserKey, keyPair.PrivateKey, hmac: true);

        var register = new RegisterArgs(
            email: mail,
            userSymmetricKey: userKeyEnc.BuildString(),
            userAsymmetricKeys: new(prvKeyEnc.BuildString(), keyPair.PublicKey.EncodeBase64()),
            masterPasswordHash: serverHsah.EncodeBase64(),
            emailVerificationToken: validationToken,
            kdf: kdf.kdf,
            kdfIterations: kdf.kdfIterations
        );
        var result = await vaultwarden.Identity.RegisterFinishAsync(register);
        result.@object.Should().NotBeNull();
    }

    [TestMethod()]
    public async Task RegisterUserAsync()
    {
        if (TestServer == null) Assert.Inconclusive();

        using var vaultwarden = new VaultwardenConnector(TestServer);

        var id = $"{DateTime.Now.Ticks:X16}";
        var mail = $"user-{id}@myserver.home";
        var pass = $"user-{id}-pass";

        await vaultwarden.Account.RegisterUserNoSmtpAsync(new(mail, pass));
    }

    [TestMethod()]
    public async Task User_CreateFolder()
    {
        var tester = await ensureTestUserAsync();

        using var vaultwarden = new VaultwardenConnector(TestServer);
        var userProfile = await vaultwarden.User.GetProfileAsync(tester.Token);
        var stretchKey = vaultwarden.Utility.CreateStretchKey(tester.Mail, tester.Password, tester.Token.ToKdfConfig());
        var userKey = SymmetricCryptoKey.From(vaultwarden.Utility.Decrypt(stretchKey.EncKey, EncryptedData.Parse(userProfile.key)));

        var folderName = $"folder-{DateTime.Now.Ticks:X16}";
        var folderNameEnc = vaultwarden.Utility.EncryptAes(userKey, folderName.EncodeUtf8(), hmac: true);
        var folder = await vaultwarden.User.CreateFolderAsync(tester.Token, new(folderNameEnc.BuildString()));
        folder.@object.Should().Be("folder");
    }

    [TestMethod()]
    public async Task Org_CreateOrgAndCollection()
    {
        var tester = await ensureTestUserAsync();

        using var vaultwarden = new VaultwardenConnector(TestServer);
        var userProfile = await vaultwarden.User.GetProfileAsync(tester.Token);
        var userPubKey = await vaultwarden.User.GetPublicKeyAsync(tester.Token, userProfile.id);
        var userPubKeyBin = userPubKey.publicKey.AsSpan().DecodeBase64() ?? [];
        var newOrgKey = SymmetricCryptoKey.From(vaultwarden.Utility.GenerateKeyData());
        var encOrgKey = vaultwarden.Utility.EncryptRsa(userPubKeyBin, newOrgKey.ToBytes());

        var newOrgName = $"org-{DateTime.Now.Ticks:X16}";
        var defCollection = $"{newOrgName}-default";
        var defCollectionEnc = vaultwarden.Utility.EncryptAes(newOrgKey, defCollection.EncodeUtf8(), hmac: true);
        var keyPair = vaultwarden.Utility.GenerateRsaKeyPair();
        var prvKeyEnc = vaultwarden.Utility.EncryptAes(newOrgKey, keyPair.PrivateKey, hmac: true);

        var orgArgs = new CreateOrgArgs(
            name: newOrgName,
            collectionName: defCollectionEnc.BuildString(),
            billingEmail: tester.Mail,
            key: encOrgKey.BuildString(),
            keys: [keyPair.PublicKey.EncodeBase64(), prvKeyEnc.BuildString()],
            planType: PlanType.Free
        );
        var orgResult = await vaultwarden.Organization.CreateAsync(tester.Token, orgArgs);
        var orgMembers = await vaultwarden.Organization.GetMembersAsync(tester.Token, orgResult.id);
        var memberId = orgMembers.data.First(m => m.userId == userProfile.id).id;

        var stretchKey = vaultwarden.Utility.CreateStretchKey(tester.Mail, tester.Password, tester.Token.ToKdfConfig());
        var userKey = SymmetricCryptoKey.From(vaultwarden.Utility.Decrypt(stretchKey.EncKey, EncryptedData.Parse(userProfile.key)));
        var userPrivateKey = vaultwarden.Utility.Decrypt(userKey.EncKey, EncryptedData.Parse(userProfile.privateKey));
        var ownerMember = new VwCollectionMembership(memberId, readOnly: false, hidePasswords: false, manage: true);
        for (var i = 0; i < 2; i++)
        {
            var colName = $"{newOrgName}-col-{i}";
            var colNameEnc = vaultwarden.Utility.EncryptAes(newOrgKey, colName.EncodeUtf8(), hmac: true);
            var collection = await vaultwarden.Organization.CreateCollectionAsync(tester.Token, orgResult.id, new(name: colNameEnc.BuildString(), users: [ownerMember], groups: []));
        }
    }

    [TestMethod()]
    public async Task Cipher_CreateAndGetItem()
    {
        var tester = await ensureTestUserAsync();

        using var vaultwarden = new VaultwardenConnector(TestServer);
        var userProfile = await vaultwarden.User.GetProfileAsync(tester.Token);
        var stretchKey = vaultwarden.Utility.CreateStretchKey(tester.Mail, tester.Password, tester.Token.ToKdfConfig());
        var userKey = SymmetricCryptoKey.From(vaultwarden.Utility.Decrypt(stretchKey.EncKey, EncryptedData.Parse(userProfile.key)));

        var itemName = vaultwarden.Utility.EncryptAes(userKey, $"item-{DateTime.Now.Ticks:X16}".EncodeUtf8(), hmac: true).BuildString();
        var userName = vaultwarden.Utility.EncryptAes(userKey, $"user-{DateTime.Now.Ticks:X16}".EncodeUtf8(), hmac: true).BuildString();
        var itemArgs = new CreateItemArgs(
            cipher: new(
                type: CipherType.Login,
                name: itemName,
                login: new(username: userName)
            ),
           collectionIds: []
        );
        var item = await vaultwarden.Cipher.CreateItemAsync(tester.Token, itemArgs);

        var items = await vaultwarden.Cipher.GetItemsAsync(tester.Token);
        items.data.Should().Contain(i => i.name == itemName);
    }
}
