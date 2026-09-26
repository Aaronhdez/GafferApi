import { createReadStream } from 'node:fs';
import { readdir, stat } from 'node:fs/promises';
import path from 'node:path';
import readline from 'node:readline';

function usage() {
  console.error('Usage: node usage-slice.mjs --file <path-or-glob> --repo <name> --window <role>=<start>/<end> [--window ...] [--agent <copilot|claude-code>]');
  process.exit(2);
}

function parseArgs(argv) {
  const options = { windows: [] };
  for (let index = 0; index < argv.length; index += 1) {
    const flag = argv[index];
    if (!['--file', '--repo', '--window', '--agent'].includes(flag) || !argv[index + 1]) usage();
    const value = argv[++index];
    if (flag === '--window') options.windows.push(value);
    else options[flag.slice(2)] = value;
  }
  if (!options.file || !options.repo || options.windows.length === 0) usage();
  return options;
}

function expandEnvironment(input) {
  const expanded = input
    .replace(/%([^%]+)%/g, (_, name) => process.env[name] ?? '')
    .replace(/\$([A-Za-z_][A-Za-z0-9_]*)/g, (_, name) => process.env[name] ?? '');
  if (/%[^%]+%/.test(expanded) || /\$[A-Za-z_]/.test(expanded)) {
    throw new Error('The file path contains an environment variable that is not set.');
  }
  return expanded.startsWith('~') ? path.join(process.env.USERPROFILE ?? process.env.HOME ?? '', expanded.slice(1)) : expanded;
}

function parseWindow(value) {
  const separator = value.indexOf('=');
  const rangeSeparator = value.lastIndexOf('/');
  if (separator < 1 || rangeSeparator <= separator) usage();
  const role = value.slice(0, separator);
  const startText = value.slice(separator + 1, rangeSeparator);
  const endText = value.slice(rangeSeparator + 1);
  const start = Date.parse(startText);
  const end = Date.parse(endText);
  if (!Number.isFinite(start) || !Number.isFinite(end) || start >= end) usage();
  return { role, start, end, startText, endText };
}

function escapeRegex(value) {
  return value.replace(/[|\\{}()[\]^$+?.]/g, '\\$&').replace(/\*/g, '.*');
}

async function expandFiles(input) {
  const resolved = path.resolve(expandEnvironment(input));
  if (!/[*?]/.test(resolved)) {
    if (!(await stat(resolved)).isFile()) throw new Error('The input path is not a file.');
    return [resolved];
  }

  const separator = resolved.lastIndexOf(path.sep);
  const directory = resolved.slice(0, separator) || path.parse(resolved).root || '.';
  const pattern = resolved.slice(separator + 1);
  if (pattern.includes(path.sep)) throw new Error('Globs are supported in the file name, not directory names.');
  const matcher = new RegExp(`^${escapeRegex(pattern)}$`, 'i');
  return (await readdir(directory, { withFileTypes: true }))
    .filter((entry) => entry.isFile() && matcher.test(entry.name))
    .map((entry) => path.join(directory, entry.name))
    .sort();
}

function normalizeAgent(value) {
  const normalized = String(value ?? 'unknown').toLowerCase();
  if (normalized.includes('copilot')) return 'copilot';
  if (normalized.includes('claude')) return 'claude-code';
  return normalized;
}

function eventTime(event) {
  const value = event.timestamp ?? event.createdAt ?? event.time ?? event.meta?.timestamp;
  if (typeof value === 'number') return value < 1e12 ? value * 1000 : value;
  const parsed = Date.parse(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function addEvent(bucket, event) {
  bucket.events += 1;
  for (const [key, fields] of Object.entries({
    input: ['inputTokens'],
    cacheRead: ['cacheReadInputTokens'],
    cacheWrite: ['cacheWriteInputTokens'],
    output: ['outputTokens'],
    cost: ['costUSD', 'costUsd'],
  })) {
    const value = fields.map((field) => event[field]).find((candidate) => Number.isFinite(candidate));
    if (value !== undefined) bucket[key] = (bucket[key] ?? 0) + value;
  }
  if (event.sourceKind === 'verified' || event.meta?.sourceKind === 'verified') bucket.verified = true;
}

function newBucket() {
  return { events: 0, input: null, cacheRead: null, cacheWrite: null, output: null, cost: null, verified: false };
}

function display(bucket) {
  if (!bucket || bucket.events === 0) return ['sin datos', 'sin datos', 'sin datos', 'sin datos', 'sin datos', 'sin datos', 'sin datos'];
  const number = (value) => value === null ? 'n/a' : String(value);
  const origin = bucket.verified ? 'medido:token-consumer' : 'estimado:token-consumer';
  return [bucket.events, number(bucket.input), number(bucket.cacheRead), number(bucket.cacheWrite), number(bucket.output), number(bucket.cost), origin];
}

async function main() {
  const options = parseArgs(process.argv.slice(2));
  const windows = options.windows.map(parseWindow);
  const files = await expandFiles(options.file);
  if (files.length === 0) throw new Error('No JSONL files matched the input path.');

  const agentFilter = options.agent ? normalizeAgent(options.agent) : null;
  const roleBuckets = new Map();
  const sessionBuckets = new Map();
  const observedAgents = new Set(agentFilter ? [agentFilter] : []);
  let malformed = 0;

  for (const file of files) {
    const lines = readline.createInterface({ input: createReadStream(file), crlfDelay: Infinity });
    for await (const line of lines) {
      if (!line.trim()) continue;
      let event;
      try {
        event = JSON.parse(line);
      } catch {
        malformed += 1;
        continue;
      }
      if (event.meta?.repository !== options.repo) continue;
      const timestamp = eventTime(event);
      if (timestamp === null) continue;
      const agent = normalizeAgent(event.agent ?? event.meta?.agent ?? event.provider);
      if (agentFilter && agent !== agentFilter) continue;
      observedAgents.add(agent);

      const matchingWindows = windows.filter((window) => timestamp >= window.start && timestamp <= window.end);
      if (agent === 'copilot') {
        if (matchingWindows.length > 0) {
          const bucket = sessionBuckets.get(agent) ?? newBucket();
          addEvent(bucket, event);
          sessionBuckets.set(agent, bucket);
        }
      } else if (matchingWindows.length > 0) {
        const window = matchingWindows[0];
        const key = `${window.role}\u0000${agent}`;
        const bucket = roleBuckets.get(key) ?? newBucket();
        addEvent(bucket, event);
        roleBuckets.set(key, bucket);
      }
    }
  }

  const agents = [...observedAgents].sort();
  if (agents.length === 0) agents.push(agentFilter ?? 'copilot');
  const rows = [];
  for (const window of windows) {
    for (const agent of agents) {
      const bucket = agent === 'copilot' ? null : roleBuckets.get(`${window.role}\u0000${agent}`);
      rows.push([`${window.role} ${window.startText}/${window.endText}`, agent, ...display(bucket)]);
    }
  }
  for (const [agent, bucket] of sessionBuckets) {
    rows.push(['sesión total (por rol: no disponible)', agent, ...display(bucket)]);
  }

  console.log('| Ventana | Agente | Eventos | Entrada nueva | Caché lectura | Caché escritura | Salida | Coste USD | Origen |');
  console.log('| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |');
  for (const row of rows) console.log(`| ${row.map((value) => String(value).replaceAll('|', '\\|')).join(' | ')} |`);
  if (malformed > 0) console.error(`Ignored malformed JSONL rows: ${malformed}`);
}

main().catch((error) => {
  console.error(`usage-slice: ${error.message}`);
  process.exitCode = 1;
});