-- Publishes the full California lease agreement (15 pages, including the
-- BROKER DISCLOSURE AND AUTHORIZATION ADDENDUM) as the live US-CA template.
--
-- Generated from CaliforniaLeaseTemplateHtml.Body (46440 chars).
--
-- Why this exists: the startup seed in SeedCaliforniaLeaseTemplateCommand
-- failed on every boot with "expected to affect 1 row(s), but actually
-- affected 0 row(s)", so production stayed on the old ~4.5 KB template that
-- renders to 3 pages. This performs the same publish the fixed seed does,
-- without requiring a code deployment.
--
-- Run with:
--   psql "$PROD_CONNECTION_STRING" -v ON_ERROR_STOP=1 -f publish-california-lease-template.sql
--
-- Safe to re-run: it no-ops when the live body already matches.

\set ON_ERROR_STOP on

BEGIN;

DO $outer$
DECLARE
    v_template_id     uuid;
    v_old_version_id  uuid;
    v_new_version_id  uuid := 'c9bc558a-7aa7-4f8b-91e0-c187ede7b17f';
    v_next_number     integer;
    v_deleted         integer;
BEGIN
    SELECT "Id", "ActiveVersionId"
      INTO v_template_id, v_old_version_id
      FROM lease_agreements.lease_templates
     WHERE jurisdiction_code = 'US-CA'
       AND NOT "IsDeleted";

    IF v_template_id IS NULL THEN
        RAISE EXCEPTION 'No US-CA lease template row found.';
    END IF;

    -- Already published? Nothing to do.
    IF EXISTS (
        SELECT 1
          FROM lease_agreements.lease_template_versions
         WHERE "Id" = v_old_version_id
           AND "BodyHtml" = $leasebody$<h1>California Lease Agreement</h1>
{{#if owner.fullName}}
<p>This Lease Agreement ("Lease") is entered into on {{lease.effectiveDate}}, by and between {{owner.fullName}} ("Owner" / "Landlord"), acting through authorized property manager {{host.fullName}}, and {{tenant.fullName}} ("Tenant").</p>
{{/if}}
{{#unless owner.fullName}}
<p>This Lease Agreement ("Lease") is entered into on {{lease.effectiveDate}}, by and between {{host.fullName}} ("Landlord") and {{tenant.fullName}} ("Tenant").</p>
{{/unless}}

<p><strong>Leased Property.</strong> The Landlord hereby leases to the Tenant the {{listing.propertyTypeLabel}} located at {{listing.fullAddress}} ("Leased Property").</p>

<p><strong>Term.</strong> This Lease will start on {{deal.startDate}} ("Start Date") and will continue for an initial fixed term of {{deal.termMonths}} months, ending on {{deal.endDate}} ("Initial Term"). After the Initial Term expires, the tenancy shall automatically continue on a month-to-month basis unless either party provides at least thirty (30) days' advance written notice of termination or intent to vacate ("Termination Date"). Notice must be delivered in person, sent by certified or registered mail, or sent via email to the designated and proper email contact of the receiving party. Rent will be due and payable up to and including the Termination Date.</p>
<p>Any request to extend the lease beyond the Initial Term or any month-to-month period must also be communicated with at least thirty (30) days' advance written notice prior to the desired extension or continuation date, and is subject to mutual agreement by both parties.</p>
<p>In the event of a sale, this Lease shall remain subject to applicable California law and any lawful termination rights available to the Landlord.</p>

<p><strong>Rent.</strong> The Tenant agrees that the Rent shall be paid either directly by the Tenant to the Landlord as rent for the use and occupancy of the Leased Property the sum of {{deal.monthlyRent}} due on the {{listing.rentDueDay}} day of each month ("Rent").</p>
<p>The Rent shall be paid by the following method(s):</p>
{{#if listing.paymentMethods}}
<ul>
<li>Electronic Payment Methods</li>
</ul>
<p>The following electronic payment methods will be accepted:</p>
<ul>
<li>{{listing.paymentMethods}}</li>
</ul>
{{/if}}
{{#unless listing.paymentMethods}}
<p>The Rent shall be paid by the method(s) designated in writing by the Landlord.</p>
{{/unless}}
{{#if owner.fullName}}
<p>The Rent shall be payable to the Landlord or the Landlord's authorized property manager. The Property Manager can be reached by email at {{host.email}} and by phone at {{host.phone}}. The Owner can be reached by email at {{owner.email}}.</p>
{{/if}}
{{#unless owner.fullName}}
<p>The Rent shall be payable to the Landlord. The Landlord can be reached by email at {{host.email}} and by phone at {{host.phone}}.</p>
{{/unless}}
<p>If any payment is returned for non-sufficient funds or because the Tenant stops payments, then, after that, the Landlord may, in writing, require the Tenant to pay future Rent payments by cash, cashier's check, or money order.</p>

<p><strong>Non-Sufficient Funds.</strong> The Tenant will be charged a monetary fee of {{listing.nsfFirstFee}} as reimbursement of the expenses incurred by the Landlord for the first check that is returned to the Landlord for lack of sufficient funds, and {{listing.nsfSubsequentFee}} for each subsequent check returned for lack of sufficient funds. This Paragraph is in accordance with California Civil Code § 1719.</p>
<p>The Landlord reserves the right to demand future Rent payments by cash, cashier's check, or money order in the event a check is returned for insufficient funds. Nothing in this Paragraph limits other remedies available to the Landlord as a payee of a dishonored check. The Landlord and the Tenant agree that 3 returned checks in any 12-month period constitute frequent return of checks due to insufficient funds and may be considered a just cause for eviction. The Landlord shall notify the Tenant of this election at least 30 days before the date the Tenant is to make the first payment by cash, cashier's check, or money order.</p>

<p><strong>Security Deposit.</strong> Upon execution, the Tenant shall pay to the Landlord a security deposit of {{deal.securityDeposit}} ("Security Deposit") for the purpose set forth in Civil Code § 1950.5. The Landlord will hold this Security Deposit for the faithful performance by the Tenant of their obligations under this Lease and for the cleaning and repairing of the Leased Property after surrender by the Tenant. The Landlord agrees to hold the Security Deposit for the Tenant, free from the claim of any creditor of the Landlord.</p>
<p>Prior to the Termination Date, the Landlord will inform the Tenant about their option to request an inspection of the Leased Property. Upon request by the Tenant, and no earlier than two weeks before the Termination Date, the Landlord will conduct an inspection of the Leased Property. After this inspection, the Landlord will furnish the Tenant with a detailed list detailing any suggested repairs and cleaning that may be deducted from the Security Deposit. The Tenant will have the opportunity to resolve these issues before the Termination Date to avoid deductions from the Security Deposit. The Landlord will return to the Tenant the full amount of the Security Deposit within 21 calendar days after the Tenant has vacated the Leased Property, minus any amounts that are reasonably necessary to remedy any defaults in the payment of Rent by the Tenant, to repair damages to the Leased Property caused by the Tenant or the Tenant's guests other than ordinary wear and tear, and to clean the Leased Property. At the time the Landlord returns the Security Deposit to the Tenant, the Landlord will furnish the Tenant with an itemized written statement of the amount of the Security Deposit received, the charges made by the Landlord against the Security Deposit, and the disposition made or to be made of the Security Deposit.</p>
<p>The Security Deposit will not be returned until the Tenant has vacated the Leased Property. Any return of the Security Deposit shall be by check made payable to the Tenant.</p>

<p><strong>Late Fee.</strong> If the Landlord has not received any Rent payment within {{listing.lateFeeGraceDays}} days after the due date, a late fee of {{listing.lateFeeAmount}} ({{listing.lateFeePercent}} of monthly Rent) shall apply. The Landlord and the Tenant agree that it is and will be impracticable and extremely difficult to fix the actual damages suffered by the Landlord in the event the Tenant makes a late payment of Rent, and that the above charge represents a reasonable approximation of the damages the Landlord is likely to suffer from a late payment. The Landlord and the Tenant further agree that this Provision does not establish a grace period of the payment of Rent, and that the Landlord may give the Tenant a three-day written notice to pay or quit the Leased Property in accordance with Cal. Code Civ. Proc. § 1161(2) at any time after the payment is due.</p>

<p><strong>Failure to Pay.</strong> Pursuant to Civil Code § 1785.26, you are hereby notified that a negative credit report reflecting on your credit record may be submitted to a credit reporting agency if you fail to fulfill the terms of your credit obligations, such as your financial obligations under the terms of this Lease.</p>

<p><strong>Default.</strong> The Landlord and the Tenant acknowledge that each condition, covenant, and provision of this Lease is essential and reasonable. A breach by the Tenant of any condition, covenant, or provision will be considered a material breach. In the event of a material breach by the Tenant, the Landlord may issue a written 3-day notice, specifying the breach and requiring the Tenant to cure the default if possible. If the Tenant fails to cure the default within the 3-day period, or if cure is not feasible, the Lease may be terminated.</p>

<p><strong>Utilities.</strong> {{listing.utilitiesResponsibility}} The Tenant also agrees to comply with any environmental, waste management, recycling, energy conservation, or water conservation programs implemented by the Landlord. Yard maintenance by the Tenant: {{listing.yardMaintenanceByTenant}}.</p>

{{#if listing.isFurnished}}
<p><strong>Furnishings.</strong> The Premises is furnished and includes, where applicable, beds and bed frames, mattresses, closets or wardrobes, sofas, dining tables, chairs, coffee tables, desks and workspaces, outdoor furniture, and lawn or patio furniture. The Premises also includes standard major appliances such as a refrigerator, stove, oven, microwave, dishwasher, washer, and dryer. Where provided, the Premises may also include televisions and basic entertainment equipment.</p>
<p>All furnishings and appliances are provided in their existing condition at move-in. The Tenant agrees to use all items with reasonable care and acknowledges that normal wear and tear is expected. Any intentional damage, loss, or misuse beyond normal wear and tear may result in repair or replacement charges. Included items: {{listing.includedAppliances}}.</p>
{{/if}}
{{#unless listing.isFurnished}}
<p><strong>Furnishings.</strong> The Premises is provided as unfurnished. Included appliances and amenities: {{listing.includedAppliances}}.</p>
{{/unless}}

<p><strong>Keys.</strong> The Tenant will be given {{listing.keyCount}} key(s) to the Leased Property. The Tenant will receive {{listing.mailboxKeyCount}} mailbox key(s). If the Tenant misplaces a key or does not return all keys following the Termination Date, the Tenant shall be charged the actual cost, or {{listing.keyReplacementFee}}, whichever the Landlord elects. The Tenant is not permitted to change any lock or place additional locking devices on any door or window of the Leased Property without the Landlord's approval. If allowed, the Tenant must provide the Landlord with keys to any changed locks immediately upon installation.</p>
<p>If the Tenant becomes locked out of the Leased Property, the Tenant will be charged {{listing.lockoutFee}} to regain entry.</p>

<p><strong>Parking.</strong> Parking spaces are to be used for parking properly licensed and operable motor vehicles. The Tenant shall park in assigned spaces only. Parking spaces shall be kept clean at all times. Vehicles leaking oil, gas, or other motor vehicle fluids shall not be parked on the Leased Property. Mechanical work or storage of inoperable vehicles is not permitted in parking spaces or elsewhere on the Leased Property.</p>
<p>Parking is permitted as follows: the Tenant shall be entitled to use {{listing.parkingSpaces}} parking space(s) for the parking of motor vehicle(s). {{#if listing.parkingDescription}}The parking space(s) provided are identified as {{listing.parkingDescription}}. {{/if}}The right to parking is included in the Rent identified in this Lease.</p>

<p><strong>Occupancy of Leased Property.</strong> Except as stated otherwise in this Paragraph, only those individuals identified in this Lease as the "Tenant" (including their minor children) may reside in the Leased Property. The individuals identified as the "Tenant" shall sign this Lease. It is explicitly understood that this Lease is between the Landlord and each Tenant signatory individually and jointly. If any one signatory defaults, the remaining signatories are collectively responsible for timely Rent payment and all other terms of this Lease. Guest count on this booking: {{deal.guestCount}}. The Tenant may have up to {{listing.maxGuests}} guests on the Leased Property at any one time. A "guest" shall be considered anyone who is invited by the Tenant to be present at the Leased Property, and who is also not included in this Lease. The Tenant may not have guests on the Leased Property for more than {{listing.maxGuestConsecutiveDays}} consecutive days. No other person shall be permitted to occupy the Leased Property except with the prior written approval of the Landlord.</p>

<p><strong>Use of Leased Property.</strong> No retail, commercial, or professional use of the Leased Property is allowed unless the Tenant receives prior written consent of the Landlord and such use conforms to applicable zoning laws. In such a case, the Landlord may require the Tenant to obtain liability insurance for the benefit of the Landlord. The Landlord reserves the right to refuse to consent to such use in its sole and absolute discretion.</p>
<p>The Tenant is required to obtain the Landlord's approval in writing before bringing pets onto the Leased Property or allowing pets to reside there. Pets allowed under this listing: {{listing.petsAllowed}}. {{listing.petsNotes}}</p>
<p>The Tenant must ensure that no actions or activities in or around the Leased Property obstruct or interfere with the rights of neighboring occupants, causing them harm or annoyance, or utilize the Leased Property for improper, illegal, or objectionable purposes. Additionally, the Tenant must prevent or refrain from creating or allowing any nuisances on the Leased Property, or engaging in any activities that may lead to increased insurance rates, affect fire insurance coverage, or result in the cancellation of any insurance policies for the property or its contents.</p>
<p>Use of the roof and/or the fire escapes by the Tenant and/or guests is limited to emergency use only. No other use is permitted, including but not limited to the placement of personal property.</p>

<p><strong>Assigning or Subletting.</strong> The Tenant may not do any of the following without the Landlord's prior written consent: (1) assign this Lease; (2) sublet all or any part of the Leased Property; (3) allow any person to use the Leased Property other than those uses specified in the Use of Leased Property Paragraph above. Unless the Tenant has obtained the Landlord's prior written consent to assign or sublease, any unapproved assignment or subletting may be deemed invalid by the Landlord, and the Tenant shall continue to remain responsible for all the terms and conditions of this Lease.</p>

<p><strong>Insurance.</strong> The Tenant shall maintain renter's liability insurance in the minimum amount of {{listing.rentersInsuranceMinLiability}} unless waived in writing by the Landlord.</p>

<p><strong>Smoking.</strong> {{#unless listing.smokingAllowed}}The Leased Property shall be smoke-free. {{/unless}}{{#if listing.smokingAllowed}}Smoking is permitted only as allowed by the Landlord in writing. {{/if}}"Smoking" or "to smoke" means and includes inhaling, exhaling, burning or carrying any lighted smoking equipment for tobacco. The Tenant will be liable for any damages caused due to the Tenant or the Tenant's guests smoking in the Leased Property.</p>

<p><strong>Landlord Access to Property.</strong> The Landlord or Landlord's agents may enter the Leased Property during reasonable hours (e.g., 9:00 a.m. to 5:00 p.m.) during the term of this Agreement and any renewal thereof for the purposes of inspection, making repairs or improvements, supplying agreed services, showing the Property to prospective buyers or tenants, or in case of an emergency. Except in an emergency, the Landlord will provide the Tenant with at least twenty-four (24) hours' written notice of intent to enter. For purposes of this Agreement, an "emergency" includes any condition that poses an immediate threat to life, health, safety, or property. Tenant agrees to cooperate and make the Leased Property reasonably available for these purposes.</p>

<p><strong>Property Maintenance.</strong></p>
<p><strong>Communication and Maintenance Requests.</strong> To help keep communication organized, the Landlord requests that the Tenant direct all routine questions, maintenance matters, or repair requests to the following contact:</p>
<p>Name: {{listing.maintenanceContactName}}<br/>Phone: {{listing.maintenanceContactPhone}}<br/>Email: {{listing.maintenanceContactEmail}}</p>
<p>This contact information is provided only for convenience and coordination and does not replace the Landlord's responsibilities or authority.</p>
<p>The Tenant should avoid contacting the Landlord directly unless specifically instructed otherwise, so that all requests can be handled efficiently and documented properly.</p>
<p>In urgent situations, the Tenant should first attempt to reach the contact person. If the issue is serious and immediate action is needed and no one can be reached, the Tenant may take reasonable temporary steps as allowed by law.</p>
<p>The Tenant acknowledges that the Leased Property from time to time may require renovations or repairs to keep it in good condition and repair, and that such work may result in temporary loss of use of portions of the Leased Property and may inconvenience the Tenant. The Tenant agrees that any such loss shall not constitute a reduction in housing services or otherwise warrant a reduction in Rent. Further, subject to local law, the Tenant agrees, upon demand of the Landlord, to temporarily vacate the Leased Property for a reasonable period, to allow for fumigation (or other methods) to control wood destroying pests or organisms, or other repairs to the Leased Property. The Tenant agrees to comply with all instructions and requirements necessary to prepare the Leased Property to accommodate pest control, fumigation, or other work, including bagging or storage of food and medicine, and removal of perishables and valuables. The Tenant shall only be entitled to a credit of Rent equal to the per diem Rent for the period of time the Tenant is required to vacate the Leased Property.</p>
<p>The Tenant further agrees to cooperate in any efforts undertaken by the Landlord to rid the Leased Property of pests of any kind. Failure of the Tenant to cooperate may be deemed an obstruction of the free use of property so as to interfere with the comfortable enjoyment of life or property, thereby constituting a nuisance.</p>
<p>The Tenant shall properly use, operate, and safeguard the Leased Property, including, if applicable, any landscaping, furniture, furnishings, and appliances, and all mechanical, electrical, gas, and plumbing fixtures, and keep them and the Leased Property clean, sanitary, and well ventilated. The Tenant shall be responsible for checking and maintaining all smoke detectors. The Tenant shall immediately notify the Landlord, in writing, of any problem, malfunction, or damage. The Tenant shall be charged for all repairs or replacements caused by the Tenant, pets, or guests of the Tenant, excluding ordinary wear and tear. The Tenant shall be charged for all damage to the Leased Property as a result of failure to report a problem in a timely manner. The Tenant shall be charged for repair of drain blockages or stoppages, unless caused by defective plumbing parts or tree roots invading sewer lines.</p>

<p><strong>Pets.</strong> Pets are not permitted on the Premises unless expressly approved in writing by the Landlord in advance. In the event that a pet is authorized or present, whether temporarily or otherwise, the Tenant shall notify the Landlord. The Tenant shall be solely responsible for any and all damages, cleaning, or repairs caused by any pet, including those of visiting guests or Occupants, and agrees to indemnify and hold the Landlord harmless from any such damage or related claims.</p>

{{#if owner.fullName}}
<p class="party">The Landlord (Owner):</p>
<p class="party-name">{{owner.fullName}}</p>
<p class="party">The Property Manager / Agent:</p>
<p class="party-name">{{host.fullName}}</p>
{{/if}}
{{#unless owner.fullName}}
<p class="party">The Landlord:</p>
<p class="party-name">{{host.fullName}}</p>
{{/unless}}
<p class="party">The Tenant:</p>
<p class="party-name">{{tenant.fullName}}</p>

<p><strong>Military Termination Clause.</strong> In the event the Tenant is, or hereafter becomes, a member of the United States Armed Forces on extended active duty and hereafter the Tenant receives permanent change of station orders to depart from the area where the Leased Property is located; is relieved from active duty, retires or separates from the military; or is ordered into military housing, the Tenant may terminate this Lease upon giving 30 days' written notice to the Landlord. The Tenant shall also provide to the Landlord a copy of the official orders or a letter signed by the Tenant's commanding officer reflecting the change that warrants termination under this clause. The Tenant will pay pro-rated Rent for any days they occupy the dwelling past the first day of the month. The Security Deposit will be promptly returned to the Tenant, provided there are no damages to the Leased Property.</p>

<p><strong>Early Termination Clause.</strong> The Tenant may, upon 30 days' written notice to the Landlord, terminate this Lease provided that the Tenant pays a termination charge equal to {{listing.earlyTerminationFeeAmount}} or the maximum allowable by law, whichever is less. Termination will be effective as of the last day of the calendar month following the end of the 30 day notice period. The termination charge will be in addition to all Rent due up to the termination day.</p>

<p><strong>Governing Law.</strong> This Lease shall be construed in accordance with the laws of the State of California.</p>

<p><strong>Severability.</strong> If any portion of this Lease shall be held to be invalid or unenforceable for any reason, the remaining provisions shall continue to be valid and enforceable. If a court finds that any provision of this Lease is invalid or unenforceable, but that by limiting such provision it would become valid and enforceable, then such provision shall be deemed to be written, construed, and enforced as so limited. The failure of either party to enforce any provisions of this Lease shall not be construed as a waiver or limitation of that party's right to subsequently enforce and compel strict compliance with every provision of this Lease.</p>

<p><strong>Estoppel Certificate.</strong> The Tenant shall execute and return a tenant estoppel certificate delivered to the Tenant by the Landlord or the Landlord's agent within 3 days after its receipt. Failure to comply with this requirement shall be deemed the Tenant's acknowledgment that the estoppel certificate is true and correct, and may be relied upon by a lender or purchaser.</p>

<p><strong>Attorney's Fees.</strong> If either party to this Lease initiates a legal action or proceeding arising from or relating to this Lease, the party that prevails in such action or proceeding shall be entitled to receive, in addition to any other remedies granted, reasonable attorney's fees, costs, and expenses incurred in the action or proceeding. This Provision also covers the recovery of expert witness fees, if applicable.</p>

<p><strong>Binding on Heirs and Successors.</strong> The provisions of this Lease shall be binding upon and inure to the benefit of both parties and their respective legal representatives, successors, and assigns.</p>

<p><strong>Time of Essence.</strong> Time is of the essence with respect to the execution of this Lease.</p>

<p><strong>Entire Lease.</strong> This Lease contains the entire agreement of the parties, and there are no other promises, conditions, understandings, or other agreements, whether oral or written, relating to the subject matter of this Lease. This Lease may be modified or amended in writing if the writing is signed by the party obligated under the amendment.</p>

<p><strong>Dispute Resolution.</strong> The parties will attempt to resolve any dispute arising out of or relating to this Lease through friendly negotiations amongst the parties. If the matter is not resolved by negotiation, the parties will resolve the dispute using the below Alternative Dispute Resolution (ADR) procedure, unless the dispute or controversy meets the requirements to be brought before California's small court claims or is an unlawful detainer proceeding.</p>
<p>Any controversies or disputes arising out of or relating to this Lease, other than those excepted above, will be submitted to mediation in accordance with any statutory rules of mediation for the State of California. If mediation does not successfully resolve the dispute, then the parties may proceed to seek an alternative form of resolution in accordance with any other rights and remedies afforded to them by law.</p>

<p><strong>Megan's Law.</strong> Notice: Pursuant to Section 290.46 of the Penal Code, information about specified registered sex offenders is made available to the public via an Internet website maintained by the Department of Justice at www.meganslaw.ca.gov. Depending on an offender's criminal history, this information will include either the address at which the offender resides or the community of residence and ZIP Code in which the offender resides.</p>

<p><strong>Asbestos.</strong> The Landlord is unaware of any asbestos-containing construction materials or any prior reports assessing their presence. Additionally, the Landlord has no knowledge of any potential carcinogens within the Leased Property.</p>

<p><strong>Lead-Based Paint.</strong> The Tenant has received the attached lead-based paint disclosure form, which is designed to satisfy federally required disclosure requirements regarding exposure to lead-based paints in the Leased Property.</p>

{{#if owner.fullName}}
<p class="party">The Landlord (Owner):</p>
<p class="sigline"></p>
<p class="party-name">{{owner.fullName}}</p>
<p class="date-line">Date</p>
<p class="party">The Property Manager / Agent:</p>
<p class="sigline"></p>
<p class="party-name">{{host.fullName}}</p>
<p class="date-line">Date</p>
{{/if}}
{{#unless owner.fullName}}
<p class="party">The Landlord:</p>
<p class="sigline"></p>
<p class="party-name">{{host.fullName}}</p>
<p class="date-line">Date</p>
{{/unless}}
<p class="party">The Tenant:</p>
<p class="sigline"></p>
<p class="party-name">{{tenant.fullName}}</p>
<p class="date-line">Date</p>

<h2>Information about Bed Bugs</h2>
<p><strong>Bed bug Appearance:</strong> Bed bugs have six legs. Adult bed bugs have flat bodies about 1/4 of an inch in length. Their color can vary from red and brown to copper colored. Young bed bugs are very small. Their bodies are about 1/16 of an inch in length. They have almost no color. When a bed bug feeds, its body swells, may lengthen, and becomes bright red, sometimes making it appear to be a different insect. Bed bugs do not fly. They can either crawl or be carried from place to place on objects, people, or animals. Bed bugs can be hard to find and identify because they are tiny and try to stay hidden.</p>
<p><strong>Life Cycle and Reproduction:</strong> An average bed bug lives for about 10 months. Female bed bugs lay one to five eggs per day. Bed bugs grow to full adulthood in about 21 days. Bed bugs can survive for months without feeding.</p>
<p><strong>Bed bug Bites:</strong> Because bed bugs usually feed at night, most people are bitten in their sleep and do not realize they were bitten. A person's reaction to insect bites is an immune response and so varies from person to person. Sometimes the red welts caused by the bites will not be noticed until many days after a person was bitten, if at all.</p>
<p><strong>Common signs and symptoms of a possible bed bug infestation:</strong></p>
<ul>
<li>Small red to reddish brown fecal spots on mattresses, box springs, bed frames, linens, upholstery, or walls.</li>
<li>Molted bed bug skins, white, sticky eggs, or empty eggshells.</li>
<li>Very heavily infested areas may have a characteristically sweet odor.</li>
<li>Red, itchy bite marks, especially on the legs, arms, and other body parts exposed while sleeping. However, some people do not show bed bug lesions on their bodies even though bed bugs may have fed on them.</li>
</ul>
<p>For more information, see the United States Environmental Protection Agency (https://www.epa.gov/bedbugs) and the National Pest Management Association (https://www.npmapestworld.org/).</p>
<p>The Tenant shall immediately notify the Landlord of the presence of any bed bugs in the Leased Property.</p>

<h2>Payments Required</h2>
<p>The Tenant shall pay the following amounts under this Lease:</p>
<ul>
<li>Security Deposit: {{deal.securityDeposit}}</li>
<li>First Month's Rent: {{deal.monthlyRent}}</li>
</ul>
<p>Both parties acknowledge and agree that all payments under this Lease shall be made directly to the Landlord or the Landlord's authorized entity as listed in this Lease Agreement.</p>
<p>All payments required under this Lease must be received by the Landlord before the Tenant or any Occupants take possession of the Premises unless otherwise agreed in writing by the Landlord.</p>

<h2>Residential Lease<br/>Inspection Checklist</h2>
<p>The Tenant has inspected the Leased Property and states that it is in satisfactory condition, free of defects, except as noted below:</p>
<table class="checklist">
<tr><th></th><th>SATISFACTORY</th><th>COMMENTS</th></tr>
<tr><td>Bathrooms</td><td></td><td></td></tr>
<tr><td>Carpeting</td><td></td><td></td></tr>
<tr><td>Ceilings</td><td></td><td></td></tr>
<tr><td>Closets</td><td></td><td></td></tr>
<tr><td>Countertops</td><td></td><td></td></tr>
<tr><td>Dishwasher</td><td></td><td></td></tr>
<tr><td>Disposal</td><td></td><td></td></tr>
<tr><td>Doors</td><td></td><td></td></tr>
<tr><td>Fireplace</td><td></td><td></td></tr>
<tr><td>Lights</td><td></td><td></td></tr>
<tr><td>Locks</td><td></td><td></td></tr>
<tr><td>Refrigerator</td><td></td><td></td></tr>
<tr><td>Screens</td><td></td><td></td></tr>
<tr><td>Stove</td><td></td><td></td></tr>
<tr><td>Walls</td><td></td><td></td></tr>
<tr><td>Windows</td><td></td><td></td></tr>
<tr><td>Window coverings</td><td></td><td></td></tr>
<tr><td></td><td></td><td></td></tr>
<tr><td></td><td></td><td></td></tr>
</table>
<p class="sigline"></p>
<p class="date-line">Date</p>
<p class="party">The Tenant:</p>
<p class="party-name">{{tenant.fullName}}</p>
<p class="date-line">Date</p>

<h2>Disclosure of Information on Lead-Based Paint or Lead-Based Hazards</h2>
<p><strong>Lead Warning Statement:</strong> Housing built before 1978 may contain lead-based paint. Lead from paint, paint chips, and dust can pose health hazards if not managed properly. Lead exposure is especially harmful to young children and pregnant women. Before renting pre-1978 housing, landlords must disclose the presence of known lead-based paint and/or lead-based paint hazards in the dwelling. The Tenant must also receive a federally approved pamphlet on lead poisoning prevention.</p>
<p><strong>Landlord's Disclosure:</strong></p>
{{#if listing.builtBefore1978}}
<p>The Leased Property was built before 1978, and the Landlord discloses the following to the Tenant in accordance with applicable law:</p>
{{/if}}
{{#unless listing.builtBefore1978}}
<p>The Leased Property was not built before 1978. The following disclosure is provided for completeness.</p>
{{/unless}}
<p><strong>(a) Presence of lead-based paint and/or lead-based paint hazards (check one):</strong></p>
<p class="check">Known lead-based paint and/or lead-based paint hazards are present in the Leased Property and/or Building (explain, including the basis for the determination that lead-based paint and/or lead-based paint hazards exist, the location of the lead-based paint and/or lead-based paint hazards, and the condition of the painted surfaces):</p>
<p class="writein"></p>
<p class="writein"></p>
<p class="check">The Landlord has no knowledge of lead-based paint and/or lead-based paint hazards in the Leased Property or Building.</p>
<p><strong>(b) Records and reports available to the Landlord (check one):</strong></p>
<p class="check">The Landlord has provided the Tenant with all available records and reports pertaining to lead-based paint and/or lead-based paint hazards in the Leased Property and/or Building (list documents below):</p>
<p class="writein"></p>
<p class="writein"></p>
<p class="check">The Landlord has no records or reports pertaining to lead-based paint and/or lead-based paint hazards in the Leased Property and/or Building.</p>
<p>{{listing.leadPaintKnowledge}}</p>
<p><strong>Acknowledgements:</strong></p>
<p class="check">The Tenant has received copies of all information listed above (Tenant initial).</p>
<p class="check">The Tenant has received the pamphlet Protect Your Family from Lead in Your Home, a copy of which is attached hereto and incorporated herein (Tenant initial).</p>
<p>The Landlord and the Tenant acknowledge and agree that the parties have reviewed the information above and each party certifies, to the best of the party's knowledge, that the information provided is true and accurate.</p>

<h2>California Lease Agreement Mold Notification Addendum</h2>
<p>The Landlord endeavors to maintain the highest quality living environment for the Tenant. Therefore, know that the Landlord has inspected the unit prior to the Lease and knows of no damp or wet building materials and knows of no mold or mildew contamination. The Tenant is hereby notified that mold, however, can grow if the Leased Property is not properly maintained or ventilated. If moisture is allowed to accumulate in the unit, it can cause mildew and mold to grow. It is important that the Tenant regularly allows air to circulate in the apartment. It is also important that the Tenant keep the interior of the unit clean and that they promptly notify the Landlord of any leaks, moisture problems, and/or mold growth.</p>
<p>The Tenant agrees to maintain the property in a manner that prevents the occurrence of an infestation of mold or mildew. The Tenant agrees to uphold this responsibility in part by complying with the following list of responsibilities:</p>
<ol>
<li>The Tenant agrees to keep the unit free of dirt and debris that can harbor mold.</li>
<li>The Tenant agrees to immediately report to the Landlord any water intrusion, such as plumbing leaks, drips, or "sweating" pipes.</li>
<li>The Tenant agrees to notify the Landlord of overflows from bathroom, kitchen, or unit laundry facilities, especially in cases where the overflow may have permeated walls or cabinets.</li>
<li>The Tenant agrees to report to the Landlord any significant mold growth on surfaces inside the premises.</li>
<li>The Tenant agrees to allow the Landlord to enter the unit to inspect and make necessary repairs.</li>
<li>The Tenant agrees to properly ventilate the bathroom while showering or bathing and to report to the Landlord any non-working fan.</li>
<li>The Tenant agrees to use exhaust fans whenever cooking, dishwashing, or cleaning.</li>
<li>The Tenant agrees to use all reasonable care to prevent outdoor water from penetrating into the interior of the unit.</li>
<li>The Tenant agrees to clean and dry any visible moisture on windows, walls, and other surfaces, including personal property, as soon as reasonably possible. (Note: Mold can grow on damp surfaces within 24 to 48 hours.)</li>
<li>The Tenant agrees to notify the Landlord of any problems with any air conditioning or heating systems that are discovered by the Tenant.</li>
<li>The Tenant agrees to indemnify and hold harmless the Landlord from any actions, claims, losses, damages, and expenses, including, but not limited to, attorneys' fees that the Landlord may sustain or incur as a result of the negligence of the Tenant or any guest, licensee, invitee or other person living in, occupying, or using the Property.</li>
</ol>
<p>If the Tenant fails to comply with the terms of this Mold Addendum, it is a material breach of the Lease it is attached to. In the event there is a conflict between this Mold Addendum and the Lease, the terms of the Mold Addendum shall govern.</p>

<h2>Rent Cap and Just Cause Addendum</h2>
<p>California law limits the amount your rent can be increased. See Section 1947.12 of the Civil Code for more information. California law also provides that after all of the tenants have continuously and lawfully occupied the property for 12 months or more, or at least one of the tenants has continuously and lawfully occupied the property for 24 months or more, a landlord must provide a statement of cause in any notice to terminate a tenancy. See Section 1946.2 of the Civil Code for more information.</p>
<p><strong>Rent Cap Requirements:</strong> California Civil Code § 1947.12 limits the amount the Landlord can increase rent as follows:</p>
<ol>
<li>Subject to certain provisions of Civil Code Section 1947.12, the Landlord cannot, over the course of any 12-month period, increase rent for the Leased Property more than 5 percent plus the percentage change in the cost of living, or 10 percent, whichever is lower, of the lowest Rent charged for the Leased Property at any time during the 12 months prior to the effective date of the increase.</li>
<li>If the Tenant remains in occupancy of the Leased Property over any 12-month period, the Landlord cannot increase rent for the Leased Property in more than two increments over that 12-month period.</li>
<li>For a new tenancy in which no tenant from the prior tenancy remains, the owner may establish the initial rate not subject to Paragraph 1 of this Section. Paragraph 1 of this Section is only applicable to subsequent increases after the initial rental rate has been established.</li>
</ol>
<p>WITH CERTAIN EXEMPTIONS, THE LANDLORD MAY BE SUBJECT TO THE JUST CAUSE PROVISIONS OF CIVIL CODE SECTION 1946.2 AND INFORMS THE TENANT OF THE FOLLOWING:</p>
<p><strong>Just Cause Requirements — At-fault Just Cause:</strong></p>
<ol>
<li>Default in payment of rent.</li>
<li>Breach of a material term of the Lease, as described in Code of Civil Procedure Section 1161, Paragraph (3), including but not limited to, violation of a provision of the Lease after being issued a written notice to correct the violation.</li>
<li>Maintaining, committing, or permitting the maintenance of a nuisance as described in Code of Civil Procedure Section 1161, Paragraph (4).</li>
<li>Committing waste as described in Code of Civil Procedure Section 1161, Paragraph (4).</li>
<li>The Tenant had a written Lease that terminated on or after January 1, 2020, and after a written request or demand from the owner, the Tenant refused to execute a written extension or renewal of the Lease for an additional term of similar duration with similar provisions, provided that those terms do not violate Section 1946.1 or any other provision of law.</li>
<li>Criminal activity by the Tenant on the residential real property, including any common areas, or any criminal threat, as defined in Penal Code Section 422, subdivision (a), directed to any owner or agent of the owner of the Leased Property.</li>
<li>Assigning or subletting the premises in violation of the Tenant's Lease.</li>
<li>The Tenant's refusal to allow the owner to enter the residential real property pursuant to a request consistent with Civil Code Sections 1101.5 and 1954, and Health and Safety Code Sections 13113.7 and 17926.1.</li>
<li>Using the premises for an unlawful purpose as described in Code of Civil Procedure Section 1161, Paragraph (4).</li>
<li>When the Tenant fails to deliver possession of the residential real property after providing the owner with written notice of the Tenant's intention to terminate the hiring of the real property or makes a written offer to surrender that is accepted in writing by the Landlord, but fails to deliver possession at the time specified in that written notice.</li>
</ol>
<p><strong>No-fault Just Cause:</strong></p>
<ol>
<li>Intent to occupy the residential real property by the owner or their spouse, domestic partner, children, grandchildren, parents, or grandparents (Family move-in). For leases entered into on or after January 1, 2020, the Tenant and the Landlord agree that the Landlord has the right to terminate this Lease if the Landlord, or their spouse, domestic partner, children, grandchildren, parents, or grandparents, unilaterally decide to occupy the Leased Property.</li>
<li>Withdrawal of the Leased Property from the rental market.</li>
<li>Unsafe habitation, as determined by a government agency that has issued an order to vacate, or to comply, or other order that necessitates vacating the residential property.</li>
<li>The intent to demolish or substantially remodel the residential real property. "Substantially remodel" means the replacement or substantial modification of any structural, electrical, plumbing, or mechanical system that requires a permit that cannot be accomplished in a safe manner with the Tenant in place and that requires the Tenant to vacate the residential real property for at least 30 days. Cosmetic improvements alone do not qualify.</li>
</ol>
<p><strong>Tenant Payments under No-Fault Just Cause Eviction:</strong> If the Landlord issues a termination of tenancy under a No-Fault Just Cause, the Landlord notifies the Tenant of the right to direct payment relocation assistance equal to one month of the Tenant's rent in effect at the time of the termination, and shall be provided within 15 calendar days of service of the notice. In lieu of direct payment, the Landlord may waive the payment of rent for the final month of tenancy prior to the rent becoming due.</p>
<p><strong>Specific Exemptions.</strong> Certain housing accommodations are exempt from just cause and/or rent-cap requirements, including housing that has been issued a certificate of occupancy within the previous 15 years, qualifying single-family owner-occupied residences, and other exemptions under the Civil Code. Rent-cap / just-cause exemption claimed for this property: {{listing.rentCapJustCauseExempt}}.</p>
{{#if listing.rentCapJustCauseExempt}}
<p><strong>Notice of Exemption.</strong> This property is not subject to the rent limits imposed by Section 1947.12 of the Civil Code and is not subject to the just cause requirements of Section 1946.2 of the Civil Code. This property meets the requirements of Sections 1947.12(d)(5) and 1946.2(e)(8) of the Civil Code AND the owner is not any of the following: (1) a real estate investment trust, as defined by Section 856 of the Internal Revenue Code; (2) a corporation; or (3) a limited liability company in which at least one member is a corporation.</p>
{{/if}}
<p>NOTE: Other exemptions under the Civil Code may apply. Additionally, this property may be subject to local rent cap and just cause eviction controls, which may impose additional restrictions.</p>
<p>The undersigned acknowledge a copy of this document and agree that the terms specified above are made a part of the Lease.</p>
{{#if owner.fullName}}
<p class="party">The Landlord (Owner):</p>
<p class="sigline"></p>
<p class="party-name">{{owner.fullName}}</p>
<p class="date-line">Date</p>
<p class="party">The Property Manager / Agent:</p>
<p class="sigline"></p>
<p class="party-name">{{host.fullName}}</p>
<p class="date-line">Date</p>
{{/if}}
{{#unless owner.fullName}}
<p class="party">The Landlord:</p>
<p class="sigline"></p>
<p class="party-name">{{host.fullName}}</p>
<p class="date-line">Date</p>
{{/unless}}
<p class="party">The Tenant:</p>
<p class="sigline"></p>
<p class="party-name">{{tenant.fullName}}</p>
<p class="date-line">Date</p>

{{#if owner.fullName}}
<h2>OWNER CONSENT AND AUTHORIZATION</h2>
<p>This Addendum is attached to and made part of the Lease Agreement dated {{lease.effectiveDate}} between the Landlord and Tenant(s).</p>
<p>This tenancy is for a term of more than thirty (30) days. {{owner.fullName}} ("Owner") is the owner of the Leased Property and is the Landlord under this Lease. The Owner authorizes {{host.fullName}} ("Property Manager") to enter into this Lease on the Owner's behalf and consents to this tenancy, including all terms of the Lease and any addenda.</p>
<p>The Owner acknowledges that California law requires the owner's consent for residential tenancies of more than thirty (30) days when the Lease is executed by a property manager or other agent.</p>
<p>The Owner recorded this consent in Lagedra on {{owner.consentDate}} (consent record {{owner.consentVersion}}).</p>
<p>Owner email: {{owner.email}}. Owner phone: {{owner.phone}}. Owner mailing address: {{owner.mailingAddress}}.</p>
<p class="party">The Owner / Landlord:</p>
<p class="sigline"></p>
<p class="party-name">{{owner.fullName}}</p>
<p class="date-line">Date</p>
<p class="party">The Property Manager / Agent:</p>
<p class="sigline"></p>
<p class="party-name">{{host.fullName}}</p>
<p class="date-line">Date</p>
{{/if}}

{{#if broker.name}}
<h2>BROKER DISCLOSURE AND AUTHORIZATION ADDENDUM</h2>
<p>This Addendum is attached to and made part of the Lease Agreement dated {{lease.effectiveDate}} between the Landlord and Tenant(s).</p>
<p><strong>Broker Disclosure, Agency, and Allocation of Responsibilities</strong></p>
<p>The Landlord has engaged a California licensed real estate broker (DRE License No. {{broker.dreLicense}}), {{broker.name}}, to act as the Landlord's authorized agent in connection with this tenancy. The Broker's scope of authority includes overseeing legal compliance related to the tenancy, including but not limited to the preparation and service of notices, coordination of eviction proceedings if required, and ensuring compliance with applicable federal, state, and local landlord-tenant laws. {{broker.scopeNotes}}</p>
<p>This Addendum does not modify any other terms of the Lease Agreement, which remain in full force and effect.</p>
<h2>BROKER / REPRESENTATIVE ACKNOWLEDGMENT</h2>
<p>Name: ________ {{broker.name}} ________</p>
<p>Role: Broker / Representative</p>
<p>Signature: _______________________</p>
<p>Date: ____________________________</p>
{{/if}}

{{#if owner.fullName}}
<p>Notices to the Landlord (Owner) shall be sent to {{owner.mailingAddress}} and {{owner.email}}. Notices to the Property Manager shall be sent to {{host.noticeAddress}} and {{host.email}}. Notices to the Tenant shall be sent to {{tenant.mailingAddress}}. Tenant phone/email: {{tenant.phone}} / {{tenant.email}}.</p>
{{/if}}
{{#unless owner.fullName}}
<p>Notices to the Landlord shall be sent to {{host.noticeAddress}}. Notices to the Tenant shall be sent to {{tenant.mailingAddress}}. Tenant phone/email: {{tenant.phone}} / {{tenant.email}}.</p>
{{/unless}}$leasebody$
    ) THEN
        RAISE NOTICE 'Live template already matches; no changes made.';
        RETURN;
    END IF;

    SELECT COALESCE(MAX("VersionNumber"), 0) + 1
      INTO v_next_number
      FROM lease_agreements.lease_template_versions
     WHERE "TemplateId" = v_template_id;

    INSERT INTO lease_agreements.lease_template_versions (
        "Id", "TemplateId", "VersionNumber", "Status",
        "EffectiveDate", "ApprovedAt", "ApprovedBy", "SecondApproverId",
        "BodyHtml", "CreatedAt", "UpdatedAt", "IsDeleted"
    ) VALUES (
        v_new_version_id, v_template_id, v_next_number, 'Active',
        date_trunc('day', now()), now(),
        -- Same well-known system principals the seed uses, so the
        -- dual-control invariant holds for this version.
        '00000000-0000-0000-0000-0000000000a1',
        '00000000-0000-0000-0000-0000000000a2',
        $leasebody$<h1>California Lease Agreement</h1>
{{#if owner.fullName}}
<p>This Lease Agreement ("Lease") is entered into on {{lease.effectiveDate}}, by and between {{owner.fullName}} ("Owner" / "Landlord"), acting through authorized property manager {{host.fullName}}, and {{tenant.fullName}} ("Tenant").</p>
{{/if}}
{{#unless owner.fullName}}
<p>This Lease Agreement ("Lease") is entered into on {{lease.effectiveDate}}, by and between {{host.fullName}} ("Landlord") and {{tenant.fullName}} ("Tenant").</p>
{{/unless}}

<p><strong>Leased Property.</strong> The Landlord hereby leases to the Tenant the {{listing.propertyTypeLabel}} located at {{listing.fullAddress}} ("Leased Property").</p>

<p><strong>Term.</strong> This Lease will start on {{deal.startDate}} ("Start Date") and will continue for an initial fixed term of {{deal.termMonths}} months, ending on {{deal.endDate}} ("Initial Term"). After the Initial Term expires, the tenancy shall automatically continue on a month-to-month basis unless either party provides at least thirty (30) days' advance written notice of termination or intent to vacate ("Termination Date"). Notice must be delivered in person, sent by certified or registered mail, or sent via email to the designated and proper email contact of the receiving party. Rent will be due and payable up to and including the Termination Date.</p>
<p>Any request to extend the lease beyond the Initial Term or any month-to-month period must also be communicated with at least thirty (30) days' advance written notice prior to the desired extension or continuation date, and is subject to mutual agreement by both parties.</p>
<p>In the event of a sale, this Lease shall remain subject to applicable California law and any lawful termination rights available to the Landlord.</p>

<p><strong>Rent.</strong> The Tenant agrees that the Rent shall be paid either directly by the Tenant to the Landlord as rent for the use and occupancy of the Leased Property the sum of {{deal.monthlyRent}} due on the {{listing.rentDueDay}} day of each month ("Rent").</p>
<p>The Rent shall be paid by the following method(s):</p>
{{#if listing.paymentMethods}}
<ul>
<li>Electronic Payment Methods</li>
</ul>
<p>The following electronic payment methods will be accepted:</p>
<ul>
<li>{{listing.paymentMethods}}</li>
</ul>
{{/if}}
{{#unless listing.paymentMethods}}
<p>The Rent shall be paid by the method(s) designated in writing by the Landlord.</p>
{{/unless}}
{{#if owner.fullName}}
<p>The Rent shall be payable to the Landlord or the Landlord's authorized property manager. The Property Manager can be reached by email at {{host.email}} and by phone at {{host.phone}}. The Owner can be reached by email at {{owner.email}}.</p>
{{/if}}
{{#unless owner.fullName}}
<p>The Rent shall be payable to the Landlord. The Landlord can be reached by email at {{host.email}} and by phone at {{host.phone}}.</p>
{{/unless}}
<p>If any payment is returned for non-sufficient funds or because the Tenant stops payments, then, after that, the Landlord may, in writing, require the Tenant to pay future Rent payments by cash, cashier's check, or money order.</p>

<p><strong>Non-Sufficient Funds.</strong> The Tenant will be charged a monetary fee of {{listing.nsfFirstFee}} as reimbursement of the expenses incurred by the Landlord for the first check that is returned to the Landlord for lack of sufficient funds, and {{listing.nsfSubsequentFee}} for each subsequent check returned for lack of sufficient funds. This Paragraph is in accordance with California Civil Code § 1719.</p>
<p>The Landlord reserves the right to demand future Rent payments by cash, cashier's check, or money order in the event a check is returned for insufficient funds. Nothing in this Paragraph limits other remedies available to the Landlord as a payee of a dishonored check. The Landlord and the Tenant agree that 3 returned checks in any 12-month period constitute frequent return of checks due to insufficient funds and may be considered a just cause for eviction. The Landlord shall notify the Tenant of this election at least 30 days before the date the Tenant is to make the first payment by cash, cashier's check, or money order.</p>

<p><strong>Security Deposit.</strong> Upon execution, the Tenant shall pay to the Landlord a security deposit of {{deal.securityDeposit}} ("Security Deposit") for the purpose set forth in Civil Code § 1950.5. The Landlord will hold this Security Deposit for the faithful performance by the Tenant of their obligations under this Lease and for the cleaning and repairing of the Leased Property after surrender by the Tenant. The Landlord agrees to hold the Security Deposit for the Tenant, free from the claim of any creditor of the Landlord.</p>
<p>Prior to the Termination Date, the Landlord will inform the Tenant about their option to request an inspection of the Leased Property. Upon request by the Tenant, and no earlier than two weeks before the Termination Date, the Landlord will conduct an inspection of the Leased Property. After this inspection, the Landlord will furnish the Tenant with a detailed list detailing any suggested repairs and cleaning that may be deducted from the Security Deposit. The Tenant will have the opportunity to resolve these issues before the Termination Date to avoid deductions from the Security Deposit. The Landlord will return to the Tenant the full amount of the Security Deposit within 21 calendar days after the Tenant has vacated the Leased Property, minus any amounts that are reasonably necessary to remedy any defaults in the payment of Rent by the Tenant, to repair damages to the Leased Property caused by the Tenant or the Tenant's guests other than ordinary wear and tear, and to clean the Leased Property. At the time the Landlord returns the Security Deposit to the Tenant, the Landlord will furnish the Tenant with an itemized written statement of the amount of the Security Deposit received, the charges made by the Landlord against the Security Deposit, and the disposition made or to be made of the Security Deposit.</p>
<p>The Security Deposit will not be returned until the Tenant has vacated the Leased Property. Any return of the Security Deposit shall be by check made payable to the Tenant.</p>

<p><strong>Late Fee.</strong> If the Landlord has not received any Rent payment within {{listing.lateFeeGraceDays}} days after the due date, a late fee of {{listing.lateFeeAmount}} ({{listing.lateFeePercent}} of monthly Rent) shall apply. The Landlord and the Tenant agree that it is and will be impracticable and extremely difficult to fix the actual damages suffered by the Landlord in the event the Tenant makes a late payment of Rent, and that the above charge represents a reasonable approximation of the damages the Landlord is likely to suffer from a late payment. The Landlord and the Tenant further agree that this Provision does not establish a grace period of the payment of Rent, and that the Landlord may give the Tenant a three-day written notice to pay or quit the Leased Property in accordance with Cal. Code Civ. Proc. § 1161(2) at any time after the payment is due.</p>

<p><strong>Failure to Pay.</strong> Pursuant to Civil Code § 1785.26, you are hereby notified that a negative credit report reflecting on your credit record may be submitted to a credit reporting agency if you fail to fulfill the terms of your credit obligations, such as your financial obligations under the terms of this Lease.</p>

<p><strong>Default.</strong> The Landlord and the Tenant acknowledge that each condition, covenant, and provision of this Lease is essential and reasonable. A breach by the Tenant of any condition, covenant, or provision will be considered a material breach. In the event of a material breach by the Tenant, the Landlord may issue a written 3-day notice, specifying the breach and requiring the Tenant to cure the default if possible. If the Tenant fails to cure the default within the 3-day period, or if cure is not feasible, the Lease may be terminated.</p>

<p><strong>Utilities.</strong> {{listing.utilitiesResponsibility}} The Tenant also agrees to comply with any environmental, waste management, recycling, energy conservation, or water conservation programs implemented by the Landlord. Yard maintenance by the Tenant: {{listing.yardMaintenanceByTenant}}.</p>

{{#if listing.isFurnished}}
<p><strong>Furnishings.</strong> The Premises is furnished and includes, where applicable, beds and bed frames, mattresses, closets or wardrobes, sofas, dining tables, chairs, coffee tables, desks and workspaces, outdoor furniture, and lawn or patio furniture. The Premises also includes standard major appliances such as a refrigerator, stove, oven, microwave, dishwasher, washer, and dryer. Where provided, the Premises may also include televisions and basic entertainment equipment.</p>
<p>All furnishings and appliances are provided in their existing condition at move-in. The Tenant agrees to use all items with reasonable care and acknowledges that normal wear and tear is expected. Any intentional damage, loss, or misuse beyond normal wear and tear may result in repair or replacement charges. Included items: {{listing.includedAppliances}}.</p>
{{/if}}
{{#unless listing.isFurnished}}
<p><strong>Furnishings.</strong> The Premises is provided as unfurnished. Included appliances and amenities: {{listing.includedAppliances}}.</p>
{{/unless}}

<p><strong>Keys.</strong> The Tenant will be given {{listing.keyCount}} key(s) to the Leased Property. The Tenant will receive {{listing.mailboxKeyCount}} mailbox key(s). If the Tenant misplaces a key or does not return all keys following the Termination Date, the Tenant shall be charged the actual cost, or {{listing.keyReplacementFee}}, whichever the Landlord elects. The Tenant is not permitted to change any lock or place additional locking devices on any door or window of the Leased Property without the Landlord's approval. If allowed, the Tenant must provide the Landlord with keys to any changed locks immediately upon installation.</p>
<p>If the Tenant becomes locked out of the Leased Property, the Tenant will be charged {{listing.lockoutFee}} to regain entry.</p>

<p><strong>Parking.</strong> Parking spaces are to be used for parking properly licensed and operable motor vehicles. The Tenant shall park in assigned spaces only. Parking spaces shall be kept clean at all times. Vehicles leaking oil, gas, or other motor vehicle fluids shall not be parked on the Leased Property. Mechanical work or storage of inoperable vehicles is not permitted in parking spaces or elsewhere on the Leased Property.</p>
<p>Parking is permitted as follows: the Tenant shall be entitled to use {{listing.parkingSpaces}} parking space(s) for the parking of motor vehicle(s). {{#if listing.parkingDescription}}The parking space(s) provided are identified as {{listing.parkingDescription}}. {{/if}}The right to parking is included in the Rent identified in this Lease.</p>

<p><strong>Occupancy of Leased Property.</strong> Except as stated otherwise in this Paragraph, only those individuals identified in this Lease as the "Tenant" (including their minor children) may reside in the Leased Property. The individuals identified as the "Tenant" shall sign this Lease. It is explicitly understood that this Lease is between the Landlord and each Tenant signatory individually and jointly. If any one signatory defaults, the remaining signatories are collectively responsible for timely Rent payment and all other terms of this Lease. Guest count on this booking: {{deal.guestCount}}. The Tenant may have up to {{listing.maxGuests}} guests on the Leased Property at any one time. A "guest" shall be considered anyone who is invited by the Tenant to be present at the Leased Property, and who is also not included in this Lease. The Tenant may not have guests on the Leased Property for more than {{listing.maxGuestConsecutiveDays}} consecutive days. No other person shall be permitted to occupy the Leased Property except with the prior written approval of the Landlord.</p>

<p><strong>Use of Leased Property.</strong> No retail, commercial, or professional use of the Leased Property is allowed unless the Tenant receives prior written consent of the Landlord and such use conforms to applicable zoning laws. In such a case, the Landlord may require the Tenant to obtain liability insurance for the benefit of the Landlord. The Landlord reserves the right to refuse to consent to such use in its sole and absolute discretion.</p>
<p>The Tenant is required to obtain the Landlord's approval in writing before bringing pets onto the Leased Property or allowing pets to reside there. Pets allowed under this listing: {{listing.petsAllowed}}. {{listing.petsNotes}}</p>
<p>The Tenant must ensure that no actions or activities in or around the Leased Property obstruct or interfere with the rights of neighboring occupants, causing them harm or annoyance, or utilize the Leased Property for improper, illegal, or objectionable purposes. Additionally, the Tenant must prevent or refrain from creating or allowing any nuisances on the Leased Property, or engaging in any activities that may lead to increased insurance rates, affect fire insurance coverage, or result in the cancellation of any insurance policies for the property or its contents.</p>
<p>Use of the roof and/or the fire escapes by the Tenant and/or guests is limited to emergency use only. No other use is permitted, including but not limited to the placement of personal property.</p>

<p><strong>Assigning or Subletting.</strong> The Tenant may not do any of the following without the Landlord's prior written consent: (1) assign this Lease; (2) sublet all or any part of the Leased Property; (3) allow any person to use the Leased Property other than those uses specified in the Use of Leased Property Paragraph above. Unless the Tenant has obtained the Landlord's prior written consent to assign or sublease, any unapproved assignment or subletting may be deemed invalid by the Landlord, and the Tenant shall continue to remain responsible for all the terms and conditions of this Lease.</p>

<p><strong>Insurance.</strong> The Tenant shall maintain renter's liability insurance in the minimum amount of {{listing.rentersInsuranceMinLiability}} unless waived in writing by the Landlord.</p>

<p><strong>Smoking.</strong> {{#unless listing.smokingAllowed}}The Leased Property shall be smoke-free. {{/unless}}{{#if listing.smokingAllowed}}Smoking is permitted only as allowed by the Landlord in writing. {{/if}}"Smoking" or "to smoke" means and includes inhaling, exhaling, burning or carrying any lighted smoking equipment for tobacco. The Tenant will be liable for any damages caused due to the Tenant or the Tenant's guests smoking in the Leased Property.</p>

<p><strong>Landlord Access to Property.</strong> The Landlord or Landlord's agents may enter the Leased Property during reasonable hours (e.g., 9:00 a.m. to 5:00 p.m.) during the term of this Agreement and any renewal thereof for the purposes of inspection, making repairs or improvements, supplying agreed services, showing the Property to prospective buyers or tenants, or in case of an emergency. Except in an emergency, the Landlord will provide the Tenant with at least twenty-four (24) hours' written notice of intent to enter. For purposes of this Agreement, an "emergency" includes any condition that poses an immediate threat to life, health, safety, or property. Tenant agrees to cooperate and make the Leased Property reasonably available for these purposes.</p>

<p><strong>Property Maintenance.</strong></p>
<p><strong>Communication and Maintenance Requests.</strong> To help keep communication organized, the Landlord requests that the Tenant direct all routine questions, maintenance matters, or repair requests to the following contact:</p>
<p>Name: {{listing.maintenanceContactName}}<br/>Phone: {{listing.maintenanceContactPhone}}<br/>Email: {{listing.maintenanceContactEmail}}</p>
<p>This contact information is provided only for convenience and coordination and does not replace the Landlord's responsibilities or authority.</p>
<p>The Tenant should avoid contacting the Landlord directly unless specifically instructed otherwise, so that all requests can be handled efficiently and documented properly.</p>
<p>In urgent situations, the Tenant should first attempt to reach the contact person. If the issue is serious and immediate action is needed and no one can be reached, the Tenant may take reasonable temporary steps as allowed by law.</p>
<p>The Tenant acknowledges that the Leased Property from time to time may require renovations or repairs to keep it in good condition and repair, and that such work may result in temporary loss of use of portions of the Leased Property and may inconvenience the Tenant. The Tenant agrees that any such loss shall not constitute a reduction in housing services or otherwise warrant a reduction in Rent. Further, subject to local law, the Tenant agrees, upon demand of the Landlord, to temporarily vacate the Leased Property for a reasonable period, to allow for fumigation (or other methods) to control wood destroying pests or organisms, or other repairs to the Leased Property. The Tenant agrees to comply with all instructions and requirements necessary to prepare the Leased Property to accommodate pest control, fumigation, or other work, including bagging or storage of food and medicine, and removal of perishables and valuables. The Tenant shall only be entitled to a credit of Rent equal to the per diem Rent for the period of time the Tenant is required to vacate the Leased Property.</p>
<p>The Tenant further agrees to cooperate in any efforts undertaken by the Landlord to rid the Leased Property of pests of any kind. Failure of the Tenant to cooperate may be deemed an obstruction of the free use of property so as to interfere with the comfortable enjoyment of life or property, thereby constituting a nuisance.</p>
<p>The Tenant shall properly use, operate, and safeguard the Leased Property, including, if applicable, any landscaping, furniture, furnishings, and appliances, and all mechanical, electrical, gas, and plumbing fixtures, and keep them and the Leased Property clean, sanitary, and well ventilated. The Tenant shall be responsible for checking and maintaining all smoke detectors. The Tenant shall immediately notify the Landlord, in writing, of any problem, malfunction, or damage. The Tenant shall be charged for all repairs or replacements caused by the Tenant, pets, or guests of the Tenant, excluding ordinary wear and tear. The Tenant shall be charged for all damage to the Leased Property as a result of failure to report a problem in a timely manner. The Tenant shall be charged for repair of drain blockages or stoppages, unless caused by defective plumbing parts or tree roots invading sewer lines.</p>

<p><strong>Pets.</strong> Pets are not permitted on the Premises unless expressly approved in writing by the Landlord in advance. In the event that a pet is authorized or present, whether temporarily or otherwise, the Tenant shall notify the Landlord. The Tenant shall be solely responsible for any and all damages, cleaning, or repairs caused by any pet, including those of visiting guests or Occupants, and agrees to indemnify and hold the Landlord harmless from any such damage or related claims.</p>

{{#if owner.fullName}}
<p class="party">The Landlord (Owner):</p>
<p class="party-name">{{owner.fullName}}</p>
<p class="party">The Property Manager / Agent:</p>
<p class="party-name">{{host.fullName}}</p>
{{/if}}
{{#unless owner.fullName}}
<p class="party">The Landlord:</p>
<p class="party-name">{{host.fullName}}</p>
{{/unless}}
<p class="party">The Tenant:</p>
<p class="party-name">{{tenant.fullName}}</p>

<p><strong>Military Termination Clause.</strong> In the event the Tenant is, or hereafter becomes, a member of the United States Armed Forces on extended active duty and hereafter the Tenant receives permanent change of station orders to depart from the area where the Leased Property is located; is relieved from active duty, retires or separates from the military; or is ordered into military housing, the Tenant may terminate this Lease upon giving 30 days' written notice to the Landlord. The Tenant shall also provide to the Landlord a copy of the official orders or a letter signed by the Tenant's commanding officer reflecting the change that warrants termination under this clause. The Tenant will pay pro-rated Rent for any days they occupy the dwelling past the first day of the month. The Security Deposit will be promptly returned to the Tenant, provided there are no damages to the Leased Property.</p>

<p><strong>Early Termination Clause.</strong> The Tenant may, upon 30 days' written notice to the Landlord, terminate this Lease provided that the Tenant pays a termination charge equal to {{listing.earlyTerminationFeeAmount}} or the maximum allowable by law, whichever is less. Termination will be effective as of the last day of the calendar month following the end of the 30 day notice period. The termination charge will be in addition to all Rent due up to the termination day.</p>

<p><strong>Governing Law.</strong> This Lease shall be construed in accordance with the laws of the State of California.</p>

<p><strong>Severability.</strong> If any portion of this Lease shall be held to be invalid or unenforceable for any reason, the remaining provisions shall continue to be valid and enforceable. If a court finds that any provision of this Lease is invalid or unenforceable, but that by limiting such provision it would become valid and enforceable, then such provision shall be deemed to be written, construed, and enforced as so limited. The failure of either party to enforce any provisions of this Lease shall not be construed as a waiver or limitation of that party's right to subsequently enforce and compel strict compliance with every provision of this Lease.</p>

<p><strong>Estoppel Certificate.</strong> The Tenant shall execute and return a tenant estoppel certificate delivered to the Tenant by the Landlord or the Landlord's agent within 3 days after its receipt. Failure to comply with this requirement shall be deemed the Tenant's acknowledgment that the estoppel certificate is true and correct, and may be relied upon by a lender or purchaser.</p>

<p><strong>Attorney's Fees.</strong> If either party to this Lease initiates a legal action or proceeding arising from or relating to this Lease, the party that prevails in such action or proceeding shall be entitled to receive, in addition to any other remedies granted, reasonable attorney's fees, costs, and expenses incurred in the action or proceeding. This Provision also covers the recovery of expert witness fees, if applicable.</p>

<p><strong>Binding on Heirs and Successors.</strong> The provisions of this Lease shall be binding upon and inure to the benefit of both parties and their respective legal representatives, successors, and assigns.</p>

<p><strong>Time of Essence.</strong> Time is of the essence with respect to the execution of this Lease.</p>

<p><strong>Entire Lease.</strong> This Lease contains the entire agreement of the parties, and there are no other promises, conditions, understandings, or other agreements, whether oral or written, relating to the subject matter of this Lease. This Lease may be modified or amended in writing if the writing is signed by the party obligated under the amendment.</p>

<p><strong>Dispute Resolution.</strong> The parties will attempt to resolve any dispute arising out of or relating to this Lease through friendly negotiations amongst the parties. If the matter is not resolved by negotiation, the parties will resolve the dispute using the below Alternative Dispute Resolution (ADR) procedure, unless the dispute or controversy meets the requirements to be brought before California's small court claims or is an unlawful detainer proceeding.</p>
<p>Any controversies or disputes arising out of or relating to this Lease, other than those excepted above, will be submitted to mediation in accordance with any statutory rules of mediation for the State of California. If mediation does not successfully resolve the dispute, then the parties may proceed to seek an alternative form of resolution in accordance with any other rights and remedies afforded to them by law.</p>

<p><strong>Megan's Law.</strong> Notice: Pursuant to Section 290.46 of the Penal Code, information about specified registered sex offenders is made available to the public via an Internet website maintained by the Department of Justice at www.meganslaw.ca.gov. Depending on an offender's criminal history, this information will include either the address at which the offender resides or the community of residence and ZIP Code in which the offender resides.</p>

<p><strong>Asbestos.</strong> The Landlord is unaware of any asbestos-containing construction materials or any prior reports assessing their presence. Additionally, the Landlord has no knowledge of any potential carcinogens within the Leased Property.</p>

<p><strong>Lead-Based Paint.</strong> The Tenant has received the attached lead-based paint disclosure form, which is designed to satisfy federally required disclosure requirements regarding exposure to lead-based paints in the Leased Property.</p>

{{#if owner.fullName}}
<p class="party">The Landlord (Owner):</p>
<p class="sigline"></p>
<p class="party-name">{{owner.fullName}}</p>
<p class="date-line">Date</p>
<p class="party">The Property Manager / Agent:</p>
<p class="sigline"></p>
<p class="party-name">{{host.fullName}}</p>
<p class="date-line">Date</p>
{{/if}}
{{#unless owner.fullName}}
<p class="party">The Landlord:</p>
<p class="sigline"></p>
<p class="party-name">{{host.fullName}}</p>
<p class="date-line">Date</p>
{{/unless}}
<p class="party">The Tenant:</p>
<p class="sigline"></p>
<p class="party-name">{{tenant.fullName}}</p>
<p class="date-line">Date</p>

<h2>Information about Bed Bugs</h2>
<p><strong>Bed bug Appearance:</strong> Bed bugs have six legs. Adult bed bugs have flat bodies about 1/4 of an inch in length. Their color can vary from red and brown to copper colored. Young bed bugs are very small. Their bodies are about 1/16 of an inch in length. They have almost no color. When a bed bug feeds, its body swells, may lengthen, and becomes bright red, sometimes making it appear to be a different insect. Bed bugs do not fly. They can either crawl or be carried from place to place on objects, people, or animals. Bed bugs can be hard to find and identify because they are tiny and try to stay hidden.</p>
<p><strong>Life Cycle and Reproduction:</strong> An average bed bug lives for about 10 months. Female bed bugs lay one to five eggs per day. Bed bugs grow to full adulthood in about 21 days. Bed bugs can survive for months without feeding.</p>
<p><strong>Bed bug Bites:</strong> Because bed bugs usually feed at night, most people are bitten in their sleep and do not realize they were bitten. A person's reaction to insect bites is an immune response and so varies from person to person. Sometimes the red welts caused by the bites will not be noticed until many days after a person was bitten, if at all.</p>
<p><strong>Common signs and symptoms of a possible bed bug infestation:</strong></p>
<ul>
<li>Small red to reddish brown fecal spots on mattresses, box springs, bed frames, linens, upholstery, or walls.</li>
<li>Molted bed bug skins, white, sticky eggs, or empty eggshells.</li>
<li>Very heavily infested areas may have a characteristically sweet odor.</li>
<li>Red, itchy bite marks, especially on the legs, arms, and other body parts exposed while sleeping. However, some people do not show bed bug lesions on their bodies even though bed bugs may have fed on them.</li>
</ul>
<p>For more information, see the United States Environmental Protection Agency (https://www.epa.gov/bedbugs) and the National Pest Management Association (https://www.npmapestworld.org/).</p>
<p>The Tenant shall immediately notify the Landlord of the presence of any bed bugs in the Leased Property.</p>

<h2>Payments Required</h2>
<p>The Tenant shall pay the following amounts under this Lease:</p>
<ul>
<li>Security Deposit: {{deal.securityDeposit}}</li>
<li>First Month's Rent: {{deal.monthlyRent}}</li>
</ul>
<p>Both parties acknowledge and agree that all payments under this Lease shall be made directly to the Landlord or the Landlord's authorized entity as listed in this Lease Agreement.</p>
<p>All payments required under this Lease must be received by the Landlord before the Tenant or any Occupants take possession of the Premises unless otherwise agreed in writing by the Landlord.</p>

<h2>Residential Lease<br/>Inspection Checklist</h2>
<p>The Tenant has inspected the Leased Property and states that it is in satisfactory condition, free of defects, except as noted below:</p>
<table class="checklist">
<tr><th></th><th>SATISFACTORY</th><th>COMMENTS</th></tr>
<tr><td>Bathrooms</td><td></td><td></td></tr>
<tr><td>Carpeting</td><td></td><td></td></tr>
<tr><td>Ceilings</td><td></td><td></td></tr>
<tr><td>Closets</td><td></td><td></td></tr>
<tr><td>Countertops</td><td></td><td></td></tr>
<tr><td>Dishwasher</td><td></td><td></td></tr>
<tr><td>Disposal</td><td></td><td></td></tr>
<tr><td>Doors</td><td></td><td></td></tr>
<tr><td>Fireplace</td><td></td><td></td></tr>
<tr><td>Lights</td><td></td><td></td></tr>
<tr><td>Locks</td><td></td><td></td></tr>
<tr><td>Refrigerator</td><td></td><td></td></tr>
<tr><td>Screens</td><td></td><td></td></tr>
<tr><td>Stove</td><td></td><td></td></tr>
<tr><td>Walls</td><td></td><td></td></tr>
<tr><td>Windows</td><td></td><td></td></tr>
<tr><td>Window coverings</td><td></td><td></td></tr>
<tr><td></td><td></td><td></td></tr>
<tr><td></td><td></td><td></td></tr>
</table>
<p class="sigline"></p>
<p class="date-line">Date</p>
<p class="party">The Tenant:</p>
<p class="party-name">{{tenant.fullName}}</p>
<p class="date-line">Date</p>

<h2>Disclosure of Information on Lead-Based Paint or Lead-Based Hazards</h2>
<p><strong>Lead Warning Statement:</strong> Housing built before 1978 may contain lead-based paint. Lead from paint, paint chips, and dust can pose health hazards if not managed properly. Lead exposure is especially harmful to young children and pregnant women. Before renting pre-1978 housing, landlords must disclose the presence of known lead-based paint and/or lead-based paint hazards in the dwelling. The Tenant must also receive a federally approved pamphlet on lead poisoning prevention.</p>
<p><strong>Landlord's Disclosure:</strong></p>
{{#if listing.builtBefore1978}}
<p>The Leased Property was built before 1978, and the Landlord discloses the following to the Tenant in accordance with applicable law:</p>
{{/if}}
{{#unless listing.builtBefore1978}}
<p>The Leased Property was not built before 1978. The following disclosure is provided for completeness.</p>
{{/unless}}
<p><strong>(a) Presence of lead-based paint and/or lead-based paint hazards (check one):</strong></p>
<p class="check">Known lead-based paint and/or lead-based paint hazards are present in the Leased Property and/or Building (explain, including the basis for the determination that lead-based paint and/or lead-based paint hazards exist, the location of the lead-based paint and/or lead-based paint hazards, and the condition of the painted surfaces):</p>
<p class="writein"></p>
<p class="writein"></p>
<p class="check">The Landlord has no knowledge of lead-based paint and/or lead-based paint hazards in the Leased Property or Building.</p>
<p><strong>(b) Records and reports available to the Landlord (check one):</strong></p>
<p class="check">The Landlord has provided the Tenant with all available records and reports pertaining to lead-based paint and/or lead-based paint hazards in the Leased Property and/or Building (list documents below):</p>
<p class="writein"></p>
<p class="writein"></p>
<p class="check">The Landlord has no records or reports pertaining to lead-based paint and/or lead-based paint hazards in the Leased Property and/or Building.</p>
<p>{{listing.leadPaintKnowledge}}</p>
<p><strong>Acknowledgements:</strong></p>
<p class="check">The Tenant has received copies of all information listed above (Tenant initial).</p>
<p class="check">The Tenant has received the pamphlet Protect Your Family from Lead in Your Home, a copy of which is attached hereto and incorporated herein (Tenant initial).</p>
<p>The Landlord and the Tenant acknowledge and agree that the parties have reviewed the information above and each party certifies, to the best of the party's knowledge, that the information provided is true and accurate.</p>

<h2>California Lease Agreement Mold Notification Addendum</h2>
<p>The Landlord endeavors to maintain the highest quality living environment for the Tenant. Therefore, know that the Landlord has inspected the unit prior to the Lease and knows of no damp or wet building materials and knows of no mold or mildew contamination. The Tenant is hereby notified that mold, however, can grow if the Leased Property is not properly maintained or ventilated. If moisture is allowed to accumulate in the unit, it can cause mildew and mold to grow. It is important that the Tenant regularly allows air to circulate in the apartment. It is also important that the Tenant keep the interior of the unit clean and that they promptly notify the Landlord of any leaks, moisture problems, and/or mold growth.</p>
<p>The Tenant agrees to maintain the property in a manner that prevents the occurrence of an infestation of mold or mildew. The Tenant agrees to uphold this responsibility in part by complying with the following list of responsibilities:</p>
<ol>
<li>The Tenant agrees to keep the unit free of dirt and debris that can harbor mold.</li>
<li>The Tenant agrees to immediately report to the Landlord any water intrusion, such as plumbing leaks, drips, or "sweating" pipes.</li>
<li>The Tenant agrees to notify the Landlord of overflows from bathroom, kitchen, or unit laundry facilities, especially in cases where the overflow may have permeated walls or cabinets.</li>
<li>The Tenant agrees to report to the Landlord any significant mold growth on surfaces inside the premises.</li>
<li>The Tenant agrees to allow the Landlord to enter the unit to inspect and make necessary repairs.</li>
<li>The Tenant agrees to properly ventilate the bathroom while showering or bathing and to report to the Landlord any non-working fan.</li>
<li>The Tenant agrees to use exhaust fans whenever cooking, dishwashing, or cleaning.</li>
<li>The Tenant agrees to use all reasonable care to prevent outdoor water from penetrating into the interior of the unit.</li>
<li>The Tenant agrees to clean and dry any visible moisture on windows, walls, and other surfaces, including personal property, as soon as reasonably possible. (Note: Mold can grow on damp surfaces within 24 to 48 hours.)</li>
<li>The Tenant agrees to notify the Landlord of any problems with any air conditioning or heating systems that are discovered by the Tenant.</li>
<li>The Tenant agrees to indemnify and hold harmless the Landlord from any actions, claims, losses, damages, and expenses, including, but not limited to, attorneys' fees that the Landlord may sustain or incur as a result of the negligence of the Tenant or any guest, licensee, invitee or other person living in, occupying, or using the Property.</li>
</ol>
<p>If the Tenant fails to comply with the terms of this Mold Addendum, it is a material breach of the Lease it is attached to. In the event there is a conflict between this Mold Addendum and the Lease, the terms of the Mold Addendum shall govern.</p>

<h2>Rent Cap and Just Cause Addendum</h2>
<p>California law limits the amount your rent can be increased. See Section 1947.12 of the Civil Code for more information. California law also provides that after all of the tenants have continuously and lawfully occupied the property for 12 months or more, or at least one of the tenants has continuously and lawfully occupied the property for 24 months or more, a landlord must provide a statement of cause in any notice to terminate a tenancy. See Section 1946.2 of the Civil Code for more information.</p>
<p><strong>Rent Cap Requirements:</strong> California Civil Code § 1947.12 limits the amount the Landlord can increase rent as follows:</p>
<ol>
<li>Subject to certain provisions of Civil Code Section 1947.12, the Landlord cannot, over the course of any 12-month period, increase rent for the Leased Property more than 5 percent plus the percentage change in the cost of living, or 10 percent, whichever is lower, of the lowest Rent charged for the Leased Property at any time during the 12 months prior to the effective date of the increase.</li>
<li>If the Tenant remains in occupancy of the Leased Property over any 12-month period, the Landlord cannot increase rent for the Leased Property in more than two increments over that 12-month period.</li>
<li>For a new tenancy in which no tenant from the prior tenancy remains, the owner may establish the initial rate not subject to Paragraph 1 of this Section. Paragraph 1 of this Section is only applicable to subsequent increases after the initial rental rate has been established.</li>
</ol>
<p>WITH CERTAIN EXEMPTIONS, THE LANDLORD MAY BE SUBJECT TO THE JUST CAUSE PROVISIONS OF CIVIL CODE SECTION 1946.2 AND INFORMS THE TENANT OF THE FOLLOWING:</p>
<p><strong>Just Cause Requirements — At-fault Just Cause:</strong></p>
<ol>
<li>Default in payment of rent.</li>
<li>Breach of a material term of the Lease, as described in Code of Civil Procedure Section 1161, Paragraph (3), including but not limited to, violation of a provision of the Lease after being issued a written notice to correct the violation.</li>
<li>Maintaining, committing, or permitting the maintenance of a nuisance as described in Code of Civil Procedure Section 1161, Paragraph (4).</li>
<li>Committing waste as described in Code of Civil Procedure Section 1161, Paragraph (4).</li>
<li>The Tenant had a written Lease that terminated on or after January 1, 2020, and after a written request or demand from the owner, the Tenant refused to execute a written extension or renewal of the Lease for an additional term of similar duration with similar provisions, provided that those terms do not violate Section 1946.1 or any other provision of law.</li>
<li>Criminal activity by the Tenant on the residential real property, including any common areas, or any criminal threat, as defined in Penal Code Section 422, subdivision (a), directed to any owner or agent of the owner of the Leased Property.</li>
<li>Assigning or subletting the premises in violation of the Tenant's Lease.</li>
<li>The Tenant's refusal to allow the owner to enter the residential real property pursuant to a request consistent with Civil Code Sections 1101.5 and 1954, and Health and Safety Code Sections 13113.7 and 17926.1.</li>
<li>Using the premises for an unlawful purpose as described in Code of Civil Procedure Section 1161, Paragraph (4).</li>
<li>When the Tenant fails to deliver possession of the residential real property after providing the owner with written notice of the Tenant's intention to terminate the hiring of the real property or makes a written offer to surrender that is accepted in writing by the Landlord, but fails to deliver possession at the time specified in that written notice.</li>
</ol>
<p><strong>No-fault Just Cause:</strong></p>
<ol>
<li>Intent to occupy the residential real property by the owner or their spouse, domestic partner, children, grandchildren, parents, or grandparents (Family move-in). For leases entered into on or after January 1, 2020, the Tenant and the Landlord agree that the Landlord has the right to terminate this Lease if the Landlord, or their spouse, domestic partner, children, grandchildren, parents, or grandparents, unilaterally decide to occupy the Leased Property.</li>
<li>Withdrawal of the Leased Property from the rental market.</li>
<li>Unsafe habitation, as determined by a government agency that has issued an order to vacate, or to comply, or other order that necessitates vacating the residential property.</li>
<li>The intent to demolish or substantially remodel the residential real property. "Substantially remodel" means the replacement or substantial modification of any structural, electrical, plumbing, or mechanical system that requires a permit that cannot be accomplished in a safe manner with the Tenant in place and that requires the Tenant to vacate the residential real property for at least 30 days. Cosmetic improvements alone do not qualify.</li>
</ol>
<p><strong>Tenant Payments under No-Fault Just Cause Eviction:</strong> If the Landlord issues a termination of tenancy under a No-Fault Just Cause, the Landlord notifies the Tenant of the right to direct payment relocation assistance equal to one month of the Tenant's rent in effect at the time of the termination, and shall be provided within 15 calendar days of service of the notice. In lieu of direct payment, the Landlord may waive the payment of rent for the final month of tenancy prior to the rent becoming due.</p>
<p><strong>Specific Exemptions.</strong> Certain housing accommodations are exempt from just cause and/or rent-cap requirements, including housing that has been issued a certificate of occupancy within the previous 15 years, qualifying single-family owner-occupied residences, and other exemptions under the Civil Code. Rent-cap / just-cause exemption claimed for this property: {{listing.rentCapJustCauseExempt}}.</p>
{{#if listing.rentCapJustCauseExempt}}
<p><strong>Notice of Exemption.</strong> This property is not subject to the rent limits imposed by Section 1947.12 of the Civil Code and is not subject to the just cause requirements of Section 1946.2 of the Civil Code. This property meets the requirements of Sections 1947.12(d)(5) and 1946.2(e)(8) of the Civil Code AND the owner is not any of the following: (1) a real estate investment trust, as defined by Section 856 of the Internal Revenue Code; (2) a corporation; or (3) a limited liability company in which at least one member is a corporation.</p>
{{/if}}
<p>NOTE: Other exemptions under the Civil Code may apply. Additionally, this property may be subject to local rent cap and just cause eviction controls, which may impose additional restrictions.</p>
<p>The undersigned acknowledge a copy of this document and agree that the terms specified above are made a part of the Lease.</p>
{{#if owner.fullName}}
<p class="party">The Landlord (Owner):</p>
<p class="sigline"></p>
<p class="party-name">{{owner.fullName}}</p>
<p class="date-line">Date</p>
<p class="party">The Property Manager / Agent:</p>
<p class="sigline"></p>
<p class="party-name">{{host.fullName}}</p>
<p class="date-line">Date</p>
{{/if}}
{{#unless owner.fullName}}
<p class="party">The Landlord:</p>
<p class="sigline"></p>
<p class="party-name">{{host.fullName}}</p>
<p class="date-line">Date</p>
{{/unless}}
<p class="party">The Tenant:</p>
<p class="sigline"></p>
<p class="party-name">{{tenant.fullName}}</p>
<p class="date-line">Date</p>

{{#if owner.fullName}}
<h2>OWNER CONSENT AND AUTHORIZATION</h2>
<p>This Addendum is attached to and made part of the Lease Agreement dated {{lease.effectiveDate}} between the Landlord and Tenant(s).</p>
<p>This tenancy is for a term of more than thirty (30) days. {{owner.fullName}} ("Owner") is the owner of the Leased Property and is the Landlord under this Lease. The Owner authorizes {{host.fullName}} ("Property Manager") to enter into this Lease on the Owner's behalf and consents to this tenancy, including all terms of the Lease and any addenda.</p>
<p>The Owner acknowledges that California law requires the owner's consent for residential tenancies of more than thirty (30) days when the Lease is executed by a property manager or other agent.</p>
<p>The Owner recorded this consent in Lagedra on {{owner.consentDate}} (consent record {{owner.consentVersion}}).</p>
<p>Owner email: {{owner.email}}. Owner phone: {{owner.phone}}. Owner mailing address: {{owner.mailingAddress}}.</p>
<p class="party">The Owner / Landlord:</p>
<p class="sigline"></p>
<p class="party-name">{{owner.fullName}}</p>
<p class="date-line">Date</p>
<p class="party">The Property Manager / Agent:</p>
<p class="sigline"></p>
<p class="party-name">{{host.fullName}}</p>
<p class="date-line">Date</p>
{{/if}}

{{#if broker.name}}
<h2>BROKER DISCLOSURE AND AUTHORIZATION ADDENDUM</h2>
<p>This Addendum is attached to and made part of the Lease Agreement dated {{lease.effectiveDate}} between the Landlord and Tenant(s).</p>
<p><strong>Broker Disclosure, Agency, and Allocation of Responsibilities</strong></p>
<p>The Landlord has engaged a California licensed real estate broker (DRE License No. {{broker.dreLicense}}), {{broker.name}}, to act as the Landlord's authorized agent in connection with this tenancy. The Broker's scope of authority includes overseeing legal compliance related to the tenancy, including but not limited to the preparation and service of notices, coordination of eviction proceedings if required, and ensuring compliance with applicable federal, state, and local landlord-tenant laws. {{broker.scopeNotes}}</p>
<p>This Addendum does not modify any other terms of the Lease Agreement, which remain in full force and effect.</p>
<h2>BROKER / REPRESENTATIVE ACKNOWLEDGMENT</h2>
<p>Name: ________ {{broker.name}} ________</p>
<p>Role: Broker / Representative</p>
<p>Signature: _______________________</p>
<p>Date: ____________________________</p>
{{/if}}

{{#if owner.fullName}}
<p>Notices to the Landlord (Owner) shall be sent to {{owner.mailingAddress}} and {{owner.email}}. Notices to the Property Manager shall be sent to {{host.noticeAddress}} and {{host.email}}. Notices to the Tenant shall be sent to {{tenant.mailingAddress}}. Tenant phone/email: {{tenant.phone}} / {{tenant.email}}.</p>
{{/if}}
{{#unless owner.fullName}}
<p>Notices to the Landlord shall be sent to {{host.noticeAddress}}. Notices to the Tenant shall be sent to {{tenant.mailingAddress}}. Tenant phone/email: {{tenant.phone}} / {{tenant.email}}.</p>
{{/unless}}$leasebody$,
        now(), now(), false
    );

    UPDATE lease_agreements.lease_template_versions
       SET "Status" = 'Deprecated', "UpdatedAt" = now()
     WHERE "TemplateId" = v_template_id
       AND "Id" <> v_new_version_id
       AND "Status" = 'Active';

    UPDATE lease_agreements.lease_templates
       SET "ActiveVersionId" = v_new_version_id,
           "Title"           = 'California Lease Agreement',
           "UpdatedAt"       = now()
     WHERE "Id" = v_template_id;

    -- Bookings whose PDF was rendered from the superseded version keep
    -- serving that stale 3-page file, because the download endpoint
    -- returns the stored blob as-is. Dropping those rows makes the next
    -- download re-render against the new template. Scoped to the old
    -- version id so host-uploaded leases (TemplateVersionId IS NULL)
    -- and anything already on the new version are left alone.
    DELETE FROM lease_agreements.deal_lease_documents
     WHERE "TemplateVersionId" = v_old_version_id;

    GET DIAGNOSTICS v_deleted = ROW_COUNT;

    RAISE NOTICE 'Published version % (%). Cleared % stale booking lease PDF(s).',
        v_next_number, v_new_version_id, v_deleted;
END
$outer$;

COMMIT;

-- Verification: expect one Active row with a body of 46440 chars.
SELECT v."VersionNumber",
       v."Status",
       length(v."BodyHtml")           AS body_chars,
       (v."Id" = t."ActiveVersionId") AS is_live
  FROM lease_agreements.lease_templates         t
  JOIN lease_agreements.lease_template_versions v ON v."TemplateId" = t."Id"
 WHERE t.jurisdiction_code = 'US-CA'
 ORDER BY v."VersionNumber";