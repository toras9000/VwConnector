# VwConnector

[![NugetShield]][NugetPackage]

[NugetPackage]: https://www.nuget.org/packages/VwConnector
[NugetShield]: https://img.shields.io/nuget/v/VwConnector

Helper class library that calls several endpoints of Vaultwarden.  
The calling methods are not exhaustive and include only those that I myself have needed.  
Since they were created based on their own analysis, it is not clear whether they are working correctly.  

Care must be taken because incorrectly invoking an endpoint can cause problems.  
Be especially careful with those that involve renewals.  
For example, if you invoke a request to create a collection with a name that is not properly encrypted, it appears that Web Vault will not be able to display any items, including existing items.  

Package versions are in semantic versioning format, but are numbered according to the following arrangement.  
The version number of this package always uses the pre-release versioning format.   
The core version part represents the version of the target server.  
The version (rev.XX) portion of the pre-release is used to indicate the version of the library, not as a pre-release.  
Therefore, differences in pre-release version numbers are not necessarily trivial changes.  


The primary class included in the library is `VaultwardenConnector`.  
An example of using this class to read a cipher item is shown below.  

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

It also includes `VaultwardenAgent` as an additional helper class that simplifies its usage.  
This class omits detailed controls and has methods that handle only a portion of the data that is likely to be of primary interest.  
An example of a reading that is almost equivalent to the above, using this class, is shown below.  

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

Other methods that affect the state of Vaultwarden are under the `Affected` property.  
The following is an example of confirming (approving) a waiting organization member.  

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
