SELECT "MigrationId", "ProductVersion"
FROM public."__EFMigrationsHistory"
WHERE "MigrationId" IN (
  '20260226211635_AddPlatformSettings',
  '20260713183301_AddReviewWindowDaysSetting',
  '20260713205724_AddReviewReminderIntervalDays',
  '20260710195921_AddDepositReturnEvidenceManifest',
  '20260804191953_AddStripePlatformFeePriceIdSetting'
)
ORDER BY 1;

-- Also list all migrations that look like platform settings ones from Infrastructure
SELECT "MigrationId", "ProductVersion"
FROM public."__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%Setting%' OR "MigrationId" LIKE '%PlatformSettings%' OR "MigrationId" LIKE '%Review%' OR "MigrationId" LIKE '%DepositReturnEvidenceManifest%'
ORDER BY 1;
