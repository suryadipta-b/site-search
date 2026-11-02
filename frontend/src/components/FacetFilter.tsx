import React from 'react';
import { FacetResult } from '../types';

interface FacetFilterProps {
  facets: FacetResult;
  selectedType: string;
  selectedProject: string;
  onSelectType: (type: string) => void;
  onSelectProject: (project: string) => void;
  onClearFilters: () => void;
}

const ALL_TYPES = ['Regulation', 'RFI', 'DailyLog', 'NCR'];

export const FacetFilter: React.FC<FacetFilterProps> = ({
  facets,
  selectedType,
  selectedProject,
  onSelectType,
  onSelectProject,
  onClearFilters
}) => {
  const hasActiveFilters = Boolean(selectedType || selectedProject);

  // Collect available projects from facet dictionary
  const availableProjects = Object.keys(facets.projects).sort();

  return (
    <aside className="facets-sidebar" aria-label="Filters">
      <div className="facets-header">
        <h3>Filters &amp; Facets</h3>
        {hasActiveFilters && (
          <button type="button" className="clear-filters-btn" onClick={onClearFilters}>
            Reset all
          </button>
        )}
      </div>

      {/* Document Type Facet */}
      <div className="facet-group">
        <h4 className="facet-title">Document Type</h4>
        <ul className="facet-list">
          <li className="facet-item">
            <button
              type="button"
              className={`facet-btn ${!selectedType ? 'facet-btn-active' : ''}`}
              onClick={() => onSelectType('')}
            >
              <span className="facet-name">All Types</span>
            </button>
          </li>
          {ALL_TYPES.map((type) => {
            const count = facets.types[type] || 0;
            const isSelected = selectedType.toLowerCase() === type.toLowerCase();
            return (
              <li key={type} className="facet-item">
                <button
                  type="button"
                  className={`facet-btn ${isSelected ? 'facet-btn-active' : ''} ${count === 0 ? 'facet-btn-disabled' : ''}`}
                  onClick={() => onSelectType(isSelected ? '' : type)}
                  disabled={count === 0 && !isSelected}
                >
                  <span className="facet-name">{type}</span>
                  <span className="facet-count">{count}</span>
                </button>
              </li>
            );
          })}
        </ul>
      </div>

      {/* Project Facet */}
      <div className="facet-group">
        <h4 className="facet-title">Project</h4>
        <ul className="facet-list">
          <li className="facet-item">
            <button
              type="button"
              className={`facet-btn ${!selectedProject ? 'facet-btn-active' : ''}`}
              onClick={() => onSelectProject('')}
            >
              <span className="facet-name">All Accessible Projects</span>
            </button>
          </li>
          {availableProjects.length === 0 ? (
            <li className="facet-empty">No accessible projects in results</li>
          ) : (
            availableProjects.map((proj) => {
              const count = facets.projects[proj] || 0;
              const isSelected = selectedProject.toLowerCase() === proj.toLowerCase();
              return (
                <li key={proj} className="facet-item">
                  <button
                    type="button"
                    className={`facet-btn ${isSelected ? 'facet-btn-active' : ''}`}
                    onClick={() => onSelectProject(isSelected ? '' : proj)}
                  >
                    <span className="facet-name">
                      {proj === 'P1'
                        ? 'P1 · Sunrise Residency'
                        : proj === 'P2'
                        ? 'P2 · Metro Depot'
                        : proj}
                    </span>
                    <span className="facet-count">{count}</span>
                  </button>
                </li>
              );
            })
          )}
        </ul>
      </div>

      <div className="facets-tip">
        <p>
          <strong>Access Control:</strong> Facet counts only reflect documents your active user has
          permission to view.
        </p>
      </div>
    </aside>
  );
};
