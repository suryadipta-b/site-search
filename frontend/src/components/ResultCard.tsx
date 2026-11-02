import React from 'react';
import { SearchHit } from '../types';
import { HighlightedText } from './HighlightedText';

interface ResultCardProps {
  hit: SearchHit;
  onSelect: (id: string) => void;
}

export const ResultCard: React.FC<ResultCardProps> = ({ hit, onSelect }) => {
  const { document, score, snippet } = hit;

  const formattedDate = new Date(document.createdAt).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  });

  const getTypeBadgeClass = (type: string) => {
    switch (type.toLowerCase()) {
      case 'regulation':
        return 'badge-regulation';
      case 'rfi':
        return 'badge-rfi';
      case 'dailylog':
        return 'badge-dailylog';
      case 'ncr':
        return 'badge-ncr';
      default:
        return 'badge-default';
    }
  };

  return (
    <article
      className="result-card"
      onClick={() => onSelect(document.id)}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          onSelect(document.id);
        }
      }}
      aria-label={`View ${document.number}: ${document.title}`}
    >
      <div className="card-top">
        <div className="card-identifiers">
          <span className={`badge-type ${getTypeBadgeClass(document.type)}`}>
            {document.type}
          </span>
          <span className="badge-number">{document.number}</span>
          {document.projectId ? (
            <span className="badge-project">Project {document.projectId}</span>
          ) : (
            <span className="badge-global">OSHA Regulation</span>
          )}
          {document.discipline && (
            <span className="badge-discipline">{document.discipline}</span>
          )}
        </div>
        <div className="card-meta">
          <time dateTime={document.createdAt}>{formattedDate}</time>
          <span className="badge-score" title={`Relevance Score: ${score}`}>
            Score: {score}
          </span>
        </div>
      </div>

      <h3 className="card-title">
        <HighlightedText text={document.title} />
      </h3>

      <div className="card-snippet">
        <HighlightedText text={snippet} />
      </div>

      {document.subpart && (
        <div className="card-subpart">
          <span>Subpart:</span> {document.subpart}
        </div>
      )}
    </article>
  );
};
