# Pure Redis Storage Plan

這個計畫旨在修改原有的 Redis 併發扣點方案，使其成為「純 Redis 儲存」模式，不再透過背景 Channel 同步回寫 SQL Server，並對 API、WinForm 用戶端及驗證腳本進行對應的修改以確保功能能正確運行並被驗證。

- [x] **步驟 1: 修改 Web API 以移除 SQL 背景同步並新增 Redis 查詢端點**
  - **說明**：
    - 在 `Program.cs` 中移除 `dbUpdateChannel` 的定義與背景消費 `Task.Run` 同步服務。
    - 移除 `POST /api/points/deduct-redis` 中對 Channel 寫入點數的程式碼.
    - 新增 `GET /api/points/redis` 用於查詢當前 Redis 中的點數，以便測試端比對。
- [x] **步驟 2: 修改 WinFormClient UI 邏輯以在 Redis 模式下讀取 Redis 點數**
  - **說明**：
    - 修改 `Form1.cs` 當模式為 Redis 時，查詢餘額改打 `/api/points/redis`。
    - 修改 UI 日誌文字，使 Redis 模式的餘額顯示與判定能精準反映 Redis 中的最新狀態。
- [x] **步驟 3: 修改 verify_test.sh 驗證腳本**
  - **說明**：
    - 在 Redis 測試段落中，將最終餘額讀取路徑修改為 `/api/points/redis`。
    - 移除 Redis 扣點後的 `sleep 0.5`（因已無背景同步任務，扣點後可立即查詢）。
- [x] **步驟 4: 執行整合壓測驗證與提交推送**
  - **說明**：
    - 編譯專案，執行 `./verify_test.sh` 確保 5 種模式皆正常防禦超扣。
    - 更新 `@tree.md`，封存計畫書至 `.archive/` 並提交推送。
