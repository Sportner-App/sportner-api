# Ops notes

Operational runbooks for Sportner API (no secrets in this folder).

| Doc | Purpose |
| --- | ------- |
| [supabase-rls.md](supabase-rls.md) | Enable RLS + revoke public roles on business tables |
| [railway-migration.md](railway-migration.md) | Render → Railway hosting cutover (DB stays on Supabase) |

Apply SQL in the Supabase SQL Editor against the intended project. Backend continues via Npgsql (table owner / pooler), which bypasses RLS.
