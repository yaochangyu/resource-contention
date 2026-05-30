# 多執行緒資源競爭與原子更新防禦示範專案

這個專案旨在具體演示在高併發（High Concurrency）的環境下，多個執行緒同時存取共享資源時所產生的**資源競爭（Race Condition）**問題，並提供使用 SQL Server 原子更新（Atomic Update）進行防禦的正確做法。

---

## 為什麼網路有快有慢，依然會發生「超扣」？

有人可能會直覺認為：「**網路速度不會每次都一樣，Web 伺服器處理也有快有慢，請求會像排隊一樣有先後順序抵達，所以不可能同時發生資源競爭。**」

這是一個常見的誤區。資源競爭發生的條件**不需要絕對的「同時刻抵達」**，只需要**「處理時間在時間軸上有重疊（Overlapping）」**。

### 資源競爭（超扣）時間軸示意圖

假設初始點數為 `10`，有兩個請求 A 與 B 先後抵達（時間僅差 10 毫秒）：

```mermaid
sequenceDiagram
    autonumber
    actor UserA as 請求 A (網路稍快)
    actor UserB as 請求 B (網路稍慢)
    participant DB as SQL Server 資料庫 (餘額: 10)

    UserA->>DB: 1. [12:00:00.010] 讀取點數 (SELECT)
    Note over UserA, DB: 讀到餘額 = 10
    
    UserB->>DB: 2. [12:00:00.020] 讀取點數 (SELECT)
    Note over UserB, DB: 讀到餘額 = 10 (此時 A 還沒扣點完成)

    Note over UserA: 3. 判定 10 > 0，允許執行任務
    Note over UserB: 4. 判定 10 > 0，允許執行任務 (超扣發生！)

    UserA->>DB: 5. [12:00:00.030] 扣點後寫回 (UPDATE to 9)
    Note over DB: 餘額更新為 9

    UserB->>DB: 6. [12:00:00.040] 扣點後寫回 (UPDATE to 9)
    Note over DB: 餘額更新為 9 (覆蓋了 A 的寫入，遺失更新)
```

### 關鍵原因分析
1. **讀取到舊資料**：即使請求 B 比請求 A 晚了 10 毫秒抵達，但只要 **A 還沒有把扣點後的結果寫回資料庫**，B 讀取到的就依然是舊的餘額（`10`）。
2. **遺失更新（Lost Update）**：兩者都以 `10 - 1 = 9` 的結果寫回資料庫，導致雖然**放行了 2 次任務，但資料庫實際上只扣了 1 點**，造成「帳目對不上」與「服務超給」的嚴重 Bug。

---

## 解決方案：資料庫原子更新（Atomic Update）

防禦資源競爭最簡單且高效的方法，是將「讀取、判斷、扣除」合併為單一的 SQL 原子操作：

```sql
UPDATE Members 
SET Points = Points - 1 
WHERE Id = 1 AND Points > 0;
```

* **原理**：利用資料庫內置的行級鎖（Row Lock）。當多個 UPDATE 同時進來時，SQL Server 會強制排隊，且每次更新都會檢查 `Points > 0` 條件。
* **驗證**：在程式中檢查該 SQL 影響的資料列數（Affected Rows）。若為 `1` 代表扣點成功；若為 `0` 代表點數已不足（被其他執行緒搶先扣完），拒絕執行任務。

---

## 解決方案：Redis 快取原子扣點（非同步背景同步）

在面臨超大規模流量（例如秒殺搶購、搶票）時，直接併發存取資料庫會導致資料庫連線池塞滿與效能雪崩。此時可使用 **Redis 作為扣點核心**：

* **原理**：將點數餘額存放在讀寫效能極高的記憶體資料庫 Redis 中。當請求進來時，API 執行 **Lua 腳本** 在 Redis 端以單執行緒原子化地完成「讀取 -> 判斷 -> 扣除」操作。
* **背景同步**：Redis 扣減成功後，API 即刻回傳 `200 OK`，並將最新的餘額推入 .NET 的記憶體 Channel 佇列中。背景由單執行緒服務依序讀取佇列並寫入 SQL Server，不僅徹底隔絕了資料庫併發競爭，也將資料庫的負載減至最輕，保證資料的最終一致性（Eventual Consistency）。

---

## 本專案的進階技術亮點

為了提供更完善且符合企業級開發規範的展示，本專案在實作中加入了以下進階設計：

1. **更嚴謹的資源競爭雙重判定**：
   * 測試工具若只檢查「透支超扣（成功次數 > 初始額度）」，當初始餘額充足（例如 500 點）而併發請求較少（例如 50 次）時，便會漏判資源競爭。
   * 因此，本工具改用**雙重條件判定**：只要滿足 **「成功次數 > 初始點數」**（透支超扣）或 **「成功次數 != 資料庫實際扣除點數」**（Lost Update / 帳目不合），即判定為發生資源競爭。這能極其清晰地展示出在 Unsafe 模式下「放行 50 次，但資料庫只扣了 2 點（其餘 48 次被更新覆蓋）」的遺失更新現象。
2. **EF Core 原始 SQL 即時日誌記錄**：
   * Web API 已配置 EF Core 的 `.LogTo(Console.WriteLine)` 與 `.EnableSensitiveDataLogging()`。
   * 當您啟動 Web API 服務並進行測試時，可以直接在終端機（Console）中實時觀看到底層生成並執行的真實參數化 SQL 語句（如原子更新 `UPDATE [Members] SET [Points] = [Points] - 1 WHERE [Id] = 1 AND [Points] > 0` 以及參數具體數值），非常利於教學展示。
3. **Rider / Visual Studio 設計器編輯支援**：
   * 本專案的 WinForm UI 配置已全面重構為與 IDE 設計檢視器相容的標準 `Form1.Designer.cs` 排版，您可以在 Rider 或 VS 中直接開啟設計器介面進行拖拉或屬性編輯。
4. **序列化背景寫回防範時序錯亂**：
   * Redis 扣點後的資料庫同步採用單執行緒的背景佇列進行循序處理（`await reader.ReadAsync()`），確保寫入資料庫的點數順序與 Redis 扣除順序完全一致，避免背景平行寫入造成 Out-of-Order 的覆蓋問題。

---

## 專案結構

* **`src/ConcurrencyRaceConditionDemo.WebApi`**：提供測試的 Minimal API 端點。
  * `POST /api/points/reset?points=X`：重設 SQL Server 與 Redis 中的點數。
  * `POST /api/points/deduct-unsafe`：不安全扣點（SELECT -> Delay 50ms -> UPDATE），模擬並重現超扣。
  * `POST /api/points/deduct-safe`：安全扣點（使用 EF Core `ExecuteUpdateAsync` 翻譯為原子更新 SQL）。
  * `POST /api/points/deduct-redis`：Redis 快取扣點（Lua 原子扣點 + Channel 背景非同步序列化寫入資料庫）。
* **`src/ConcurrencyRaceConditionDemo.WinFormClient`**：WinForm 測試客戶端。可設定初始額度與併發請求數，並支援選擇 Unsafe、Safe 與 Redis 三種模式進行測試與統計。
* **`verify_test.sh`**：自動化驗證腳本，可在背景啟動 Web API，並發送高併發 `curl` 請求以比對 Unsafe、Safe 與 Redis 三種模式的驗證報告。

---

## 快速開始

### 1. 啟動服務
本專案使用 Docker Compose 啟動 SQL Server 與 Redis 服務：
```bash
docker compose up -d
```

### 2. 執行自動化驗證測試 (Linux/WSL)
專案根目錄下有自動化腳本，會自動編譯、啟動 Web API，並發送 50 個併發請求以呈現 Unsafe 與 Safe 的結果對比：
```bash
./verify_test.sh
```

### 3. 執行 WinForm 介面測試 (Windows)
您可以在 Windows 環境下使用 Visual Studio 或 `dotnet run` 啟動 `WinFormClient` 進行視覺化展示。
