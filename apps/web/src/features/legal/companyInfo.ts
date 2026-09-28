/** Public business identity from IRS Notice CP575G. Do not put the EIN on the website. */
export const COMPANY = {
  brand: "Lagedra",
  legalName: "Lagedra LLC",
  street: "14622 Ventura Boulevard",
  city: "Sherman Oaks",
  region: "CA",
  postalCode: "91403",
  country: "United States",
  email: "info@lagedra.com",
  phoneDisplay: "+1 (213) 377-5272",
  phoneE164: "+12133775272",
  website: "https://www.lagedra.com",
} as const;

export const COMPANY_MAILING_ADDRESS = `${COMPANY.street}, ${COMPANY.city}, ${COMPANY.region} ${COMPANY.postalCode}`;
