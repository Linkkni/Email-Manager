// ===== src/components/ComposeEmailForm.js =====
import React, { useState } from "react";
import * as emailApiService from "../services/emailService";
import { toast } from "react-toastify";
import "./ComposeEmailForm.css";

function ComposeEmailForm({
  onEmailSent,
  defaultRecipient = "",
  sendViaGmail = false,
}) {
  const [sender, setSender] = useState(
    sendViaGmail ? "" : "localuser@example.com"
  );
  const [recipient, setRecipient] = useState(defaultRecipient);
  const [subject, setSubject] = useState("");
  const [body, setBody] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if ((!sender && !sendViaGmail) || !recipient || !subject) {
      toast.error(
        "Người gửi (nếu không qua Gmail), người nhận và chủ đề không được để trống."
      );
      return;
    }

    setIsSubmitting(true);
    try {
      const emailData = { sender, recipient, subject, body };
      if (sendViaGmail) {
        await emailApiService.sendGmail({
          to: recipient,
          subject,
          body,
          from: sender,
        });
        toast.success("Email đã được gửi qua Gmail thành công!");
      } else {
        await emailApiService.createEmail(emailData);
        toast.success("Email cục bộ đã được tạo thành công!");
      }

      setRecipient("");
      setSubject("");
      setBody("");
      if (onEmailSent) {
        onEmailSent(sendViaGmail);
      }
    } catch (err) {
      toast.error(
        `Lỗi khi gửi email ${sendViaGmail ? "qua Gmail" : "cục bộ"}.`
      );
      console.error(err);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="compose-email-form">
      <h2>Soạn Email Mới {sendViaGmail ? "(Qua Gmail)" : "(Lưu Cục bộ)"}</h2>
      {!sendViaGmail && (
        <div>
          <label htmlFor="sender">Người gửi:</label>
          <input
            type="email"
            id="sender"
            value={sender}
            onChange={(e) => setSender(e.target.value)}
            required={!sendViaGmail}
          />
        </div>
      )}
      {sendViaGmail && (
        <div>
          <label htmlFor="sender-gmail">Từ (Gmail):</label>
          <input
            type="email"
            id="sender-gmail"
            value={sender}
            onChange={(e) => setSender(e.target.value)}
            placeholder="Để trống để dùng tài khoản đã kết nối, hoặc nhập email 'Gửi thay mặt' (nếu được phép)"
          />
        </div>
      )}
      <div>
        <label htmlFor="recipient">Người nhận:</label>
        <input
          type="email"
          id="recipient"
          value={recipient}
          onChange={(e) => setRecipient(e.target.value)}
          required
        />
      </div>
      <div>
        <label htmlFor="subject">Chủ đề:</label>
        <input
          type="text"
          id="subject"
          value={subject}
          onChange={(e) => setSubject(e.target.value)}
          required
        />
      </div>
      <div>
        <label htmlFor="body">Nội dung:</label>
        <textarea
          id="body"
          value={body}
          onChange={(e) => setBody(e.target.value)}
          rows="8"
        ></textarea>
      </div>
      <button type="submit" disabled={isSubmitting} className="submit-button">
        {isSubmitting
          ? "Đang gửi..."
          : sendViaGmail
          ? "Gửi qua Gmail"
          : "Lưu Email Cục bộ"}
      </button>
    </form>
  );
}
export default ComposeEmailForm;
