# Random Delay Simulation Plan

這個計畫旨在將 Web API 中所有併發防禦端點的固定 50ms 延遲，修改為 10ms 到 100ms 之間的隨機延遲。這能更真實地模擬生產環境中因網路抖動與系統負載產生的隨機性（「夠亂」），使 Race Condition 的展示與防禦驗證更具說服力。

- [x] **步驟 1: 修改 Web API 中所有端點的 Delay 邏輯**
  - **說明**：
    - 將 `Program.cs` 中所有端點的 `await Task.Delay(50);` 統一修改為 `await Task.Delay(Random.Shared.Next(10, 100));` 以引入隨機延遲。
- [x] **步驟 2: 執行整合測試驗證隨機性效果**
  - **說明**：
    - 執行 `./verify_test.sh` 確保在隨機延遲下，Unsafe 模式依舊會發生嚴重的資源競爭，而其餘四種防禦模式（Safe、Redis、Pessimistic、Optimistic）仍能穩健防禦。
- [x] **步驟 3: 歸檔計畫書與提交推送**
  - **說明**：
    - 將此計畫書封存至 `.archive/`。
    - 更新 `@tree.md`。
    - 提交變更並推送至 GitHub 遠端。
