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

    /// <summary>アカウント系のメソッドグループ</summary>
    public IVwAccount Account { get; }

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

        this.Utility = new VmUtility(this);
        this.Identity = new VwIdentity(this);
        this.Admin = new VwAdmin(this);
        this.Account = new VwAccount(this);
        this.User = new VwUser(this);
        this.Organization = new VwOrganization(this);
        this.Cipher = new VwCipher(this);
        this.Public = new VwPublic(this);
        this.Raw = new VwRaw(this);
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
    public IVwUtility Utility { get; }

    /// <summary>接続系のメソッドグループ</summary>
    public IVwIdentity Identity { get; }

    /// <summary>Adminエンドポイント関連のメソッドグループ</summary>
    public IVwAdmin Admin { get; }

    /// <summary>アカウント系のメソッドグループ</summary>
    public IVwAccount Account { get; }

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
        IVwConnector IVwScope.Connector => outer;
        JsonSerializerOptions IVwScope.SerializeOptions => outer.apiSerializeOptions;
        HttpClient IVwScope.Http => outer.http;
    }

    private class VmUtility(VaultwardenConnector outer) : VwScopeBase(outer), IVwUtility;
    private class VwIdentity(VaultwardenConnector outer) : VwScopeBase(outer), IVwIdentity;
    private class VwAdmin(VaultwardenConnector outer) : VwScopeBase(outer), IVwAdmin;
    private class VwAccount(VaultwardenConnector outer) : VwScopeBase(outer), IVwAccount;
    private class VwUser(VaultwardenConnector outer) : VwScopeBase(outer), IVwUser;
    private class VwOrganization(VaultwardenConnector outer) : VwScopeBase(outer), IVwOrganization;
    private class VwCipher(VaultwardenConnector outer) : VwScopeBase(outer), IVwCipher;
    private class VwPublic(VaultwardenConnector outer) : VwScopeBase(outer), IVwPublic;
    private class VwRaw(VaultwardenConnector outer) : VwScopeBase(outer), IVwRaw;

    private HttpClient http;
    private bool disposed;
    private readonly JsonSerializerOptions apiSerializeOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, };
}
