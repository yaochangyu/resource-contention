# Redis Concurrency Demo Plan

這個計畫旨在原有專案基礎上，新增「思路 B：Redis 快取原子扣點 + 背景非同步同步資料庫」模式，以展示高併發下的極致效能與資料庫減壓方案。

- [x] **步驟 1: 修改 Docker Compose 加上 Redis 服務**
  - **說明**：在 `docker-compose.yml` 中新增 Redis 7.0 容器並對應 `6379:6379`，啟動該服務。
- [x] **步驟 2: 安裝 StackExchange.Redis 套件**
  - **說明**：在 Web API 專案中安裝 `StackExchange.Redis` 以進行 Redis 連線與操作。
- [x] **步驟 3: 修改 Program.cs 初始化 Redis 與 Reset API**
  - **說明**：
    - 在服務中註冊 `IConnectionMultiplexer`。
    - 更新 `POST /api/points/reset`，重設點數時除了寫入 SQL Server，也必須同步將點數以 `string` 格式（如 `member:1:points`）寫入 Redis。
- [x] **步驟 4: 實作 Redis 扣點端點與背景寫回邏輯**
  - **說明**：
    - 新增 `POST /api/points/deduct-redis`。
    - 撰寫 Lua 腳本，在 Redis 端原子判斷點數是否大於 0，是的話扣 1 並回傳。
    - 扣點成功後，以非同步背景任務將資料更新至 SQL Server。
- [x] **步驟 5: 修改 WinFormClient UI 加上 Redis 模式**
  - **說明**：在 Form1 的模式選擇中，新增一個 RadioButton 「Redis 模式 (非同步快取扣點)」，並在發送請求時將端點導向 `/api/points/deduct-redis`。
- [x] **步驟 6: 修改 verify_test.sh 並執行整合測試驗證**
  - **說明**：在驗證腳本中，加入「Redis 模式」的高併發測試，編譯運行並驗證其是否具備「不超扣」且「最終能同步回 SQL Server」的特性。
