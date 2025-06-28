# VwConnector

[![NugetShield]][NugetPackage]

[NugetPackage]: https://www.nuget.org/packages/VwConnector
[NugetShield]: https://img.shields.io/nuget/v/VwConnector

Vaultwardenのいくつかのエンドポイントを呼び出すヘルパークラスライブラリ。  
呼び出しメソッドは網羅的ではなく、自分が必要としたものだけを含んでいます。  
独自の解析に基づいて作成したものであり、正しく機能しているかどうかはわかりません。  

エンドポイントの呼び出し方法を誤ると、問題を引き起こすことがあるため注意が必要です。  
特に更新を伴うものには気を付けてください。  
たとえば、正しく暗号化されていない名称を指定してコレクションの作成要求を呼び出した場合、Web Vault は既存のアイテムも含めて一切表示できなくなるようです。  


ライブラリに含む主要なクラスは `VaultwardenConnector` です。  
このクラスを利用して暗号アイテムを読み取る例は以下のようになります。  

```csharp
var service = new Uri(@"http://<your-hosting-server>");
var userMail = "xxxxxxxxx";
var masterPass = "yyyyyyy";
using var vaultwarden = new VaultwardenConnector(service);
var userPrelogin = await vaultwarden.Identity.PreloginAsync(new(userMail));
var userPassHsah = vaultwarden.Utility.CreatePasswordHash(userMail, masterPass, userPrelogin);
var userPassHashB64 = Convert.ToBase64String(userPassHsah);
var userCredential = new PasswordConnectTokenModel(
    scope: "api offline_access",
    client_id: "web",
    device_type: ClientDeviceType.UnknownBrowser,
    device_name: "device-name",
    device_identifier: "device-identifier",
    username: userMail,
    password: userPassHashB64
);
var userToken = await vaultwarden.Identity.ConnectTokenAsync(userCredential);
var userProfile = await vaultwarden.User.GetProfileAsync(userToken);
var stretchKey = vaultwarden.Utility.CreateStretchKey(userMail, masterPass, userToken.ToKdfConfig());
var userKey = SymmetricCryptoKey.From(vaultwarden.Utility.Decrypt(stretchKey.EncKey, EncryptedData.Parse(userProfile.key)));
var userPrivateKey = vaultwarden.Utility.Decrypt(userKey.EncKey, EncryptedData.Parse(userProfile.privateKey));
var orgKeys = userProfile.organizations.ToDictionary(
    o => o.id,
    o => new { profile = o, key = SymmetricCryptoKey.From(vaultwarden.Utility.Decrypt(userPrivateKey, EncryptedData.Parse(o.key))), }
);
var items = await vaultwarden.Cipher.GetItemsAsync(userToken);
foreach (var item in items.data)
{
    var info = item.deletedDate.HasValue ? " (deleted)" : "";
    if (string.IsNullOrWhiteSpace(item.organizationId))
    {
        var decryptName = Encoding.UTF8.GetString(vaultwarden.Utility.Decrypt(userKey.EncKey, EncryptedData.Parse(item.name)));
        WriteLine($"- [{item.type}] {decryptName}{info}");
    }
    else if (orgKeys.TryGetValue(item.organizationId, out var org))
    {
        var decryptName = Encoding.UTF8.GetString(vaultwarden.Utility.Decrypt(org.key.EncKey, EncryptedData.Parse(item.name)));
        WriteLine($"- [{item.type}] {decryptName}{info} : {org.profile.name}");
    }
    else
    {
        WriteLine($"- Unknown");
    }
}
```

また、暗号アイテム読み出し用の追加ヘルパクラスとして `VaultwardenAgent` を含みます。  
このクラスには暗号アイテム読み出し処理だけを含み、他の操作は含まれません。  
このクラスを利用して上記とほぼ同等の、暗号項目を読み取る例は以下のようになります。  

```csharp
var service = new Uri(@"http://<your-hosting-server>");
var userMail = "xxxxxxxxx";
var masterPass = "yyyyyyy";
using var vaultwarden = await VaultwardenAgent.CreateAsync(service, new(userMail, masterPass));
var items = await vaultwarden.GetItemsAsync();
foreach (var item in items)
{
    var info = item.Deleted ? " (deleted)" : "";
    if (string.IsNullOrWhiteSpace(item.OrgName))
    {
        WriteLine($"- [{item.Type}] {item.Name}{info} : {item.OrgName}");
    }
    else
    {
        WriteLine($"- [{item.Type}] {item.Name}{info}");
    }
}
```

