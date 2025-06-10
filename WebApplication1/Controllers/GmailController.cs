using EmailWebApi.Api.Services;
using EmailWebApi.Api.Services;
using Google.Apis.Auth.OAuth2; // Cần cho UserCredential
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration; // Cần cho IConfiguration
using System;
using System.Collections.Generic;
using System.IO; // Cần cho Path và Directory (trong logging)
using System.Linq; // Cần cho .Any()
using System.Threading.Tasks;

namespace EmailWebApi.Api.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class GmailController : ControllerBase
    {
        private readonly IGmailApiService _gmailService;
        private readonly IConfiguration _configuration;

        // QUAN TRỌNG: Sử dụng một userIdentifier CỐ ĐỊNH và NHẤT QUÁN cho TẤT CẢ các thao tác
        // liên quan đến Gmail của một "phiên làm việc" hoặc một "người dùng ứng dụng" cụ thể.
        // Trong ứng dụng thực tế, bạn sẽ có cơ chế để lấy ID này dựa trên người dùng đã đăng nhập vào ứng dụng của bạn.
        // Để gỡ lỗi, chúng ta dùng một giá trị cố định.
        private const string AppUserIdentifierForGmail = "my_unique_fixed_gmail_user_id_123";

        private string GetConfiguredRedirectUri() => _configuration["GoogleApi:RedirectUri"] ?? "urn:ietf:wg:oauth:2.0:oob";

        public GmailController(IGmailApiService gmailService, IConfiguration configuration)
        {
            _gmailService = gmailService;
            _configuration = configuration;
        }

        [HttpGet("auth/url")]
        public ActionResult<object> GetAuthorizationUrl()
        {
            string frontendRedirectUri = GetConfiguredRedirectUri();
            Console.WriteLine($"[GmailController.GetAuthorizationUrl] Frontend Redirect URI được sử dụng: {frontendRedirectUri}");
            if (frontendRedirectUri == "urn:ietf:wg:oauth:2.0:oob" && !(Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || Request.Host.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine("[GmailController.GetAuthorizationUrl] Cảnh báo: Sử dụng redirect URI mặc định 'urn:ietf:wg:oauth:2.0:oob'.");
            }
            try
            {
                string authUrl = _gmailService.GetAuthorizationUrl(frontendRedirectUri);
                return Ok(new { authorizationUrl = authUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GmailController.GetAuthorizationUrl] Lỗi khi tạo URL xác thực Google: {ex.ToString()}");
                return StatusCode(500, "Không thể tạo URL xác thực. Vui lòng kiểm tra cấu hình server (ví dụ: credentials.json).");
            }
        }

        public class OAuthCallbackDto
        {
            public string Code { get; set; }
        }

        [HttpPost("auth/callback")]
        public async Task<IActionResult> HandleOAuthCallback([FromBody] OAuthCallbackDto callbackDto)
        {
            if (string.IsNullOrEmpty(callbackDto?.Code))
            {
                Console.WriteLine("[GmailController.HandleOAuthCallback] Lỗi: Authorization code rỗng.");
                return BadRequest("Authorization code không được để trống.");
            }
            Console.WriteLine($"[GmailController.HandleOAuthCallback] Nhận được authorization_code (bắt đầu bằng): {callbackDto.Code.Substring(0, Math.Min(10, callbackDto.Code.Length))}...");
            try
            {
                string frontendRedirectUri = GetConfiguredRedirectUri();
                Console.WriteLine($"[GmailController.HandleOAuthCallback] Sẽ sử dụng Redirect URI: {frontendRedirectUri} và userIdentifier: {AppUserIdentifierForGmail} để đổi code.");

                // Sử dụng AppUserIdentifierForGmail NHẤT QUÁN ở đây
                UserCredential credential = await _gmailService.ExchangeCodeForTokenAsync(callbackDto.Code, frontendRedirectUri, AppUserIdentifierForGmail);

                Console.WriteLine($"[GmailController.HandleOAuthCallback] Đã đổi code thành công cho userIdentifier: {AppUserIdentifierForGmail}. AccessToken: {(string.IsNullOrEmpty(credential.Token.AccessToken) ? "NULL" : "CÓ")}, RefreshToken: {(string.IsNullOrEmpty(credential.Token.RefreshToken) ? "KHÔNG CÓ MỚI (có thể đã có và được load từ trước)" : "CÓ MỚI")}");

                // Kiểm tra xem file token đã được tạo chưa
                string expectedTokenPathDir = Path.Combine("Tokens_GmailApi", AppUserIdentifierForGmail);
                string expectedTokenFile = Path.Combine(expectedTokenPathDir, $"Google.Apis.Auth.OAuth2.Responses.TokenResponse-{AppUserIdentifierForGmail}"); // Tên file chuẩn của FileDataStore

                Console.WriteLine($"[GmailController.HandleOAuthCallback] Kiểm tra sự tồn tại của thư mục token: {Path.GetFullPath(expectedTokenPathDir)}");
                Console.WriteLine($"[GmailController.HandleOAuthCallback] Thư mục token có tồn tại không? {Directory.Exists(expectedTokenPathDir)}");
                Console.WriteLine($"[GmailController.HandleOAuthCallback] Kiểm tra sự tồn tại của file token: {Path.GetFullPath(expectedTokenFile)}");

                if (System.IO.File.Exists(expectedTokenFile))
                {
                    Console.WriteLine($"[GmailController.HandleOAuthCallback] KIỂM TRA THÀNH CÔNG: File token ĐÃ được tạo/cập nhật tại: {Path.GetFullPath(expectedTokenFile)}");
                }
                else
                {
                    Console.WriteLine($"[GmailController.HandleOAuthCallback] CẢNH BÁO: File token KHÔNG được tìm thấy tại: {Path.GetFullPath(expectedTokenFile)} sau khi ExchangeCodeForTokenAsync. Điều này cho thấy FileDataStore có thể chưa lưu token đúng cách hoặc có vấn đề với userIdentifier.");
                }

                return Ok(new { message = $"Kết nối Gmail thành công cho người dùng {AppUserIdentifierForGmail}! Token đã được lưu trữ/cập nhật." });
            }
            catch (Google.Apis.Auth.OAuth2.Responses.TokenResponseException tex)
            {
                Console.WriteLine($"[GmailController.HandleOAuthCallback] Lỗi TokenResponseException khi đổi code cho {AppUserIdentifierForGmail}: {tex.Error.ErrorDescription}. Chi tiết: {tex.ToString()}");
                return BadRequest($"Lỗi từ Google khi xác thực: {tex.Error.ErrorDescription ?? tex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GmailController.HandleOAuthCallback] Lỗi Exception trong quá trình callback OAuth cho {AppUserIdentifierForGmail}: {ex.ToString()}");
                return StatusCode(500, "Đã xảy ra lỗi trong quá trình xử lý xác thực.");
            }
        }
        public class PaginatedGmailMessagesDto
        {
            public List<MessageDataDto> Emails { get; set; }
            public string NextPageToken { get; set; }
        }
        [HttpGet("messages")]
        public async Task<ActionResult<PaginatedGmailMessagesDto>> GetGmailMessages([FromQuery] string labelIds = "INBOX", [FromQuery] uint maxResults = 15, [FromQuery] string pageToken = null)
        {
            Console.WriteLine($"[GetGmailMessages] Yêu cầu lấy messages cho user: {AppUserIdentifierForGmail}, pageToken: {pageToken ?? "NULL"}");
            try
            {
                // Gọi service với pageToken
                var listResponse = await _gmailService.ListMessagesAsync(AppUserIdentifierForGmail, "me", labelIds, maxResults, pageToken);

                if (listResponse.Messages == null || !listResponse.Messages.Any())
                {
                    return Ok(new PaginatedGmailMessagesDto { Emails = new List<MessageDataDto>(), NextPageToken = null });
                }

                var messageDetailsPromises = listResponse.Messages.Select(async messageMeta =>
                {
                    var detailedMessage = await _gmailService.GetMessageAsync(AppUserIdentifierForGmail, messageMeta.Id, "me", UsersResource.MessagesResource.GetRequest.FormatEnum.Metadata);
                    if (detailedMessage != null && detailedMessage.Payload != null)
                    {
                        return new MessageDataDto
                        { /* ... mapping ... */
                            Id = detailedMessage.Id,
                            ThreadId = detailedMessage.ThreadId,
                            Snippet = detailedMessage.Snippet,
                            InternalDate = detailedMessage.InternalDate,
                            Subject = detailedMessage.Payload.Headers?.FirstOrDefault(h => h.Name.Equals("Subject", StringComparison.OrdinalIgnoreCase))?.Value,
                            From = detailedMessage.Payload.Headers?.FirstOrDefault(h => h.Name.Equals("From", StringComparison.OrdinalIgnoreCase))?.Value,
                            Date = detailedMessage.Payload.Headers?.FirstOrDefault(h => h.Name.Equals("Date", StringComparison.OrdinalIgnoreCase))?.Value,
                            IsRead = !(detailedMessage.LabelIds?.Contains("UNREAD") ?? false)
                        };
                    }
                    return null;
                });

                var messageDetails = (await Task.WhenAll(messageDetailsPromises)).Where(md => md != null).ToList();

                var result = new PaginatedGmailMessagesDto
                {
                    Emails = messageDetails.OrderByDescending(m => m.InternalDate).ToList(),
                    NextPageToken = listResponse.NextPageToken
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                // ... (xử lý lỗi giữ nguyên) ...
                Console.WriteLine($"[GetGmailMessages] Lỗi khi lấy danh sách Gmail: {ex.ToString()}");
                if (ex.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("invalid_grant"))
                {
                    return Unauthorized(new { message = "Lỗi xác thực với Gmail. Vui lòng kết nối lại tài khoản.", details = ex.Message });
                }
                return StatusCode(500, $"Đã xảy ra lỗi khi truy cập Gmail: {ex.Message}");
            }
        }

        public class MessageDataDto { public string Id { get; set; } public string ThreadId { get; set; } public string Snippet { get; set; } public long? InternalDate { get; set; } public string Subject { get; set; } public string From { get; set; } public string Date { get; set; } public bool IsRead { get; set; } }
        public class MessageDetailDto : MessageDataDto { public string To { get; set; } public string Cc { get; set; } public string Body { get; set; } public string HtmlBody { get; set; } }
        public class SendEmailDto { public string To { get; set; } public string Subject { get; set; } public string Body { get; set; } public string From { get; set; } }

        [HttpGet("messages/{id}")]
        public async Task<ActionResult<MessageDetailDto>> GetGmailMessageDetail(string id)
        {
            Console.WriteLine($"[GmailController.GetGmailMessageDetail] Yêu cầu lấy chi tiết message ID: {id} cho userIdentifier: {AppUserIdentifierForGmail}");
            try
            {
                // Sử dụng AppUserIdentifierForGmail NHẤT QUÁN ở đây
                var message = await _gmailService.GetMessageAsync(AppUserIdentifierForGmail, messageId: id, userId: "me", format: Google.Apis.Gmail.v1.UsersResource.MessagesResource.GetRequest.FormatEnum.Full);

                if (message == null || message.Payload == null) return NotFound($"Không tìm thấy email với ID: {id}");
                var messageDetail = new MessageDetailDto
                {
                    Id = message.Id,
                    ThreadId = message.ThreadId,
                    Snippet = message.Snippet,
                    InternalDate = message.InternalDate,
                    Subject = message.Payload.Headers?.FirstOrDefault(h => h.Name.Equals("Subject", StringComparison.OrdinalIgnoreCase))?.Value,
                    From = message.Payload.Headers?.FirstOrDefault(h => h.Name.Equals("From", StringComparison.OrdinalIgnoreCase))?.Value,
                    To = message.Payload.Headers?.FirstOrDefault(h => h.Name.Equals("To", StringComparison.OrdinalIgnoreCase))?.Value,
                    Cc = message.Payload.Headers?.FirstOrDefault(h => h.Name.Equals("Cc", StringComparison.OrdinalIgnoreCase))?.Value,
                    Date = message.Payload.Headers?.FirstOrDefault(h => h.Name.Equals("Date", StringComparison.OrdinalIgnoreCase))?.Value,
                    Body = _gmailService.GetEmailBody(message.Payload),
                    IsRead = !(message.LabelIds?.Contains("UNREAD") ?? false),
                    HtmlBody = (message.Payload.MimeType == "text/html" || message.Payload.Parts?.Any(p => p.MimeType == "text/html") == true) ? _gmailService.GetEmailBody(message.Payload) : null
                };
                return Ok(messageDetail);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GmailController.GetGmailMessageDetail] Lỗi khi lấy chi tiết email Gmail ({id}) cho user {AppUserIdentifierForGmail}: {ex.ToString()}");
                if (ex.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("token has been expired or revoked", StringComparison.OrdinalIgnoreCase) || ex is Google.Apis.Auth.OAuth2.Responses.TokenResponseException)
                {
                    return Unauthorized(new { message = "Lỗi xác thực với Gmail. Token có thể không hợp lệ hoặc đã hết hạn. Vui lòng kết nối lại tài khoản.", details = ex.Message });
                }
                return StatusCode(500, $"Đã xảy ra lỗi khi truy cập Gmail: {ex.Message}");
            }
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendGmailMessage([FromBody] SendEmailDto emailDto)
        {
            if (emailDto == null || string.IsNullOrWhiteSpace(emailDto.To) || string.IsNullOrWhiteSpace(emailDto.Subject)) return BadRequest("Người nhận và chủ đề không được để trống.");
            Console.WriteLine($"[GmailController.SendGmailMessage] Yêu cầu gửi email cho userIdentifier: {AppUserIdentifierForGmail}");
            try
            {
                // Sử dụng AppUserIdentifierForGmail NHẤT QUÁN ở đây
                var sentMessage = await _gmailService.SendMessageAsync(AppUserIdentifierForGmail, emailDto.To, emailDto.Subject, emailDto.Body, emailDto.From, "me");
                return Ok(new { message = "Email đã được gửi thành công qua Gmail!", messageId = sentMessage.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GmailController.SendGmailMessage] Lỗi khi gửi email qua Gmail cho user {AppUserIdentifierForGmail}: {ex.ToString()}");
                if (ex.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("token has been expired or revoked", StringComparison.OrdinalIgnoreCase) || ex is Google.Apis.Auth.OAuth2.Responses.TokenResponseException)
                {
                    return Unauthorized(new { message = "Lỗi xác thực với Gmail. Vui lòng kết nối lại tài khoản.", details = ex.Message });
                }
                return StatusCode(500, $"Đã xảy ra lỗi khi gửi email qua Gmail: {ex.Message}");
            }
        }

        [HttpDelete("messages/{id}")]
        public async Task<IActionResult> TrashGmailMessage(string id)
        {
            Console.WriteLine($"[GmailController.TrashGmailMessage] Yêu cầu chuyển vào thùng rác email ID: {id} cho userIdentifier: {AppUserIdentifierForGmail}");
            try
            {
                // Sử dụng AppUserIdentifierForGmail NHẤT QUÁN ở đây
                bool success = await _gmailService.TrashMessageAsync(AppUserIdentifierForGmail, messageId: id, userId: "me");
                if (success) return NoContent();
                return StatusCode(500, $"Không thể chuyển email ID: {id} vào thùng rác.");
            }
            catch (Google.GoogleApiException gex)
            {
                Console.WriteLine($"[GmailController.TrashGmailMessage] Lỗi Google API khi chuyển email ID {id} vào thùng rác cho user {AppUserIdentifierForGmail}: {gex.ToString()}");
                if (gex.Error != null && gex.Error.Code == 404) return NotFound($"Không tìm thấy email với ID: {id} trên Gmail.");
                if (gex.Error != null && (gex.Error.Code == 401 || gex.Error.Code == 403)) return Unauthorized(new { message = "Lỗi xác thực hoặc ủy quyền với Gmail.", details = gex.Message });
                return StatusCode(500, $"Lỗi Google API khi xử lý email ID {id}: {gex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GmailController.TrashGmailMessage] Lỗi không xác định khi chuyển email ID {id} vào thùng rác cho user {AppUserIdentifierForGmail}: {ex.ToString()}");
                return StatusCode(500, $"Đã xảy ra lỗi không mong muốn khi xử lý email ID {id}.");
            }
        }

        [HttpDelete("messages/{id}/permanent")]
        public async Task<IActionResult> DeleteGmailMessagePermanently(string id)
        {
            Console.WriteLine($"[GmailController.DeleteGmailMessagePermanently] Yêu cầu XÓA VĨNH VIỄN email ID: {id} cho userIdentifier: {AppUserIdentifierForGmail}");
            try
            {
                // Sử dụng AppUserIdentifierForGmail NHẤT QUÁN ở đây
                bool success = await _gmailService.DeleteMessagePermanentlyAsync(AppUserIdentifierForGmail, messageId: id, userId: "me");
                if (success) return NoContent();
                return StatusCode(500, $"Không thể xóa vĩnh viễn email ID: {id}.");
            }
            catch (Google.GoogleApiException gex)
            {
                Console.WriteLine($"[GmailController.DeleteGmailMessagePermanently] Lỗi Google API khi xóa vĩnh viễn email ID {id} cho user {AppUserIdentifierForGmail}: {gex.ToString()}");
                if (gex.Error != null && gex.Error.Code == 404) return NotFound($"Không tìm thấy email với ID: {id} trên Gmail để xóa vĩnh viễn.");
                if (gex.Error != null && (gex.Error.Code == 401 || gex.Error.Code == 403)) return Unauthorized(new { message = "Lỗi xác thực hoặc ủy quyền với Gmail.", details = gex.Message });
                return StatusCode(500, $"Lỗi Google API khi xử lý xóa vĩnh viễn email ID {id}: {gex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DeleteGmailMessagePermanently] Lỗi không xác định khi xóa vĩnh viễn email ID {id} cho user {AppUserIdentifierForGmail}: {ex.ToString()}");
                return StatusCode(500, $"Đã xảy ra lỗi không mong muốn khi xử lý xóa vĩnh viễn email ID {id}.");
            }
        }
    }
}
