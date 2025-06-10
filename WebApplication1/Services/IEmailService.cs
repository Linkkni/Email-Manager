using EmailWebApi.Api.Models;

namespace EmailWebApi.Api.Services
{
    public interface IEmailService
    {
        Task<IEnumerable<Email>> GetAllEmailsAsync();
        Task<Email> GetEmailByIdAsync(Guid id);
        Task<Email> CreateEmailAsync(Email email); // Nhận đối tượng Email đầy đủ
        Task<bool> UpdateEmailReadStatusAsync(Guid id, bool isRead);
        Task<bool> DeleteEmailAsync(Guid id);
        Task<IEnumerable<Email>> SearchEmailsAsync(string keyword);
    }
}
