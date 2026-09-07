import { Link } from "react-router-dom";
import { COMPANY, COMPANY_MAILING_ADDRESS } from "./companyInfo";
import type { LegalDocument } from "./legalTypes";

export const TERMS_EFFECTIVE_DATE = "September 3, 2026";

export const termsDocument: LegalDocument = {
  title: "Terms and Conditions",
  metaTitle: "Terms and Conditions — Lagedra",
  lede:
    "These Terms and Conditions govern your use of Lagedra, the mid-term rental marketplace and trust protocol at lagedra.com. By creating an account, listing a home, requesting a stay, or otherwise using the platform, you agree to this agreement.",
  effectiveDateLabel: TERMS_EFFECTIVE_DATE,
  sections: [
    {
      id: "agreement",
      title: "1. Agreement to these terms",
      content: (
        <>
          <p>
            This agreement is between you and the operator of the Lagedra platform
            (&ldquo;Lagedra,&rdquo; &ldquo;we,&rdquo; &ldquo;us&rdquo;). It applies to the website,
            applications, and related services we provide at{" "}
            <a href="https://www.lagedra.com">www.lagedra.com</a> and affiliated
            domains (the &ldquo;Services&rdquo;).
          </p>
          <p>
            If you use the Services on behalf of a company or other organization, you
            represent that you have authority to bind that organization, and
            &ldquo;you&rdquo; includes that organization.
          </p>
          <p>
            Our{" "}
            <Link to="/privacy">Privacy Policy</Link> explains how we collect and
            use personal information. Additional terms may apply to a specific
            booking, listing, insurance product, partner program, or payment
            feature. If those terms conflict with this agreement, the more specific
            terms control for that feature.
          </p>
        </>
      ),
    },
    {
      id: "platform",
      title: "2. What Lagedra is — and is not",
      content: (
        <>
          <p>
            Lagedra is a marketplace and trust protocol for mid-term, typically
            furnished residential stays — generally 30 days or longer. Hosts can
            publish listings, guests can search and request to book, and
            institutional partners can source housing for clients. The Services
            include listing tools, messaging and inquiry, applications, quotes,
            identity verification, a sealed deal record (the Truth Surface),
            payments, lease generation or host-supplied lease documents, stay
            access, compliance tooling, and an in-platform dispute process.
          </p>
          <p>
            <strong>Lagedra is not a landlord, property manager, real-estate
            broker, insurer, bank, or law firm</strong>, unless we expressly say
            otherwise in writing for a specific offering. Hosts — not Lagedra —
            offer the home. Guests — not Lagedra — occupy it. When a booking is
            confirmed, the binding stay terms are the sealed Truth Surface and the
            applicable lease or host-provided agreement, not this platform
            agreement alone.
          </p>
          <p>
            We do not guarantee that any listing, host, guest, or partner is
            suitable, available, or legally permitted for your situation. You are
            responsible for reviewing the listing, the deal terms, and applicable
            housing law before you commit.
          </p>
        </>
      ),
    },
    {
      id: "eligibility",
      title: "3. Eligibility and accounts",
      content: (
        <>
          <p>
            You must be at least 18 years old and able to form a binding contract
            to use the Services. We may refuse, suspend, or close an account if we
            believe you are ineligible, have provided false information, or have
            violated this agreement.
          </p>
          <p>When you create an account you agree to:</p>
          <ul>
            <li>Provide accurate registration and profile information and keep it current.</li>
            <li>Keep your login credentials confidential and notify us of unauthorized use.</li>
            <li>
              Complete identity verification when we require it to list, book, or
              receive payouts.
            </li>
            <li>
              Record required consents, including data-processing and KYC consent,
              before certain write actions on the platform.
            </li>
          </ul>
          <p>
            You are responsible for activity that occurs under your account. We
            may restrict accounts for fraud, abuse, unpaid amounts, safety
            concerns, or legal risk.
          </p>
        </>
      ),
    },
    {
      id: "hosts",
      title: "4. Hosts, listings, and listing review",
      content: (
        <>
          <p>
            If you list a home, you represent that you have the legal right to
            offer it for the stays you advertise — as owner, authorized property
            manager, or other lawful agent — and that the listing complies with
            local housing, zoning, tax, HOA, and licensing rules.
          </p>
          <p>
            Listings submitted for publication are reviewed by Lagedra before they
            go live. We may approve, deny, or ask you to revise a listing. Denial
            does not by itself determine whether you may legally rent the home
            off-platform. We may also take a published listing down later if it
            no longer meets our standards or the law.
          </p>
          <p>You are responsible for:</p>
          <ul>
            <li>
              Accurate photos, descriptions, pricing, availability, house rules,
              fees, and stay length.
            </li>
            <li>
              Choosing a Lagedra lease template for the listing&apos;s jurisdiction
              or uploading your own lease if you elect a host-provided agreement.
              If you upload your own lease, you are responsible for its legality
              and for making sure guests can review it before they book.
            </li>
            <li>
              Connecting a valid payout method (currently Stripe Connect) if you
              want to receive rent and deposits through Lagedra.
            </li>
            <li>
              Honoring confirmed bookings and the sealed deal terms, including
              deposit, cancellation, and access commitments.
            </li>
            <li>
              Collecting and remitting occupancy, tourist, income, and other taxes
              that apply to you, except where we expressly collect a tax on your
              behalf and say so at checkout.
            </li>
          </ul>
          <p>
            Connecting a channel manager or other distribution tool (for example
            Hostaway, Guesty, or similar) does not transfer your obligations to
            Lagedra. You remain responsible for the imported content and for
            calendar and reservation conflicts.
          </p>
        </>
      ),
    },
    {
      id: "guests",
      title: "5. Guests and bookings",
      content: (
        <>
          <p>
            Guests may browse public listings, inquire, apply, and — where
            enabled — book instantly or request host approval. A booking is not
            confirmed until the required steps for that listing are complete,
            which typically include agreeing to the Truth Surface terms, providing
            a payment method, paying amounts due at checkout, and any host or
            owner consent the listing requires.
          </p>
          <p>As a guest you agree to:</p>
          <ul>
            <li>Occupy only as permitted by the listing and the sealed deal terms.</li>
            <li>
              Pay rent, deposits, and fees when due through the methods we
              support.
            </li>
            <li>Follow house rules, occupancy limits, and applicable law.</li>
            <li>
              Treat the home and neighbors with care and report material damage
              promptly.
            </li>
            <li>
              Not use a booking to operate an unauthorized hotel, event venue,
              or sublet unless the deal terms expressly allow it.
            </li>
          </ul>
          <p>
            Precise street address and counterpart contact details are generally
            withheld until a booking is confirmed. Do not attempt to circumvent
            that privacy control.
          </p>
        </>
      ),
    },
    {
      id: "partners",
      title: "6. Institutional partners",
      content: (
        <>
          <p>
            Relocation firms, insurers, and other approved organizations may use
            partner tools to source housing, refer guests, or — where a host
            allows it — place a direct reservation on a member&apos;s behalf.
            Partners must have authority from the guest or client they represent,
            must not misstate a placement, and must follow any partner program
            rules we publish.
          </p>
          <p>
            A partner reservation does not make Lagedra the employer, insurer, or
            housing provider for the placed guest. The guest and host remain
            responsible for the stay terms once the deal is confirmed.
          </p>
        </>
      ),
    },
    {
      id: "payments",
      title: "7. Fees and payments",
      content: (
        <>
          <p>
            Payment processing is provided by Stripe and similar providers. Card
            and bank details are collected by those providers, not stored as raw
            card numbers on Lagedra. Rent and security deposits for a stay are
            generally paid to the host&apos;s connected account. Lagedra collects
            its own service, protocol, or similar fees, and collects the
            included stay-protection fee at booking.
          </p>
          <p>
            Amounts shown before you confirm a booking are estimates based on the
            listing, dates, guest count, and any accepted offer. The amount you
            authorize at checkout is the amount due at that time. Recurring rent
            after move-in, if collected through Lagedra, follows the sealed deal
            and billing schedule.
          </p>
          <p>
            Hosts may owe a protocol or subscription fee for using the platform.
            Fee schedules can change; we will describe material fees before you
            complete a charge or, for host fees, in the host billing materials.
          </p>
          <p>
            You authorize us and our payment providers to charge the payment
            method you have on file for amounts you owe, including rent,
            deposits, fees, and amounts arising from damage claims or chargebacks
            that the deal terms allow. Failed payments may result in late fees
            (if the lease provides for them), listing or account restrictions, or
            cancellation of a pending booking.
          </p>
          <p>
            Lagedra is not a bank and does not hold guest funds as a general
            deposit-taking institution. Timing of payouts depends on Stripe,
            bank networks, and whether a booking has reached the required status.
          </p>
        </>
      ),
    },
    {
      id: "agreements",
      title: "8. Truth Surface, leases, and sealed terms",
      content: (
        <>
          <p>
            Before a stay is confirmed, the parties review a Truth Surface — a
            structured, cryptographically sealed record of the headline deal
            terms (dates, rent, deposit, guests, and related conditions). Once
            confirmed, that record is intended to be the binding commercial
            snapshot of the booking on Lagedra.
          </p>
          <p>
            A lease or occupancy agreement is also generated or attached:
          </p>
          <ul>
            <li>
              <strong>Lagedra template.</strong> We may generate a jurisdiction
              template (for example a California residential lease) filled with
              the listing and deal details. Templates are provided as a
              convenience. They are not a substitute for legal advice, and we do
              not warrant that a template is complete or current for every
              situation.
            </li>
            <li>
              <strong>Host-provided lease.</strong> If the host elects their own
              document, that document — not the Lagedra template — is the
              occupancy agreement for the stay. Review it before you confirm.
            </li>
          </ul>
          <p>
            If there is a conflict between the sealed Truth Surface and a
            generated summary in the app, the sealed record controls. If there is
            a conflict between the Truth Surface and a host-provided lease on a
            matter the lease lawfully governs, the parties should treat the more
            specific signed lease as controlling for that matter and contact
            support so we can record the issue.
          </p>
        </>
      ),
    },
    {
      id: "verification",
      title: "9. Identity verification and background checks",
      content: (
        <>
          <p>
            We may require government-ID and selfie verification, phone
            verification, and other checks before you list, book, or receive
            payouts. Verification may be reviewed by Lagedra staff and/or
            identity vendors. A &ldquo;verified&rdquo; badge means the person
            completed our process — not that we guarantee their character,
            credit, or future conduct.
          </p>
          <p>
            Some features may request a consumer report or background check.
            Those checks are subject to the Fair Credit Reporting Act and similar
            laws. We will ask for a separate FCRA consent before ordering a
            consumer report. You may refuse; some bookings or deposit options
            may then be unavailable.
          </p>
        </>
      ),
    },
    {
      id: "insurance",
      title: "10. Insurance and protection products",
      content: (
        <>
          <p>
            Eligible bookings include stay protection provided through our
            partner Truvi (Screen &amp; Protect). The fee is charged to the guest
            at booking and is not optional. Stay protection is guest screening
            plus discretionary damage protection. It is not renter&apos;s
            insurance, does not replace any renter&apos;s-liability requirement
            in a lease, and does not cover pet damage. Coverage, exclusions, and
            claims follow Truvi&apos;s guest agreement, not this agreement. We
            are not an insurance company and do not decide claims unless a
            policy names us in a limited administrative role.
          </p>
          <p>
            By completing a booking you agree to Truvi&apos;s{" "}
            <a
              href="https://truvi.com/screen-and-protect/guest-agreement/"
              target="_blank"
              rel="noopener noreferrer"
            >
              Screen &amp; Protect guest agreement
            </a>
            . A cancelled or rejected screening may leave a small non-refundable
            screening charge.
          </p>
        </>
      ),
    },
    {
      id: "cancellations",
      title: "11. Cancellations, changes, and refunds",
      content: (
        <>
          <p>
            Each listing has a cancellation policy (for example Flexible,
            Moderate, Strict, or custom terms). Refunds of rent, deposits, fees,
            and stay-protection charges follow that policy, the sealed deal, and
            any mandatory housing law that cannot be waived. A stay-protection
            refund may retain the screening charge. Platform service fees are
            refundable only if the applicable policy or a written Lagedra
            decision says so.
          </p>
          <p>
            We may cancel a booking that cannot be fulfilled — for example if
            the listing is removed, payment fails, verification is not completed,
            or we reasonably believe the booking poses a safety, fraud, or legal
            risk. In those cases we will describe the available refund path.
          </p>
        </>
      ),
    },
    {
      id: "conduct",
      title: "12. Acceptable use",
      content: (
        <>
          <p>You may not:</p>
          <ul>
            <li>Violate law, housing discrimination rules, or another person&apos;s rights.</li>
            <li>
              Post false listings, fake reviews, or misleading verification
              information.
            </li>
            <li>
              Circumvent fees, identity checks, address privacy, or payment
              flows in bad faith.
            </li>
            <li>
              Scrape, overload, reverse engineer, or interfere with the Services.
            </li>
            <li>
              Upload malware or content we reasonably consider illegal, hateful,
              or pornographic involving minors.
            </li>
            <li>
              Use the Services to traffic people, launder money, or commit fraud.
            </li>
          </ul>
          <p>
            We may remove content, cancel bookings, hold payouts pending
            investigation, or restrict accounts when we believe this section was
            violated.
          </p>
        </>
      ),
    },
    {
      id: "communications",
      title: "13. Communications",
      content: (
        <>
          <p>
            We send transactional email and in-app notices about your account,
            bookings, and security. You can manage marketing email preferences
            in the product or by contacting us. You still receive messages that
            are necessary to operate a booking or comply with law.
          </p>
          <p>
            Automated text messages are optional and are described in{" "}
            <a href="#sms">Text messages</a>. Phone-verification codes are sent
            only when you request one and are not marketing or campaign
            messages.
          </p>
        </>
      ),
    },
    {
      id: "sms",
      title: "14. Text messages (SMS)",
      content: (
        <>
          <p>
            If you opt in, Lagedra may send automated text messages about
            booking and payment activity, promotional offers, and important
            account updates. Message frequency is{" "}
            <strong>up to 8 messages per month</strong>. Message and data rates
            may apply depending on your mobile phone service plan.
          </p>
          <p>
            Reply <strong>HELP</strong> for help or <strong>STOP</strong> to
            cancel any time. You can also opt in or out at{" "}
            <Link to="/sms">www.lagedra.com/sms</Link> or in notification
            preferences after you sign in. By providing your phone number and
            confirming consent, you agree to receive those texts from Lagedra.
            Consent is not required to book a stay or to use Lagedra.
          </p>
          <p>
            The checkbox on the SMS form is never pre-selected. We store your
            opt-in against the mobile number you provide so a later STOP
            applies to that number. We use Twilio to deliver texts. Our{" "}
            <Link to="/privacy#sms">Privacy Policy</Link> describes how we
            handle that number.
          </p>
          <p>
            One-time passcodes are a separate program. You opt in by creating
            an account, adding a mobile number, and tapping{" "}
            <strong>Send verification code</strong> on Verification. We then
            send a 6-digit Lagedra code that expires in 10 minutes. Those
            texts are not campaign messages. The public description of that
            flow is at <Link to="/sms#otp">www.lagedra.com/sms#otp</Link>.
          </p>
          <p>
            Support: <a href="mailto:info@lagedra.com">info@lagedra.com</a> or{" "}
            <a href="tel:+12137352362">213-735-2362</a>.
          </p>
        </>
      ),
    },
    {
      id: "ip",
      title: "15. Intellectual property and your content",
      content: (
        <>
          <p>
            Lagedra and its licensors own the Services, including trademarks,
            software, and design. You receive a limited, revocable license to use
            the Services for their intended purpose.
          </p>
          <p>
            You retain rights in photos, descriptions, and other content you
            upload. You grant Lagedra a worldwide, non-exclusive license to host,
            display, and distribute that content as needed to operate, promote,
            and improve the marketplace (including showing listing photos in
            search and emails). You represent that you have the rights needed to
            grant that license.
          </p>
        </>
      ),
    },
    {
      id: "disclaimers",
      title: "16. Disclaimers",
      content: (
        <>
          <p>
            THE SERVICES ARE PROVIDED &ldquo;AS IS&rdquo; AND &ldquo;AS
            AVAILABLE.&rdquo; TO THE MAXIMUM EXTENT PERMITTED BY LAW, LAGEDRA
            DISCLAIMS WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
            PURPOSE, TITLE, AND NON-INFRINGEMENT. We do not warrant that listings
            are accurate, that a stay will meet your expectations, that the
            Services will be uninterrupted, or that generated lease text is
            legally sufficient for your transaction.
          </p>
        </>
      ),
    },
    {
      id: "liability",
      title: "17. Limitation of liability",
      content: (
        <>
          <p>
            TO THE MAXIMUM EXTENT PERMITTED BY LAW, LAGEDRA AND ITS OFFICERS,
            EMPLOYEES, AND AGENTS WILL NOT BE LIABLE FOR INDIRECT, INCIDENTAL,
            SPECIAL, CONSEQUENTIAL, EXEMPLARY, OR PUNITIVE DAMAGES, OR FOR LOST
            PROFITS, LOST DATA, OR LOSS OF GOODWILL, ARISING OUT OF THE SERVICES
            OR A STAY ARRANGED THROUGH THEM.
          </p>
          <p>
            OUR TOTAL LIABILITY FOR ANY CLAIM RELATING TO THE SERVICES IS LIMITED
            TO THE GREATER OF (A) THE AMOUNTS YOU PAID TO LAGEDRA IN PLATFORM
            FEES (EXCLUDING RENT AND DEPOSITS PASSED THROUGH TO A HOST) DURING
            THE TWELVE MONTHS BEFORE THE CLAIM, OR (B) ONE HUNDRED U.S. DOLLARS
            (US $100). These limits do not apply to liability that the law does
            not allow us to limit, such as certain personal-injury claims or
            liability caused by our fraud.
          </p>
        </>
      ),
    },
    {
      id: "indemnity",
      title: "18. Indemnification",
      content: (
        <>
          <p>
            You will defend and indemnify Lagedra against claims, damages, and
            expenses (including reasonable attorneys&apos; fees) arising from
            your content, your listings or stays, your breach of this agreement,
            or your violation of law or third-party rights, except to the extent
            caused by Lagedra&apos;s willful misconduct.
          </p>
        </>
      ),
    },
    {
      id: "disputes",
      title: "19. Disputes, arbitration, and governing law",
      content: (
        <>
          <p>
            If a problem arises from a confirmed stay — for example damage,
            deposit return, or an alleged breach of the sealed deal — you agree
            to use Lagedra&apos;s in-platform case and evidence tools when they
            are available for that deal. Decisions issued through that process
            apply as described in the deal terms and any published arbitration
            rules. That process does not take away rights that housing or
            consumer law makes non-waivable.
          </p>
          <p>
            For disputes about the Services themselves (your account, these
            Terms, or platform fees), contact{" "}
            <a href="mailto:info@lagedra.com">info@lagedra.com</a> first so we
            can try to resolve the issue informally. This agreement is governed
            by the laws of the State of California, excluding conflict-of-laws
            rules. Courts located in California will have exclusive jurisdiction,
            except that either party may seek injunctive relief in any court of
            competent jurisdiction to protect intellectual property or
            confidential information.
          </p>
          <p>
            Local residential-tenancy law of the listing&apos;s jurisdiction may
            also apply to a stay and can override conflicting private terms.
          </p>
        </>
      ),
    },
    {
      id: "changes",
      title: "20. Changes, termination, and assignment",
      content: (
        <>
          <p>
            We may update these Terms. The effective date at the top will change
            when we do. Material changes will be posted on this page. Continued
            use after the new date constitutes acceptance, except that a
            confirmed booking keeps the deal terms that were sealed for that
            stay.
          </p>
          <p>
            You may stop using the Services and request account closure as
            described in the{" "}
            <Link to="/privacy">Privacy Policy</Link>. We may suspend or
            terminate access if you breach this agreement or if we discontinue
            the Services. Surviving sections include fees owed, IP, disclaimers,
            liability limits, indemnity, and dispute terms.
          </p>
          <p>
            You may not assign this agreement without our consent. We may assign
            it in connection with a reorganization or sale of the business.
          </p>
        </>
      ),
    },
    {
      id: "contact",
      title: "21. Contact",
      content: (
        <>
          <p>
            Questions about these Terms:{" "}
            <a href="mailto:info@lagedra.com">info@lagedra.com</a>
            <br />
            Phone:{" "}
            <a href="tel:+12137352362">213-735-2362</a>
            <br />
            Mail: {COMPANY.legalName}, {COMPANY_MAILING_ADDRESS}
            <br />
            Website:{" "}
            <a href="https://www.lagedra.com">www.lagedra.com</a>
          </p>
        </>
      ),
    },
  ],
};
