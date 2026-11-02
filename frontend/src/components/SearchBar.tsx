import React, { useEffect, useState } from 'react';
import { useDebounce } from '../hooks/useDebounce';

interface SearchBarProps {
  value: string;
  onChange: (query: string) => void;
  isLoading: boolean;
}

const SAMPLE_QUERIES = [
  'sariya spacing slab',
  'shuttering removal time',
  'scafold guardrail',
  'fall protection',
  'RFI-0042',
  'excavation 5 feet protective system'
];

export const SearchBar: React.FC<SearchBarProps> = ({ value, onChange, isLoading }) => {
  const [localValue, setLocalValue] = useState(value);
  const debouncedValue = useDebounce(localValue, 250);

  // Sync external value changes (e.g., URL popstate or quick chips)
  useEffect(() => {
    setLocalValue(value);
  }, [value]);

  // Trigger parent change when debounced value updates
  useEffect(() => {
    if (debouncedValue !== value) {
      onChange(debouncedValue);
    }
  }, [debouncedValue, onChange, value]);

  const handleClear = () => {
    setLocalValue('');
    onChange('');
  };

  return (
    <div className="search-bar-container">
      <div className="search-input-wrapper">
        <svg
          className="search-icon"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <circle cx="11" cy="11" r="8" />
          <line x1="21" y1="21" x2="16.65" y2="16.65" />
        </svg>

        <input
          type="text"
          className="search-input"
          placeholder="Search safety regulations, RFIs, daily logs, NCRs (e.g. sariya, shuttering, 1926.451)..."
          value={localValue}
          onChange={(e) => setLocalValue(e.target.value)}
          autoFocus
        />

        {isLoading && (
          <div className="search-spinner" aria-label="Searching...">
            <div className="spinner-ring" />
          </div>
        )}

        {localValue && (
          <button
            type="button"
            className="search-clear-btn"
            onClick={handleClear}
            title="Clear search"
            aria-label="Clear search"
          >
            ×
          </button>
        )}
      </div>

      <div className="quick-queries">
        <span className="quick-label">Try domain queries:</span>
        <div className="chips-list">
          {SAMPLE_QUERIES.map((q) => (
            <button
              key={q}
              type="button"
              className={`chip ${value === q ? 'chip-active' : ''}`}
              onClick={() => {
                setLocalValue(q);
                onChange(q);
              }}
            >
              {q}
            </button>
          ))}
        </div>
      </div>
    </div>
  );
};
