// Writes every Copilot hook event as ONE JSON line with a fixed set of fields.
// Usage (from the hook config): node .github/hooks/scripts/log-event.cjs <eventName>
const fs = require('fs');
const path = require('path');

const eventName = process.argv[2] || 'unknown';
const repoRoot = path.resolve(__dirname, '..', '..', '..');
const logFile = path.join(repoRoot, 'docs', 'ai-log', 'ai-interactions.jsonl');

let raw = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', (chunk) => { raw += chunk; });
process.stdin.on('end', () => {
  let input = {};
  try { input = raw ? JSON.parse(raw) : {}; } catch { input = { unparsed: raw }; }

  // Copilot and VS Code name fields differently (camelCase vs snake_case), so try both.
  const pick = (...keys) => {
    for (const key of keys) {
      if (input[key] !== undefined && input[key] !== null) return input[key];
    }
    return null;
  };
  const shorten = (value) => {
    if (value === null || value === undefined) return null;
    const text = typeof value === 'string' ? value : JSON.stringify(value);
    const redacted = text.replace(/gh[pousr]_[A-Za-z0-9]{20,}/g, '[REDACTED_TOKEN]');
    return redacted.length > 2000 ? redacted.slice(0, 2000) + ' [truncated]' : redacted;
  };

  const entry = {
    loggedAt: new Date().toISOString(),
    event: eventName,
    sessionId: pick('sessionId', 'session_id'),
    agent: pick('agentType', 'agent_type', 'agentName'),
    prompt: shorten(pick('prompt', 'initialPrompt', 'initial_prompt')),
    tool: pick('toolName', 'tool_name'),
    toolInput: shorten(pick('toolArgs', 'tool_input', 'toolInput')),
    toolResult: shorten(pick('toolResult', 'tool_response', 'toolResponse')),
    error: shorten(pick('error', 'errorMessage')),
  };

  fs.mkdirSync(path.dirname(logFile), { recursive: true });
  fs.appendFileSync(logFile, JSON.stringify(entry) + '\n');

  // Hooks must print exactly one JSON object. {} means "carry on as normal".
  process.stdout.write('{}');
});