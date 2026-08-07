/**
 * Deal-linked inquiry threads are owned by the booking and open on the deal
 * page. Pre-booking threads (no dealId) stay on the session route.
 */
export function inquiryThreadHref(session: {
  sessionId: string;
  dealId?: string | null;
}): string {
  return session.dealId
    ? `/app/deals/${session.dealId}/inquiry`
    : `/app/inquiry/${session.sessionId}`;
}
