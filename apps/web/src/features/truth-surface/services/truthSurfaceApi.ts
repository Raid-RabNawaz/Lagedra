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

  async getSnapshotByDealId(dealId: string): Promise<TruthSurfaceDto> {
    const response = await http.get<TruthSurfaceDto>(
      endpoints.truthSurface.byDeal(dealId),
    );
    return response.data;
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
};
