{

&#x20; "contract\_name": "Task\_Visualization\_Protocol",

&#x20; "version": "1.0.0",

&#x20; "description": "規範 AI 輸出格式，使 UI 能實現接、播、追、稽核功能。",

&#x20; "schema": {

&#x20;   "run\_id": "string (UUID v4 格式)",

&#x20;   "timestamp": "ISO-8601 string",

&#x20;   "stage": "enum \[intake, analysis, plan, execution, verification, delivery]",

&#x20;   "status": "enum \[pending, in\_progress, completed, failed]",

&#x20;   "active\_context": "array (可填入 \[coding.md, debug.md, uiux.md])",

&#x20;   "payload": {

&#x20;     "summary": "string (目前進度的簡短摘要)",

&#x20;     "evidence": "string | object (例如：程式碼片段、Git 指令或使用者路徑圖)",

&#x20;     "next\_action": "string (下一步預計執行的動作)"

&#x20;   }

&#x20; },

&#x20; "constraints": \[

&#x20;   "所有非 JSON 的自然語言說明必須置於 JSON 區塊之外。",

&#x20;   "當 status 為 'failed' 時，evidence 必須包含具體的錯誤訊息與修正計畫。",

&#x20;   "active\_context 欄位必須據實反映當前行為所依據的專家文件。"

&#x20; ]

}

