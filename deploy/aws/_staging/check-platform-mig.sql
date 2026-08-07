SELECT "Key", LEFT("Value",40) AS value_prefix FROM platform.platform_settings WHERE "Key" LIKE 'stripe%' ORDER BY 1;
SELECT migration_id FROM platform."__EFMigrationsHistory" ORDER BY 1;
SELECT COUNT(*) AS hist_count FROM platform."__EFMigrationsHistory";
