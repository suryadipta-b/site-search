import React, { useEffect, useRef, useState } from 'react';
import { getDocumentById, getUsers, searchDocuments } from './api';
import { DocumentModal } from './components/DocumentModal';
import { FacetFilter } from './components/FacetFilter';
import { Header } from './components/Header';
import { Pagination } from './components/Pagination';
import { ResultCard } from './components/ResultCard';
import { SearchBar } from './components/SearchBar';
import { useUrlState } from './hooks/useUrlState';
import { DocumentItem, SearchHit, SearchResponse, UserOption } from './types';
import './App.css';

export const App: React.FC = () => {
  const [filters, setFilters] = useUrlState('ravi');
  const [users, setUsers] = useState<UserOption[]>([]);
  const [searchResponse, setSearchResponse] = useState<SearchResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Document modal state
  const [selectedDocId, setSelectedDocId] = useState<string | null>(null);
  const [modalDoc, setModalDoc] = useState<DocumentItem | null>(null);
  const [isModalLoading, setIsModalLoading] = useState(false);
  const [modalError, setModalError] = useState<string | null>(null);

  // Race condition protection: request sequence ID and AbortController (F1)
  const currentRequestId = useRef(0);
  const abortControllerRef = useRef<AbortController | null>(null);

  // Load available mock users once
  useEffect(() => {
    getUsers().then(setUsers);
  }, []);

  // Main search effect triggered when filters change
  useEffect(() => {
    // Cancel previous in-flight request
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }

    const controller = new AbortController();
    abortControllerRef.current = controller;
    const reqId = ++currentRequestId.current;

    setIsLoading(true);
    setErrorMessage(null);

    searchDocuments(filters, controller.signal)
      .then((data) => {
        // Stale response guard: ensure this is still the newest request
        if (reqId === currentRequestId.current) {
          setSearchResponse(data);
          setIsLoading(false);
        }
      })
      .catch((err) => {
        if (err.name !== 'AbortError' && reqId === currentRequestId.current) {
          setErrorMessage(err.message || 'An unexpected search error occurred.');
          setIsLoading(false);
        }
      });

    return () => {
      controller.abort();
    };
  }, [filters]);

  // Document detail modal loader
  const handleOpenDocument = (id: string) => {
    setSelectedDocId(id);
    setIsModalLoading(true);
    setModalError(null);
    setModalDoc(null);

    getDocumentById(id, filters.asUser)
      .then((doc) => {
        setModalDoc(doc);
        setIsModalLoading(false);
      })
      .catch((err) => {
        setModalError(err.message);
        setIsModalLoading(false);
      });
  };

  const handleCloseModal = () => {
    setSelectedDocId(null);
    setModalDoc(null);
    setModalError(null);
  };

  return (
    <div className="site-search-app">
      <div className="stripe-bar" aria-hidden="true" />

      <Header
        currentUser={filters.asUser}
        users={users}
        onSelectUser={(newUser) => {
          setFilters({ asUser: newUser, page: 1 });
        }}
      />

      <main className="main-content">
        <div className="container">
          <SearchBar
            value={filters.q}
            onChange={(newQuery) => {
              setFilters({ q: newQuery, page: 1 });
            }}
            isLoading={isLoading}
          />

          <div className="search-layout">
            <FacetFilter
              facets={searchResponse?.facets || { types: {}, projects: {} }}
              selectedType={filters.type}
              selectedProject={filters.project}
              onSelectType={(newType) => {
                setFilters({ type: newType, page: 1 });
              }}
              onSelectProject={(newProj) => {
                setFilters({ project: newProj, page: 1 });
              }}
              onClearFilters={() => {
                setFilters({ type: '', project: '', page: 1 });
              }}
            />

            <section className="results-container" aria-live="polite">
              {/* Active filters breadcrumbs */}
              {(filters.type || filters.project || filters.q) && (
                <div className="active-filter-chips">
                  <span className="chips-title">Active:</span>
                  {filters.q && (
                    <span className="active-chip">
                      Query: <strong>"{filters.q}"</strong>
                      <button onClick={() => setFilters({ q: '', page: 1 })}>×</button>
                    </span>
                  )}
                  {filters.type && (
                    <span className="active-chip">
                      Type: <strong>{filters.type}</strong>
                      <button onClick={() => setFilters({ type: '', page: 1 })}>×</button>
                    </span>
                  )}
                  {filters.project && (
                    <span className="active-chip">
                      Project: <strong>{filters.project}</strong>
                      <button onClick={() => setFilters({ project: '', page: 1 })}>×</button>
                    </span>
                  )}
                </div>
              )}

              {/* Status Header */}
              <div className="results-status-header">
                {isLoading ? (
                  <div className="status-text loading-text">Searching project documents and regulations...</div>
                ) : errorMessage ? (
                  <div className="status-text error-text">Search failed</div>
                ) : searchResponse ? (
                  <div className="status-text">
                    Found <strong>{searchResponse.total}</strong> results
                    {filters.q && <span> for <em>"{filters.q}"</em></span>}
                  </div>
                ) : null}
              </div>

              {/* Error State */}
              {errorMessage && (
                <div className="state-card state-error">
                  <div className="state-icon">⚠️</div>
                  <h3>Unable to fetch search results</h3>
                  <p>{errorMessage}</p>
                  <button
                    type="button"
                    className="btn btn-primary"
                    onClick={() => setFilters({ ...filters })}
                  >
                    Try Again
                  </button>
                </div>
              )}

              {/* Empty State */}
              {!isLoading && !errorMessage && searchResponse?.hits.length === 0 && (
                <div className="state-card state-empty">
                  <div className="state-icon">🔍</div>
                  <h3>No matching documents found</h3>
                  <p>
                    No documents matched your query with current filters for user <strong>{filters.asUser}</strong>.
                  </p>
                  <ul className="empty-suggestions">
                    <li>Check if the document belongs to another project (Asha sees P1, Ravi sees P2).</li>
                    <li>Try broader terms or synonyms (e.g. <em>sariya</em>, <em>shuttering</em>, <em>drywall</em>).</li>
                    <li>Try searching for exact identifiers like <code>RFI-0042</code> or <code>1926.451</code>.</li>
                  </ul>
                  <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={() => setFilters({ q: '', type: '', project: '', page: 1 })}
                  >
                    Reset All Filters
                  </button>
                </div>
              )}

              {/* Results List */}
              {!errorMessage && searchResponse && searchResponse.hits.length > 0 && (
                <div className="results-list">
                  {searchResponse.hits.map((hit: SearchHit) => (
                    <ResultCard
                      key={hit.document.id}
                      hit={hit}
                      onSelect={handleOpenDocument}
                    />
                  ))}

                  <Pagination
                    currentPage={filters.page}
                    pageSize={filters.pageSize}
                    totalHits={searchResponse.total}
                    onPageChange={(newPage) => setFilters({ page: newPage })}
                  />
                </div>
              )}
            </section>
          </div>
        </div>
      </main>

      {/* Document View Modal (F5) */}
      {selectedDocId && (
        <DocumentModal
          document={modalDoc}
          isLoading={isModalLoading}
          error={modalError}
          onClose={handleCloseModal}
        />
      )}
    </div>
  );
};
