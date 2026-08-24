import { lazy } from "react";
import { Navigate, createBrowserRouter, useLocation } from "react-router-dom";
import { RequireAuth } from "@/app/auth/RequireAuth";
import { RequireLaunchAccess } from "@/app/auth/RequireLaunchAccess";
import { RequirePreLaunchHostSurface } from "@/app/auth/RequirePreLaunchHostSurface";
import { RequireMember } from "@/app/auth/RequireMember";
import { RequireRole } from "@/app/auth/RequireRole";
import { RequireArbitrationAccess } from "@/app/auth/RequireArbitrationAccess";
import { roles } from "@/app/auth/roles";
import { AuthLayout } from "@/features/auth/pages/AuthLayout";
import { AppShell } from "@/app/layout/AppShell";
import { MarketplaceLayout } from "@/app/layout/MarketplaceLayout";
import { LazyPage } from "@/app/layout/LazyPage";
import { RouteErrorBoundary } from "@/app/layout/RouteErrorBoundary";

const LoginPage = lazy(() => import("@/features/auth/pages/LoginPage").then((m) => ({ default: m.LoginPage })));
const JoinPage = lazy(() => import("@/features/join/pages/JoinPage").then((m) => ({ default: m.JoinPage })));
const HowItWorksPage = lazy(() => import("@/features/join/pages/HowItWorksPage").then((m) => ({ default: m.HowItWorksPage })));
const VerifyEmailPage = lazy(() => import("@/features/auth/pages/VerifyEmailPage").then((m) => ({ default: m.VerifyEmailPage })));
const ForgotPasswordPage = lazy(() => import("@/features/auth/pages/ForgotPasswordPage").then((m) => ({ default: m.ForgotPasswordPage })));
const ResetPasswordPage = lazy(() => import("@/features/auth/pages/ResetPasswordPage").then((m) => ({ default: m.ResetPasswordPage })));
const DashboardPage = lazy(() => import("@/features/auth/pages/DashboardPage").then((m) => ({ default: m.DashboardPage })));
const ProfilePage = lazy(() => import("@/features/auth/pages/ProfilePage").then((m) => ({ default: m.ProfilePage })));
const PublicProfilePage = lazy(() => import("@/features/auth/pages/PublicProfilePage").then((m) => ({ default: m.PublicProfilePage })));
const UsersPage = lazy(() => import("@/features/admin/pages/UsersPage").then((m) => ({ default: m.UsersPage })));
const DefinitionsPage = lazy(() => import("@/features/admin/pages/DefinitionsPage").then((m) => ({ default: m.DefinitionsPage })));
const InsuranceUnknownQueuePage = lazy(() => import("@/features/admin/pages/InsuranceUnknownQueuePage").then((m) => ({ default: m.InsuranceUnknownQueuePage })));
const FraudFlagsPage = lazy(() => import("@/features/admin/pages/FraudFlagsPage").then((m) => ({ default: m.FraudFlagsPage })));
const ArbitrationBacklogPage = lazy(() => import("@/features/admin/pages/ArbitrationBacklogPage").then((m) => ({ default: m.ArbitrationBacklogPage })));
const EvidenceReviewPage = lazy(() => import("@/features/admin/pages/EvidenceReviewPage").then((m) => ({ default: m.EvidenceReviewPage })));
const ManualVerificationPage = lazy(() => import("@/features/admin/pages/ManualVerificationPage").then((m) => ({ default: m.ManualVerificationPage })));
const ComplianceViolationsPage = lazy(() => import("@/features/admin/pages/ComplianceViolationsPage").then((m) => ({ default: m.ComplianceViolationsPage })));
const UserRestrictionsPage = lazy(() => import("@/features/admin/pages/UserRestrictionsPage").then((m) => ({ default: m.UserRestrictionsPage })));
const LeaseAgreementTemplatesPage = lazy(() => import("@/features/admin/pages/LeaseAgreementTemplatesPage").then((m) => ({ default: m.LeaseAgreementTemplatesPage })));
const DualControlApprovalsPage = lazy(() => import("@/features/admin/pages/DualControlApprovalsPage").then((m) => ({ default: m.DualControlApprovalsPage })));
const BlogPostsPage = lazy(() => import("@/features/admin/pages/BlogPostsPage").then((m) => ({ default: m.BlogPostsPage })));
const BlogPostEditorPage = lazy(() => import("@/features/admin/pages/BlogPostEditorPage").then((m) => ({ default: m.BlogPostEditorPage })));
const SeoPage = lazy(() => import("@/features/admin/pages/SeoPage").then((m) => ({ default: m.SeoPage })));
const AuditSearchPage = lazy(() => import("@/features/admin/pages/AuditSearchPage").then((m) => ({ default: m.AuditSearchPage })));
const PlatformSettingsPage = lazy(() => import("@/features/admin/pages/PlatformSettingsPage").then((m) => ({ default: m.PlatformSettingsPage })));
const AnalyticsDashboardPage = lazy(() => import("@/features/admin/pages/AnalyticsDashboardPage").then((m) => ({ default: m.AnalyticsDashboardPage })));
const ListingAnalyticsPage = lazy(() => import("@/features/admin/pages/ListingAnalyticsPage").then((m) => ({ default: m.ListingAnalyticsPage })));
const ListingReviewPage = lazy(() => import("@/features/admin/pages/ListingReviewPage").then((m) => ({ default: m.ListingReviewPage })));
const SearchPage = lazy(() => import("@/features/listings/pages/SearchPage").then((m) => ({ default: m.SearchPage })));
const MarketplaceHomePage = lazy(() => import("@/features/listings/pages/MarketplaceHomePage").then((m) => ({ default: m.MarketplaceHomePage })));
const ListingDetailPage = lazy(() => import("@/features/listings/pages/ListingDetailPage").then((m) => ({ default: m.ListingDetailPage })));
const MyListingsPage = lazy(() => import("@/features/listings/pages/MyListingsPage").then((m) => ({ default: m.MyListingsPage })));
const CreateListingPage = lazy(() => import("@/features/listings/pages/CreateListingPage").then((m) => ({ default: m.CreateListingPage })));
const EditListingPage = lazy(() => import("@/features/listings/pages/EditListingPage").then((m) => ({ default: m.EditListingPage })));
const LandlordListingDetailPage = lazy(() => import("@/features/listings/pages/LandlordListingDetailPage").then((m) => ({ default: m.LandlordListingDetailPage })));
const SavedListingsPage = lazy(() => import("@/features/listings/pages/SavedListingsPage").then((m) => ({ default: m.SavedListingsPage })));
const ApplicationsPage = lazy(() => import("@/features/applications/pages/ApplicationsPage").then((m) => ({ default: m.ApplicationsPage })));
const ApplicationDetailPage = lazy(() => import("@/features/applications/pages/ApplicationDetailPage").then((m) => ({ default: m.ApplicationDetailPage })));
const MyApplicationsPage = lazy(() => import("@/features/applications/pages/MyApplicationsPage").then((m) => ({ default: m.MyApplicationsPage })));
const HostApprovePage = lazy(() => import("@/features/applications/pages/HostApprovePage").then((m) => ({ default: m.HostApprovePage })));
const OwnerConsentPage = lazy(() => import("@/features/applications/pages/OwnerConsentPage").then((m) => ({ default: m.OwnerConsentPage })));
const OwnerConsentsPage = lazy(() => import("@/features/applications/pages/OwnerConsentsPage").then((m) => ({ default: m.OwnerConsentsPage })));
const InquiryThreadPage = lazy(() => import("@/features/inquiry/pages/InquiryThreadPage").then((m) => ({ default: m.InquiryThreadPage })));
const ListingInquiryPage = lazy(() => import("@/features/inquiry/pages/ListingInquiryPage").then((m) => ({ default: m.ListingInquiryPage })));
const HostInquiriesPage = lazy(() => import("@/features/inquiry/pages/HostInquiriesPage").then((m) => ({ default: m.HostInquiriesPage })));
const MyInquiriesPage = lazy(() => import("@/features/inquiry/pages/MyInquiriesPage").then((m) => ({ default: m.MyInquiriesPage })));
const TruthSurfaceConfirmationPage = lazy(() => import("@/features/truth-surface/pages/TruthSurfaceConfirmationPage").then((m) => ({ default: m.TruthSurfaceConfirmationPage })));
const BillingPage = lazy(() => import("@/features/activation-billing/pages/BillingPage").then((m) => ({ default: m.BillingPage })));
const HostBillingStatementPage = lazy(() => import("@/features/activation-billing/pages/HostBillingStatementPage").then((m) => ({ default: m.HostBillingStatementPage })));
const CheckoutPage = lazy(() => import("@/features/activation-billing/pages/CheckoutPage"));
const PaymentMethodPage = lazy(() => import("@/features/activation-billing/pages/PaymentMethodPage").then((m) => ({ default: m.PaymentMethodPage })));
const VerificationPage = lazy(() => import("@/features/verification/pages/VerificationPage").then((m) => ({ default: m.VerificationPage })));
const NotificationsPage = lazy(() => import("@/features/notifications/pages/NotificationsPage").then((m) => ({ default: m.NotificationsPage })));
const NotificationPreferencesPage = lazy(() => import("@/features/notifications/pages/NotificationPreferencesPage").then((m) => ({ default: m.NotificationPreferencesPage })));
const HostStripeOnboardingPage = lazy(() => import("@/features/host-onboarding/pages/HostStripeOnboardingPage"));
const ChannelsPage = lazy(() => import("@/features/channels/pages/ChannelsPage").then((m) => ({ default: m.ChannelsPage })));
const DealTruthSurfacePage = lazy(() => import("@/features/truth-surface/pages/DealTruthSurfacePage").then((m) => ({ default: m.DealTruthSurfacePage })));
const CreateTruthSurfacePage = lazy(() => import("@/features/truth-surface/pages/CreateTruthSurfacePage").then((m) => ({ default: m.CreateTruthSurfacePage })));
const MyDealsPage = lazy(() => import("@/features/deals/pages/MyDealsPage").then((m) => ({ default: m.MyDealsPage })));
const DealDetailPage = lazy(() => import("@/features/deals/pages/DealDetailPage").then((m) => ({ default: m.DealDetailPage })));
const DealCompliancePage = lazy(() => import("@/features/compliance/pages/DealCompliancePage").then((m) => ({ default: m.DealCompliancePage })));
const DealTrustLedgerPage = lazy(() => import("@/features/compliance/pages/TrustLedgerPage").then((m) => ({ default: m.DealTrustLedgerPage })));
const UserTrustLedgerPage = lazy(() => import("@/features/compliance/pages/TrustLedgerPage").then((m) => ({ default: m.UserTrustLedgerPage })));
const CaseListPage = lazy(() => import("@/features/arbitration/pages/CaseListPage").then((m) => ({ default: m.CaseListPage })));
const CaseDetailPage = lazy(() => import("@/features/arbitration/pages/CaseDetailPage").then((m) => ({ default: m.CaseDetailPage })));
const PartnerDashboardPage = lazy(() => import("@/features/partners/pages/PartnerDashboardPage").then((m) => ({ default: m.PartnerDashboardPage })));
const PartnerOnboardingPage = lazy(() => import("@/features/partners/pages/PartnerOnboardingPage").then((m) => ({ default: m.PartnerOnboardingPage })));
const PartnerMembersPage = lazy(() => import("@/features/partners/pages/PartnerMembersPage").then((m) => ({ default: m.PartnerMembersPage })));
const PartnerReferralsPage = lazy(() => import("@/features/partners/pages/PartnerReferralsPage").then((m) => ({ default: m.PartnerReferralsPage })));
const PartnerReservationsPage = lazy(() => import("@/features/partners/pages/PartnerReservationsPage").then((m) => ({ default: m.PartnerReservationsPage })));
const PartnerGuestsPage = lazy(() => import("@/features/partners/pages/PartnerGuestsPage").then((m) => ({ default: m.PartnerGuestsPage })));
const PartnerInquiriesPage = lazy(() => import("@/features/inquiry/pages/PartnerInquiriesPage").then((m) => ({ default: m.PartnerInquiriesPage })));
const PartnerEndorsementsPage = lazy(() => import("@/features/partners/pages/PartnerEndorsementsPage").then((m) => ({ default: m.PartnerEndorsementsPage })));
const PartnerLayoutGuard = lazy(() => import("@/features/partners/components/PartnerLayoutGuard").then((m) => ({ default: m.PartnerLayoutGuard })));
const PartnerVerificationPage = lazy(() => import("@/features/admin/pages/PartnerVerificationPage").then((m) => ({ default: m.PartnerVerificationPage })));

/** Legacy password-reset emails pointed at /reset-password; preserve query params. */
function LegacyResetPasswordRedirect() {
  const { search } = useLocation();
  return <Navigate to={`/auth/reset-password${search}`} replace />;
}

export const router = createBrowserRouter([
  {
    path: "/",
    element: <Navigate to="/listings" replace />,
    errorElement: <RouteErrorBoundary />,
  },

  // Public marketplace routes. Gated by RequireLaunchAccess so that while
  // pre-launch is on, browsing is closed to everyone except operational staff —
  // non-staff visitors are bounced to the join flow (the only allowed pages).
  {
    element: <RequireLaunchAccess />,
    errorElement: <RouteErrorBoundary />,
    children: [
      {
        element: <MarketplaceLayout />,
        errorElement: <RouteErrorBoundary />,
        children: [
          { path: "/listings", element: <LazyPage><MarketplaceHomePage /></LazyPage> },
          { path: "/listings/search", element: <LazyPage><SearchPage /></LazyPage> },
          { path: "/listings/:id", element: <LazyPage><ListingDetailPage /></LazyPage> },
        ],
      },
    ],
  },

  // Founding-partner / pre-launch join flow (full-screen, own layout).
  {
    path: "/join",
    element: <LazyPage><JoinPage /></LazyPage>,
    errorElement: <RouteErrorBoundary />,
  },
  {
    path: "/how-it-works",
    element: <LazyPage><HowItWorksPage /></LazyPage>,
    errorElement: <RouteErrorBoundary />,
  },

  // Phase 16.10 — anonymous one-tap host approval landing page
  {
    path: "/host/approve",
    element: <LazyPage><HostApprovePage /></LazyPage>,
    errorElement: <RouteErrorBoundary />,
  },
  {
    path: "/owner/consent",
    element: <LazyPage><OwnerConsentPage /></LazyPage>,
    errorElement: <RouteErrorBoundary />,
  },

  // Legacy forgot-password emails used /reset-password (missing /auth prefix)
  {
    path: "/reset-password",
    element: <LegacyResetPasswordRedirect />,
    errorElement: <RouteErrorBoundary />,
  },

  // Auth routes
  {
    path: "/auth",
    element: <AuthLayout />,
    errorElement: <RouteErrorBoundary />,
    children: [
      { path: "login", element: <LazyPage><LoginPage /></LazyPage> },
      { path: "register", element: <Navigate to="/join" replace /> },
      { path: "verify-email", element: <LazyPage><VerifyEmailPage /></LazyPage> },
      { path: "forgot-password", element: <LazyPage><ForgotPasswordPage /></LazyPage> },
      { path: "reset-password", element: <LazyPage><ResetPasswordPage /></LazyPage> },
    ],
  },

  // Authenticated app routes
  {
    element: <RequireAuth />,
    errorElement: <RouteErrorBoundary />,
    children: [
      {
        // Pre-launch: staff + founding hosts enter the app; hosts are then
        // limited to listings, Hostaway, profile, verification, and payout by RequirePreLaunchHostSurface.
        element: <RequireLaunchAccess surface="app" />,
        children: [
      {
        path: "/app",
        element: <AppShell />,
        errorElement: <RouteErrorBoundary />,
        children: [
          {
            element: <RequirePreLaunchHostSurface />,
            children: [
          { index: true, element: <LazyPage><DashboardPage /></LazyPage> },
          { path: "profile", element: <LazyPage><ProfilePage /></LazyPage> },
          { path: "users/:userId", element: <LazyPage><PublicProfilePage /></LazyPage> },
          { path: "verification", element: <LazyPage><VerificationPage /></LazyPage> },
          { path: "notifications", element: <LazyPage><NotificationsPage /></LazyPage> },
          { path: "notification-preferences", element: <LazyPage><NotificationPreferencesPage /></LazyPage> },
          { path: "saved", element: <LazyPage><SavedListingsPage /></LazyPage> },
          { path: "applications/:id", element: <LazyPage><ApplicationDetailPage /></LazyPage> },
          { path: "my-applications", element: <LazyPage><MyApplicationsPage /></LazyPage> },
          { path: "owner-consents", element: <LazyPage><OwnerConsentsPage /></LazyPage> },
          { path: "my-inquiries", element: <LazyPage><MyInquiriesPage /></LazyPage> },
          { path: "deals", element: <LazyPage><MyDealsPage /></LazyPage> },
          { path: "reservations", element: <Navigate to="/app/deals" replace /> },
          { path: "deals/:dealId", element: <LazyPage><DealDetailPage /></LazyPage> },
          { path: "deals/:dealId/inquiry", element: <LazyPage><InquiryThreadPage /></LazyPage> },
          { path: "inquiry/:sessionId", element: <LazyPage><ListingInquiryPage /></LazyPage> },
          { path: "deals/:dealId/truth-surface", element: <LazyPage><DealTruthSurfacePage /></LazyPage> },
          { path: "deals/:dealId/create-truth-surface", element: <LazyPage><CreateTruthSurfacePage /></LazyPage> },
          { path: "truth-surface/:snapshotId", element: <LazyPage><TruthSurfaceConfirmationPage /></LazyPage> },
          { path: "deals/:dealId/billing", element: <LazyPage><BillingPage /></LazyPage> },
          { path: "deals/:dealId/checkout", element: <LazyPage><CheckoutPage /></LazyPage> },
          { path: "deals/:dealId/payment-method", element: <LazyPage><PaymentMethodPage /></LazyPage> },
          { path: "deals/:dealId/compliance", element: <LazyPage><DealCompliancePage /></LazyPage> },
          { path: "deals/:dealId/trust-ledger", element: <LazyPage><DealTrustLedgerPage /></LazyPage> },
          { path: "trust-ledger", element: <LazyPage><UserTrustLedgerPage /></LazyPage> },
          {
            path: "arbitration",
            element: <RequireArbitrationAccess />,
            children: [
              { index: true, element: <LazyPage><CaseListPage /></LazyPage> },
              { path: ":caseId", element: <LazyPage><CaseDetailPage /></LazyPage> },
            ],
          },
          {
            path: "lease-agreements",
            element: <RequireRole allowed={[roles.arbitrator, roles.platformAdmin]} />,
            children: [
              { index: true, element: <LazyPage><LeaseAgreementTemplatesPage /></LazyPage> },
            ],
          },
          {
            element: <RequireMember />,
            children: [
              { path: "listings", element: <LazyPage><MyListingsPage /></LazyPage> },
              { path: "listings/new", element: <LazyPage><CreateListingPage /></LazyPage> },
              { path: "listings/:id", element: <LazyPage><LandlordListingDetailPage /></LazyPage> },
              { path: "listings/:id/edit", element: <LazyPage><EditListingPage /></LazyPage> },
              { path: "applications", element: <LazyPage><ApplicationsPage /></LazyPage> },
              { path: "inquiries", element: <LazyPage><HostInquiriesPage /></LazyPage> },
              { path: "channels", element: <LazyPage><ChannelsPage /></LazyPage> },
              { path: "payout-setup", element: <LazyPage><HostStripeOnboardingPage /></LazyPage> },
              { path: "billing", element: <LazyPage><HostBillingStatementPage /></LazyPage> },
              { path: "stripe-onboarding", element: <Navigate to="/app/payout-setup" replace /> },
            ],
          },
          {
            path: "partner",
            element: <RequireRole allowed={[roles.institutionPartner, roles.platformAdmin]} />,
            children: [
              {
                path: "onboarding",
                element: (
                  <LazyPage>
                    <PartnerLayoutGuard requireMembership={false} />
                  </LazyPage>
                ),
                children: [{ index: true, element: <LazyPage><PartnerOnboardingPage /></LazyPage> }],
              },
              {
                element: (
                  <LazyPage>
                    <PartnerLayoutGuard />
                  </LazyPage>
                ),
                children: [
                  { index: true, element: <LazyPage><PartnerDashboardPage /></LazyPage> },
                  { path: "members", element: <LazyPage><PartnerMembersPage /></LazyPage> },
                  { path: "referrals", element: <LazyPage><PartnerReferralsPage /></LazyPage> },
                  { path: "reservations", element: <LazyPage><PartnerReservationsPage /></LazyPage> },
                  { path: "guests", element: <LazyPage><PartnerGuestsPage /></LazyPage> },
                  { path: "endorsements", element: <LazyPage><PartnerEndorsementsPage /></LazyPage> },
                  { path: "inquiries", element: <LazyPage><PartnerInquiriesPage /></LazyPage> },
                ],
              },
            ],
          },
          {
            path: "admin",
            element: <RequireRole allowed={[roles.platformAdmin]} />,
            children: [
              { path: "users", element: <LazyPage><UsersPage /></LazyPage> },
              { path: "listing-review", element: <LazyPage><ListingReviewPage /></LazyPage> },
              { path: "partners", element: <LazyPage><PartnerVerificationPage /></LazyPage> },
              { path: "definitions", element: <LazyPage><DefinitionsPage /></LazyPage> },
              { path: "insurance-queue", element: <LazyPage><InsuranceUnknownQueuePage /></LazyPage> },
              { path: "fraud-flags", element: <LazyPage><FraudFlagsPage /></LazyPage> },
              { path: "arbitration-backlog", element: <LazyPage><ArbitrationBacklogPage /></LazyPage> },
              { path: "evidence-review", element: <LazyPage><EvidenceReviewPage /></LazyPage> },
              { path: "manual-verification", element: <LazyPage><ManualVerificationPage /></LazyPage> },
              { path: "compliance-violations", element: <LazyPage><ComplianceViolationsPage /></LazyPage> },
              { path: "restrictions", element: <LazyPage><UserRestrictionsPage /></LazyPage> },
              { path: "lease-agreements", element: <LazyPage><LeaseAgreementTemplatesPage /></LazyPage> },
              { path: "dual-control", element: <LazyPage><DualControlApprovalsPage /></LazyPage> },
              { path: "settings", element: <LazyPage><PlatformSettingsPage /></LazyPage> },
              { path: "blog", element: <LazyPage><BlogPostsPage /></LazyPage> },
              { path: "blog/new", element: <LazyPage><BlogPostEditorPage /></LazyPage> },
              { path: "blog/:postId/edit", element: <LazyPage><BlogPostEditorPage /></LazyPage> },
              { path: "seo", element: <LazyPage><SeoPage /></LazyPage> },
              { path: "audit", element: <LazyPage><AuditSearchPage /></LazyPage> },
              { path: "analytics", element: <LazyPage><AnalyticsDashboardPage /></LazyPage> },
              { path: "listing-analytics", element: <LazyPage><ListingAnalyticsPage /></LazyPage> },
            ],
          },
            ],
          },
        ],
      },
        ],
      },
    ],
  },

  // Catch-all
  {
    path: "*",
    element: <Navigate to="/listings" replace />,
  },
]);
