BEGIN;
SELECT COUNT(*) AS before_count FROM identity.host_stripe_accounts;
CREATE TABLE IF NOT EXISTS identity.host_stripe_accounts_backup_20260729 AS
SELECT * FROM identity.host_stripe_accounts;
DELETE FROM identity.host_stripe_accounts;
SELECT COUNT(*) AS after_count FROM identity.host_stripe_accounts;
SELECT COUNT(*) AS backup_count FROM identity.host_stripe_accounts_backup_20260729;
COMMIT;
