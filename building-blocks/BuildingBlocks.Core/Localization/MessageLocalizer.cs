using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Core.Localization;

/// <summary>
/// Biên dịch message đa ngôn ngữ dựa trên error code.
/// 
/// Cách dùng:
///   var msg = localizer.Get("INT_CFG_001", "vi");  // "Không tìm thấy cấu hình connector"
///   var msg = localizer.Get("INT_CFG_001", "en");  // "Connector config not found"
///   var msg = localizer.Get("INT_CFG_001", "ja");  // "コネクタ設定が見つかりません"
/// 
/// Hỗ trợ placeholder:
///   var msg = localizer.Get("INT_VAL_002", "vi", "fromAccount");
///   // "Trường 'fromAccount' là bắt buộc"
/// 
/// Load từ JSON files: locales/vi.json, locales/en.json, ...
/// Hoặc dùng built-in defaults.
/// </summary>
public interface IMessageLocalizer
{
    string Get(string errorCode, string lang = "en", params object[] args);
    IReadOnlyDictionary<string, string> GetAllForLang(string lang);
}

public class MessageLocalizer : IMessageLocalizer
{
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _messages = new();
    private readonly ILogger<MessageLocalizer> _logger;
    private const string DefaultLang = "en";

    public MessageLocalizer(ILogger<MessageLocalizer> logger, string? localeDirectory = null)
    {
        _logger = logger;

        // Load built-in defaults
        LoadBuiltInMessages();

        // Load từ JSON files nếu có
        var dir = localeDirectory ?? Path.Combine(AppContext.BaseDirectory, "locales");
        if (Directory.Exists(dir))
            LoadFromDirectory(dir);
    }

    public string Get(string errorCode, string lang = "en", params object[] args)
    {
        // Tìm theo lang → fallback en → fallback code
        if (_messages.TryGetValue(lang, out var langMessages) && langMessages.TryGetValue(errorCode, out var msg))
            return args.Length > 0 ? string.Format(msg, args) : msg;

        if (lang != DefaultLang && _messages.TryGetValue(DefaultLang, out var defaultMessages) && defaultMessages.TryGetValue(errorCode, out var defaultMsg))
            return args.Length > 0 ? string.Format(defaultMsg, args) : defaultMsg;

        return errorCode; // Fallback: trả code nếu không tìm thấy message
    }

    public IReadOnlyDictionary<string, string> GetAllForLang(string lang)
    {
        return _messages.TryGetValue(lang, out var messages) ? messages : new Dictionary<string, string>();
    }

    private void LoadFromDirectory(string directory)
    {
        foreach (var file in Directory.GetFiles(directory, "*.json"))
        {
            try
            {
                var lang = Path.GetFileNameWithoutExtension(file); // vi.json → "vi"
                var json = File.ReadAllText(file);
                var messages = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (messages != null)
                {
                    _messages.AddOrUpdate(lang, messages, (_, existing) =>
                    {
                        foreach (var kv in messages) existing[kv.Key] = kv.Value;
                        return existing;
                    });
                    _logger.LogInformation("Loaded locale: {Lang} ({Count} messages)", lang, messages.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load locale file: {File}", file);
            }
        }
    }

    private void LoadBuiltInMessages()
    {
        // ═══ English ═══
        _messages["en"] = new Dictionary<string, string>
        {
            // Integration
            ["INT_CFG_001"] = "Connector config '{0}' not found",
            ["INT_CFG_002"] = "Connector config '{0}' is not active (status: {1})",
            ["INT_CFG_003"] = "Operation '{0}' not found in config '{1}'",
            ["INT_VAL_001"] = "Request validation failed",
            ["INT_VAL_002"] = "Field '{0}' is required",
            ["INT_VAL_003"] = "Field '{0}' has invalid format",
            ["INT_NET_001"] = "HTTP error connecting to third-party: {0}",
            ["INT_NET_002"] = "Connection refused to {0}",
            ["INT_NET_003"] = "DNS resolution failed for {0}",
            ["INT_TMO_001"] = "Request timed out after {0}ms",
            ["INT_TMO_002"] = "Request was cancelled",
            ["INT_BIZ_001"] = "Third-party returned error: {0}",
            ["INT_BIZ_002"] = "Duplicate request (idempotency key: {0})",
            ["INT_SYS_001"] = "Internal server error",
            ["INT_SYS_002"] = "Failed to load connector '{0}': {1}",
            // Connector
            ["CON_NET_001"] = "HTTP {0} from third-party",
            ["CON_AUTH_001"] = "Authentication failed for connector '{0}'",
            ["CON_AUTH_002"] = "Invalid signature",
            ["CON_BIZ_001"] = "Third-party business error: {0}",
            ["CON_SYS_001"] = "Connector internal error: {0}",
            ["CON_TMO_001"] = "Connector timeout after {0}ms",
            // Config
            ["CFG_VAL_001"] = "Config schema validation failed: {0}",
            ["CFG_VAL_002"] = "Invalid status transition: {0} → {1}",
            ["CFG_BIZ_001"] = "Config '{0}' not found",
            ["CFG_BIZ_002"] = "Version {0} not found for config '{1}'",
            // Messaging
            ["MSG_NET_001"] = "RabbitMQ connection failed: {0}",
            ["MSG_BIZ_001"] = "Failed to publish message: {0}",
            // Gateway
            ["GW_AUTH_001"] = "Unauthorized access",
            ["GW_AUTH_002"] = "Token expired",
            ["GW_TMO_001"] = "Rate limit exceeded. Try again later.",
            // System
            ["SYS_ERR_001"] = "An unexpected error occurred",
            ["SYS_ERR_002"] = "Service temporarily unavailable"
        };

        // ═══ Tiếng Việt ═══
        _messages["vi"] = new Dictionary<string, string>
        {
            ["INT_CFG_001"] = "Không tìm thấy cấu hình connector '{0}'",
            ["INT_CFG_002"] = "Cấu hình connector '{0}' không hoạt động (trạng thái: {1})",
            ["INT_CFG_003"] = "Không tìm thấy operation '{0}' trong cấu hình '{1}'",
            ["INT_VAL_001"] = "Dữ liệu yêu cầu không hợp lệ",
            ["INT_VAL_002"] = "Trường '{0}' là bắt buộc",
            ["INT_VAL_003"] = "Trường '{0}' không đúng định dạng",
            ["INT_NET_001"] = "Lỗi kết nối HTTP tới hệ thống bên thứ 3: {0}",
            ["INT_NET_002"] = "Không thể kết nối tới {0}",
            ["INT_NET_003"] = "Không thể phân giải tên miền {0}",
            ["INT_TMO_001"] = "Yêu cầu hết thời gian chờ sau {0}ms",
            ["INT_TMO_002"] = "Yêu cầu đã bị hủy",
            ["INT_BIZ_001"] = "Hệ thống bên thứ 3 trả về lỗi: {0}",
            ["INT_BIZ_002"] = "Yêu cầu trùng lặp (mã idempotency: {0})",
            ["INT_SYS_001"] = "Lỗi hệ thống nội bộ",
            ["INT_SYS_002"] = "Không thể tải connector '{0}': {1}",
            ["CON_NET_001"] = "HTTP {0} từ hệ thống bên thứ 3",
            ["CON_AUTH_001"] = "Xác thực thất bại cho connector '{0}'",
            ["CON_AUTH_002"] = "Chữ ký không hợp lệ",
            ["CON_BIZ_001"] = "Lỗi nghiệp vụ từ bên thứ 3: {0}",
            ["CON_SYS_001"] = "Lỗi nội bộ connector: {0}",
            ["CON_TMO_001"] = "Connector hết thời gian chờ sau {0}ms",
            ["CFG_VAL_001"] = "Cấu hình không hợp lệ: {0}",
            ["CFG_VAL_002"] = "Chuyển trạng thái không hợp lệ: {0} → {1}",
            ["CFG_BIZ_001"] = "Không tìm thấy cấu hình '{0}'",
            ["CFG_BIZ_002"] = "Không tìm thấy phiên bản {0} của cấu hình '{1}'",
            ["MSG_NET_001"] = "Không thể kết nối RabbitMQ: {0}",
            ["MSG_BIZ_001"] = "Không thể gửi message: {0}",
            ["GW_AUTH_001"] = "Truy cập không được phép",
            ["GW_AUTH_002"] = "Token đã hết hạn",
            ["GW_TMO_001"] = "Vượt quá giới hạn truy cập. Vui lòng thử lại sau.",
            ["SYS_ERR_001"] = "Đã xảy ra lỗi không mong muốn",
            ["SYS_ERR_002"] = "Dịch vụ tạm thời không khả dụng"
        };

        // ═══ 日本語 ═══
        _messages["ja"] = new Dictionary<string, string>
        {
            ["INT_CFG_001"] = "コネクタ設定 '{0}' が見つかりません",
            ["INT_CFG_002"] = "コネクタ設定 '{0}' は無効です（状態: {1}）",
            ["INT_CFG_003"] = "設定 '{1}' にオペレーション '{0}' が見つかりません",
            ["INT_VAL_001"] = "リクエストの検証に失敗しました",
            ["INT_VAL_002"] = "フィールド '{0}' は必須です",
            ["INT_TMO_001"] = "リクエストが {0}ms 後にタイムアウトしました",
            ["INT_SYS_001"] = "内部サーバーエラー",
            ["SYS_ERR_001"] = "予期しないエラーが発生しました",
            ["SYS_ERR_002"] = "サービスは一時的に利用できません"
        };

        // ═══ 中文 ═══
        _messages["zh"] = new Dictionary<string, string>
        {
            ["INT_CFG_001"] = "未找到连接器配置 '{0}'",
            ["INT_CFG_002"] = "连接器配置 '{0}' 未激活（状态: {1}）",
            ["INT_VAL_001"] = "请求验证失败",
            ["INT_VAL_002"] = "字段 '{0}' 为必填项",
            ["INT_TMO_001"] = "请求在 {0}ms 后超时",
            ["INT_SYS_001"] = "内部服务器错误",
            ["SYS_ERR_001"] = "发生意外错误",
            ["SYS_ERR_002"] = "服务暂时不可用"
        };
    }
}
