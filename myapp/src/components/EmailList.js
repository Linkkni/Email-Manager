// ===== src/components/EmailList.js =====
import React, { useState, useEffect } from "react";
import EmailListItem from "./EmailListItem";
import * as emailApiService from "../services/emailService";
import { toast } from "react-toastify";
import "./EmailList.css";

function EmailList({
  onEmailSelected,
  refreshKey,
  dataSourceType,
  onFetchGmailError,
}) {
  const [emails, setEmails] = useState([]);
  const [filteredEmails, setFilteredEmails] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [nextPageToken, setNextPageToken] = useState(null);
  const [pageTokens, setPageTokens] = useState([""]);

  const fetchAndSetEmails = async (token) => {
    try {
      setLoading(true);
      let data;
      if (dataSourceType === "gmail") {
        // Gọi API với page token
        data = await emailApiService.getGmailMessages("INBOX", 15, token);
        setEmails(data.emails || []); // API trả về { emails, nextPageToken }
        setNextPageToken(data.nextPageToken); // Lưu token cho trang tiếp theo
      } else {
        // Phân trang cho email cục bộ có thể được thêm sau
        data = await emailApiService.getAllEmails();
        setEmails(data);
        setNextPageToken(null); // Không có phân trang cho email cục bộ trong ví dụ này
      }
      setError(null);
    } catch (err) {
      // ... (xử lý lỗi giữ nguyên) ...
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    // Lấy trang đầu tiên khi component mount hoặc dataSourceType/refreshKey thay đổi
    const firstToken = pageTokens[pageTokens.length - 1];
    fetchAndSetEmails(firstToken);
  }, [refreshKey, dataSourceType]);

  useEffect(() => {
    if (!emails) return; // Guard clause
    if (searchTerm === "") {
      setFilteredEmails(emails);
    } else {
      const lowerSearchTerm = searchTerm.toLowerCase();
      const results = emails.filter((email) => {
        const subject =
          dataSourceType === "gmail"
            ? email.subject || email.snippet
            : email.subject;
        const sender = dataSourceType === "gmail" ? email.from : email.sender;
        const recipient = dataSourceType === "gmail" ? "" : email.recipient;
        const body = dataSourceType === "gmail" ? email.snippet : email.body;

        return (
          subject?.toLowerCase().includes(lowerSearchTerm) ||
          sender?.toLowerCase().includes(lowerSearchTerm) ||
          recipient?.toLowerCase().includes(lowerSearchTerm) ||
          body?.toLowerCase().includes(lowerSearchTerm)
        );
      });
      setFilteredEmails(results);
    }
  }, [searchTerm, emails, dataSourceType]);

  const handleNextPage = () => {
    if (nextPageToken) {
      const newPageTokens = [...pageTokens, nextPageToken];
      setPageTokens(newPageTokens); // Lưu token của trang mới vào mảng
      fetchAndSetEmails(nextPageToken);
    }
  };

  const handlePreviousPage = () => {
    if (pageTokens.length > 1) {
      // Lấy token của trang trước trang hiện tại
      const previousToken = pageTokens[pageTokens.length - 2];
      // Xóa token của trang hiện tại khỏi mảng
      const newPageTokens = pageTokens.slice(0, pageTokens.length - 1);
      setPageTokens(newPageTokens);
      fetchAndSetEmails(previousToken);
    }
  };

  const handleDeleteLocal = async (emailId) => {
    if (dataSourceType === "gmail") return;
    try {
      await emailApiService.deleteEmailById(emailId);
      toast.success("Email cục bộ đã được xóa thành công!");
      fetchAndSetEmails();
      if (onEmailSelected && typeof onEmailSelected === "function") {
        onEmailSelected(null, false);
      }
    } catch (err) {
      toast.error("Lỗi khi xóa email cục bộ.");
    }
  };

  const handleToggleReadLocal = async (emailId, newReadStatus) => {
    if (dataSourceType === "gmail") return;
    try {
      await emailApiService.updateEmailReadStatus(emailId, newReadStatus);
      const updatedEmails = emails.map((email) =>
        email.id === emailId ? { ...email, isRead: newReadStatus } : email
      );
      setEmails(updatedEmails);
    } catch (err) {
      toast.error("Lỗi khi cập nhật trạng thái đọc email cục bộ.");
    }
  };

  const handleTrashGmail = async (emailId) => {
    try {
      await emailApiService.trashGmailMessage(emailId);
      toast.success("Đã chuyển email vào thùng rác trên Gmail.");
      fetchAndSetEmails();
      if (onEmailSelected && typeof onEmailSelected === "function") {
        onEmailSelected(null, true);
      }
    } catch (err) {
      toast.error("Lỗi khi chuyển email vào thùng rác.");
      console.error(err);
    }
  };

  const handleDeleteGmailPermanently = async (emailId) => {
    try {
      await emailApiService.deleteGmailMessagePermanently(emailId);
      toast.warn("Email đã được xóa vĩnh viễn trên Gmail.");
      fetchAndSetEmails();
      if (onEmailSelected && typeof onEmailSelected === "function") {
        onEmailSelected(null, true);
      }
    } catch (err) {
      toast.error("Lỗi khi xóa vĩnh viễn email.");
      console.error(err);
    }
  };

  if (loading) {
    return (
      <p className="status-message">
        Đang tải danh sách email (
        {dataSourceType === "gmail" ? "Gmail" : "Cục bộ"})...
      </p>
    );
  }

  if (error) {
    return <p className="status-message error-message">Lỗi: {error}</p>;
  }

  return (
    <div className="email-list-container">
      <div className="email-list-header">
        <h2>
          {dataSourceType === "gmail" ? "Hộp thư Gmail" : "Hộp thư cục bộ"}(
          {emails?.length || 0})
        </h2>
        <div className="email-list-controls">
          <input
            type="text"
            placeholder="Tìm kiếm..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="search-input"
          />
          <button onClick={fetchAndSetEmails} className="refresh-button">
            Làm mới
          </button>
        </div>
      </div>
      {!filteredEmails || filteredEmails.length === 0 ? (
        <p className="status-message">
          {searchTerm
            ? `Không tìm thấy email nào khớp với "${searchTerm}".`
            : "Hộp thư trống."}
        </p>
      ) : (
        <ul className="email-list-ul">
          {filteredEmails.map((email) => (
            <EmailListItem
              key={email.id}
              email={email}
              onSelectEmail={onEmailSelected}
              onDeleteEmail={
                dataSourceType !== "gmail" ? handleDeleteLocal : undefined
              }
              onToggleRead={
                dataSourceType !== "gmail" ? handleToggleReadLocal : undefined
              }
              onTrashGmail={
                dataSourceType === "gmail" ? handleTrashGmail : undefined
              }
              onDeleteGmailPermanently={
                dataSourceType === "gmail"
                  ? handleDeleteGmailPermanently
                  : undefined
              }
              isGmail={dataSourceType === "gmail"}
            />
          ))}
        </ul>
      )}
      {/* Hiển thị danh sách email */}
      {/* {!emails || emails.length === 0 ? (
        <p className="status-message">Hộp thư trống.</p>
      ) : (
        <ul className="email-list-ul">
          {emails.map((email) => (
            <EmailListItem
              key={email.id}
              email={email}
              onSelectEmail={onEmailSelected}
              // ... các props khác
              isGmail={dataSourceType === "gmail"}
            />
          ))}
        </ul>
      )} */}

      {/* --- Footer Phân trang --- */}
      {dataSourceType === "gmail" && (
        <div className="pagination-footer">
          <button
            onClick={handlePreviousPage}
            disabled={pageTokens.length <= 1 || loading}
          >
            &larr; Trang trước
          </button>
          <span>Trang {pageTokens.length}</span>
          <button onClick={handleNextPage} disabled={!nextPageToken || loading}>
            Trang sau &rarr;
          </button>
        </div>
      )}
    </div>
  );
}
export default EmailList;
