using EmailWebApi.Api.Data;
using EmailWebApi.Api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmailWebApi.Api.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailDBContact _context;

        public EmailService(EmailDBContact context) // Constructor này không phải là nguyên nhân trực tiếp của lỗi
        {
            _context = context;
        }

        public async Task<Email> CreateEmailAsync(Email email) // Chú ý tên: GetAllEmailsAsync (có 's' ở Emails)
        {
            email.Id = Guid.NewGuid();
            email.TimeStamp = DateTime.Now;
            email.IsRead = false;

            _context.Emails.Add(email);
            await _context.SaveChangesAsync();
            return email;
        }

        public async Task<bool> DeleteEmailAsync(Guid id) // Chú ý tên: GetAllEmailsAsync (có 's' ở Emails)
        {
            var emailToDelete = await _context.Emails.FindAsync(id);
            if(emailToDelete == null)
            {
                return false;

            }
            _context.Emails.Remove(emailToDelete);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Email>> GetAllEmailsAsync()
        {
            return await _context.Emails
                .OrderByDescending(e=> e.TimeStamp)
                .ToListAsync();
        }
        public async Task<Email> GetEmailByIdAsync(Guid id)
        {
            var email = await _context.Emails.FindAsync(id);
            if(email != null && !email.IsRead)
            {

            }
            return email;
        }

        public async Task<IEnumerable<Email>> SearchEmailsAsync(string keyword)
        {
            if(string.IsNullOrWhiteSpace(keyword))
            {
                return await _context.Emails.OrderByDescending(e => e.TimeStamp).ToListAsync();
            }

            var lowerKeyword = keyword.ToLowerInvariant();
            var allEmails = await _context.Emails.ToListAsync();
            var foundEmails = allEmails
                .Where(e => (e.Sender != null && e.Sender.ToLowerInvariant().Contains(lowerKeyword)) ||
                            (e.Recipient != null && e.Recipient.ToLowerInvariant().Contains(lowerKeyword)) ||
                            (e.Subject != null && e.Subject.ToLowerInvariant().Contains(lowerKeyword)) ||
                            (e.Body != null && e.Body.ToLowerInvariant().Contains(lowerKeyword))).OrderByDescending(e => e.TimeStamp).ToList();
            return foundEmails;
        }

        public async Task<bool> UpdateEmailReadStatusAsync(Guid id, bool isRead)
        {
            var emailToUpdate = await _context.Emails.FindAsync(id);
            if (emailToUpdate == null)
            {
                return false;

            }
            emailToUpdate.IsRead = isRead;
            await _context.SaveChangesAsync();
            return true;
        }

        
    }


}
