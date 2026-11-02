import { DocumentItem, SearchFilters, SearchResponse, UserOption } from './types';

const BASE_URL = '/api';

export async function searchDocuments(
  filters: SearchFilters,
  signal?: AbortSignal
): Promise<SearchResponse> {
  const params = new URLSearchParams();
  if (filters.q) params.set('q', filters.q);
  if (filters.type) params.set('type', filters.type);
  if (filters.project) params.set('project', filters.project);
  if (filters.page > 1) params.set('page', filters.page.toString());
  if (filters.pageSize) params.set('pageSize', filters.pageSize.toString());
  if (filters.asUser) params.set('as', filters.asUser);

  const res = await fetch(`${BASE_URL}/search?${params.toString()}`, {
    headers: {
      'Accept': 'application/json',
      'X-User': filters.asUser
    },
    signal
  });

  if (!res.ok) {
    throw new Error(`Search request failed: HTTP ${res.status} ${res.statusText}`);
  }

  return res.json();
}

export async function getDocumentById(id: string, asUser: string): Promise<DocumentItem> {
  const res = await fetch(`${BASE_URL}/documents/${encodeURIComponent(id)}?as=${encodeURIComponent(asUser)}`, {
    headers: {
      'Accept': 'application/json',
      'X-User': asUser
    }
  });

  if (!res.ok) {
    if (res.status === 404) {
      throw new Error(`Document '${id}' not found or you do not have permission to view it.`);
    }
    throw new Error(`Failed to load document: HTTP ${res.status}`);
  }

  return res.json();
}

export async function getUsers(): Promise<UserOption[]> {
  try {
    const res = await fetch(`${BASE_URL}/users`);
    if (res.ok) {
      return res.json();
    }
  } catch {
    // fallback if offline
  }

  return [
    { username: 'asha', projectMemberships: ['P1'], description: 'Asha · P1 member' },
    { username: 'ravi', projectMemberships: ['P2'], description: 'Ravi · P2 member' },
    { username: 'meera', projectMemberships: ['P1', 'P2'], description: 'Meera · P1 & P2 member' },
    { username: 'guest', projectMemberships: [], description: 'Guest · Public regulations only' }
  ];
}
