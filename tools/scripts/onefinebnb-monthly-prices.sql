-- The first pass only set PmsPriceBasis = Monthly. These imports have no
-- PmsSourceAmountCents, so the listing rent was left at the old × 30 amount.
-- This divides that rent by 30 and stores the result as the PMS source amount.
--
-- Host: help@onefinebnb.com
-- Id:   5ff49308-ef96-4e19-8a7b-3bd69fffa4d9
--
-- Only rents above $40,000 are divided by 30. Ramsdale Draft at $8,406 is left
-- as-is. $299,970 becomes $9,999. $67,800 becomes $2,260. Run against lagedra_prod.

BEGIN;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM auth."AspNetUsers"
        WHERE "Id" = '5ff49308-ef96-4e19-8a7b-3bd69fffa4d9'::uuid
          AND lower("Email") = 'help@onefinebnb.com'
    ) THEN
        RAISE EXCEPTION 'Refusing to update: 5ff49308-ef96-4e19-8a7b-3bd69fffa4d9 is not help@onefinebnb.com';
    END IF;
END $$;

CREATE TEMP TABLE onefinebnb_rent_fix ON COMMIT DROP AS
SELECT
    l."Id" AS listing_id,
    l."MonthlyRentCents" / 30 AS source_cents
FROM listings.listings l
WHERE l."LandlordUserId" = '5ff49308-ef96-4e19-8a7b-3bd69fffa4d9'::uuid
  AND l."IsDeleted" = false
  AND l."MonthlyRentCents" > 4000000
  AND l."MonthlyRentCents" % 30 = 0
  AND l."Id" IN (
      'd405ee77-4e8b-4d7e-b1de-5dcee9d8b681'::uuid,
      '847eb35e-2d05-4a3e-a7cd-9c142a385990'::uuid,
      '984738f5-f566-45f1-8158-a5b4db378d67'::uuid,
      'e9874c44-b848-4932-a43a-e738838fbe33'::uuid,
      '6a1224be-9396-44f0-aa7f-3a157d9fbc56'::uuid,
      '3fc3fd44-706a-4dcc-a74d-b4ab66acd1a8'::uuid,
      '5b06db5b-d14f-4a9f-b2e4-b0ec8e462d89'::uuid,
      'b6f9493f-5cd6-4dfa-84a9-f1dfb80758a7'::uuid,
      '8f6436b5-cfc1-423c-b59d-ab54152287f8'::uuid,
      '065ec513-f4a6-4911-9aba-af84b5fae63b'::uuid,
      '7dd918b5-3b39-4582-a197-043f04bff6dc'::uuid,
      '77af6d93-31a7-4229-80d7-7a2d2df0d0c3'::uuid,
      'd008b46a-d1ca-4375-a04f-293345e1acaa'::uuid,
      '91c0418d-2161-4e51-bb01-f8903a15b93c'::uuid,
      'a9e8a63f-f831-428c-99ac-b6307c9a6169'::uuid,
      '29a1dd6b-5b1a-4a35-a09b-80672d4ef6e9'::uuid,
      'ad447c89-e52a-4807-853c-2ce71138fba5'::uuid,
      '1a3aca3a-81e9-44f6-959c-6b23894dd350'::uuid,
      '2b5c2bcf-0977-4cfa-a9f9-8880522c9c20'::uuid,
      '5052d80d-7334-461d-9345-24a52db9d57f'::uuid,
      '51a9669b-9206-4672-b3b5-8a1b947e771d'::uuid
  );

UPDATE listings.listing_price_history h
SET "EffectiveTo" = GREATEST(CURRENT_DATE, h."EffectiveFrom"),
    "UpdatedAt" = now()
FROM onefinebnb_rent_fix f
WHERE h."ListingId" = f.listing_id
  AND h."EffectiveTo" IS NULL
  AND h."IsDeleted" = false;

INSERT INTO listings.listing_price_history (
    "Id",
    "ListingId",
    "MonthlyRentCents",
    "EffectiveFrom",
    "EffectiveTo",
    "CreatedAt",
    "UpdatedAt",
    "IsDeleted",
    "DeletedAt"
)
SELECT
    gen_random_uuid(),
    f.listing_id,
    f.source_cents,
    CURRENT_DATE,
    NULL,
    now(),
    now(),
    false,
    NULL
FROM onefinebnb_rent_fix f;

UPDATE listings.listings l
SET "MonthlyRentCents" = f.source_cents,
    "UpdatedAt" = now()
FROM onefinebnb_rent_fix f
WHERE l."Id" = f.listing_id
  AND l."MonthlyRentCents" <> f.source_cents;

UPDATE channel_integration.channel_listing_maps m
SET "PmsSourceAmountCents" = f.source_cents,
    "PmsPriceBasis" = 'Monthly',
    "UpdatedAt" = now()
FROM onefinebnb_rent_fix f
JOIN channel_integration.channel_connections c
    ON c."HostUserId" = '5ff49308-ef96-4e19-8a7b-3bd69fffa4d9'::uuid
   AND c."IsDeleted" = false
WHERE m."ListingId" = f.listing_id
  AND m."ConnectionId" = c."Id"
  AND m."IsDeleted" = false;

COMMIT;
