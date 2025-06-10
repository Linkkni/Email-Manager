// ===== src/components/EmailDetail.js =====
import React, { useState, useEffect } from "react";
import * as emailApiService from "../services/emailService";
import { toast } from "react-toastify";
import "./EmailDetail.css";

function EmailDetail({ emailId, onBackToList, isGmail, onActionSuccess }) {
  const [email, setEmail] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (emailId) {
      const fetchEmailDetail = async () => {
        setLoading(true);
        setError(null);
        try {
          const data = isGmail
            ? await emailApiService.getGmailMessageDetail(emailId)
            : await emailApiService.getEmailById(emailId);
          setEmail(data);
        } catch (err) {
          setError(`Không thể tải chi tiết email.`);
          console.error(err);
        } finally {
          setLoading(false);
        }
      };
      fetchEmailDetail();
    } else {
      setEmail(null);
    }
  }, [emailId, isGmail]);

  const handleTrashClick = async () => {
    if (!emailId || !isGmail) return;
    if (window.confirm("Chuyển email này vào thùng rác trên Gmail?")) {
      try {
        await emailApiService.trashGmailMessage(emailId);
        toast.success("Đã chuyển email vào thùng rác.");
        if (onActionSuccess) onActionSuccess();
      } catch (err) {
        toast.error("Lỗi khi chuyển vào thùng rác.");
      }
    }
  };

  const handleDeletePermanentlyClick = async () => {
    if (!emailId || !isGmail) return;
    const confirmationText = "xóa";
    const userInput = prompt(
      `HÀNH ĐỘNG NGUY HIỂM: Xóa vĩnh viễn email này? Vui lòng gõ "${confirmationText}" để xác nhận.`
    );
    if (userInput === confirmationText) {
      try {
        await emailApiService.deleteGmailMessagePermanently(emailId);
        toast.warn("Email đã được xóa vĩnh viễn.");
        if (onActionSuccess) onActionSuccess();
      } catch (err) {
        toast.error("Lỗi khi xóa vĩnh viễn.");
      }
    } else if (userInput !== null) {
      alert("Xác nhận không hợp lệ.");
    }
  };

  if (!emailId) {
    return (
      <div className="email-detail-placeholder">
        <p>Vui lòng chọn một email từ danh sách để xem chi tiết.</p>
      </div>
    );
  }
  if (loading) {
    return <p className="status-message">Đang tải chi tiết email...</p>;
  }
  if (error) {
    return <p className="status-message error-message">Lỗi: {error}</p>;
  }
  if (!email) {
    return (
      <p className="status-message">
        Không tìm thấy email, hoặc email đã bị xóa.
      </p>
    );
  }

  const displaySubject = email.subject || (isGmail ? "Không có chủ đề" : "");
  const displaySender = isGmail ? email.from : email.sender;
  const displayRecipient = isGmail ? email.to : email.recipient;
  const displayDate = isGmail
    ? email.date ||
      (email.internalDate
        ? new Date(email.internalDate).toLocaleString("vi-VN")
        : "")
    : new Date(email.timeStamp).toLocaleString("vi-VN");
  const displayIsRead = email.isRead;
  const displayBody = isGmail
    ? email.htmlBody || email.body?.replace(/\n/g, "<br />")
    : email.body?.replace(/\n/g, "<br />");

  return (
    <div className="email-detail-container">
      <div className="email-detail-actions-top">
        <button onClick={onBackToList} className="back-button">
          &larr; Quay lại
        </button>
        {isGmail && (
          <div className="gmail-actions">
            <button
              onClick={handleTrashClick}
              className="action-button trash-button"
            >
              Thùng rác
            </button>
            <button
              onClick={handleDeletePermanentlyClick}
              className="action-button delete-button"
            >
              Xóa vĩnh viễn
            </button>
          </div>
        )}
      </div>
      <h2>{displaySubject}</h2>
      <div className="email-detail-meta">
        <p>
          <strong>Từ:</strong> {displaySender}
        </p>
        <p>
          <strong>Tới:</strong> {displayRecipient}
        </p>
        {isGmail && email.cc && (
          <p>
            <strong>Cc:</strong> {email.cc}
          </p>
        )}
        <p>
          <strong>Ngày gửi:</strong> {displayDate}
        </p>
        <p>
          <strong>Trạng thái:</strong> {displayIsRead ? "Đã đọc" : "Chưa đọc"}
        </p>
      </div>
      <hr />
      <div
        className="email-body"
        dangerouslySetInnerHTML={{ __html: displayBody }}
      ></div>
    </div>
  );
}
export default EmailDetail;
