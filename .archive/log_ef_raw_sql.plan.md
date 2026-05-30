# Log EF Raw SQL Plan

這個計畫旨在啟用 EF Core 的 Raw SQL 日誌輸出功能，以利在演示高併發測試時，能直接於 Web API 終端機看見底層的 SQL 運作（包含參數值）。

- [x] **步驟 1: 修改 Program.cs 配置 LogTo**
  - **說明**：在 `Program.cs` 中的 `builder.Services.AddDbContext` 連線設定中，鏈結 `.LogTo(...)` 與 `.EnableSensitiveDataLogging()`，使之能輸出 SQL 語法與具體參數值。
- [x] **步驟 2: 整合測試與提交推送**
  - **說明**：編譯方案，啟動 API 進行併發測試，確認 Raw SQL 成功印出在 Console。驗證成功後，提交變更並推送到 GitHub 遠端儲存庫。
