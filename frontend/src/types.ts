export interface DocumentItem {
  id: string;
  projectId: string | null;
  type: string; // Regulation | RFI | DailyLog | NCR
  number: string;
  title: string;
  body: string;
  discipline?: string;
  createdAt: string;
  subpart?: string;
}

export interface SearchHit {
  document: DocumentItem;
  score: number;
  snippet: string;
  matchedTokens: string[];
}

export interface FacetResult {
  types: Record<string, number>;
  projects: Record<string, number>;
}

export interface SearchResponse {
  total: number;
  page: number;
  pageSize: number;
  hits: SearchHit[];
  facets: FacetResult;
  activeUser: string;
}

export interface UserOption {
  username: string;
  projectMemberships: string[];
  description: string;
}

export interface SearchFilters {
  q: string;
  type: string;
  project: string;
  page: number;
  pageSize: number;
  asUser: string;
}
