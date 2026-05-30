# Fix Concurrency Detection Plan

這個計畫旨在修正 WinForm 測試工具在初始 Quota 大於併發請求數時，會漏判「遺失更新（Lost Update）」資源競爭的 Bug。

- [x] **步驟 1: 修改 Form1.cs 判定邏輯**
  - **說明**：在 `Form1.cs` 中，將資源競爭判定修改為雙重條件：`successCount > initialQuota`（透支超扣）或 `successCount != pointsDeducted`（帳目不合），並在 UI 與 Log 上清楚印出對應的警告訊息。
- [x] **步驟 2: 編譯、驗證與提交推送**
  - **說明**：重新編譯專案，啟動 WinFormClient 以相同參數（Quota=500, Requests=50）測試 Unsafe 模式，驗證是否能正確判定為「⚠️ 資源競爭發生（帳目不合）」。驗證成功後，提交並推送到 GitHub 遠端儲存庫。
