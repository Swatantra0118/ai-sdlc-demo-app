// After an agent tool call that touched a .cs file, apply the repo's formatting rules.
const { execSync } = require('child_process');
const path = require('path');

const repoRoot = path.resolve(__dirname, '..', '..', '..');
let raw = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', (chunk) => { raw += chunk; });
process.stdin.on('end', () => {
  if (/\.cs\b/.test(raw)) {
    try {
      execSync('dotnet format whitespace backend/InvoiceApp.sln', {
        cwd: repoRoot, stdio: 'ignore', timeout: 55000,
      });
    } catch {
      // Never block the agent just because formatting failed.
    }
  }
  process.stdout.write('{}');
});