import { http } from "@/api/http";
import { endpoints } from "@/api/endpoints";
import type {
  StayReviewWindowDto,
  StayReviewDto,
  SubmitStayReviewRequest,
  UserReputationDto,
  PartnerServiceReviewDto,
  PartnerReputationDto,
  SubmitPartnerServiceReviewRequest,
} from "@/api/types";

export const reviewsApi = {
  getDealReviews(dealId: string): Promise<StayReviewWindowDto> {
    return http
      .get<StayReviewWindowDto>(endpoints.reviews.deal(dealId))
      .then((r) => r.data);
  },

  submitStayReview(
    dealId: string,
    payload: SubmitStayReviewRequest,
  ): Promise<StayReviewDto> {
    return http
      .post<StayReviewDto>(endpoints.reviews.deal(dealId), payload)
      .then((r) => r.data);
  },

  getUserReviews(userId: string): Promise<StayReviewDto[]> {
    return http
      .get<StayReviewDto[]>(endpoints.reviews.userReviews(userId))
      .then((r) => r.data);
  },

  getUserReputation(userId: string): Promise<UserReputationDto> {
    return http
      .get<UserReputationDto>(endpoints.reviews.userReputation(userId))
      .then((r) => r.data);
  },

  getListingReviews(listingId: string): Promise<StayReviewDto[]> {
    return http
      .get<StayReviewDto[]>(endpoints.reviews.listing(listingId))
      .then((r) => r.data);
  },

  listPartnerReviews(orgId: string): Promise<PartnerServiceReviewDto[]> {
    return http
      .get<PartnerServiceReviewDto[]>(endpoints.reviews.partner(orgId))
      .then((r) => r.data);
  },

  submitPartnerReview(
    orgId: string,
    payload: SubmitPartnerServiceReviewRequest,
  ): Promise<PartnerServiceReviewDto> {
    return http
      .post<PartnerServiceReviewDto>(endpoints.reviews.partner(orgId), payload)
      .then((r) => r.data);
  },

  getPartnerReputation(orgId: string): Promise<PartnerReputationDto> {
    return http
      .get<PartnerReputationDto>(endpoints.reviews.partnerReputation(orgId))
      .then((r) => r.data);
  },
};
