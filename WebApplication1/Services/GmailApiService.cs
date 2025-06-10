// File: EmailWebApp.Api/Services/GmailApiService.cs
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static EmailWebApi.Api.Services.GmailApiService;

namespace EmailWebApi.Api.Services
{
    public interface IGmailApiService
    {
        // userIdentifier sẽ là ID duy nhất của người dùng trong hệ thống của bạn
        Task<GmailService> GetAuthenticatedServiceAsync(string userIdentifier); // Bỏ giá trị mặc định
        Task<GmailMessageListResponse> ListMessagesAsync(string userIdentifier, string userId = "me", string labelIds = "INBOX", uint maxResults = 20, string pageToken = null);
        Task<Message> GetMessageAsync(string userIdentifier, string messageId, string userId = "me", UsersResource.MessagesResource.GetRequest.FormatEnum format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full);
        Task<Message> SendMessageAsync(string userIdentifier, string to, string subject, string bodyText, string fromOverride = null, string userId = "me");
        Task<bool> TrashMessageAsync(string userIdentifier, string messageId, string userId = "me");
        Task<bool> DeleteMessagePermanentlyAsync(string userIdentifier, string messageId, string userId = "me");
        string GetEmailBody(Google.Apis.Gmail.v1.Data.MessagePart payload);
        string DecodeBase64UrlString(string base64Url);
        string GetAuthorizationUrl(string redirectUri);
        Task<UserCredential> ExchangeCodeForTokenAsync(string authorizationCode, string redirectUri, string userIdentifier);

    }
    public class GmailMessageListResponse
    {
        public IList<Message> Messages { get; set; }
        public string NextPageToken { get; set; }
    }
    public class GmailApiService : IGmailApiService
    {
        private static readonly string[] Scopes = { GmailService.Scope.MailGoogleCom };
        private static readonly string ApplicationName = "Email Web App";
        private const string CredentialsPath = "credi.json";
        private const string TokenDirectoryPath = "Tokens_GmailApi";

        private readonly IConfiguration _configuration;

        

        public GmailApiService(IConfiguration configuration)
        {
            _configuration = configuration;
            if (!Directory.Exists(TokenDirectoryPath))
            {
                Directory.CreateDirectory(TokenDirectoryPath);
            }
        }

        private async Task<ClientSecrets> LoadClientSecretsAsync()
        {
            if (File.Exists(CredentialsPath))
            {
                using (var stream = new FileStream(CredentialsPath, FileMode.Open, FileAccess.Read))
                {
                    var googleClientSecretsContainer = await GoogleClientSecrets.FromStreamAsync(stream);
                    if (googleClientSecretsContainer.Secrets != null) return googleClientSecretsContainer.Secrets;
                    
                    throw new InvalidOperationException("File credentials.json không chứa thông tin 'web' hoặc 'installed' client secrets hợp lệ.");
                }
            }
            var clientId = _configuration["GoogleApi:ClientId"];
            var clientSecret = _configuration["GoogleApi:ClientSecret"];
            if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
            {
                return new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret };
            }
            throw new FileNotFoundException($"Không tìm thấy file credentials tại '{Path.GetFullPath(CredentialsPath)}' và cấu hình GoogleApi:ClientId/ClientSecret cũng không có.", CredentialsPath);
        }

        private async Task<UserCredential> GetUserCredentialAsync(string userIdentifier)
        {
            if (string.IsNullOrEmpty(userIdentifier))
            {
                // Trong ứng dụng thực tế, bạn không nên để userIdentifier rỗng.
                // Đây là một fallback tạm thời, nhưng nên được xử lý tốt hơn.
                // Hoặc ném ra một ArgumentNullException.
                Console.WriteLine("Cảnh báo: userIdentifier rỗng hoặc null trong GetUserCredentialAsync. Sử dụng giá trị mặc định 'default_token_user'.");
                userIdentifier = "default_token_user"; // Cần đảm bảo giá trị này nhất quán
            }

            ClientSecrets clientSecrets = await LoadClientSecretsAsync();
            string userTokenFolderPath = Path.Combine(TokenDirectoryPath, userIdentifier);
            Console.WriteLine($"[GetUserCredentialAsync] Sử dụng FileDataStore tại: {Path.GetFullPath(userTokenFolderPath)} cho userIdentifier: {userIdentifier}");

            // FileDataStore sẽ tìm file có tên dựa trên userIdentifier bên trong userTokenFolderPath
            var fileDataStore = new FileDataStore(userTokenFolderPath, true);

            // Kiểm tra xem token đã tồn tại chưa (chỉ để debug)
            var storedToken = await fileDataStore.GetAsync<TokenResponse>(userIdentifier);
            if (storedToken != null)
            {
                Console.WriteLine($"[GetUserCredentialAsync] Đã tìm thấy token đã lưu cho {userIdentifier}. RefreshToken: {(string.IsNullOrEmpty(storedToken.RefreshToken) ? "KHÔNG CÓ" : "CÓ")}");
            }
            else
            {
                Console.WriteLine($"[GetUserCredentialAsync] KHÔNG tìm thấy token đã lưu cho {userIdentifier}. Sẽ thử khởi tạo luồng OAuth mới.");
            }

            return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                Scopes,
                userIdentifier, // userIdentifier này được dùng làm key cho FileDataStore
                CancellationToken.None,
                fileDataStore
            );
        }

        public async Task<GmailService> GetAuthenticatedServiceAsync(string userIdentifier)
        {
            Console.WriteLine($"[GetAuthenticatedServiceAsync] Yêu cầu service cho userIdentifier: {userIdentifier}");
            UserCredential credential = await GetUserCredentialAsync(userIdentifier);
            Console.WriteLine($"[GetAuthenticatedServiceAsync] Đã lấy UserCredential cho userIdentifier: {userIdentifier}, IsStale: {credential.Token.IsStale}, IsExpired: {credential.Token.IsExpired(Google.Apis.Util.SystemClock.Default)}");

            // Kiểm tra xem token có cần làm mới không
            if (credential.Token.IsStale || credential.Token.IsExpired(Google.Apis.Util.SystemClock.Default))
            {
                Console.WriteLine($"[GetAuthenticatedServiceAsync] Token cho {userIdentifier} đã cũ hoặc hết hạn, đang thử làm mới...");
                bool refreshed = await credential.RefreshTokenAsync(CancellationToken.None);
                if (refreshed)
                {
                    Console.WriteLine($"[GetAuthenticatedServiceAsync] Token cho {userIdentifier} đã được làm mới thành công.");
                }
                else
                {
                    Console.WriteLine($"[GetAuthenticatedServiceAsync] KHÔNG THỂ làm mới token cho {userIdentifier}.");
                    // Ở đây, bạn có thể muốn ném lỗi hoặc yêu cầu người dùng xác thực lại.
                    // Nếu RefreshTokenAsync thất bại, có thể refresh token đã bị thu hồi hoặc không hợp lệ.
                }
            }


            return new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        public string GetAuthorizationUrl(string redirectUri)
        {
            ClientSecrets clientSecrets = Task.Run(async () => await LoadClientSecretsAsync()).GetAwaiter().GetResult();
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                Scopes = Scopes
                // Không cần DataStore ở đây vì chúng ta chỉ tạo URL
            });
            var authorizationUrl = flow.CreateAuthorizationCodeRequest(redirectUri).Build();
            Console.WriteLine($"[GetAuthorizationUrl] Tạo URL xác thực: {authorizationUrl}");
            return authorizationUrl.ToString();
        }

        public async Task<UserCredential> ExchangeCodeForTokenAsync(string authorizationCode, string redirectUri, string userIdentifier)
        {
            if (string.IsNullOrEmpty(userIdentifier))
            {
                Console.WriteLine("Cảnh báo: userIdentifier rỗng hoặc null trong ExchangeCodeForTokenAsync. Sử dụng giá trị mặc định 'default_token_user'.");
                userIdentifier = "default_token_user"; // Phải nhất quán với GetUserCredentialAsync
            }

            ClientSecrets clientSecrets = await LoadClientSecretsAsync();
            string userTokenFolderPath = Path.Combine(TokenDirectoryPath, userIdentifier);
            Console.WriteLine($"[ExchangeCodeForTokenAsync] Sử dụng FileDataStore tại: {Path.GetFullPath(userTokenFolderPath)} cho userIdentifier: {userIdentifier}");

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                Scopes = Scopes,
                DataStore = new FileDataStore(userTokenFolderPath, true) // Token sẽ được lưu vào đây
            });

            Console.WriteLine($"[ExchangeCodeForTokenAsync] Đang đổi code: {authorizationCode.Substring(0, Math.Min(10, authorizationCode.Length))}... cho user: {userIdentifier} với redirectUri: {redirectUri}");
            TokenResponse token = await flow.ExchangeCodeForTokenAsync(
                userIdentifier, // User ID (key) mà DataStore sẽ sử dụng để lưu token này
                authorizationCode,
                redirectUri,
                CancellationToken.None);

            Console.WriteLine($"[ExchangeCodeForTokenAsync] Đổi code thành công cho user: {userIdentifier}. AccessToken: {(string.IsNullOrEmpty(token.AccessToken) ? "NULL" : "CÓ")}, RefreshToken: {(string.IsNullOrEmpty(token.RefreshToken) ? "NULL" : "CÓ")}");

            // FileDataStore đã tự động lưu token vào thư mục userTokenFolderPath với key là userIdentifier
            // Bạn có thể kiểm tra file tại: Tokens_GmailApi\[userIdentifier]\Google.Apis.Auth.OAuth2.Responses.TokenResponse-[userIdentifier]
            string expectedTokenFile = Path.Combine(userTokenFolderPath, $"Google.Apis.Auth.OAuth2.Responses.TokenResponse-{userIdentifier}");
            if (File.Exists(expectedTokenFile))
            {
                Console.WriteLine($"[ExchangeCodeForTokenAsync] File token ĐÃ được tạo/cập nhật tại: {Path.GetFullPath(expectedTokenFile)}");
            }
            else
            {
                Console.WriteLine($"[ExchangeCodeForTokenAsync] CẢNH BÁO: File token KHÔNG được tìm thấy tại: {Path.GetFullPath(expectedTokenFile)} sau khi ExchangeCodeForTokenAsync. Kiểm tra quyền ghi và logic của FileDataStore.");
            }

            return new UserCredential(flow, userIdentifier, token);
        }

        // Các phương thức sau cần truyền userIdentifier từ Controller
        public async Task<GmailMessageListResponse> ListMessagesAsync(string userIdentifier, string userId = "me", string labelIds = "INBOX", uint maxResults = 20, string pageToken = null)
        {
            var service = await GetAuthenticatedServiceAsync(userIdentifier);
            var request = service.Users.Messages.List(userId);
            if (!string.IsNullOrEmpty(labelIds)) request.LabelIds = new List<string> { labelIds };
            request.MaxResults = maxResults;
            request.IncludeSpamTrash = false;
            if(!string.IsNullOrEmpty(pageToken))
            {
                request.PageToken = pageToken;
            }
            ListMessagesResponse response = await request.ExecuteAsync();
            return new GmailMessageListResponse
            {
                Messages = response.Messages ?? new List<Message>(),
                NextPageToken = response.NextPageToken
            };
        }

        public async Task<Message> GetMessageAsync(string userIdentifier, string messageId, string userId = "me", UsersResource.MessagesResource.GetRequest.FormatEnum format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full)
        {
            var service = await GetAuthenticatedServiceAsync(userIdentifier);
            var request = service.Users.Messages.Get(userId, messageId);
            request.Format = format;
            return await request.ExecuteAsync();
        }

        public async Task<Message> SendMessageAsync(string userIdentifier, string to, string subject, string bodyText, string fromOverride = null, string userId = "me")
        {
            var service = await GetAuthenticatedServiceAsync(userIdentifier);
            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse(to));
            mimeMessage.Subject = subject;
            var bodyBuilder = new BodyBuilder { TextBody = bodyText };
            mimeMessage.Body = bodyBuilder.ToMessageBody();
            using (var memoryStream = new MemoryStream())
            {
                mimeMessage.WriteTo(memoryStream);
                var rawMessage = Convert.ToBase64String(memoryStream.ToArray()).Replace('+', '-').Replace('/', '_').Replace("=", "");
                var gmailMessage = new Message { Raw = rawMessage };
                return await service.Users.Messages.Send(gmailMessage, userId).ExecuteAsync();
            }
        }

        // Các hàm tiện ích giữ nguyên
        public string DecodeBase64UrlString(string base64Url)
        {
            if (string.IsNullOrEmpty(base64Url)) return "";
            string padded = base64Url.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4) { case 0: break; case 2: padded += "=="; break; case 3: padded += "="; break; default: throw new ArgumentException("Illegal base64url string!", nameof(base64Url)); }
            byte[] data = Convert.FromBase64String(padded);
            return Encoding.UTF8.GetString(data);
        }

        public string GetEmailBody(Google.Apis.Gmail.v1.Data.MessagePart payload)
        {
            if (payload == null) return "";
            if (!string.IsNullOrEmpty(payload.Body?.Data))
            {
                if (payload.MimeType == "text/plain" || payload.MimeType == "text/html" || string.IsNullOrEmpty(payload.MimeType))
                {
                    return DecodeBase64UrlString(payload.Body.Data);
                }
            }
            if (payload.Parts != null && payload.Parts.Any())
            {
                var plainTextPart = payload.Parts.FirstOrDefault(p => p.MimeType == "text/plain" && !string.IsNullOrEmpty(p.Body?.Data));
                if (plainTextPart != null) return DecodeBase64UrlString(plainTextPart.Body.Data);
                var htmlPart = payload.Parts.FirstOrDefault(p => p.MimeType == "text/html" && !string.IsNullOrEmpty(p.Body?.Data));
                if (htmlPart != null) return DecodeBase64UrlString(htmlPart.Body.Data);
                foreach (var part in payload.Parts)
                {
                    string nestedBody = GetEmailBody(part);
                    if (!string.IsNullOrEmpty(nestedBody)) return nestedBody;
                }
            }
            return "";
        }

        public async Task<bool> TrashMessageAsync(string userIdentifier, string messageId, string userId = "me")
        {
            try
            {
                var service = await GetAuthenticatedServiceAsync(userIdentifier);
                await service.Users.Messages.Trash(userId, messageId).ExecuteAsync();

                Console.WriteLine($"[TrashMessageAsync] Email với ID: {messageId} đã được chuyển vào thùng rác cho userIdentifier: {userIdentifier}.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TrashMessageAsync] Lỗi khi chuyển email ID {messageId} vào thùng rác: {ex.ToString()}");
                return false;
            }
        }

        public async Task<bool> DeleteMessagePermanentlyAsync(string userIdentifier, string messageId, string userId = "me")
        {
            try
            {
                var service = await GetAuthenticatedServiceAsync(userIdentifier);
                await service.Users.Messages.Delete(userId, messageId).ExecuteAsync();

                Console.WriteLine($"[DeleteMessagePermanentlyAsync] Email với ID: {messageId} đã được XÓA VĨNH VIỄN cho userIdentifier: {userIdentifier}.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DeleteMessagePermanentlyAsync] Lỗi khi xóa vĩnh viễn email ID {messageId}: {ex.ToString()}");
                return false;
            }
        }


    }
}
