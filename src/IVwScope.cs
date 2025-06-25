using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace VwConnector;

public interface IVwScope
{
    internal Uri BaseUri { get; }
    internal JsonSerializerOptions SerializeOptions { get; }
    internal HttpClient Http { get; }

    internal HttpRequestMessage CreateRequest(HttpMethod method, string endpoint, ConnectTokenResult? token, HttpContent? content = default)
    {
        var apiEndpoint = new Uri(this.BaseUri, endpoint);
        var message = new HttpRequestMessage(method, apiEndpoint);
        if (token != null) message.Headers.Authorization = new AuthenticationHeaderValue(token.token_type, token.access_token);
        if (content != null) message.Content = content;
        return message;
    }

    internal HttpRequestMessage CreateJsonRequest<TData>(HttpMethod method, string endpoint, ConnectTokenResult? token, TData? data = default)
    {
        var content = default(HttpContent);
        if (data != null) content = JsonContent.Create(data, options: this.SerializeOptions);
        return CreateRequest(method, endpoint, token, content);
    }

    internal HttpRequestMessage CreateUrlEncodedRequest<TData>(HttpMethod method, string endpoint, ConnectTokenResult? token, TData? data = default)
    {
        var content = default(HttpContent);
        if (data != null) content = CreateFormUrlEncodedContent(data);
        return CreateRequest(method, endpoint, token, content);
    }

    internal FormUrlEncodedContent CreateFormUrlEncodedContent<T>(T value)
    {
        static IEnumerable<KeyValuePair<string, string>> enumerateMembers(T obj)
        {
            foreach (var prop in typeof(T).GetProperties())
            {
                var name = prop.Name;
                var value = prop.GetValue(obj);
                yield return new(name, $"{value}");
            }
        }

        return new FormUrlEncodedContent(enumerateMembers(value));
    }

}
