import { existsSync } from 'node:fs';
import { loadEnvFile } from 'node:process';
import { fileURLToPath } from 'node:url';
import { defineConfig } from 'prisma/config';

// Studio is a local database viewer. EF Core remains responsible for migrations.
const envPath = fileURLToPath(new URL('./.env', import.meta.url));
if (existsSync(envPath)) loadEnvFile(envPath);

function required(name) {
  const value = process.env[name];
  if (!value) throw new Error(`Required database setting is missing: ${name}`);
  return value;
}

const connection = new URL('postgresql://127.0.0.1');
connection.hostname = process.env.POSTGRES_HOST || '127.0.0.1';
connection.port = process.env.POSTGRES_PORT || '5433';
connection.username = required('POSTGRES_USER');
connection.password = required('POSTGRES_PASSWORD');
connection.pathname = `/${encodeURIComponent(required('POSTGRES_DB'))}`;

export default defineConfig({ datasource: { url: connection.toString() } });
