import React from 'react';
import { UserOption } from '../types';

interface HeaderProps {
  currentUser: string;
  users: UserOption[];
  onSelectUser: (user: string) => void;
}

export const Header: React.FC<HeaderProps> = ({ currentUser, users, onSelectUser }) => {
  const activeUser = users.find(u => u.username.toLowerCase() === currentUser.toLowerCase()) || {
    username: currentUser,
    projectMemberships: [],
    description: currentUser
  };

  return (
    <header className="app-header">
      <div className="header-inner">
        <div className="brand">
          <div className="brand-badge">VConstruct</div>
          <div className="brand-title">
            <h1>SiteSearch</h1>
            <span className="brand-sub">Project Knowledge &amp; OSHA Safety Regulations</span>
          </div>
        </div>

        <div className="user-control">
          <label htmlFor="user-select" className="user-label">
            Active User:
          </label>
          <div className="user-select-wrap">
            <select
              id="user-select"
              value={currentUser}
              onChange={(e) => onSelectUser(e.target.value)}
              className="user-select"
              aria-label="Switch User for Access Control"
            >
              {users.map((u) => (
                <option key={u.username} value={u.username}>
                  {u.description}
                </option>
              ))}
            </select>
            <div className="user-pill">
              {activeUser.projectMemberships.length > 0 ? (
                <span className="pill-badge pill-project">
                  Projects: {activeUser.projectMemberships.join(', ')}
                </span>
              ) : (
                <span className="pill-badge pill-guest">Regulations Only</span>
              )}
            </div>
          </div>
        </div>
      </div>
    </header>
  );
};
