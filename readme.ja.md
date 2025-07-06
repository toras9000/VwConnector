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

パッケージバージョンはセマンティックバージョン形式ですが、以下のように意味づけを行っています。  
このパッケージでは常にプレリリース番号付きのバージョン番号を利用します。  
コアバージョン部分は、ライブラリがターゲットとするサーバのバージョンを表しています。  
プレリリース番号部分(rev.XX)は、プレリリースとしてではなく、(同じコアバージョンに対する)ライブラリのバージョンを表すために使っています。  
したがって、プレリリース番号の違いは、必ずしも些細な変更とは限りません。  


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

また、利用方法を簡略化した追加のヘルパクラスとして `VaultwardenAgent` を含みます。  
このクラスは細かな制御を省いて、主要な関心になると思われるデータの一部のみを扱うメソッドを持っています。  
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

そのほか、Vaultwarden の状態に影響を与えるメソッド郡は `Affected` プロパティ配下に持ちます。  
以下は待機中の組織メンバーを確認(承認)する例です。  

```csharp
var service = new Uri(@"http://<your-hosting-server>");
var ownerMail = "xxxxxxxxx";
var ownerPass = "yyyyyyy";
var orgName = "TestOrg";
using var vaultwarden = await VaultwardenAgent.CreateAsync(service, new(ownerMail, ownerPass));
var profile = await vaultwarden.Connector.User.GetProfileAsync(vaultwarden.Token);
var org = profile.organizations.FirstOrDefault(o => o.name == orgName) ?? throw new Exception("Org not found");
var members = await vaultwarden.Connector.Organization.GetMembersAsync(vaultwarden.Token, org.id);
foreach (var member in members.data)
{
    if (member.status != MembershipStatus.Accepted) continue;
    await vaultwarden.Affect.ConfirmMemberAsync(org.id, new(member.id, member.userId));
}
```
