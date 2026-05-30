#!/usr/bin/env bash

# 設定錯誤時中斷
set -e

API_URL="http://localhost:5000"
API_PROJECT="src/ConcurrencyRaceConditionDemo.WebApi/ConcurrencyRaceConditionDemo.WebApi.csproj"

echo "=== 啟動 Web API 服務 ==="
dotnet run --project "$API_PROJECT" > /dev/null 2>&1 &
API_PID=$!

# 確保結束時會關閉背景的 API 服務
cleanup() {
  echo "=== 關閉 Web API 服務 (PID: $API_PID) ==="
  kill $API_PID || true
}
trap cleanup EXIT

# 等待 Web API 啟動完成
echo "等待 API 啟動..."
for i in {1..10}; do
  if curl -s "$API_URL/api/points" > /dev/null; then
    echo "API 已就緒！"
    break
  fi
  sleep 1
done

run_concurrency_test() {
  local endpoint=$1
  local mode_name=$2
  local initial_quota=10
  local request_count=50

  echo ""
  echo "--------------------------------------------------"
  echo " 執行測試模式: $mode_name"
  echo "--------------------------------------------------"
  
  # 1. 重設點數為 10
  echo "重設資料庫點數為 $initial_quota..."
  curl -s -X POST "$API_URL/api/points/reset?points=$initial_quota" -o /dev/null

  # 2. 併發發送 50 個扣點請求
  echo "正在併發發送 $request_count 個請求到 $endpoint..."
  
  # 建立臨時檔案記錄結果
  local temp_file=$(mktemp)
  
  # 併發發送
  local pids=()
  for i in $(seq 1 $request_count); do
    curl -s -w "%{http_code}\n" -o /dev/null -X POST "$API_URL$endpoint" >> "$temp_file" &
    pids+=($!)
  done
  
  # 等待所有背景 curl 任務完成
  for pid in "${pids[@]}"; do
    wait "$pid" 2>/dev/null || true
  done
  
  # 3. 統計狀態碼
  local success_count=$(grep -c "200" "$temp_file" || true)
  local fail_count=$(grep -c "400" "$temp_file" || true)
  rm -f "$temp_file"

  # 4. 撈取最終資料庫點數
  local db_result=$(curl -s "$API_URL/api/points")
  # 解析 JSON 欄位 "points" (簡單用 grep -o)
  local final_points=$(echo "$db_result" | grep -o '"points":[0-9\-]*' | cut -d':' -f2 || echo "0")

  # 5. 輸出報告
  echo "測試結果統計:"
  echo "  - 初始點數: $initial_quota"
  echo "  - 總請求數: $request_count"
  echo "  - 成功扣除次數 (200 OK): $success_count"
  echo "  - 被拒絕次數 (400 Bad Request): $fail_count"
  echo "  - 資料庫最終餘額: $final_points"

  if [ "$success_count" -gt "$initial_quota" ]; then
    echo "  ⚠️ 驗證結果: [失敗] 發生資源競爭！超扣數量: $(($success_count - $initial_quota)) 筆！"
  else
    echo "  ✅ 驗證結果: [成功] 無超扣現象。扣點數量被精準限制在 $initial_quota 筆。"
  fi
}

# 執行不安全扣點測試
run_concurrency_test "/api/points/deduct-unsafe" "Unsafe (未處理資源競爭)"

# 執行安全扣點測試
run_concurrency_test "/api/points/deduct-safe" "Safe (使用資料庫原子更新防禦)"
