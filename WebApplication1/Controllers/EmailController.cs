using EmailWebApi.Api.Models;
using EmailWebApi.Api.Services;
using Google.Apis.Gmail.v1.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace EmailWebApi.Api.Controllers
{

    [Route("api/[Controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Email>>> GetEmails()
        {
            var emails = await _emailService.GetAllEmailsAsync();
            return Ok(emails);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Email>> GetEmail(Guid id)
        {
            var email = await _emailService.GetEmailByIdAsync(id);
            if(email == null)
            {
                return NotFound(new { Message = $"Không tìm thấy email với ID: {{id" });
            }

            if(!email.IsRead)
            {
                await _emailService.UpdateEmailReadStatusAsync(id, true);
                email.IsRead = true;
            }
            return Ok(email);
        }

        public class CreateEmailDto
        {
            public string Sender { get; set; }

            public string Recipient { get; set; }
            public string Subject { get; set; }
            public string Body { get; set; }

        }

        [HttpPost]
        public async Task<ActionResult<Email>> CreateEmail([FromBody] CreateEmailDto createEmailDto)
        {
            if (createEmailDto == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);

            }

            var emailToCreate = new Email
            {
                Sender = createEmailDto.Sender,
                Recipient = createEmailDto.Recipient,
                Subject = createEmailDto.Subject,
                Body = createEmailDto.Body
            };

            var createEmail = await _emailService.CreateEmailAsync(emailToCreate);

            return CreatedAtAction(nameof(GetEmail), new { id = createEmail.Id }, createEmail);

        } 

        public class UpdateReadStatusDto
        {
            public bool Isread { get; set; }
        }

        [HttpPut("{id}/readstatus")]
        public async Task<ActionResult> UpdateReadStatus(Guid id, [FromBody] UpdateReadStatusDto updateDto)
        {
            var success = await _emailService.UpdateEmailReadStatusAsync(id, updateDto.Isread);
            if (!success)
            {
                return NotFound(new { Message = $"Không tìm thấy email với ID: {id} để cập nhật." });
            }
            return NoContent(); // 204 No Content - Thành công, không có nội dung trả về
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmail(Guid id)
        {
            var success = await _emailService.DeleteEmailAsync(id);
            if (!success)
            {
                return NotFound(new { Message = $"Không tìm thấy email với ID: {id} để cập nhật." });
            }
            return NoContent();
        }

        [HttpGet("search")] 
        public async Task<ActionResult<IEnumerable<Email>>> SearchEmails([FromQuery] string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return BadRequest(new { Message = "Từ khóa tìm kiếm không được để trống." });
            }
            var emails = await _emailService.SearchEmailsAsync(keyword);
            return Ok(emails);
        }


    }
}
