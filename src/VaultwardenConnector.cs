using System.Text.Json;
using System.Text.Json.Serialization;

namespace VwConnector;

/// <summary>Vaultwardenクライアント処理インタフェース</summary>
public interface IVwConnector
{
    /// <summary>サービスベースアドレス</summary>
    public Uri BaseUri { get; }

    /// <summary>ユーティリティ処理関連のメソッドグループ</summary>
    /// <remarks>このグループに含まれるメソッドはネットワーク接続を利用しないローカル処理です</remarks>
    public IVwUtility Utility { get; }

    /// <summary>接続系のメソッドグループ</summary>
    public IVwIdentity Identity { get; }

    /// <summary>Adminエンドポイント関連のメソッドグループ</summary>
    public IVwAdmin Admin { get; }

    /// <summary>ユーザ情報関連のメソッドグループ</summary>
    public IVwUser User { get; }

    /// <summary>組織関連のメソッドグループ</summary>
    public IVwOrganization Organization { get; }

    /// <summary>保管項目関連のメソッドグループ</summary>
    public IVwCipher Cipher { get; }

    /// <summary>Bitwarden Public API関連のメソッドグループ</summary>
    public IVwPublic Public { get; }

    /// <summary>任意エンドポイントへの要求関連</summary>
    public IVwRaw Raw { get; }
}

/// <summary>Vaultwardenクライアント処理クラス</summary>
public class VaultwardenConnector : IVwConnector, IDisposable
{
    /// <summary>コンストラクタ</summary>
    /// <param name="baseUri">サービスのベースURL。エンドポイントパス 'api/***' などを付与するベースとなる。</param>
    public VaultwardenConnector(Uri baseUri)
    {
        this.BaseUri = baseUri;
        this.http = new HttpClient(new HttpClientHandler() { UseCookies = false, });

        this.scopeUtility = new VmUtility(this);
        this.scopeIdentity = new VwIdentity(this);
        this.scopeAdmin = new VwAdmin(this);
        this.scopeUser = new VwUser(this);
        this.scopeOrg = new VwOrganization(this);
        this.scopeCipher = new VwCipher(this);
        this.scopePublic = new VwPublic(this);
        this.scopeRaw = new VwRaw(this);
    }

    /// <summary>サービスベースアドレス</summary>
    public Uri BaseUri { get; }

    /// <summary>タイムアウト時間</summary>
    public TimeSpan Timeout
    {
        get => this.http.Timeout;
        set => this.http.Timeout = value;
    }

    /// <summary>ユーティリティ処理関連のメソッドグループ</summary>
    /// <remarks>このグループに含まれるメソッドはネットワーク接続を利用しないローカル処理です</remarks>
    public IVwUtility Utility => this.scopeUtility;

    /// <summary>接続系のメソッドグループ</summary>
    public IVwIdentity Identity => this.scopeIdentity;

    /// <summary>Adminエンドポイント関連のメソッドグループ</summary>
    public IVwAdmin Admin => this.scopeAdmin;

    /// <summary>ユーザ情報関連のメソッドグループ</summary>
    public IVwUser User => this.scopeUser;

    /// <summary>組織関連のメソッドグループ</summary>
    public IVwOrganization Organization => this.scopeOrg;

    /// <summary>保管項目関連のメソッドグループ</summary>
    public IVwCipher Cipher => this.scopeCipher;

    /// <summary>Bitwarden Public API関連のメソッドグループ</summary>
    public IVwPublic Public => this.scopePublic;

    /// <summary>任意エンドポイントへの要求関連</summary>
    public IVwRaw Raw => this.scopeRaw;

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                this.http?.Dispose();
                this.http = null!;
            }

            this.disposed = true;
        }
    }

    private class VwScopeBase(VaultwardenConnector outer) : IVwScope
    {
        Uri IVwScope.BaseUri => outer.BaseUri;
        JsonSerializerOptions IVwScope.SerializeOptions => outer.apiSerializeOptions;
        HttpClient IVwScope.Http => outer.http;
    }

    private class VmUtility(VaultwardenConnector outer) : VwScopeBase(outer), IVwUtility;
    private class VwIdentity(VaultwardenConnector outer) : VwScopeBase(outer), IVwIdentity;
    private class VwAdmin(VaultwardenConnector outer) : VwScopeBase(outer), IVwAdmin;
    private class VwUser(VaultwardenConnector outer) : VwScopeBase(outer), IVwUser;
    private class VwOrganization(VaultwardenConnector outer) : VwScopeBase(outer), IVwOrganization;
    private class VwCipher(VaultwardenConnector outer) : VwScopeBase(outer), IVwCipher;
    private class VwPublic(VaultwardenConnector outer) : VwScopeBase(outer), IVwPublic;
    private class VwRaw(VaultwardenConnector outer) : VwScopeBase(outer), IVwRaw;

    private HttpClient http;
    private bool disposed;
    private readonly JsonSerializerOptions apiSerializeOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, };

    private readonly IVwUtility scopeUtility;
    private readonly IVwIdentity scopeIdentity;
    private readonly IVwAdmin scopeAdmin;
    private readonly IVwUser scopeUser;
    private readonly IVwOrganization scopeOrg;
    private readonly IVwCipher scopeCipher;
    private readonly IVwPublic scopePublic;
    private readonly IVwRaw scopeRaw;
}
