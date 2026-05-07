import { useCallback, useEffect, useState } from "react";
import { partnerApi } from "@/features/partners/services/partnerApi";
import type { MyPartnerMembershipDto } from "@/api/types";

type State = {
  isLoading: boolean;
  membership: MyPartnerMembershipDto | null;
  error: unknown;
};

export function usePartnerMembership() {
  const [state, setState] = useState<State>({ isLoading: true, membership: null, error: null });

  const refresh = useCallback(async () => {
    setState((s) => ({ ...s, isLoading: true, error: null }));
    try {
      const membership = await partnerApi.getMyMembership();
      setState({ isLoading: false, membership, error: null });
    } catch (err) {
      setState({ isLoading: false, membership: null, error: err });
    }
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  return { ...state, refresh };
}
