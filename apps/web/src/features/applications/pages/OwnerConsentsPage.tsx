import { ShieldCheck } from "lucide-react";
import { useOwnerPendingApplications } from "@/features/applications/hooks/useApplications";
import { ApplicationCard } from "@/features/applications/components/ApplicationCard";
import { PageHeader } from "@/components/shared/PageHeader";
import { ListRowsSkeleton } from "@/components/shared/ListSkeleton";
import { EmptyState } from "@/components/shared/EmptyState";
import { ErrorState } from "@/components/shared/ErrorState";

export const OwnerConsentsPage = () => {
  const { data, isLoading, isError, refetch } = useOwnerPendingApplications();
  const applications = data ?? [];

  return (
    <div className="mx-auto max-w-5xl space-y-6">
      <PageHeader
        icon={ShieldCheck}
        title="Owner consent"
        description="Stays over 30 days on homes you own, listed by a property manager, need your consent before they can be accepted."
      />

      {isLoading && <ListRowsSkeleton rows={3} />}

      {isError && (
        <ErrorState
          title="Couldn’t load owner consent requests"
          onRetry={() => void refetch()}
        />
      )}

      {!isLoading && !isError && applications.length === 0 && (
        <EmptyState
          title="No pending owner consents"
          description="When a guest applies to a home you own that a property manager listed, it will appear here."
        />
      )}

      {!isLoading && !isError && applications.length > 0 && (
        <div className="space-y-3">
          {applications.map((application) => (
            <ApplicationCard
              key={application.applicationId}
              application={application}
              perspective="owner"
              showListingPreview
            />
          ))}
        </div>
      )}
    </div>
  );
};
