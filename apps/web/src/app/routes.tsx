import { lazy, Suspense } from "react";
import { Navigate, createBrowserRouter } from "react-router-dom";
import { RequireAuth } from "@/app/auth/RequireAuth";
import { RequireLandlord } from "@/app/auth/RequireLandlord";
import { RequireRole } from "@/app/auth/RequireRole";
import { roles } from "@/app/auth/roles";
import { AuthLayout } from "@/features/auth/pages/AuthLayout";
import { AppShell } from "@/app/layout/AppShell";
import { MarketplaceLayout } from "@/app/layout/MarketplaceLayout";
import { Loader } from "@/components/shared/Loader";

const LoginPage = lazy(() => import("@/features/auth/pages/LoginPage").then((m) => ({ default: m.LoginPage })));
const RegisterPage = lazy(() => import("@/features/auth/pages/RegisterPage").then((m) => ({ default: m.RegisterPage })));
const VerifyEmailPage = lazy(() => import("@/features/auth/pages/VerifyEmailPage").then((m) => ({ default: m.VerifyEmailPage })));
const ForgotPasswordPage = lazy(() => import("@/features/auth/pages/ForgotPasswordPage").then((m) => ({ default: m.ForgotPasswordPage })));
const ResetPasswordPage = lazy(() => import("@/features/auth/pages/ResetPasswordPage").then((m) => ({ default: m.ResetPasswordPage })));
const DashboardPage = lazy(() => import("@/features/auth/pages/DashboardPage").then((m) => ({ default: m.DashboardPage })));
const ProfilePage = lazy(() => import("@/features/auth/pages/ProfilePage").then((m) => ({ default: m.ProfilePage })));
const UsersPage = lazy(() => import("@/features/admin/pages/UsersPage").then((m) => ({ default: m.UsersPage })));
const DefinitionsPage = lazy(() => import("@/features/admin/pages/DefinitionsPage").then((m) => ({ default: m.DefinitionsPage })));
const SearchPage = lazy(() => import("@/features/listings/pages/SearchPage").then((m) => ({ default: m.SearchPage })));
const ListingDetailPage = lazy(() => import("@/features/listings/pages/ListingDetailPage").then((m) => ({ default: m.ListingDetailPage })));
const MyListingsPage = lazy(() => import("@/features/listings/pages/MyListingsPage").then((m) => ({ default: m.MyListingsPage })));
const CreateListingPage = lazy(() => import("@/features/listings/pages/CreateListingPage").then((m) => ({ default: m.CreateListingPage })));
const EditListingPage = lazy(() => import("@/features/listings/pages/EditListingPage").then((m) => ({ default: m.EditListingPage })));
const SavedListingsPage = lazy(() => import("@/features/listings/pages/SavedListingsPage").then((m) => ({ default: m.SavedListingsPage })));
const ApplicationsPage = lazy(() => import("@/features/applications/pages/ApplicationsPage").then((m) => ({ default: m.ApplicationsPage })));
const ApplicationDetailPage = lazy(() => import("@/features/applications/pages/ApplicationDetailPage").then((m) => ({ default: m.ApplicationDetailPage })));
const MyApplicationsPage = lazy(() => import("@/features/applications/pages/MyApplicationsPage").then((m) => ({ default: m.MyApplicationsPage })));

function LazyPage({ children }: { children: React.ReactNode }) {
  return <Suspense fallback={<Loader fullPage label="Loading..." />}>{children}</Suspense>;
}

export const router = createBrowserRouter([
  {
    path: "/",
    element: <Navigate to="/listings" replace />,
  },

  // Public marketplace routes
  {
    element: <MarketplaceLayout />,
    children: [
      { path: "/listings", element: <LazyPage><SearchPage /></LazyPage> },
      { path: "/listings/:id", element: <LazyPage><ListingDetailPage /></LazyPage> },
    ],
  },

  // Auth routes
  {
    path: "/auth",
    element: <AuthLayout />,
    children: [
      { path: "login", element: <LazyPage><LoginPage /></LazyPage> },
      { path: "register", element: <LazyPage><RegisterPage /></LazyPage> },
      { path: "verify-email", element: <LazyPage><VerifyEmailPage /></LazyPage> },
      { path: "forgot-password", element: <LazyPage><ForgotPasswordPage /></LazyPage> },
      { path: "reset-password", element: <LazyPage><ResetPasswordPage /></LazyPage> },
    ],
  },

  // Authenticated app routes
  {
    element: <RequireAuth />,
    children: [
      {
        path: "/app",
        element: <AppShell />,
        children: [
          { index: true, element: <LazyPage><DashboardPage /></LazyPage> },
          { path: "profile", element: <LazyPage><ProfilePage /></LazyPage> },
          { path: "saved", element: <LazyPage><SavedListingsPage /></LazyPage> },
          { path: "applications/:id", element: <LazyPage><ApplicationDetailPage /></LazyPage> },
          { path: "my-applications", element: <LazyPage><MyApplicationsPage /></LazyPage> },
          {
            element: <RequireLandlord />,
            children: [
              { path: "listings", element: <LazyPage><MyListingsPage /></LazyPage> },
              { path: "listings/new", element: <LazyPage><CreateListingPage /></LazyPage> },
              { path: "listings/:id/edit", element: <LazyPage><EditListingPage /></LazyPage> },
              { path: "applications", element: <LazyPage><ApplicationsPage /></LazyPage> },
            ],
          },
          {
            path: "admin",
            element: <RequireRole allowed={[roles.platformAdmin]} />,
            children: [
              { path: "users", element: <LazyPage><UsersPage /></LazyPage> },
              { path: "definitions", element: <LazyPage><DefinitionsPage /></LazyPage> },
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
