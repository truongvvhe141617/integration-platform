using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace ConfigService.Api.Controllers;

/// <summary>
/// Mock API giả lập third-party.
/// Response dựa trên request data gửi lên (không fix cứng).
/// Production: đổi config baseUrl sang API thật, xóa mock này.
/// </summary>
[ApiController]
[Route("mock")]
public class MockApiController : ControllerBase
{
    [HttpPost("bank-b/v1/fund-transfer")]
    public IActionResult BankBTransfer([FromBody] JsonElement request)
    {
        var amount = request.TryGetProperty("txn_amount", out var a) ? a.ToString() : "0";
        var srcAcct = request.TryGetProperty("src_acct_no", out var s) ? s.GetString() : "";
        var destAcct = request.TryGetProperty("dest_acct_no", out var d) ? d.GetString() : "";
        var partnerRef = request.TryGetProperty("partner_ref_no", out var p) ? p.GetString() : "";

        return Ok(new
        {
            result_code = "00",
            result_msg = "Transaction successful",
            data = new
            {
                bank_ref_no = $"BANKB-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(10000, 99999)}",
                txn_date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                fee = 11000,
                status = "COMPLETED",
                // Echo lại data từ request
                src_acct_no = srcAcct,
                dest_acct_no = destAcct,
                txn_amount = amount,
                partner_ref_no = partnerRef
            }
        });
    }

    [HttpPost("bank-b/v1/balance")]
    public IActionResult BankBBalance([FromBody] JsonElement request)
    {
        var acctNo = request.TryGetProperty("acct_no", out var a) ? a.GetString() : "unknown";
        var balance = Random.Shared.Next(5000000, 50000000);

        return Ok(new
        {
            result_code = "00",
            result_msg = "Success",
            data = new
            {
                avail_bal = balance,
                current_bal = balance + 250000,
                currency = "VND",
                acct_no = acctNo,
                acct_name = "NGUYEN VAN A"
            }
        });
    }

    [HttpPost("bank-c/api/transfer")]
    public IActionResult BankCTransfer([FromBody] JsonElement request)
    {
        var amount = request.TryGetProperty("transfer_amount", out var a) ? a.ToString() : "0";
        var sender = request.TryGetProperty("sender_account", out var s) ? s.GetString() : "";
        var receiver = request.TryGetProperty("receiver_account", out var r) ? r.GetString() : "";

        return Ok(new
        {
            code = "SUCCESS",
            message = "Transfer completed successfully",
            transaction_id = $"BANKC-TXN-{Random.Shared.Next(100000, 999999)}",
            processed_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            fee_amount = 8800,
            sender_account = sender,
            receiver_account = receiver,
            transfer_amount = amount
        });
    }

    [HttpPost("ewallet/api/v2/payment")]
    public IActionResult EWalletPayment([FromBody] JsonElement request)
    {
        var orderId = request.TryGetProperty("partner_ref_id", out var o) ? o.GetString() : "";
        var amount = request.TryGetProperty("amount", out var a) ? a.ToString() : "0";
        var phone = request.TryGetProperty("customer_phone", out var p) ? p.GetString() : "";
        var transId = $"WALLET-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}";

        return Ok(new
        {
            resultCode = "0",
            message = "Success",
            transId,
            payUrl = $"https://payment.ewallet.vn/pay/{transId}",
            amount,
            partner_ref_id = orderId,
            customer_phone = phone,
            created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        });
    }

    [HttpPost("sms/api/send")]
    public IActionResult SmsSend([FromBody] JsonElement request)
    {
        var phone = request.TryGetProperty("phone", out var p) ? p.GetString() : "";
        var content = request.TryGetProperty("content", out var c) ? c.GetString() : "";
        var brand = request.TryGetProperty("brand", out var b) ? b.GetString() : "DEFAULT";

        return Ok(new
        {
            status = "SENT",
            message_id = $"SMS-{Random.Shared.Next(100000, 999999)}",
            phone,
            content,
            brand,
            sent_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        });
    }

    [HttpPost("kyc/api/v1/verify")]
    public IActionResult KycVerify([FromBody] JsonElement request)
    {
        var idCard = request.TryGetProperty("id_card_number", out var i) ? i.GetString() : "";
        var name = request.TryGetProperty("full_name", out var n) ? n.GetString() : "";
        var dob = request.TryGetProperty("dob", out var d) ? d.GetString() : "";

        return Ok(new
        {
            verified = true,
            score = 95.5,
            full_name = name,
            id_number = idCard?[..Math.Min(10, idCard.Length)] + "**",
            dob,
            verified_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        });
    }

    // ═══ CASE 2: Bank E — yêu cầu signature ═══

    [HttpPost("bank-e/partner/v1/transfer")]
    public IActionResult BankETransfer([FromBody] JsonElement request)
    {
        if (!Request.Headers.ContainsKey("X-Signature"))
            return BadRequest(new { error_code = "INVALID_SIGNATURE", error_msg = "Missing X-Signature header" });
        if (!Request.Headers.ContainsKey("X-Merchant-Id"))
            return BadRequest(new { error_code = "INVALID_MERCHANT", error_msg = "Missing X-Merchant-Id header" });

        var amount = request.TryGetProperty("transfer_amount", out var a) ? a.ToString() : "0";
        var debit = request.TryGetProperty("debit_account", out var d) ? d.GetString() : "";
        var credit = request.TryGetProperty("credit_account", out var c) ? c.GetString() : "";
        var txnRef = $"BANKE-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(10000, 99999)}";

        return Ok(new
        {
            response_code = "000",
            response_message = "Approved",
            transaction_ref = txnRef,
            bank_trace_no = $"TRACE-{Random.Shared.Next(1000000, 9999999)}",
            transaction_date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            fee = 15000,
            debit_account = debit,
            credit_account = credit,
            transfer_amount = amount,
            merchant_id = Request.Headers["X-Merchant-Id"].ToString(),
            signature_verified = true
        });
    }
}
