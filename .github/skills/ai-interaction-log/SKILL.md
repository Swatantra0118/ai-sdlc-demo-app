---
name: ai-interaction-log
description: Append a structured summary of the AI task just completed to docs/ai-log/AI_INTERACTIONS.md. Use at the end of every task, or when the user types /ai-interaction-log.
---
# AI interaction log entry

Append (never rewrite) one entry to docs/ai-log/AI_INTERACTIONS.md using exactly this template:

### <YYYY-MM-DD HH:mm> — <short task title>
- Agent: <agent name and model if known>
- Prompt (summary): <one or two lines>
- Files changed: <list or "none">
- Commands run: <list or "none">
- Result: <what happened, tests passed/failed with counts>
- Human verdict: <accepted / edited / rejected and why — leave as TODO for the human>