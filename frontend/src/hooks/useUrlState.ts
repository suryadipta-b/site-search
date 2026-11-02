import { useCallback, useEffect, useState } from 'react';
import { SearchFilters } from '../types';

export function useUrlState(defaultUser = 'ravi') {
  const parseUrl = useCallback((): SearchFilters => {
    const params = new URLSearchParams(window.location.search);
    return {
      q: params.get('q') || '',
      type: params.get('type') || '',
      project: params.get('project') || '',
      page: parseInt(params.get('page') || '1', 10) || 1,
      pageSize: parseInt(params.get('pageSize') || '10', 10) || 10,
      asUser: params.get('user') || params.get('as') || defaultUser
    };
  }, [defaultUser]);

  const [filters, setFiltersState] = useState<SearchFilters>(parseUrl);

  // Sync state to URL
  const setFilters = useCallback((updater: Partial<SearchFilters> | ((prev: SearchFilters) => SearchFilters)) => {
    setFiltersState(prev => {
      const next = typeof updater === 'function' ? updater(prev) : { ...prev, ...updater };
      
      const params = new URLSearchParams();
      if (next.q) params.set('q', next.q);
      if (next.type) params.set('type', next.type);
      if (next.project) params.set('project', next.project);
      if (next.page > 1) params.set('page', next.page.toString());
      if (next.pageSize !== 10) params.set('pageSize', next.pageSize.toString());
      if (next.asUser) params.set('user', next.asUser);

      const queryString = params.toString();
      const newUrl = queryString ? `?${queryString}` : window.location.pathname;

      if (window.location.search !== (queryString ? `?${queryString}` : '')) {
        window.history.pushState(next, '', newUrl);
      }

      return next;
    });
  }, []);

  // Handle browser back and forward navigation (popstate)
  useEffect(() => {
    const handlePopState = () => {
      setFiltersState(parseUrl());
    };

    window.addEventListener('popstate', handlePopState);
    return () => {
      window.removeEventListener('popstate', handlePopState);
    };
  }, [parseUrl]);

  return [filters, setFilters] as const;
}
