using System.Text.Json;

namespace VwConnector;

#region Identifiers
public enum MembershipStatus
{
    Revoked = -1,
    Invited = 0,
    Accepted = 1,
    Confirmed = 2,
}

public enum MembershipType
{
    Owner = 0,
    Admin = 1,
    User = 2,
    Manager = 3,
}


public enum EncryptionType
{
    AesCbc256 = 0,
    AesCbc128_HmacSha256 = 1,
    AesCbc256_HmacSha256 = 2,
    Rsa2048_OaepSha256 = 3,
    Rsa2048_OaepSha1 = 4,
    Rsa2048_OaepSha256_HmacSha256 = 5,
    Rsa2048_OaepSha1_HmacSha256 = 6,
}

public enum PlanType
{
    Free = 0,
    Custom = 6,
}
#endregion

#region Admin
public record AdminToken(string token);
public record VwUserEx(
    long _status, string @object, string id, string name, string email, bool emailVerified,
    bool premium, bool premiumFromOrganization, string? masterPasswordHint,
    string culture, bool twoFactorEnabled, string key, string? privateKey, string securityStamp,
    VwOrganizationProfile[] organizations, object[] providers, object[] providerOrganizations,
    bool forcePasswordReset, string? avatarColor, bool usesKeyConnector, DateTime creationDate,
    bool userEnabled, string createdAt, string? lastActive
) : VwUserProfile(
     _status, @object, id, name, email, emailVerified,
     premium, premiumFromOrganization, masterPasswordHint,
     culture, twoFactorEnabled, key, privateKey, securityStamp,
     organizations, providers, providerOrganizations,
     forcePasswordReset, avatarColor, usesKeyConnector, creationDate
);
#endregion

#region ConnectToken
public enum ClientDeviceType
{
    Android = 0,
    iOS = 1,
    ChromeExtension = 2,
    FirefoxExtension = 3,
    OperaExtension = 4,
    EdgeExtension = 5,
    WindowsDesktop = 6,
    MacOsDesktop = 7,
    LinuxDesktop = 8,
    ChromeBrowser = 9,
    FirefoxBrowser = 10,
    OperaBrowser = 11,
    EdgeBrowser = 12,
    IEBrowser = 13,
    UnknownBrowser = 14,
    AndroidAmazon = 15,
    Uwp = 16,
    SafariBrowser = 17,
    VivaldiBrowser = 18,
    VivaldiExtension = 19,
    SafariExtension = 20,
    Sdk = 21,
    Server = 22,
    WindowsCLI = 23,
    MacOsCLI = 24,
    LinuxCLI = 25,
};
public record ConnectTokenModel(string grant_type);
public record RefreshConnectTokenModel(string refresh_token) : ConnectTokenModel("refresh_token");
public record ScopedConnectTokenModel(string grant_type, string scope, string client_id, ClientDeviceType device_type, string device_name, string device_identifier) : ConnectTokenModel(grant_type);
public record PasswordConnectTokenModel(string scope, string client_id, ClientDeviceType device_type, string device_name, string device_identifier, string username, string password)
    : ScopedConnectTokenModel("password", scope, client_id, device_type, device_name, device_identifier);
public record ClientCredentialsConnectTokenModel(string scope, string client_id, ClientDeviceType device_type, string device_name, string device_identifier, string client_secret)
    : ScopedConnectTokenModel("client_credentials", scope, client_id, device_type, device_name, device_identifier);

public record ConnectTokenResult(string token_type, string access_token, long expires_in, string scope);

public record ClientCredentialsConnectTokenResult(
    string token_type, string access_token, long expires_in, string scope,
    KdfType Kdf, int KdfIterations, int? KdfMemory, int? KdfParallelism,
    string Key, string PrivateKey,
    bool ResetMasterPassword
) : ConnectTokenResult(token_type, access_token, expires_in, scope)
{
    public KdfConfig ToKdfConfig() => new KdfConfig(this.Kdf, this.KdfIterations, this.KdfMemory, this.KdfParallelism);
}

public record PasswordConnectTokenMasterPasswordPolicy(string @object);
public record PasswordConnectTokenUserDecryptionOptions(string Object, bool userDecryptionOptions);
public record PasswordConnectTokenResult(
    string token_type, string access_token, long expires_in, string scope,
    KdfType Kdf, int KdfIterations, int? KdfMemory, int? KdfParallelism,
    string Key, string PrivateKey,
    string refresh_token,
    bool ForcePasswordReset, bool ResetMasterPassword,
    PasswordConnectTokenMasterPasswordPolicy MasterPasswordPolicy,
    PasswordConnectTokenUserDecryptionOptions UserDecryptionOptions
) : ConnectTokenResult(token_type, access_token, expires_in, scope)
{
    public KdfConfig ToKdfConfig() => new KdfConfig(this.Kdf, this.KdfIterations, this.KdfMemory, this.KdfParallelism);
}
#endregion

#region Key
public record KeysData(string encryptedPrivateKey, string publicKey);
#endregion

#region Register
public record RegisterVerificationMailArgs(string email, string? name = default);

public record RegisterArgs(
    string email,
    string userSymmetricKey,
    string masterPasswordHash,
    KeysData? userAsymmetricKeys = default,
    string? name = default,
    string? masterPasswordHint = default,
    string? organizationUserId = default,
    KdfType? kdf = default, int? kdfIterations = default, long? kdfMemory = default, long? kdfParallelism = default,
    // Used only from the register/finish endpoint
    string? emailVerificationToken = default,
    string? acceptEmergencyAccessId = default,
    string? acceptEmergencyAccessInviteToken = default,
    string? orgInviteToken = default
);
public record RegisterResult(string @object, string? captchaBypassToken);
#endregion

#region Prelogin
public enum KdfType
{
    Pbkdf2 = 0,
    Argon2id = 1,
}
public record PreloginArgs(string email);
public record KdfConfig(KdfType kdf, int kdfIterations, long? kdfMemory = default, long? kdfParallelism = default);
public record PreloginResult(KdfType kdf, int kdfIterations, long? kdfMemory, long? kdfParallelism) : KdfConfig(kdf, kdfIterations, kdfMemory, kdfParallelism);
#endregion

#region Keys
public record VwApiKey(string @object, string apiKey, DateTime revisionDate);
public record PasswordOrOtp(string? masterPasswordHash = default, string? otp = default);
public record UserPublicKey(string @object, string userId, string publicKey);
public record OrgPublicKey(string @object, string publicKey);
#endregion

#region Entities
public record VwUserProfile(
    long _status, string @object, string id, string name, string email, bool emailVerified,
    bool premium, bool premiumFromOrganization, string? masterPasswordHint,
    string culture, bool twoFactorEnabled, string key, string? privateKey, string securityStamp,
    VwOrganizationProfile[] organizations, object[] providers, object[] providerOrganizations,
    bool forcePasswordReset, string? avatarColor, bool usesKeyConnector, DateTime creationDate
);

public record VwPermissions(
    bool accessEventLogs, bool accessImportExport, bool accessReports,
    bool createNewCollections, bool editAnyCollection, bool deleteAnyCollection,
    bool manageGroups, bool managePolicies, bool manageSso,
    bool manageUsers, bool manageResetPassword, bool manageScim
);

public record VwFolder(string @object, string id, string name, DateTime revisionDate);

public record VwOrganizationProfile(
    string @object, string id, string name, string organizationUserId,
    bool enabled, string userId, MembershipStatus status, MembershipType type, string key,
    int maxStorageGb, int planProductType, int productTierType,
    object? providerId, object? providerName, object? providerType,
    object? identifier, object? seats, object? maxCollections,
    bool hasPublicAndPrivateKeys, bool selfHost, bool usersGetPremium,
    bool use2fa, bool useActivateAutofillPolicy, bool useApi,
    bool useCustomPermissions, bool useDirectory, bool useEvents, bool useGroups,
    bool useKeyConnector, bool usePasswordManager, bool usePolicies, bool useResetPassword,
    bool useScim, bool useSecretsManager, bool useSso, bool useTotp,
    bool resetPasswordEnrolled, bool ssoBound, bool userIsManagedByOrganization,
    bool accessSecretsManager, bool allowAdminAccessToAllCollectionItems,
    bool familySponsorshipAvailable, object? familySponsorshipFriendlyName,
    object? familySponsorshipLastSyncDate, object? familySponsorshipToDelete, object? familySponsorshipValidUntil,
    bool limitCollectionCreation, bool limitCollectionCreationDeletion, bool limitCollectionDeletion,
    bool keyConnectorEnabled, object? keyConnectorUrl,
    VwPermissions? permissions
);

public record VwCollection(string id, bool readOnly, bool hidePasswords, bool manage);

public record VwCollectionGroup(string id, bool readOnly, bool hidePasswords, bool manage);

public record VwCollectionMembership(string id, bool readOnly, bool hidePasswords, bool manage);
#endregion

#region Folders
public record CreateFolderArgs(string name, string? id = default);
public record GetFoldersResult(string @object, VwFolder[] data);
#endregion

#region OrgMembers
public record OrgMembersArgs(bool? includeCollections = default, bool? includeGroups = default);
public record OrgMembersUser(
    string @object, string id, string name, string email, string userId, string externalId,
    MembershipStatus status, MembershipType type,
    string? avatarColor, string[]? groups, VwCollection[]? collections, VwPermissions? permissions,
    bool accessAll, bool twoFactorEnabled, bool resetPasswordEnrolled, bool hasMasterPassword,
    bool ssoBound, bool usesKeyConnector, bool accessSecretsManager
);
public record OrgMembersResult(string @object, OrgMembersUser[] data);
#endregion

#region OrgCollections
public record OrgCollectionDetail(string @object, string id, string organizationId, string name, string[] externalId, bool readOnly, bool hidePasswords, bool manage);
public record CreateCollectionArgs(string name, VwCollectionMembership[] users, VwCollectionGroup[] groups, string? id = default, string? external_id = default);
public record OrgCollection(string @object, string id, string organizationId, string name, string[] externalId);
public record OrgCollectionsResult(string @object, OrgCollection[] data);
#endregion

#region CreateOrg
public record CreateOrgArgs(string name, string billingEmail, string key, KeysData? keys, string collectionName, PlanType planType);

public record CreateOrgResult(
    string @object, string id, string name, string billingEmail,
    string businessName, object? businessTaxNumber, object? businessCountry,
    object? businessAddress1, object? businessAddress2, object? businessAddress3,
    int maxStorageGb, object? maxCollections,
    object? maxAutoscaleSeats, object? maxAutoscaleSmSeats, object? maxAutoscaleSmServiceAccounts,
    bool limitCollectionCreation, bool limitCollectionDeletion,
    object? seats, object? secretsManagerPlan,
    object? smSeats, object? smServiceAccounts,
    bool allowAdminAccessToAllCollectionItems, PlanType planType,
    bool hasPublicAndPrivateKeys, bool selfHost, bool usersGetPremium,
    bool use2fa, bool useApi,
    bool useCustomPermissions, bool useDirectory, bool useEvents, bool useGroups,
    bool useKeyConnector, bool usePasswordManager, bool usePolicies, bool useResetPassword,
    bool useScim, bool useSecretsManager, bool useSso, bool useTotp
);
#endregion

#region EditOrg
public enum EditMembershipType
{
    Owner = 0,
    Admin = 1,
    User = 2,
    Manager = 3,
    Custom = 4,
}
public record EditOrgMemberPermissions(bool createNewCollections, bool editAnyCollection, bool deleteAnyCollection);
public record EditOrgMemberArgs(EditMembershipType type, VwCollection[]? collections, string[]? groups, bool access_all, EditOrgMemberPermissions permissions);
#endregion

#region OrgImport
public record ImportOrgGroup(string name, string externalId, string[]? memberExternalIds);
public record ImportOrgMember(string externalId, string? email, bool deleted);
public record ImportOrgArgs(bool overwriteExisting, ImportOrgMember[] members, ImportOrgGroup[] groups);
#endregion

#region OrgAccept
public record AcceptInviteArgs(string token, string? resetPasswordKey = default);
#endregion

#region OrgConfirm
public record ConfirmMemberArgs(string key);
#endregion

#region Ciphers
public enum CipherType
{
    Login = 1,
    SecureNote = 2,
    Card = 3,
    Identity = 4,
    SshKey = 5,
}
public enum RepromptType
{
    None = 0,
    Password = 1,
}
public enum CipherCustomFieldType
{
    Text,
    Hidden,
    Checkbox,
    Linked,
}
public record CipherCustomField(string name, CipherCustomFieldType type, string value, int? linkedId);
public record CipherItemUri(object? match, string uri, string uriChecksum);
public record CipherItemAttachment(
    string @object, string id, string fileName,
    string key, string size, string sizeName, string uri
);
public record CipherPasswordChanged(DateTime lastUsedDate, string password);
public record CipherItemLogin(
    string? username = default, string? password = default, object? passwordRevisionDate = default,
    string? totp = default, string? uri = default, CipherItemUri[]? uris = default,
    object? autofillOnPageLoad = default
);
public record CipherItemCard(
    string? brand = default, string? cardholderName = default, string? number = default,
    string? expYear = default, string? expMonth = default, string? code = default
);
public record CipherItemIdentity(
    string? username = default, string? firstName = default, string? middleName = default, string? lastName = default, string? email = default,
    string? country = default, string? state = default, string? city = default, string? postalCode = default,
    string? address1 = default, string? address2 = default, string? address3 = default,
    string? company = default, string? title = default, string? phone = default,
    string? licenseNumber = default, string? passportNumber = default, string? ssn = default
);
public record CipherItemMemo(int type);
public record CipherItemSshKey(string? keyFingerprint = default, string? privateKey = default, string? publicKey = default);
public record CipherItem(
    string @object, string id, CipherType type,
    DateTime creationDate, DateTime revisionDate, DateTime? deletedDate,
    string organizationId, bool organizationUseTotp,
    string name, string? key, RepromptType reprompt,
    bool favorite, bool edit, bool viewPassword,
    string? folderId, string? notes,
    JsonElement? data,
    string[] collectionIds,
    CipherCustomField[]? fields,
    CipherItemLogin? login,
    CipherItemCard? card,
    CipherItemIdentity? identity,
    CipherItemMemo? secureNote,
    CipherItemSshKey? sshKey,
    CipherItemAttachment[]? attachments,
    CipherPasswordChanged[]? passwordHistory
);
public record CipherItemsResult(string @object, CipherItem[] data);

public record CipherData(
    CipherType type, string name,
    string? id = default, string? folderId = default, string? organizationId = default,
    string? key = default, string? notes = default,
    CipherCustomField[]? fields = default,
    CipherItemLogin? login = default,
    CipherItemCard? card = default,
    CipherItemIdentity? identity = default,
    CipherItemMemo? secureNote = default,
    CipherItemSshKey? sshKey = default,
    bool? favorite = default,
    int? reprompt = default,
    CipherPasswordChanged[]? passwordHistory = default,
    JsonElement? attachments = default,
    JsonElement? attachments2 = default,
    string? last_known_revision_date = default
);
public record CreateItemArgs(CipherData cipher, string[] collectionIds);
#endregion