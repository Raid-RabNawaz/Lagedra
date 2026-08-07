-- Mark Stripe platform fee setting migration as applied (row already exists with live price id)
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260804191953_AddStripePlatformFeePriceIdSetting', '9.0.2')
ON CONFLICT DO NOTHING;

SELECT "MigrationId", "ProductVersion"
FROM public."__EFMigrationsHistory"
WHERE "MigrationId" = '20260804191953_AddStripePlatformFeePriceIdSetting';
