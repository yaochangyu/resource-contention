# Concurrency Race Condition Demo Plan

這個計畫旨在建立一個 ASP.NET 10 Web API 與 SQL Server 的示範專案，並搭配 WinForm 測試客戶端，來具體呈現多執行緒併發下的「資源競爭（超扣）」與「正確處理（安全扣點）」之差異。

- [x] **步驟 1: 建立專案結構與 SQL Server 開發環境**
  - **說明**：建立方案（Solution），並使用 Docker Compose 啟動 Microsoft SQL Server 容器，準備好測試用的資料庫。
- [x] **步驟 2: 建立 ASP.NET 10 Web API 與資料庫模型**
  - **說明**：使用 .NET 10 建立 Web API 專案。設定 EF Core 連接字串，定義 `Account`（包含 `Points`）的資料表，並撰寫 Database Migrations 建立資料表與初始測試資料。
- [x] **步驟 3: 實作不安全扣點（Unsafe）與安全扣點（Safe）API 端點**
  - **說明**：
    - `POST /api/points/reset`：重設點數為指定額度。
    - `POST /api/points/deduct-unsafe`：先 SELECT 後 UPDATE，不加任何鎖（中間加上 `Task.Delay` 模擬高延遲），重現超扣。
    - `POST /api/points/deduct-safe`：使用資料庫原子更新（Atomic Update）確保不可超扣。
- [x] **步驟 4: 建立 WinForm 測試客戶端**
  - **說明**：建立 WinForm 專案。設計 UI 介面，讓使用者可以輸入「初始 Quota」與「併發請求數」，並能選擇測試「安全」或「不安全」模式。
- [x] **步驟 5: 實作 WinForm 併發請求邏輯**
  - **說明**：使用 `HttpClient` 與 `Task.WhenAll` 發送高併發請求，記錄請求成功的數量，並在測試後撈取資料庫最終餘額，呈現給使用者看是否發生超扣。
- [x] **步驟 6: 執行系統整合測試與驗證**
  - **說明**：編譯整個方案，啟動 API 與 WinForm，輸入測試參數（例如 Quota = 10，請求數 = 50），驗證 Unsafe 模式下出現超扣（餘額變負數或處理次數超過 Quota），而 Safe 模式下能精準停在 0。
