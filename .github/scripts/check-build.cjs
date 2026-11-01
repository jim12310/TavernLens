// Approves a new Hearthstone card build for combat odds when every simulator test passes against it.
// Run after `node src/update-data.cjs` (which downloads the current card data).
// Exit codes: 0 = nothing to do or approved (simulator/compatibility.json updated), 1 = tests failed (file restored).
const fs = require('node:fs');
const path = require('node:path');
const {execFileSync} = require('node:child_process');

const root = path.resolve(__dirname, '..', '..');
const compatFile = path.join(root, 'simulator', 'compatibility.json');
const build = JSON.parse(fs.readFileSync(path.join(root, 'data', 'cards.json'))).build;
const original = fs.readFileSync(compatFile, 'utf8');
const compat = JSON.parse(original);
const out = process.env.GITHUB_OUTPUT;
const report = (key, value) => { if (out) fs.appendFileSync(out, `${key}=${value}\n`); };

report('build', build);
if (!/^\d+$/.test(build || '')) { console.error('No card build in data/cards.json'); process.exit(1); }
if (compat.testedCardBuilds.includes(build)) {
  console.log(`Card build ${build} is already approved for simulator ${compat.version}.`);
  report('approved', 'false');
  process.exit(0);
}

console.log(`New card build ${build}: testing simulator ${compat.version} / reference ${compat.referenceVersion} against it.`);
const candidate = {...compat, testedCardBuild: build, testedCardBuilds: [...compat.testedCardBuilds, build], reviewedAt: new Date().toISOString().slice(0, 10)};
fs.writeFileSync(compatFile, JSON.stringify(candidate));

const suites = ['test-release', 'test-combat', 'test-duos', 'test-feeds'];
try {
  for (const suite of suites) {
    const result = execFileSync(process.execPath, [path.join(root, 'src', suite + '.cjs')], {cwd: root, encoding: 'utf8'});
    console.log(`${suite}: ${result.trim().split('\n').pop()}`);
  }
} catch (e) {
  fs.writeFileSync(compatFile, original);
  console.error(`Tests failed for card build ${build}; approval withheld.\n${e.stdout || ''}${e.stderr || e.message}`);
  report('approved', 'false');
  process.exit(1);
}
console.log(`All suites passed: card build ${build} approved.`);
report('approved', 'true');
