import React from 'react';

interface HighlightedTextProps {
  text: string;
  className?: string;
}

/**
 * Safely renders highlighted snippets without dangerouslySetInnerHTML.
 * Parses <mark>...</mark> tags emitted by backend into native React elements.
 */
export const HighlightedText: React.FC<HighlightedTextProps> = ({ text, className }) => {
  if (!text) return null;

  // Split text by <mark> and </mark>
  const parts: React.ReactNode[] = [];
  const regex = /<mark>(.*?)<\/mark>/gi;

  let lastIndex = 0;
  let match: RegExpExecArray | null;

  while ((match = regex.exec(text)) !== null) {
    // Add text preceding the <mark> tag
    if (match.index > lastIndex) {
      parts.push(text.substring(lastIndex, match.index));
    }

    // Add the highlighted content safely as a React <mark> element
    parts.push(
      <mark key={match.index} className="search-highlight">
        {match[1]}
      </mark>
    );

    lastIndex = regex.lastIndex;
  }

  // Add any remaining text
  if (lastIndex < text.length) {
    parts.push(text.substring(lastIndex));
  }

  return <span className={className}>{parts}</span>;
};
