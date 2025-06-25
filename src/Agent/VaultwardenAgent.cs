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

public class VaultwardenAgent : IDisposable
{
    public DecryptedCipherItem? DecryptItem(CipherItem item)
        => dcryptChiperItem(item);

    public async ValueTask<DecryptedCipherItem[]> GetItemsAsync(CancellationToken cancelToken = default)
    {
        var items = await this.connector.Cipher.GetItemsAsync(this.session.Token, cancelToken);
        var decrypted = items.data.Select(i => DecryptItem(i)).Where(i => i != null).Select(i => i!).ToArray();
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

    public void Dispose()
    {
        if (this.own)
        {
            this.connector?.Dispose();
            this.connector = default!;
            this.session = default!;
        }
    }

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


    private VaultwardenAgent(VaultwardenConnector connector, bool own, UserContext user, SessionContext session)
    {
        this.connector = connector;
        this.own = own;
        this.user = user;
        this.session = session;
    }

    private record OrgInfo(VwOrganizationProfile Profile, SymmetricCryptoKey Key);
    private record SessionContext(ConnectTokenResult Token, VwUserProfile Profile, SymmetricCryptoKey UserKey, Dictionary<string, VwFolder> Folders, Dictionary<string, OrgInfo> Orgs);

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
        var userFolders = await connector.User.GetFoldersAsync(userToken, cancelToken);
        var stretchKey = connector.Utility.CreateStretchKey(user.Mail, user.Password, userPrelogin);
        var userKey = SymmetricCryptoKey.From(connector.Utility.Decrypt(stretchKey.EncKey, EncryptedData.Parse(userProfile.key)));
        var userPrivateKey = connector.Utility.Decrypt(userKey.EncKey, EncryptedData.Parse(userProfile.privateKey));
        var orgKeys = userProfile.organizations.ToDictionary(
            o => o.id,
            o => new OrgInfo(o, SymmetricCryptoKey.From(connector.Utility.Decrypt(userPrivateKey, EncryptedData.Parse(o.key))))
        );
        var folders = userFolders.data.ToDictionary(f => f.id);
        return new(userToken, userProfile, userKey, folders, orgKeys);
    }

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
