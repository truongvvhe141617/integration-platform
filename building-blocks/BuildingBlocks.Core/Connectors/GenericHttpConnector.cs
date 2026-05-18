using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BuildingBlocks.Abstractions.Connectors;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Core.Connectors;

public class GenericHttpConnector : IConnector
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    public string ConnectorType => "generic-http";

    public GenericHttpConnector(IHttpClientFactory httpClientFactory, ILogger logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ConnectorResponse> ExecuteAsync(
        ConnectorRequest context, CancellationToken cancellationToken = default)
    {
        var config = context.Config
            ?? throw new ArgumentNullException(nameof(context.Config));

        var operation = config.Operations.FirstOrDefault(o => o.Name == context.Operation)
            ?? throw new InvalidOperationException(
                $"Operation '{context.Operation}' not found in config '{config.Id}'");

        var sw = Stopwatch.StartNew();
        var client = _httpClientFactory.CreateClient("integration-connector");

        try
        {
            var url = $"{config.Endpoint.BaseUrl.TrimEnd('/')}/{operation.Path.TrimStart('/')}";
            using var request = BuildHttpRequest(operation, url, context);

            await ApplyAuthentication(request, config.Authentication);

            foreach (var header in config.Endpoint.DefaultHeaders)
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            foreach (var header in context.Headers)
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);

            request.Headers.TryAddWithoutValidation("X-Correlation-Id", context.CorrelationId);

            _logger.LogInformation(
                "Connector executing {Method} {Url} | Config={ConfigId} Op={Op} CorrelationId={CorrId}",
                operation.HttpMethod, url, config.Id, operation.Name, context.CorrelationId);

            using var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            sw.Stop();

            _logger.LogInformation(
                "Connector response {StatusCode} in {Ms}ms | Config={ConfigId} CorrelationId={CorrId}",
                (int)response.StatusCode, sw.ElapsedMilliseconds, config.Id, context.CorrelationId);

            return new ConnectorResponse
            {
                IsSuccess = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                RawResponse = responseBody,
                Data = ParseJsonResponse(responseBody),
                ErrorMessage = response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}",
                ErrorCode = response.IsSuccessStatusCode ? null : $"HTTP_{(int)response.StatusCode}",
                ExecutionTime = sw.Elapsed
            };
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            sw.Stop();
            _logger.LogWarning("Request cancelled | Config={ConfigId} Op={Op} after {Ms}ms",
                config.Id, context.Operation, sw.ElapsedMilliseconds);
            return new ConnectorResponse
            {
                IsSuccess = false, StatusCode = 408,
                ErrorMessage = "Request was cancelled", ErrorCode = "CANCELLED",
                ExecutionTime = sw.Elapsed
            };
        }
        catch (TaskCanceledException)
        {
            sw.Stop();
            _logger.LogWarning("Request timed out | Config={ConfigId} Op={Op} after {Ms}ms",
                config.Id, context.Operation, sw.ElapsedMilliseconds);
            return new ConnectorResponse
            {
                IsSuccess = false, StatusCode = 408,
                ErrorMessage = "Request timed out", ErrorCode = "TIMEOUT",
                ExecutionTime = sw.Elapsed
            };
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _logger.LogError(ex, "HTTP error | Config={ConfigId} Op={Op}", config.Id, context.Operation);
            return new ConnectorResponse
            {
                IsSuccess = false, StatusCode = 0,
                ErrorMessage = ex.Message, ErrorCode = "HTTP_ERROR",
                ExecutionTime = sw.Elapsed
            };
        }
    }

    public Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    private static HttpRequestMessage BuildHttpRequest(
        OperationConfig operation, string url, ConnectorRequest context)
    {
        var method = new HttpMethod(operation.HttpMethod.ToUpperInvariant());
        var request = new HttpRequestMessage(method, url);

        if (method != HttpMethod.Get && context.Payload.Count > 0)
        {
            var json = JsonSerializer.Serialize(context.Payload);
            request.Content = new StringContent(json, Encoding.UTF8, operation.ContentType);
        }

        return request;
    }

    private async Task ApplyAuthentication(HttpRequestMessage request, AuthConfig auth)
    {
        switch (auth.Type.ToLowerInvariant())
        {
            case "apikey":
                var headerName = auth.Parameters.GetValueOrDefault("HeaderName", "X-API-Key");
                request.Headers.TryAddWithoutValidation(headerName,
                    auth.Parameters.GetValueOrDefault("ApiKey", ""));
                break;

            case "basicauth":
                var creds = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                    $"{auth.Parameters.GetValueOrDefault("Username", "")}:{auth.Parameters.GetValueOrDefault("Password", "")}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", creds);
                break;

            case "bearertoken":
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
                    auth.Parameters.GetValueOrDefault("Token", ""));
                break;

            case "oauth2":
                var token = await GetOAuth2Token(auth.Parameters);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                break;
        }
    }

    private async Task<string> GetOAuth2Token(Dictionary<string, string> parameters)
    {
        using var client = _httpClientFactory.CreateClient("oauth2");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = parameters.GetValueOrDefault("ClientId", ""),
            ["client_secret"] = parameters.GetValueOrDefault("ClientSecret", "")
        });

        var response = await client.PostAsync(parameters.GetValueOrDefault("TokenUrl", ""), content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json).GetProperty("access_token").GetString() ?? "";
    }

    /// <summary>
    /// Parse JSON response — top-level primitives unwrapped, nested objects kept as JsonElement
    /// for dot-notation navigation in MappingEngine.
    /// </summary>
    private static Dictionary<string, object> ParseJsonResponse(string content)
    {
        try
        {
            var doc = JsonDocument.Parse(content);
            var result = new Dictionary<string, object>();
            if (doc.RootElement.ValueKind != JsonValueKind.Object) 
                return new() { ["raw"] = content };

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                result[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => (object)(prop.Value.GetString() ?? ""),
                    JsonValueKind.Number => prop.Value.TryGetInt64(out var l) ? l : prop.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => "",
                    _ => prop.Value // nested objects/arrays: keep as JsonElement
                };
            }
            return result;
        }
        catch
        {
            return new() { ["raw"] = content };
        }
    }
}
