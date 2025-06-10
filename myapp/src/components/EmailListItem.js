import React from "react";
import "./EmailListItem.css";

function EmailListItem({
  email,
  onSelectEmail,
  onDeleteEmail,
  onToggleRead,
  onTrashGmail,
  onDeleteGmailPermanently,
  isGmail = false,
}) {
  if (!email) {
    return null;
  }

  const handleItemClick = () => {
    if (onSelectEmail) onSelectEmail(email.id, isGmail);
  };
  const handleDeleteLocalClick = (e) => {
    e.stopPropagation();
    if (window.confirm(`Bạn có chắc chắn muốn xóa email cục bộ này không?`)) {
      if (onDeleteEmail) onDeleteEmail(email.id);
    }
  };
  const handleToggleReadClick = (e) => {
    e.stopPropagation();
    if (onToggleRead) onToggleRead(email.id, !email.isRead);
  };
  const handleTrashGmailClick = (e) => {
    e.stopPropagation();
    if (
      window.confirm(
        `Email này sẽ được chuyển vào thùng rác trên tài khoản Gmail của bạn. Bạn có chắc chắn không?`
      )
    ) {
      if (onTrashGmail) onTrashGmail(email.id);
    }
  };
  const handleDeleteGmailPermanentlyClick = (e) => {
    e.stopPropagation();
    const confirmationText = "xóa vĩnh viễn";
    const userInput = prompt(
      `HÀNH ĐỘNG NGUY HIỂM: Email sẽ bị xóa vĩnh viễn và không thể khôi phục. Vui lòng gõ "${confirmationText}" để xác nhận.`
    );
    if (userInput === confirmationText) {
      if (onDeleteGmailPermanently) onDeleteGmailPermanently(email.id);
    } else if (userInput !== null) {
      alert("Xác nhận không hợp lệ. Hành động xóa đã bị hủy.");
    }
  };

  const displaySender = isGmail
    ? (email.from || "").split("<")[0].trim()
    : email.sender;
  const displaySubject = email.subject || "Không có chủ đề";
  const displaySnippet = isGmail
    ? email.snippet
    : (email.body || "").substring(0, 100);
  const displayDate = email.internalDate
    ? new Date(email.internalDate).toLocaleDateString("vi-VN")
    : new Date(email.timeStamp).toLocaleDateString("vi-VN");
  const displayIsRead = email.isRead;

  return (
    <li
      className={`email-list-item ${displayIsRead ? "read" : "unread"}`}
      onClick={handleItemClick}
      title="Nhấp để xem chi tiết"
    >
      <div className="email-sender" title={isGmail ? email.from : email.sender}>
        {displaySender}
      </div>
      <div className="email-content">
        <span className="email-subject">{displaySubject}</span>
        <span className="email-separator"> - </span>
        <span className="email-snippet">{displaySnippet}</span>
      </div>
      <div className="email-date">{displayDate}</div>
      <div className="email-actions-hover">
        {isGmail ? (
          <>
            <button
              onClick={handleTrashGmailClick}
              className="action-button-hover"
              title="Chuyển vào thùng rác"
            >
              🗑️
            </button>
            <button
              onClick={handleDeleteGmailPermanentlyClick}
              className="action-button-hover"
              title="Xóa vĩnh viễn"
            >
              🔥
            </button>
          </>
        ) : (
          <>
            <button
              onClick={handleToggleReadClick}
              className="action-button-hover"
              title={displayIsRead ? "Đánh dấu chưa đọc" : "Đánh dấu đã đọc"}
            >
              {displayIsRead ? "✉️" : "📧"}
            </button>
            <button
              onClick={handleDeleteLocalClick}
              className="action-button-hover"
              title="Xóa email cục bộ"
            >
              🗑️
            </button>
          </>
        )}
      </div>
    </li>
  );
}
export default EmailListItem;
