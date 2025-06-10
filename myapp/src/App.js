// ===== src/App.js =====
import React, { useState, useEffect } from "react";
import "./App.css";
import EmailList from "./components/EmailList";
import EmailDetail from "./components/EmailDetail";
import ComposeEmailForm from "./components/ComposeEmailForm";
import { ToastContainer, toast } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";
import * as emailApiService from "./services/emailService";

function App() {
  const [selectedEmailId, setSelectedEmailId] = useState(null);
  const [isGmailSelected, setIsGmailSelected] = useState(false);
  const [view, setView] = useState("inbox-local");
  const [refreshEmailListKey, setRefreshEmailListKey] = useState(0);
  const [isGmailConnected, setIsGmailConnected] = useState(false);
  const [authInProgress, setAuthInProgress] = useState(false);

  useEffect(() => {
    const handleAuthCallback = async () => {
      const queryParams = new URLSearchParams(window.location.search);
      const code = queryParams.get("code");
      if (code) {
        window.history.replaceState(
          {},
          document.title,
          window.location.pathname
        );
        setAuthInProgress(true);
        toast.info("Đang xử lý xác thực Gmail...");
        try {
          await emailApiService.handleGmailOAuthCallback(code);
          toast.success("Kết nối Gmail thành công!");
          setIsGmailConnected(true);
          setView("inbox-gmail");
          localStorage.setItem("lastInboxView", "inbox-gmail");
          setRefreshEmailListKey((prev) => prev + 1);
        } catch (error) {
          toast.error("Lỗi khi kết nối Gmail. Vui lòng thử lại.");
          console.error("Lỗi callback OAuth:", error);
        } finally {
          setAuthInProgress(false);
        }
      }
    };
    handleAuthCallback();
  }, []);

  const handleConnectGmail = async () => {
    setAuthInProgress(true);
    try {
      const authUrl = await emailApiService.getGmailAuthUrl();
      window.location.href = authUrl;
    } catch (error) {
      toast.error(
        "Không thể lấy URL xác thực Gmail. Kiểm tra kết nối API backend."
      );
      console.error("Lỗi lấy auth URL:", error);
      setAuthInProgress(false);
    }
  };

  const handleFetchGmailError = () => {
    setIsGmailConnected(false);
    toast.warn("Có lỗi khi tải email từ Gmail. Vui lòng kết nối lại.");
    setView("inbox-local");
    localStorage.setItem("lastInboxView", "inbox-local");
  };

  const handleEmailSelected = (emailId, fromGmail = false) => {
    setSelectedEmailId(emailId);
    setIsGmailSelected(fromGmail);
    setView("detail");
  };

  const handleBackToList = () => {
    const lastInboxView =
      localStorage.getItem("lastInboxView") || "inbox-local";
    setView(lastInboxView);
    setSelectedEmailId(null);
    setIsGmailSelected(false);
    setRefreshEmailListKey((prev) => prev + 1);
  };

  const handleSetInboxView = (inboxType) => {
    if (inboxType === "inbox-gmail" && !isGmailConnected) {
      toast.info("Vui lòng kết nối tài khoản Gmail trước.");
      return;
    }
    setView(inboxType);
    setSelectedEmailId(null);
    localStorage.setItem("lastInboxView", inboxType);
  };

  const handleShowCompose = () => {
    const currentInbox = view.includes("gmail")
      ? "compose-gmail"
      : "compose-local";
    setView(currentInbox);
    setSelectedEmailId(null);
  };

  const handleEmailSent = (sentViaGmail = false) => {
    const inboxView = sentViaGmail ? "inbox-gmail" : "inbox-local";
    setView(inboxView);
    localStorage.setItem("lastInboxView", inboxView);
    setRefreshEmailListKey((prevKey) => prevKey + 1);
  };

  const handleActionOnDetailViewSuccess = () => {
    const previousView = isGmailSelected ? "inbox-gmail" : "inbox-local";
    setView(previousView);
    setSelectedEmailId(null);
    setIsGmailSelected(false);
    setRefreshEmailListKey((prev) => prev + 1);
  };

  const currentDataSourceType = view.includes("gmail") ? "gmail" : "local";

  return (
    <div className="App">
      <ToastContainer position="top-right" autoClose={4000} newestOnTop />
      <header className="app-header">
        <h1>Trình Quản Lý Email</h1>
        <nav className="app-nav">
          <button
            onClick={() => handleSetInboxView("inbox-local")}
            disabled={view === "inbox-local"}
          >
            Hộp Thư Cục Bộ
          </button>
          <button
            onClick={() => handleSetInboxView("inbox-gmail")}
            disabled={view === "inbox-gmail" || !isGmailConnected}
            title={!isGmailConnected ? "Kết nối Gmail để sử dụng" : ""}
          >
            Hộp Thư Gmail
          </button>
          <button onClick={handleShowCompose}>Soạn Email</button>
          {!isGmailConnected && (
            <button onClick={handleConnectGmail} disabled={authInProgress}>
              {authInProgress ? "Đang kết nối..." : "Kết nối với Gmail"}
            </button>
          )}
        </nav>
      </header>

      <main className="app-main">
        {view.startsWith("inbox-") && (
          <EmailList
            key={`${currentDataSourceType}-${refreshEmailListKey}`}
            onEmailSelected={handleEmailSelected}
            dataSourceType={currentDataSourceType}
            onFetchGmailError={handleFetchGmailError}
          />
        )}
        {view === "detail" && selectedEmailId && (
          <EmailDetail
            emailId={selectedEmailId}
            onBackToList={handleBackToList}
            isGmail={isGmailSelected}
            onActionSuccess={handleActionOnDetailViewSuccess}
          />
        )}
        {view === "compose-local" && (
          <ComposeEmailForm
            onEmailSent={() => handleEmailSent(false)}
            sendViaGmail={false}
          />
        )}
        {view === "compose-gmail" && isGmailConnected && (
          <ComposeEmailForm
            onEmailSent={() => handleEmailSent(true)}
            sendViaGmail={true}
          />
        )}
      </main>
    </div>
  );
}
export default App;
