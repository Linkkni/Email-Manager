// src/index.js (thường không cần sửa nhiều)
import React from 'react';
import ReactDOM from 'react-dom/client';
import './index.css'; // File CSS global
import App from './App';
// import reportWebVitals from './reportWebVitals'; // Tùy chọn

const root = ReactDOM.createRoot(document.getElementById('root'));
root.render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
// reportWebVitals();

// src/App.css (Tạo file này nếu chưa có)
/* Bạn có thể thêm CSS tùy chỉnh ở đây */

