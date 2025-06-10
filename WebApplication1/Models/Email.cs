using System.ComponentModel.DataAnnotations;

namespace EmailWebApi.Api.Models
{
    public class Email
    {
        public Guid Id { get; set; }
        [Required(ErrorMessage = "Người gửi không được để trống")] // Ví dụ về validation
        [MaxLength(255)]
        public string Sender { get; set; }

        [Required(ErrorMessage = "Người nhận không được để trống")]
        [MaxLength(255)]
        public string Recipient { get; set; }

        public string Subject { get; set; }
        [Required(ErrorMessage = "Chủ đề không được để trống")]
        [MaxLength(255)]
        public string Body { get; set; }
        public DateTime TimeStamp { get; set; }
        public bool IsRead { get; set; }

        // khoi tao // Constructor để dễ dàng tạo email mới

        public Email(string sender, string recipient, string subject, string body)
        {
            Id = Guid.NewGuid();
            Sender = sender;
            Recipient = recipient;
            Subject = subject;
            Body = body;
            TimeStamp = DateTime.Now;
            IsRead = false;// Mặc định email mới là chưa đọc
        }

        public Email() { } // Constructor không tham số cần thiết cho việc Deserialization JSON

        public override string ToString()
        {
            return $"{(IsRead ? "" : "[Not Read Yet]")} From: {Sender}, To: {Recipient}, Subject: {Subject} ({TimeStamp:dd/MM/yyyy HH:mm})";




        }


        //Tạo lớp EmailManager để quản lý các hoạt động
    }
}
