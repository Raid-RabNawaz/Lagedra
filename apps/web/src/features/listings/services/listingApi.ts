import { endpoints } from "@/api/endpoints";
import { http } from "@/api/http";
import type {
  AddListingPhotoRequest,
  AvailabilityBlockDto,
  BlockDatesRequest,
  CreateAmenityDefinitionRequest,
  CreateConsiderationDefinitionRequest,
  CreateListingRequest,
  CreateSafetyDeviceDefinitionRequest,
  ListingDetailsDto,
  ListingPhotoDto,
  ListingPriceHistoryDto,
  ListingSummaryDto,
  LockPreciseAddressRequest,
  SavedListingCollectionDto,
  SearchListingsParams,
  SearchListingsResultDto,
  SetApproxLocationRequest,
  UpdateAmenityDefinitionRequest,
  UpdateConsiderationDefinitionRequest,
  UpdateListingRequest,
  UpdateSafetyDeviceDefinitionRequest,
  AmenityDefinitionDto,
  SafetyDeviceDefinitionDto,
  ConsiderationDefinitionDto,
} from "@/api/types";

export const listingApi = {
  async search(params: SearchListingsParams = {}): Promise<SearchListingsResultDto> {
    const cleaned = Object.fromEntries(
      Object.entries(params).filter(([, v]) => {
        if (v === undefined || v === null || v === "") return false;
        if (Array.isArray(v)) return v.length > 0;
        return true;
      }),
    );
    const response = await http.get<SearchListingsResultDto>(endpoints.listings.search, {
      params: cleaned,
      paramsSerializer: (p) => {
        const search = new URLSearchParams();
        for (const [key, value] of Object.entries(p)) {
          if (value === undefined || value === null) continue;
          if (Array.isArray(value)) {
            for (const item of value) {
              search.append(key, String(item));
            }
          } else {
            search.append(key, String(value));
          }
        }
        return search.toString();
      },
    });
    return response.data;
  },

  async getMine(): Promise<ListingSummaryDto[]> {
    const response = await http.get<ListingSummaryDto[]>(endpoints.listings.mine);
    return response.data;
  },

  async getDetail(id: string): Promise<ListingDetailsDto> {
    const response = await http.get<ListingDetailsDto>(endpoints.listings.detail(id));
    return response.data;
  },

  async getSimilar(id: string): Promise<ListingSummaryDto[]> {
    const response = await http.get<ListingSummaryDto[]>(endpoints.listings.similar(id));
    return response.data;
  },

  async create(payload: CreateListingRequest): Promise<ListingDetailsDto> {
    const response = await http.post<ListingDetailsDto>(endpoints.listings.search, payload);
    return response.data;
  },

  async update(id: string, payload: UpdateListingRequest): Promise<ListingDetailsDto> {
    const response = await http.put<ListingDetailsDto>(endpoints.listings.detail(id), payload);
    return response.data;
  },

  async publish(id: string): Promise<ListingDetailsDto> {
    const response = await http.post<ListingDetailsDto>(endpoints.listings.publish(id));
    return response.data;
  },

  async close(id: string): Promise<ListingDetailsDto> {
    const response = await http.post<ListingDetailsDto>(endpoints.listings.close(id));
    return response.data;
  },

  async setApproxLocation(listingId: string, payload: SetApproxLocationRequest): Promise<ListingDetailsDto> {
    const response = await http.post<ListingDetailsDto>(
      endpoints.listings.approxLocation(listingId),
      payload,
    );
    return response.data;
  },

  async lockAddress(listingId: string, payload: LockPreciseAddressRequest): Promise<ListingDetailsDto> {
    const response = await http.post<ListingDetailsDto>(
      endpoints.listings.lockAddress(listingId),
      payload,
    );
    return response.data;
  },

  async addPhoto(listingId: string, payload: AddListingPhotoRequest): Promise<ListingPhotoDto> {
    const response = await http.post<ListingPhotoDto>(endpoints.listings.addPhoto(listingId), payload);
    return response.data;
  },

  async removePhoto(listingId: string, photoId: string): Promise<void> {
    await http.delete(endpoints.listings.photo(listingId, photoId));
  },

  async setCoverPhoto(listingId: string, photoId: string): Promise<void> {
    await http.put(endpoints.listings.coverPhoto(listingId, photoId));
  },

  async reorderPhotos(listingId: string, photoIdsInOrder: string[]): Promise<void> {
    await http.put(endpoints.listings.reorderPhotos(listingId), { photoIdsInOrder });
  },

  // ── Availability calendar ──────────────────────────────────

  async getAvailability(listingId: string): Promise<AvailabilityBlockDto[]> {
    const response = await http.get<AvailabilityBlockDto[]>(
      endpoints.listings.availability(listingId),
    );
    return response.data;
  },

  async blockDates(listingId: string, payload: BlockDatesRequest): Promise<AvailabilityBlockDto> {
    const response = await http.post<AvailabilityBlockDto>(
      endpoints.listings.blockDates(listingId),
      payload,
    );
    return response.data;
  },

  async unblockDates(listingId: string, blockId: string): Promise<void> {
    await http.delete(endpoints.listings.unblockDates(listingId, blockId));
  },

  // ── Price history ─────────────────────────────────────────

  async getPriceHistory(listingId: string): Promise<ListingPriceHistoryDto[]> {
    const response = await http.get<ListingPriceHistoryDto[]>(
      endpoints.listings.priceHistory(listingId),
    );
    return response.data;
  },

  // ── Definitions ───────────────────────────────────────────

  async getAmenityDefinitions(): Promise<AmenityDefinitionDto[]> {
    const response = await http.get<AmenityDefinitionDto[]>(endpoints.definitions.amenities);
    return response.data;
  },

  async getSafetyDeviceDefinitions(): Promise<SafetyDeviceDefinitionDto[]> {
    const response = await http.get<SafetyDeviceDefinitionDto[]>(endpoints.definitions.safetyDevices);
    return response.data;
  },

  async getConsiderationDefinitions(): Promise<ConsiderationDefinitionDto[]> {
    const response = await http.get<ConsiderationDefinitionDto[]>(endpoints.definitions.considerations);
    return response.data;
  },

  async getSavedListings(page = 1, pageSize = 20): Promise<ListingSummaryDto[]> {
    const response = await http.get<ListingSummaryDto[]>(endpoints.savedListings.list, {
      params: { page, pageSize },
    });
    return response.data;
  },

  async saveListing(listingId: string): Promise<void> {
    await http.post(endpoints.savedListings.save(listingId));
  },

  async unsaveListing(listingId: string): Promise<void> {
    await http.delete(endpoints.savedListings.save(listingId));
  },

  async getCollections(): Promise<SavedListingCollectionDto[]> {
    const response = await http.get<SavedListingCollectionDto[]>(endpoints.savedListings.collections);
    return response.data;
  },

  async getCollectionListings(collectionId: string, page = 1, pageSize = 20): Promise<ListingSummaryDto[]> {
    const response = await http.get<ListingSummaryDto[]>(
      endpoints.savedListings.collection(collectionId),
      { params: { page, pageSize } },
    );
    return response.data;
  },

  async createCollection(name: string): Promise<SavedListingCollectionDto> {
    const response = await http.post<SavedListingCollectionDto>(
      endpoints.savedListings.collections,
      { name },
    );
    return response.data;
  },

  async addToCollection(listingId: string, collectionId: string): Promise<void> {
    await http.post(endpoints.savedListings.addToCollection(listingId, collectionId));
  },

  async removeFromCollection(listingId: string): Promise<void> {
    await http.delete(endpoints.savedListings.removeFromCollection(listingId));
  },

  // ── Admin listing definitions ─────────────────────────────

  async adminGetAmenities(): Promise<AmenityDefinitionDto[]> {
    const response = await http.get<AmenityDefinitionDto[]>(endpoints.adminDefinitions.amenities);
    return response.data;
  },

  async adminCreateAmenity(payload: CreateAmenityDefinitionRequest): Promise<AmenityDefinitionDto> {
    const response = await http.post<AmenityDefinitionDto>(
      endpoints.adminDefinitions.amenities,
      payload,
    );
    return response.data;
  },

  async adminUpdateAmenity(id: string, payload: UpdateAmenityDefinitionRequest): Promise<AmenityDefinitionDto> {
    const response = await http.put<AmenityDefinitionDto>(
      endpoints.adminDefinitions.amenity(id),
      payload,
    );
    return response.data;
  },

  async adminGetSafetyDevices(): Promise<SafetyDeviceDefinitionDto[]> {
    const response = await http.get<SafetyDeviceDefinitionDto[]>(endpoints.adminDefinitions.safetyDevices);
    return response.data;
  },

  async adminCreateSafetyDevice(payload: CreateSafetyDeviceDefinitionRequest): Promise<SafetyDeviceDefinitionDto> {
    const response = await http.post<SafetyDeviceDefinitionDto>(
      endpoints.adminDefinitions.safetyDevices,
      payload,
    );
    return response.data;
  },

  async adminUpdateSafetyDevice(id: string, payload: UpdateSafetyDeviceDefinitionRequest): Promise<SafetyDeviceDefinitionDto> {
    const response = await http.put<SafetyDeviceDefinitionDto>(
      endpoints.adminDefinitions.safetyDevice(id),
      payload,
    );
    return response.data;
  },

  async adminGetConsiderations(): Promise<ConsiderationDefinitionDto[]> {
    const response = await http.get<ConsiderationDefinitionDto[]>(endpoints.adminDefinitions.considerations);
    return response.data;
  },

  async adminCreateConsideration(payload: CreateConsiderationDefinitionRequest): Promise<ConsiderationDefinitionDto> {
    const response = await http.post<ConsiderationDefinitionDto>(
      endpoints.adminDefinitions.considerations,
      payload,
    );
    return response.data;
  },

  async adminUpdateConsideration(id: string, payload: UpdateConsiderationDefinitionRequest): Promise<ConsiderationDefinitionDto> {
    const response = await http.put<ConsiderationDefinitionDto>(
      endpoints.adminDefinitions.consideration(id),
      payload,
    );
    return response.data;
  },
};
