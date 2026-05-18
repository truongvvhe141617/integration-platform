using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingBlocks.Abstractions.Connectors;

namespace Connector.BankE;

/// <summary>
/// Case 2: Custom DLL Connector cho Bank E.
/// Bank E yêu cầu HMAC-SHA256 signature + Merchant ID header.
/// GenericHttpConnector không làm được → cần custom connector.
/// </summary>
public class BankEConnector : IConnector
{
    public string ConnectorType => "bank-e";

    public async Task<ConnectorResponse> ExecuteAsync(
        ConnectorRequest context, CancellationToken cancellationToken = default)
    {
        var config = context.Config!;
        var operation = config.Operations.First(o => o.Name == context.Operation);
        var sw = Stopwatch.StartNew();

        try
        {
            var url = $"{config.Endpoint.BaseUrl.TrimEnd('/')}/{operation.Path.TrimStart('/')}";
            var body = JsonSerializer.Serialize(context.Payload);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            // Custom: HMAC-SHA256 signature
            var secretKey = config.Authentication.Parameters.GetValueOrDefault("SecretKey", "");
            var merchantId = config.Authentication.Parameters.GetValueOrDefault("MerchantId", "");
            var signature = GenerateHmacSha256($"{merchantId}|{timestamp}|{body}", secretKey);

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(config.TimeoutMs) };
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            // Custom headers Bank E yêu cầu
            request.Headers.TryAddWithoutValidation("X-Signature", signature);
            request.Headers.TryAddWithoutValidation("X-Merchant-Id", merchantId);
            request.Headers.TryAddWithoutValidation("X-Timestamp", timestamp);
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", context.CorrelationId);

            foreach (var h in config.Endpoint.DefaultHeaders)
                request.Headers.TryAddWithoutValidation(h.Key, h.Value);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
                return new ConnectorResponse
                {
                    IsSuccess = false, StatusCode = (int)response.StatusCode,
                    RawResponse = responseBody,
                    ErrorMessage = $"Bank E returned HTTP {(int)response.StatusCode}",
                    ErrorCode = "BANK_E_ERROR", ExecutionTime = sw.Elapsed
                };

            var data = ParseResponse(responseBody);
            return new ConnectorResponse
            {
                IsSuccess = true, StatusCode = (int)response.StatusCode,
                Data = data, RawResponse = responseBody, ExecutionTime = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new ConnectorResponse
            {
                IsSuccess = false, StatusCode = 0,
                ErrorMessage = $"Bank E connector error: {ex.Message}",
                ErrorCode = "BANK_E_CONNECTOR_ERROR", ExecutionTime = sw.Elapsed
            };
        }
    }

    public Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    private static string GenerateHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
    }

    private static Dictionary<string, object> ParseResponse(string json)
    {
        var result = new Dictionary<string, object>();
        var doc = JsonDocument.Parse(json);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            result[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.String => (object)(prop.Value.GetString() ?? ""),
                JsonValueKind.Number => prop.Value.TryGetInt64(out var l) ? l : prop.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => prop.Value.ToString()
            };
        }
        return result;
    }
}
