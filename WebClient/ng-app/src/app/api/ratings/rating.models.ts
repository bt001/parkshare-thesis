/** A saved review without private participant identifiers. */
export interface Rating { id: number; stars: number; comment: string; createdAt: string; }
/** Public review page and aggregate across all listing reviews. */
export interface ListingRatings { page: number; pageSize: number; totalCount: number; averageStars: number | null; items: Rating[]; }
