import React, { useEffect } from 'react';
import { DocumentItem } from '../types';

interface DocumentModalProps {
  document: DocumentItem | null;
  isLoading: boolean;
  error: string | null;
  onClose: () => void;
}

export const DocumentModal: React.FC<DocumentModalProps> = ({
  document,
  isLoading,
  error,
  onClose
}) => {
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [onClose]);

  if (!document && !isLoading && !error) return null;

  return (
    <div className="modal-backdrop" onClick={onClose} role="dialog" aria-modal="true">
      <div className="modal-container" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <div className="modal-badges">
            {document && (
              <>
                <span className="badge-type">{document.type}</span>
                <span className="badge-number">{document.number}</span>
                {document.projectId ? (
                  <span className="badge-project">Project {document.projectId}</span>
                ) : (
                  <span className="badge-global">OSHA Regulation</span>
                )}
                {document.discipline && (
                  <span className="badge-discipline">{document.discipline}</span>
                )}
              </>
            )}
          </div>
          <button type="button" className="modal-close-btn" onClick={onClose} aria-label="Close">
            ×
          </button>
        </div>

        <div className="modal-body">
          {isLoading && (
            <div className="modal-loading">
              <div className="spinner-ring" />
              <p>Loading document details...</p>
            </div>
          )}

          {error && (
            <div className="modal-error">
              <h4>Error Loading Document</h4>
              <p>{error}</p>
            </div>
          )}

          {document && !isLoading && !error && (
            <div className="document-detail">
              <h2 className="doc-title">{document.title}</h2>

              <dl className="doc-grid">
                <div>
                  <dt>Document ID</dt>
                  <dd><code>{document.id}</code></dd>
                </div>
                <div>
                  <dt>Created Date</dt>
                  <dd>{new Date(document.createdAt).toLocaleString()}</dd>
                </div>
                {document.subpart && (
                  <div>
                    <dt>Subpart</dt>
                    <dd>{document.subpart}</dd>
                  </div>
                )}
                <div>
                  <dt>Access Classification</dt>
                  <dd>{document.projectId ? `Confidential to Project ${document.projectId}` : 'Public Safety Regulation'}</dd>
                </div>
              </dl>

              <div className="doc-content-section">
                <h3>Document Content</h3>
                <div className="doc-body-text">
                  {document.body.split('\n\n').map((paragraph, idx) => (
                    <p key={idx}>{paragraph}</p>
                  ))}
                </div>
              </div>
            </div>
          )}
        </div>

        <div className="modal-footer">
          <button type="button" className="btn btn-secondary" onClick={onClose}>
            Close
          </button>
        </div>
      </div>
    </div>
  );
};
