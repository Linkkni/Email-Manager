import axios from "axios";

const API_BASE_URL = "http://localhost:5127/api"; // Cập nhật port nếu API của bạn chạy ở port khác

// === Local Email API Calls ===
export const getAllEmails = async () => {
  try {
    const response = await axios.get(`${API_BASE_URL}/email`);
    return response.data;
  } catch (error) {
    console.error("Lỗi khi lấy email cục bộ:", error);
    throw error;
  }
};

export const trashGmailMessage = async (id) => {
  try {
    const response = await axios.delete(`${API_BASE_URL}/gmail/messages/${id}`);
    return response; // Trả về response để kiểm tra status code nếu cần
  } catch (error) {
    console.error(`Lỗi khi chuyển email Gmail ID ${id} vào thùng rác:`, error);
    throw error;
  }
};
export const deleteGmailMessagePermanently = async (id) => {
  try {
    const response = await axios.delete(
      `${API_BASE_URL}/gmail/messages/${id}/permanent`
    );
    return response;
  } catch (error) {
    console.error(`Lỗi khi xóa vĩnh viễn email Gmail ID ${id}:`, error);
    throw error;
  }
};

export const getEmailById = async (id) => {
  try {
    const response = await axios.get(`${API_BASE_URL}/email/${id}`);
    return response.data;
  } catch (error) {
    console.error(`Lỗi khi lấy email cục bộ với ID ${id}:`, error);
    throw error;
  }
};

export const createEmail = async (emailData) => {
  try {
    const response = await axios.post(`${API_BASE_URL}/email`, emailData);
    return response.data;
  } catch (error) {
    console.error("Lỗi khi tạo email cục bộ:", error);
    throw error;
  }
};

export const deleteEmailById = async (id) => {
  try {
    await axios.delete(`${API_BASE_URL}/email/${id}`);
  } catch (error) {
    console.error(`Lỗi khi xóa email cục bộ với ID ${id}:`, error);
    throw error;
  }
};

export const updateEmailReadStatus = async (id, isRead) => {
  try {
    await axios.put(`${API_BASE_URL}/email/${id}/readstatus`, { isRead });
  } catch (error) {
    console.error(
      `Lỗi khi cập nhật trạng thái đọc email cục bộ ID ${id}:`,
      error
    );
    throw error;
  }
};

// === Gmail API Calls ===
export const getGmailAuthUrl = async () => {
  try {
    const response = await axios.get(`${API_BASE_URL}/gmail/auth/url`);
    return response.data.authorizationUrl; // API backend trả về { authorizationUrl: "..." }
  } catch (error) {
    console.error("Lỗi khi lấy URL xác thực Gmail:", error);
    throw error;
  }
};

export const handleGmailOAuthCallback = async (code) => {
  try {
    const response = await axios.post(`${API_BASE_URL}/gmail/auth/callback`, {
      code,
    });
    return response.data; // API backend trả về { message: "..." }
  } catch (error) {
    console.error("Lỗi khi xử lý callback OAuth Gmail:", error);
    throw error;
  }
};

export const getGmailMessages = async (labelIds = 'INBOX', maxResults = 15, pageToken = null) => {
  try {
    const response = await axios.get(`${API_BASE_URL}/gmail/messages`, {
      params: {
        labelIds,
        maxResults,
        pageToken, // Gửi pageToken lên API
      },
    });
    return response.data; // API giờ trả về { emails: [...], nextPageToken: "..." }
  } catch (error) {
    console.error("Lỗi khi lấy tin nhắn Gmail:", error);
    throw error;
  }
};

export const getGmailMessageDetail = async (id) => {
  try {
    const response = await axios.get(`${API_BASE_URL}/gmail/messages/${id}`);
    return response.data;
  } catch (error) {
    console.error(`Lỗi khi lấy chi tiết tin nhắn Gmail ID ${id}:`, error);
    throw error;
  }
};

export const sendGmail = async (emailData) => {
  // emailData: { to: "...", subject: "...", body: "..." }
  try {
    const response = await axios.post(`${API_BASE_URL}/gmail/send`, emailData);
    return response.data;
  } catch (error) {
    console.error("Lỗi khi gửi email qua Gmail:", error);
    throw error;
  }
};

