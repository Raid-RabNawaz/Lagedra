import type { AxiosError } from "axios";
import { endpoints } from "@/api/endpoints";
import { http } from "@/api/http";
import type {
  TruthSurfaceDto,
  SnapshotProofDto,
  CreateSnapshotRequest,
  ConfirmSnapshotRequest,
  ReconfirmSnapshotRequest,
} from "@/api/types";

export const truthSurfaceApi = {
  async getSnapshot(snapshotId: string): Promise<TruthSurfaceDto> {
    const response = await http.get<TruthSurfaceDto>(
      endpoints.truthSurface.snapshot(snapshotId),
    );
    return response.data;
  },

  /**
   * Returns the active Truth Surface for a deal, or `null` if none has been
   * created yet. 404 is the API's way of saying "no snapshot exists" which
   * is a normal pre-confirmation state — not an error — so we collapse it
   * to a null result here. Callers can use `data === null` to render
   * "create" affordances and `data` to render "review" affordances without
   * juggling react-query's `isError` state.
   */
  async getSnapshotByDealId(dealId: string): Promise<TruthSurfaceDto | null> {
    try {
      const response = await http.get<TruthSurfaceDto>(
        endpoints.truthSurface.byDeal(dealId),
      );
      return response.data;
    } catch (err) {
      const status = (err as AxiosError)?.response?.status;
      if (status === 404) {
        return null;
      }
      throw err;
    }
  },

  async create(payload: CreateSnapshotRequest): Promise<TruthSurfaceDto> {
    const response = await http.post<TruthSurfaceDto>(
      endpoints.truthSurface.create,
      payload,
    );
    return response.data;
  },

  async createFromDeal(dealId: string): Promise<TruthSurfaceDto> {
    const response = await http.post<TruthSurfaceDto>(
      endpoints.truthSurface.fromDeal(dealId),
    );
    return response.data;
  },

  async confirm(
    snapshotId: string,
    payload: ConfirmSnapshotRequest,
  ): Promise<TruthSurfaceDto> {
    const response = await http.post<TruthSurfaceDto>(
      endpoints.truthSurface.confirm(snapshotId),
      payload,
    );
    return response.data;
  },

  async reconfirm(
    snapshotId: string,
    payload: ReconfirmSnapshotRequest,
  ): Promise<TruthSurfaceDto> {
    const response = await http.post<TruthSurfaceDto>(
      endpoints.truthSurface.reconfirm(snapshotId),
      payload,
    );
    return response.data;
  },

  async verify(snapshotId: string): Promise<SnapshotProofDto> {
    const response = await http.get<SnapshotProofDto>(
      endpoints.truthSurface.verify(snapshotId),
    );
    return response.data;
  },

  async downloadReceipt(snapshotId: string): Promise<{ blob: Blob; filename: string }> {
    const response = await http.get(endpoints.truthSurface.receipt(snapshotId), {
      responseType: "blob",
    });
    const disposition = response.headers["content-disposition"] as string | undefined;
    const fallback = `truth-surface-${snapshotId}.json`;
    const match = disposition?.match(/filename\*?=(?:UTF-8'')?"?([^";]+)"?/i);
    const filename = match?.[1] ? decodeURIComponent(match[1]) : fallback;
    return { blob: response.data as Blob, filename };
  },

  /**
   * Downloads the filled lease agreement PDF for a confirmed deal.
   * Generates on demand when the async post-seal job has not stored one yet.
   * Throws with a readable message when generation fails (missing profile
   * fields, unpublished template, etc.).
   */
  async downloadLeasePdf(dealId: string): Promise<{ blob: Blob; filename: string } | null> {
    try {
      const response = await http.get(endpoints.leaseAgreements.dealPdf(dealId), {
        responseType: "blob",
      });
      const disposition = response.headers["content-disposition"] as string | undefined;
      const fallback = `lease-agreement-${dealId}.pdf`;
      const match = disposition?.match(/filename\*?=(?:UTF-8'')?"?([^";]+)"?/i);
      const filename = match?.[1] ? decodeURIComponent(match[1]) : fallback;
      return { blob: response.data as Blob, filename };
    } catch (err) {
      const axiosErr = err as AxiosError;
      const status = axiosErr?.response?.status;
      if (status === 404) {
        return null;
      }

      const data = axiosErr?.response?.data;
      if (data instanceof Blob) {
        const text = await data.text();
        let message = text || "Lease PDF could not be generated.";
        try {
          const json = JSON.parse(text) as {
            detail?: string;
            title?: string;
            error?: string;
          };
          message =
            json.detail || json.title || json.error || message;
        } catch {
          // Keep raw text when the body isn't JSON ProblemDetails.
        }
        throw new Error(message);
      }

      throw err;
    }
  },
};
