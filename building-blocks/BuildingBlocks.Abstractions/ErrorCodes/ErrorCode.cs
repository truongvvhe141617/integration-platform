namespace BuildingBlocks.Abstractions.ErrorCodes;

/// <summary>
/// Quy hoạch mã lỗi + HTTP status code.
/// Mỗi error code map 1:1 với HTTP status.
/// </summary>
public static class ErrorCodes
{
    // ═══ 400 Bad Request ═══
    public const string INT_VAL_001 = "INT_VAL_001";  // Request validation failed
    public const string INT_VAL_002 = "INT_VAL_002";  // Missing required field
    public const string INT_VAL_003 = "INT_VAL_003";  // Invalid field format
    public const string INT_CFG_002 = "INT_CFG_002";  // Config inactive
    public const string INT_CFG_003 = "INT_CFG_003";  // Operation not found in config
    public const string CFG_VAL_001 = "CFG_VAL_001";  // Config schema validation failed
    public const string CFG_VAL_002 = "CFG_VAL_002";  // Invalid status transition

    // ═══ 401 Unauthorized ═══
    public const string GW_AUTH_001 = "GW_AUTH_001";   // Unauthorized
    public const string GW_AUTH_002 = "GW_AUTH_002";   // Token expired
    public const string CON_AUTH_001 = "CON_AUTH_001"; // Third-party auth failed
    public const string CON_AUTH_002 = "CON_AUTH_002"; // Invalid signature

    // ═══ 404 Not Found ═══
    public const string INT_CFG_001 = "INT_CFG_001";  // Config not found
    public const string CFG_BIZ_001 = "CFG_BIZ_001";  // Config not found
    public const string CFG_BIZ_002 = "CFG_BIZ_002";  // Version not found

    // ═══ 408 Request Timeout ═══
    public const string INT_TMO_001 = "INT_TMO_001";  // Request timeout
    public const string INT_TMO_002 = "INT_TMO_002";  // Request cancelled
    public const string CON_TMO_001 = "CON_TMO_001";  // Connector timeout

    // ═══ 409 Conflict ═══
    public const string INT_BIZ_002 = "INT_BIZ_002";  // Idempotency duplicate

    // ═══ 429 Too Many Requests ═══
    public const string GW_TMO_001 = "GW_TMO_001";    // Rate limit exceeded

    // ═══ 500 Internal Server Error ═══
    public const string INT_SYS_001 = "INT_SYS_001";  // Internal server error
    public const string INT_SYS_002 = "INT_SYS_002";  // Connector load failed
    public const string CON_SYS_001 = "CON_SYS_001";  // Connector internal error
    public const string SYS_ERR_001 = "SYS_ERR_001";  // Unhandled exception

    // ═══ 502 Bad Gateway ═══
    public const string INT_NET_001 = "INT_NET_001";  // HTTP error from third-party
    public const string INT_NET_002 = "INT_NET_002";  // Connection refused
    public const string INT_NET_003 = "INT_NET_003";  // DNS resolution failed
    public const string INT_BIZ_001 = "INT_BIZ_001";  // Third-party returned error
    public const string CON_NET_001 = "CON_NET_001";  // HTTP error from third-party
    public const string CON_NET_002 = "CON_NET_002";  // SSL/TLS error
    public const string CON_BIZ_001 = "CON_BIZ_001";  // Third-party business error

    // ═══ 503 Service Unavailable ═══
    public const string SYS_ERR_002 = "SYS_ERR_002";  // Service unavailable
    public const string MSG_NET_001 = "MSG_NET_001";  // RabbitMQ connection failed
    public const string MSG_BIZ_001 = "MSG_BIZ_001";  // Message publish failed

    /// <summary>
    /// Map error code → HTTP status code.
    /// </summary>
    public static int ToHttpStatus(string errorCode)
    {
        return errorCode switch
        {
            // 400
            INT_VAL_001 or INT_VAL_002 or INT_VAL_003
            or INT_CFG_002 or INT_CFG_003
            or CFG_VAL_001 or CFG_VAL_002 => 400,

            // 401
            GW_AUTH_001 or GW_AUTH_002
            or CON_AUTH_001 or CON_AUTH_002 => 401,

            // 404
            INT_CFG_001 or CFG_BIZ_001 or CFG_BIZ_002 => 404,

            // 408
            INT_TMO_001 or INT_TMO_002 or CON_TMO_001 => 408,

            // 409
            INT_BIZ_002 => 409,

            // 429
            GW_TMO_001 => 429,

            // 500
            INT_SYS_001 or INT_SYS_002
            or CON_SYS_001 or SYS_ERR_001 => 500,

            // 502
            INT_NET_001 or INT_NET_002 or INT_NET_003
            or INT_BIZ_001 or CON_NET_001 or CON_NET_002
            or CON_BIZ_001 => 502,

            // 503
            SYS_ERR_002 or MSG_NET_001 or MSG_BIZ_001 => 503,

            _ => 500
        };
    }
}
