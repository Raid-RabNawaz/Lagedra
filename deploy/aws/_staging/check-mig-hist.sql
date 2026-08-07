SELECT schemaname, tablename FROM pg_tables WHERE tablename = '__EFMigrationsHistory' ORDER BY 1;
SELECT "MigrationId" FROM public."__EFMigrationsHistory" WHERE "MigrationId" LIKE '%Stripe%' OR "MigrationId" LIKE '%Platform%' OR "MigrationId" LIKE '%20260804%' ORDER BY 1;
SELECT "MigrationId" FROM public."__EFMigrationsHistory" ORDER BY 1 DESC LIMIT 25;
