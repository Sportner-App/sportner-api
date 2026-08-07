# Supabase RLS + Data API hardening

**Goal:** Business tables must not be readable/writable via the public Supabase Data API (`anon` / `authenticated`).  
**Backend:** Continues to use the Npgsql connection string (pooler / postgres role). Table owner bypasses RLS.

Apply in **Supabase → SQL Editor**. Review once, then run.

After SQL: prefer disabling Data API under **Integrations → Data API** (or leave enabled but useless without grants/policies).

---

## 1) Enable RLS + revoke public access (all business tables)

```sql
-- Sportner business tables (EF PascalCase names in public schema)
DO $$
DECLARE
  t text;
  tables text[] := ARRAY[
    'Users',
    'UserProfiles',
    'UserDevices',
    'UserSessions',
    'UserSports',
    'UserSavedLocations',
    'UserStatistics',
    'Sports',
    'Events',
    'EventParticipants',
    'EventWaitlists',
    'EventReminderDispatches',
    'Conversations',
    'ConversationMembers',
    'Messages',
    'Friendships',
    'Posts',
    'PostMedia',
    'PostLikes',
    'PostComments',
    'Reviews',
    'Notifications',
    'NotificationSettings',
    'Badges',
    'UserBadges',
    'Reports',
    'ReportReasons'
  ];
BEGIN
  FOREACH t IN ARRAY tables
  LOOP
    EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', t);
    -- No policies for anon/authenticated → no access via those roles
    EXECUTE format('REVOKE ALL ON TABLE public.%I FROM anon, authenticated', t);
  END LOOP;
END $$;
```

## 2) Sanity checks

```sql
-- RLS should be enabled
SELECT c.relname, c.relrowsecurity
FROM pg_class c
JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE n.nspname = 'public'
  AND c.relkind = 'r'
  AND c.relname = ANY (ARRAY[
    'Users','UserProfiles','Events','Posts','Messages','Reports'
  ])
ORDER BY 1;

-- Should return no permissive policies for anon/authenticated on these tables
SELECT schemaname, tablename, policyname, roles, cmd
FROM pg_policies
WHERE schemaname = 'public'
  AND tablename IN ('Users','UserProfiles','Events','Posts','Messages','Reports');
```

## 3) Backend smoke (after apply)

1. `dotnet run --project src/API`
2. OTP + `GET /api/user-profiles/me` still works (Npgsql path).
3. Optional: call Supabase REST with **anon** key on `Users` → should fail / empty.

## Notes

- Do **not** add `GRANT` to `anon` / `authenticated` for business tables.
- Do **not** paste DB passwords or service_role keys into this file.
- If a new EF table is added later, append its name to the array above and re-run.
