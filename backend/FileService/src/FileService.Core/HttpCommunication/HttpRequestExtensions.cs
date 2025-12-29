using System.Collections;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace FileService.Core.HttpCommunication;

public static class HttpRequestExtensions
{
    extension(HttpClient client)
    {
        public async Task<HttpResponseMessage> GetAsync<TQuery>(
            string url,
            TQuery query,
            CancellationToken cancellationToken = default)
            where TQuery : class
            => await client.GetAsync(GenerateUrlWithQuery(url, query), cancellationToken);

        public async Task<HttpResponseMessage> DeleteAsync<TQuery>(
            string url,
            TQuery query,
            CancellationToken cancellationToken = default)
            where TQuery : class
            => await client.DeleteAsync(GenerateUrlWithQuery(url, query), cancellationToken);
    }

    private static string GenerateUrlWithQuery<TQuery>(
        string url,
        TQuery query)
        where TQuery : class
    {
        var parameters = new Dictionary<string, StringValues>();

        foreach (PropertyInfo prop in query.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            object? value = prop.GetValue(query, null);

            string propName = JsonNamingPolicy.CamelCase.ConvertName(prop.Name);

            if (value is IEnumerable enumerable and not string)
            {
                string?[] items = enumerable
                    .OfType<object>()
                    .Select(item => item.ToString())
                    .ToArray();

                if (items.Length > 0)
                    parameters[propName] = new StringValues(items);
            }
            else
            {
                parameters[propName] = new StringValues(value?.ToString());
            }
        }

        return QueryHelpers.AddQueryString(url, parameters);
    }
}