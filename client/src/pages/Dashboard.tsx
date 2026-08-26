import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import type { DashboardStats } from '../types';

export function DashboardPage() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    api.dashboard.getStats()
      .then(setStats)
      .catch((e) => setError(e.message));
  }, []);

  if (error) {
    return <div className="alert error">Failed to load dashboard: {error}</div>;
  }

  if (!stats) {
    return <div className="loading">Loading dashboard...</div>;
  }

  const cards = [
    { label: 'Total Contacts', value: stats.totalContacts, link: '/contacts' },
    { label: 'Email Templates', value: stats.totalTemplates, link: '/templates' },
    { label: 'Campaigns', value: stats.totalCampaigns, link: '/campaigns' },
    { label: 'Active Campaigns', value: stats.activeCampaigns, link: '/campaigns' },
    { label: 'Emails Sent Today', value: stats.emailsSentToday, link: '/campaigns' },
  ];

  return (
    <div>
      <header className="page-header">
        <h2>Dashboard</h2>
        <p>Overview of your email marketing activity</p>
      </header>

      <div className="stats-grid">
        {cards.map((card) => (
          <Link key={card.label} to={card.link} className="stat-card">
            <span className="stat-value">{card.value}</span>
            <span className="stat-label">{card.label}</span>
          </Link>
        ))}
      </div>

      <section className="card quick-start">
        <h3>Quick Start</h3>
        <ol>
          <li><Link to="/contacts">Import contacts</Link> from CSV or Excel</li>
          <li><Link to="/templates">Create an email template</Link> with personalization tags</li>
          <li><Link to="/campaigns">Launch a campaign</Link> and send via SendGrid</li>
        </ol>
        <p className="hint">
          Personalization tags: <code>{'{{firstName}}'}</code>, <code>{'{{lastName}}'}</code>, <code>{'{{email}}'}</code>, <code>{'{{fullName}}'}</code>
        </p>
      </section>
    </div>
  );
}
