import { endpoints } from "@/api/endpoints";
import { http } from "@/api/http";
import type {
  ManifestDto,
  ManifestType,
  ManifestUploadDto,
  UploadUrlDto,
  ScanResultDto,
  DownloadUrlDto,
} from "@/api/types";

export const evidenceApi = {
  async createManifest(dealId: string, manifestType: ManifestType): Promise<ManifestDto> {
    const response = await http.post<ManifestDto>(endpoints.evidence.createManifest, {
      dealId,
      manifestType,
    });
    return response.data;
  },

  async sealManifest(manifestId: string): Promise<ManifestDto> {
    const response = await http.post<ManifestDto>(
      endpoints.evidence.sealManifest(manifestId),
    );
    return response.data;
  },

  async getManifest(manifestId: string): Promise<ManifestDto> {
    const response = await http.get<ManifestDto>(
      endpoints.evidence.getManifest(manifestId),
    );
    return response.data;
  },

  async requestUploadUrl(
    manifestId: string,
    fileName: string,
    mimeType: string,
  ): Promise<UploadUrlDto> {
    const response = await http.post<UploadUrlDto>(
      endpoints.evidence.requestUploadUrl,
      { manifestId, fileName, mimeType },
    );
    return response.data;
  },

  async completeUpload(
    uploadId: string,
    manifestId: string,
    originalFileName: string,
    storageKey: string,
    mimeType: string,
    fileHashHex: string,
  ): Promise<void> {
    await http.post(endpoints.evidence.completeUpload(uploadId), {
      manifestId,
      originalFileName,
      storageKey,
      mimeType,
      fileHashHex,
    });
  },

  async directUpload(manifestId: string, file: File): Promise<ManifestUploadDto> {
    const form = new FormData();
    form.append("manifestId", manifestId);
    form.append("file", file, file.name);
    const response = await http.post<ManifestUploadDto>(
      endpoints.evidence.directUpload,
      form,
      { timeout: 120_000 },
    );
    return response.data;
  },

  async getScanStatus(uploadId: string): Promise<ScanResultDto> {
    const response = await http.get<ScanResultDto>(
      endpoints.evidence.scanStatus(uploadId),
    );
    return response.data;
  },

  async getDownloadUrl(uploadId: string): Promise<DownloadUrlDto> {
    const response = await http.get<DownloadUrlDto>(
      endpoints.evidence.downloadUrl(uploadId),
    );
    return response.data;
  },
};
