using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace VwConnector.Agent;

public record ClientInfo(string Name, string Identifier);
public record UserContext(string Mail, string Password, ClientInfo? Client = default);

public record DecryptedCipherItemLogin(string? Username = default, string? Password = default, string? Totp = default);
public record DecryptedCipherItemCard(string? Brand = default, string? CardholderName = default, string? Number = default, string? ExpYear = default, string? ExpMonth = default, string? Code = default);
public record DecryptedCipherItemSshKey(string? Fingerprint = default, string? PrivateKey = default, string? PublicKey = default);
public record DecryptedCipherItem(
    string Id, CipherType Type, bool Deleted, string Name,
    string? OrgName = default, string? FolderName = default,
    string? Memo = default,
    DecryptedCipherItemLogin? Login = default,
    DecryptedCipherItemCard? Card = default,
    DecryptedCipherItemSshKey? SshKey = default
);

public record DecryptedCollection(string Id, string OrgId, string Name, string[] ExternalId);

public record AgentCreateCipherArgs(string Name, string? FolderId = default, string? OrgId = default, string? Notes = default);
public record AgentCreatedCipher(string Id);
public record AgentCreateLoginArgs(string? Username = default, string? Password = default, string? Totp = default, string? Uri = default);

public record AgentCreatedOrg(string Id, string Name, string BillingEmail);
public record AgentCreatedFolder(string Id, string Name);
public record AgentCreatedCollection(string Id, string Name, string OrgId);

public record AgentConfirmMemberArgs(string MemberId, string UserId);

public interface IAgentAffectOperators
{
    ValueTask<AgentCreatedCipher> CreateCipherItemLoginAsync(AgentCreateCipherArgs cipher, AgentCreateLoginArgs login, string[]? collectionIds = default, CancellationToken cancelToken = default);
    ValueTask<AgentCreatedCipher> CreateCipherItemNotesAsync(AgentCreateCipherArgs cipher, string[]? collectionIds = default, CancellationToken cancelToken = default);
    ValueTask<AgentCreatedFolder> CreateFolderAsync(string name, CancellationToken cancelToken = default);
    ValueTask<AgentCreatedOrg> CreateOrganizationAsync(string name, string defaultCollection, CancellationToken cancelToken = default);
    ValueTask<AgentCreatedCollection> CreateCollectionAsync(string orgId, string name, CancellationToken cancelToken = default);
    ValueTask ConfirmMemberAsync(string orgId, AgentConfirmMemberArgs member, CancellationToken cancelToken = default);
}

public class VaultwardenAgent : IDisposable
{
    public static async ValueTask<VaultwardenAgent> CreateAsync(Uri service, UserContext user, CancellationToken cancelToken = default)
    {
        var connector = default(VaultwardenConnector);
        try
        {
            connector = new VaultwardenConnector(service);
            var connection = await createConnectionAsync(connector, user, cancelToken);
            return new(connector, own: true, user, connection);
        }
        catch
        {
            connector?.Dispose();
            throw;
        }
    }

    public static async ValueTask<VaultwardenAgent> CreateAsync(VaultwardenConnector connector, UserContext user, CancellationToken cancelToken = default)
    {
        var connection = await createConnectionAsync(connector, user, cancelToken);
        return new(connector, own: false, user, connection);
    }

    public IVwConnector Connector => this.connector;

    public IAgentAffectOperators Affect { get; }

    public DecryptedCipherItem? DecryptItem(CipherItem item)
        => dcryptChiperItem(item);

    public async ValueTask<DecryptedCipherItem[]> GetItemsAsync(CancellationToken cancelToken = default)
    {
        var items = await this.connector.Cipher.GetItemsAsync(this.session.Token, cancelToken);
        var decrypted = items.data.Select(i => dcryptChiperItem(i)).Where(i => i != null).Select(i => i!).ToArray();
        return decrypted;
    }

    public async ValueTask<DecryptedCipherItem?> GetItemAsync(string id, CancellationToken cancelToken = default)
    {
        var item = default(CipherItem);
        try { item = await this.connector.Cipher.GetItemAsync(this.session.Token, id, cancelToken); } catch { }

        var decrypted = dcryptChiperItem(item);
        return decrypted;
    }

    public async ValueTask<DecryptedCipherItem?> FindItemAsync(CipherType type, string name, CancellationToken cancelToken = default)
    {
        var items = await this.connector.Cipher.GetItemsAsync(this.session.Token, cancelToken);
        foreach (var item in items.data.Where(i => i.type == type))
        {
            var decryptedName = decryptChiperItemText(item, item.name);
            if (decryptedName != name) continue;
            var decrypted = dcryptChiperItem(item, decryptedName);
            return decrypted;
        }

        return default;
    }

    public async ValueTask<DecryptedCollection[]> GetCollectionsAsync(string orgId, CancellationToken cancelToken = default)
    {
        if (!this.session.Orgs.TryGetValue(orgId, out var orgInfo)) throw new NotSupportedException();
        var collections = await this.connector.Organization.GetCollectionsAsync(this.session.Token, orgId, cancelToken);
        var decrypted = collections.data
            .Select(c => new DecryptedCollection(c.id, c.organizationId, decryptEncText(orgInfo.Key.EncKey, c.name) ?? "", c.externalId))
            .Where(i => i != null).Select(i => i!)
            .ToArray();
        return decrypted;
    }

    public void Dispose()
    {
        if (this.own)
        {
            this.connector?.Dispose();
            this.connector = default!;
            this.session = default!;
        }
    }

    private VaultwardenAgent(VaultwardenConnector connector, bool own, UserContext user, SessionContext session)
    {
        this.connector = connector;
        this.own = own;
        this.user = user;
        this.session = session;
        this.Affect = new AffectOperators(this);
    }

    private class AffectOperators : IAgentAffectOperators
    {
        public AffectOperators(VaultwardenAgent outer) { this.outer = outer; }

        public async ValueTask<AgentCreatedCipher> CreateCipherItemLoginAsync(AgentCreateCipherArgs cipher, AgentCreateLoginArgs login, string[]? collectionIds, CancellationToken cancelToken = default)
        {
            var userKey = this.outer.session.UserKey;
            var itemArgs = new CreateItemArgs(
                cipher: new(
                    type: CipherType.Login,
                    name: encryptText(userKey, cipher.Name).BuildString(),
                    folderId: cipher.FolderId,
                    organizationId: cipher.OrgId,
                    notes: encryptText(userKey, cipher.Notes)?.BuildString(),
                    login: new(
                        username: encryptText(userKey, login.Username)?.BuildString(),
                        password: encryptText(userKey, login.Password)?.BuildString(),
                        totp: encryptText(userKey, login.Totp)?.BuildString(),
                        uri: encryptText(userKey, login.Uri)?.BuildString()
                    )
                ),
               collectionIds: collectionIds ?? []
            );
            var item = await this.outer.connector.Cipher.CreateItemAsync(this.outer.session.Token, itemArgs);
            return new(item.id);
        }

        public async ValueTask<AgentCreatedCipher> CreateCipherItemNotesAsync(AgentCreateCipherArgs cipher, string[]? collectionIds, CancellationToken cancelToken = default)
        {
            var userKey = this.outer.session.UserKey;
            var itemArgs = new CreateItemArgs(
                cipher: new(
                    type: CipherType.SecureNote,
                    name: encryptText(userKey, cipher.Name).BuildString(),
                    folderId: cipher.FolderId,
                    organizationId: cipher.OrgId,
                    notes: encryptText(userKey, cipher.Notes)?.BuildString(),
                    secureNote: new(0)
                ),
               collectionIds: collectionIds ?? []
            );
            var item = await this.outer.connector.Cipher.CreateItemAsync(this.outer.session.Token, itemArgs);
            return new(item.id);
        }

        public async ValueTask<AgentCreatedFolder> CreateFolderAsync(string name, CancellationToken cancelToken = default)
        {
            var encName = encryptText(this.outer.session.UserKey, name);
            var folder = await this.outer.connector.User.CreateFolderAsync(this.outer.session.Token, new(encName.BuildString()), cancelToken);
            this.outer.session.Folders[folder.id] = folder;
            var decName = decryptText(this.outer.session.UserKey.EncKey, folder.name) ?? "";
            return new AgentCreatedFolder(folder.id, decName);
        }

        public async ValueTask<AgentCreatedOrg> CreateOrganizationAsync(string name, string? defaultCollection, CancellationToken cancelToken)
        {
            var newOrgKey = SymmetricCryptoKey.From(this.outer.connector.Utility.GenerateKeyData());
            var encOrgKey = this.outer.connector.Utility.EncryptRsa(this.outer.session.UserPublicKey, newOrgKey.ToBytes());
            var keyPair = this.outer.connector.Utility.GenerateRsaKeyPair();
            var prvKeyEnc = this.outer.connector.Utility.EncryptAes(newOrgKey, keyPair.PrivateKey, hmac: true);
            var defCollectionName = string.IsNullOrEmpty(defaultCollection) ? "DefaultCollection" : defaultCollection;
            var defCollectionEnc = encryptText(newOrgKey, defCollectionName);

            var orgArgs = new CreateOrgArgs(
                name: name,
                collectionName: defCollectionEnc.BuildString(),
                billingEmail: this.outer.session.Profile.email,
                key: encOrgKey.BuildString(),
                keys: [keyPair.PublicKey.EncodeBase64(), prvKeyEnc.BuildString()],
                planType: PlanType.Free
            );
            var orgResult = await this.outer.connector.Organization.CreateAsync(this.outer.session.Token, orgArgs, cancelToken);
            var userProfile = await this.outer.connector.User.GetProfileAsync(this.outer.session.Token, cancelToken);
            this.outer.session.Orgs.Clear();
            foreach (var org in userProfile.organizations)
            {
                var orgKey = SymmetricCryptoKey.From(this.outer.connector.Utility.Decrypt(this.outer.session.UserPrivateKey, EncryptedData.Parse(org.key)));
                this.outer.session.Orgs[org.id] = new OrgInfo(org, orgKey);
            }
            return new AgentCreatedOrg(orgResult.id, orgResult.name, orgResult.billingEmail);
        }

        public async ValueTask<AgentCreatedCollection> CreateCollectionAsync(string orgId, string name, CancellationToken cancelToken = default)
        {
            if (!this.outer.session.Orgs.TryGetValue(orgId, out var org)) throw new ArgumentException();

            var colNameEnc = encryptText(org.Key, name);
            var ownerMember = new VwCollectionMembership(org.Profile.organizationUserId, readOnly: false, hidePasswords: false, manage: true);
            var colArgs = new CreateCollectionArgs(
                name: colNameEnc.BuildString(),
                users: [ownerMember],
                groups: []
            );
            var collection = await this.outer.connector.Organization.CreateCollectionAsync(this.outer.session.Token, orgId, colArgs, cancelToken);
            var decName = decryptText(org.Key.EncKey, collection.name) ?? "";
            return new AgentCreatedCollection(collection.id, decName, collection.organizationId);
        }

        public async ValueTask ConfirmMemberAsync(string orgId, AgentConfirmMemberArgs member, CancellationToken cancelToken = default)
        {
            if (!this.outer.session.Orgs.TryGetValue(orgId, out var org)) throw new ArgumentException();

            var memberPubKey = await this.outer.connector.User.GetPublicKeyAsync(this.outer.session.Token, member.UserId, cancelToken);
            var memberPubKeyBytes = memberPubKey.publicKey.AsSpan().DecodeBase64();
            var confirmKey = this.outer.connector.Utility.EncryptRsa(memberPubKeyBytes!, org.Key.ToBytes());
            await this.outer.connector.Organization.ConfirmMemberAsync(this.outer.session.Token, orgId, member.MemberId, new(confirmKey.BuildString()), cancelToken);
        }

        private readonly VaultwardenAgent outer;

        [return: NotNullIfNotNull(nameof(text))]
        private EncryptedData? encryptText(SymmetricCryptoKey key, string? text) => text == null ? null : this.outer.connector.Utility.EncryptAes(key, text.EncodeUtf8(), hmac: true);

        [return: NotNullIfNotNull(nameof(text))]
        private string? decryptText(byte[] key, string? text) => text == null ? null : this.outer.decryptEncText(key, text);
    }

    private record OrgInfo(VwOrganizationProfile Profile, SymmetricCryptoKey Key);
    private record SessionContext(
        ConnectTokenResult Token,
        string RefreshToken,
        KdfConfig Kdf,
        VwUserProfile Profile,
        SymmetricCryptoKey UserKey,
        byte[] UserPrivateKey,
        byte[] UserPublicKey,
        Dictionary<string, VwFolder> Folders,
        Dictionary<string, OrgInfo> Orgs
    );

    private VaultwardenConnector connector;
    private readonly bool own;
    private readonly UserContext user;
    private SessionContext session;

    private static async ValueTask<SessionContext> createConnectionAsync(VaultwardenConnector connector, UserContext user, CancellationToken cancelToken = default)
    {
        var userPrelogin = await connector.Identity.PreloginAsync(new(user.Mail), cancelToken);
        var userPassHsah = connector.Utility.CreatePasswordHash(user.Mail, user.Password, userPrelogin);
        var userPassHashB64 = userPassHsah.EncodeBase64();
        var userCredential = new PasswordConnectTokenModel(
            scope: "api offline_access",
            client_id: "web",
            device_type: ClientDeviceType.UnknownBrowser,
            device_name: user.Client?.Name ?? Environment.MachineName,
            device_identifier: user.Client?.Identifier ?? $"{Environment.MachineName}-VaultwardenAgent",
            username: user.Mail,
            password: userPassHashB64
        );
        var userToken = await connector.Identity.ConnectTokenAsync(userCredential, cancelToken);
        var userProfile = await connector.User.GetProfileAsync(userToken, cancelToken);
        var userPubKey = await connector.User.GetPublicKeyAsync(userToken, userProfile.id);
        var userPubKeyBin = userPubKey.publicKey.AsSpan().DecodeBase64() ?? [];
        var userFolders = await connector.User.GetFoldersAsync(userToken, cancelToken);
        var stretchKey = connector.Utility.CreateStretchKey(user.Mail, user.Password, userPrelogin);
        var userKey = SymmetricCryptoKey.From(connector.Utility.Decrypt(stretchKey.EncKey, EncryptedData.Parse(userProfile.key)));
        var userPrivateKey = connector.Utility.Decrypt(userKey.EncKey, EncryptedData.Parse(userProfile.privateKey));
        var orgKeys = userProfile.organizations.ToDictionary(
            o => o.id,
            o => new OrgInfo(o, SymmetricCryptoKey.From(connector.Utility.Decrypt(userPrivateKey, EncryptedData.Parse(o.key))))
        );
        var folders = userFolders.data.ToDictionary(f => f.id);
        return new(userToken, userToken.refresh_token, userToken.ToKdfConfig(), userProfile, userKey, userPrivateKey, userPubKeyBin, folders, orgKeys);
    }

    [return: NotNullIfNotNull(nameof(text))]
    private string? decryptEncText(byte[] key, string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var enc = EncryptedData.Parse(text);
        var decrypted = this.connector.Utility.Decrypt(key, enc);
        return Encoding.UTF8.GetString(decrypted);
    }

    private string? decryptChiperItemText(CipherItem item, string? text)
    {
        var key = string.IsNullOrWhiteSpace(item.organizationId) ? this.session.UserKey.EncKey
                : this.session.Orgs.TryGetValue(item.organizationId, out var org) ? org.Key.EncKey
                : default;
        if (key == null) return null;

        return decryptEncText(key, text);
    }

    private DecryptedCipherItemLogin? dcryptChiperItemLogin(byte[] key, CipherItemLogin? login)
    {
        if (login == null) return null;
        return new(
            Username: decryptEncText(key, login.username),
            Password: decryptEncText(key, login.password),
            Totp: decryptEncText(key, login.totp)
        );
    }

    private DecryptedCipherItemCard? decryptChiperItemCard(byte[] key, CipherItemCard? card)
    {
        if (card == null) return null;
        return new(
            Brand: decryptEncText(key, card.brand),
            CardholderName: decryptEncText(key, card.cardholderName),
            Number: decryptEncText(key, card.number),
            ExpYear: decryptEncText(key, card.expYear),
            ExpMonth: decryptEncText(key, card.expMonth),
            Code: decryptEncText(key, card.code)
        );
    }

    private DecryptedCipherItemSshKey? decryptChiperItemCard(byte[] key, CipherItemSshKey? sshKey)
    {
        if (sshKey == null) return null;
        return new(
            Fingerprint: decryptEncText(key, sshKey.keyFingerprint),
            PrivateKey: decryptEncText(key, sshKey.privateKey),
            PublicKey: decryptEncText(key, sshKey.publicKey)
        );
    }

    private DecryptedCipherItem? dcryptChiperItem(CipherItem? item, string? name = default)
    {
        if (item == null) return null;

        var folder = item.folderId != null && this.session.Folders.TryGetValue(item.folderId, out var f) ? f : default;
        var org = item.organizationId != null && this.session.Orgs.TryGetValue(item.organizationId, out var o) ? o : default;
        var key = org?.Key.EncKey ?? this.session.UserKey.EncKey;

        var decName = name ?? decryptEncText(key, item.name) ?? "";
        var decMemo = decryptEncText(key, item.notes);
        var decFolder = decryptEncText(this.session.UserKey.EncKey, folder?.name);
        var decItem = new DecryptedCipherItem(
            Id: item.id,
            Type: item.type,
            Deleted: item.deletedDate.HasValue,
            Name: decName,
            OrgName: org?.Profile.name,
            FolderName: decFolder,
            Memo: decMemo,
            Login: dcryptChiperItemLogin(key, item.login),
            Card: decryptChiperItemCard(key, item.card),
            SshKey: decryptChiperItemCard(key, item.sshKey)
        );
        return decItem;
    }
}
