using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingBlocks.Abstractions.Connectors;

namespace Connector.SampleBank;

/// <summary>
/// Ví dụ Custom DLL Connector cho một ngân hàng có API phức tạp.
/// 
/// Khi nào cần custom connector:
/// - API yêu cầu signature/encryption đặc biệt
/// - Protocol không phải REST chuẩn (SOAP, ISO 8583, ...)
/// - Logic xử lý phức tạp không thể config-driven
/// - Cần transform data phức tạp
/// 
/// Build: dotnet build → copy DLL vào thư mục plugins/
/// Integration Service sẽ tự động load DLL runtime.
/// </summary>
public class SampleBankConnector : IConnector
{
    public string ConnectorType => "sample-bank";

    public async Task<ConnectorResponse> ExecuteAsync(
        ConnectorRequest context,
        CancellationToken cancellationToken = default)
    {
        var config = context.Config!;
        var operation = config.Operations.First(o => o.Name == context.Operation);

        // 1. Build request với signature đặc biệt của bank
        var requestBody = BuildSignedRequest(context.Payload, config.Authentication);

        // 2. Gọi API
        using var httpClient = new HttpClient();
        var url = $"{config.Endpoint.BaseUrl.TrimEnd('/')}/{operation.Path.TrimStart('/')}";

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        // Apply custom headers
        httpRequest.Headers.TryAddWithoutValidation("X-Bank-Signature",
            GenerateSignature(requestBody, config.Authentication.Parameters));
        httpRequest.Headers.TryAddWithoutValidation("X-Correlation-Id", context.CorrelationId);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        sw.Stop();

        // 3. Parse và validate response
        if (!response.IsSuccessStatusCode)
        {
            return new ConnectorResponse
            {
                IsSuccess = false,
                StatusCode = (int)response.StatusCode,
                RawResponse = responseBody,
                ErrorMessage = $"Bank API returned {response.StatusCode}",
                ErrorCode = "BANK_ERROR",
                ExecutionTime = sw.Elapsed
            };
        }

        // 4. Verify response signature (bank-specific logic)
        var bankResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
        var data = ParseBankResponse(bankResponse);

        return new ConnectorResponse
        {
            IsSuccess = true,
            StatusCode = (int)response.StatusCode,
            Data = data,
            RawResponse = responseBody,
            ExecutionTime = sw.Elapsed
        };
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        // Implement bank-specific health check
        return await Task.FromResult(true);
    }

    /// <summary>
    /// Build request body với format đặc biệt của bank.
    /// </summary>
    private string BuildSignedRequest(Dictionary<string, object> payload, AuthConfig auth)
    {
        var request = new
        {
            merchantId = auth.Parameters.GetValueOrDefault("MerchantId", ""),
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            data = payload
        };
        return JsonSerializer.Serialize(request);
    }

    /// <summary>
    /// Generate HMAC-SHA256 signature (bank-specific).
    /// </summary>
    private string GenerateSignature(string body, Dictionary<string, string> authParams)
    {
        var secretKey = authParams.GetValueOrDefault("SecretKey", "");
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Parse bank-specific response format.
    /// </summary>
    private Dictionary<string, object> ParseBankResponse(JsonElement response)
    {
        var result = new Dictionary<string, object>();

        if (response.TryGetProperty("resultCode", out var code))
            result["resultCode"] = code.GetString() ?? "";

        if (response.TryGetProperty("resultMessage", out var msg))
            result["resultMessage"] = msg.GetString() ?? "";

        if (response.TryGetProperty("data", out var data))
            result["data"] = data.ToString();

        return result;
    }
}
