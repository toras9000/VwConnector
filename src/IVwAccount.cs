namespace VwConnector;

public record AccountRegisterUserArgs(string Mail, string Password, string? Name = default);

public interface IVwAccount : IVwScope
{
    public async ValueTask RegisterUserEmailAsync(AccountRegisterUserArgs args, string verificationToken, CancellationToken cancelToken = default)
    {
        var register = createRegisterArgs(args) with
        {
            emailVerificationToken = verificationToken,
        };
        await this.Connector.Identity.RegisterFinishAsync(register, cancelToken);
    }

    public async ValueTask RegisterUserInviteAsync(AccountRegisterUserArgs args, string orgUserId, string inviteToken, CancellationToken cancelToken = default)
    {
        var register = createRegisterArgs(args) with
        {
            organizationUserId = orgUserId,
            orgInviteToken = inviteToken,
        };
        await this.Connector.Identity.RegisterFinishAsync(register, cancelToken);
    }

    public async ValueTask RegisterUserNoSmtpAsync(AccountRegisterUserArgs args, CancellationToken cancelToken = default)
    {
        var verificationToken = await this.Connector.Identity.SendRegisterVerificationMailAsync(new(args.Mail, args.Name), cancelToken);
        var register = createRegisterArgs(args) with
        {
            emailVerificationToken = verificationToken,
        };
        await this.Connector.Identity.RegisterFinishAsync(register, cancelToken);
    }

    private RegisterArgs createRegisterArgs(AccountRegisterUserArgs args)
    {
        var kdf = new KdfConfig(KdfType.Pbkdf2, 600000);
        var masterKey = this.Connector.Utility.CreateMasterKey(args.Mail, args.Password, kdf);
        var serverHsah = this.Connector.Utility.CreateMasterKeyHash(masterKey, args.Password, 1);
        var localHsah = this.Connector.Utility.CreateMasterKeyHash(masterKey, args.Password, 2);

        var stretchKey = this.Connector.Utility.CreateStretchKey(masterKey);
        var newUserKey = SymmetricCryptoKey.From(this.Connector.Utility.GenerateKeyData());
        var keyPair = this.Connector.Utility.GenerateRsaKeyPair();
        var userKeyEnc = this.Connector.Utility.EncryptAes(stretchKey, newUserKey.ToBytes(), hmac: true);
        var prvKeyEnc = this.Connector.Utility.EncryptAes(newUserKey, keyPair.PrivateKey, hmac: true);

        var register = new RegisterArgs(
            email: args.Mail,
            userSymmetricKey: userKeyEnc.BuildString(),
            userAsymmetricKeys: new(prvKeyEnc.BuildString(), keyPair.PublicKey.EncodeBase64()),
            masterPasswordHash: serverHsah.EncodeBase64(),
            name: args.Name,
            kdf: kdf.kdf,
            kdfIterations: kdf.kdfIterations
        );
        return register;
    }
}