# Pessimistic & Optimistic Concurrency Plan

這個計畫旨在原有專案基礎上，新增「方案一：悲觀鎖（UPDLOCK）」與「方案二：樂觀鎖（自訂 Version）」模式，以展示不同的並發控制防禦邏輯，並進行比較。

- [x] **步驟 1: 修改 Member 實體並設定自動資料庫重建**
  - **說明**：
    - 在 `Member.cs` 中新增 `Version` 屬性並標上 `[ConcurrencyCheck]` 特性。
    - 修改 `Program.cs` 在啟動時呼叫 `EnsureDeleted()` 再 `EnsureCreated()`，以確保資料庫會以含有 Version 欄位的新結構重新建立。
- [x] **步驟 2: 實作悲觀鎖與樂觀鎖 API 端點**
  - **說明**：
    - 新增 `POST /api/points/deduct-pessimistic`：使用資料庫交易並執行 `WITH (UPDLOCK, HOLDLOCK)` 悲觀鎖查詢。
    - 新增 `POST /api/points/deduct-optimistic`：讀取後手動遞增 Version，並在 `try-catch` 捕捉 `DbUpdateConcurrencyException` 樂觀鎖衝突。
- [x] **步驟 3: 修改 WinFormClient UI 支援 5 種模式排版**
  - **說明**：修改 `Form1.Designer.cs` 與 `Form1.cs`，將 `gbMode` 調整為兩行排列，並新增「悲觀鎖 (UPDLOCK)」與「樂觀鎖 (自訂 Version)」兩個 RadioButton，綁定對應端點與統計標籤。
- [x] **步驟 4: 修改 verify_test.sh 加入方案一、二測試**
  - **說明**：在自動化驗證腳本中，加入悲觀鎖與樂觀鎖的併發測試呼叫，驗證兩者是否皆能精準防禦不超扣。
- [x] **步驟 5: 整合測試、驗證與提交推送**
  - **說明**：編譯整個方案，執行 `./verify_test.sh` 進行 5 種模式的完整對比測試。測試成功後，將變更提交並推送至 GitHub 遠端儲存庫，並將此計畫書封存。
