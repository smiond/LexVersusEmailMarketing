import { useEffect, useState, type FormEvent } from 'react';
import { api } from '../api/client';
import type { Campaign, EmailList, EmailTemplate, SenderIdentity } from '../types';

export function CampaignsPage() {
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [templates, setTemplates] = useState<EmailTemplate[]>([]);
  const [lists, setLists] = useState<EmailList[]>([]);
  const [senders, setSenders] = useState<SenderIdentity[]>([]);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({
    name: '',
    emailTemplateId: '',
    emailListId: '',
    senderIdentityId: '',
    subjectOverride: '',
    scheduledAt: '',
    sendMode: 'draft' as 'draft' | 'schedule' | 'now',
  });
  const [sending, setSending] = useState<string | null>(null);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  const loadData = async () => {
    const [campaignsData, templatesData, listsData, sendersData] = await Promise.all([
      api.campaigns.getAll(),
      api.templates.getAll(),
      api.contacts.getLists(),
      api.senders.getAll(true),
    ]);
    setCampaigns(campaignsData);
    setTemplates(templatesData);
    setLists(listsData);
    setSenders(sendersData);

    const defaultSender = sendersData.find((s) => s.isDefault) ?? sendersData[0];
    if (defaultSender) {
      setForm((prev) => ({
        ...prev,
        senderIdentityId: prev.senderIdentityId || defaultSender.id,
      }));
    }
  };

  useEffect(() => {
    loadData().catch((e) => setError(e.message));
  }, []);

  const handleCreate = async (e: FormEvent) => {
    e.preventDefault();
    setError('');

    try {
      const created = await api.campaigns.create({
        name: form.name.trim(),
        emailTemplateId: form.emailTemplateId,
        emailListId: form.emailListId || undefined,
        senderIdentityId: form.senderIdentityId || undefined,
        subjectOverride: form.subjectOverride.trim() || undefined,
        scheduledAt: form.sendMode === 'schedule' && form.scheduledAt
          ? new Date(form.scheduledAt).toISOString()
          : undefined,
      });

      if (form.sendMode === 'now') {
        const result = await api.campaigns.send(created.id);
        setMessage(`Campaign sent: ${result.sent} delivered, ${result.failed} failed out of ${result.total}.`);
      } else if (form.sendMode === 'schedule') {
        setMessage(`Campaign scheduled for ${new Date(form.scheduledAt).toLocaleString()}.`);
      } else {
        setMessage('Campaign created as draft.');
      }

      const defaultSender = senders.find((s) => s.isDefault) ?? senders[0];
      setForm({
        name: '',
        emailTemplateId: '',
        emailListId: '',
        senderIdentityId: defaultSender?.id ?? '',
        subjectOverride: '',
        scheduledAt: '',
        sendMode: 'draft',
      });
      setShowForm(false);
      await loadData();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create campaign');
    }
  };

  const handleSend = async (id: string) => {
    if (!confirm('Send this campaign now? This will email all recipients via SendGrid.')) return;

    setSending(id);
    setError('');

    try {
      const result = await api.campaigns.send(id);
      setMessage(`Campaign sent: ${result.sent} delivered, ${result.failed} failed out of ${result.total}.`);
      await loadData();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Send failed');
    } finally {
      setSending(null);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this campaign?')) return;
    await api.campaigns.delete(id);
    await loadData();
  };

  const statusClass = (status: string) => `badge status-${status.toLowerCase()}`;
  const senderLabel = (c: Campaign) =>
    c.senderEmail
      ? `${c.senderDisplayName ?? ''} <${c.senderEmail}>`.trim()
      : 'Default sender';

  return (
    <div>
      <header className="page-header">
        <h2>Campaigns</h2>
        <p>Create and send email campaigns to your contacts</p>
      </header>

      {message && <div className="alert success" onClick={() => setMessage('')}>{message}</div>}
      {error && <div className="alert error" onClick={() => setError('')}>{error}</div>}

      <div className="actions-bar">
        <button
          className="btn primary"
          onClick={() => setShowForm(true)}
          disabled={templates.length === 0}
        >
          + New Campaign
        </button>
        {templates.length === 0 && <span className="hint">Create a template first</span>}
        {senders.length === 0 && (
          <span className="hint">Add a verified sender under Senders before sending</span>
        )}
      </div>

      {showForm && (
        <section className="card">
          <h3>New Campaign</h3>
          <form onSubmit={handleCreate} className="form">
            <label>
              Campaign Name
              <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
            </label>
            <label>
              Email Template
              <select
                value={form.emailTemplateId}
                onChange={(e) => setForm({ ...form, emailTemplateId: e.target.value })}
                required
              >
                <option value="">Select template...</option>
                {templates.map((t) => (
                  <option key={t.id} value={t.id}>{t.name}</option>
                ))}
              </select>
            </label>
            <label>
              From Sender
              <select
                value={form.senderIdentityId}
                onChange={(e) => setForm({ ...form, senderIdentityId: e.target.value })}
              >
                <option value="">Default sender</option>
                {senders.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.displayName} &lt;{s.email}&gt;{s.isDefault ? ' (default)' : ''}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Recipient List
              <select
                value={form.emailListId}
                onChange={(e) => setForm({ ...form, emailListId: e.target.value })}
              >
                <option value="">All subscribed contacts</option>
                {lists.map((l) => (
                  <option key={l.id} value={l.id}>{l.name} ({l.contactCount})</option>
                ))}
              </select>
            </label>
            <label>
              Subject Override (optional)
              <input
                value={form.subjectOverride}
                onChange={(e) => setForm({ ...form, subjectOverride: e.target.value })}
                placeholder="Leave empty to use template subject"
              />
            </label>
            <label>
              Send Options
              <select
                value={form.sendMode}
                onChange={(e) => setForm({ ...form, sendMode: e.target.value as 'draft' | 'schedule' | 'now' })}
              >
                <option value="draft">Save as draft</option>
                <option value="schedule">Schedule for later</option>
                <option value="now">Send immediately</option>
              </select>
            </label>
            {form.sendMode === 'schedule' && (
              <label>
                Scheduled Date & Time
                <input
                  type="datetime-local"
                  value={form.scheduledAt}
                  onChange={(e) => setForm({ ...form, scheduledAt: e.target.value })}
                  required
                />
              </label>
            )}
            <div className="form-actions">
              <button type="submit" className="btn primary">Create Campaign</button>
              <button type="button" className="btn" onClick={() => setShowForm(false)}>Cancel</button>
            </div>
          </form>
        </section>
      )}

      <section className="card">
        <h3>All Campaigns ({campaigns.length})</h3>
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Template</th>
              <th>From</th>
              <th>Audience</th>
              <th>Status</th>
              <th>Scheduled</th>
              <th>Recipients</th>
              <th>Sent / Failed</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {campaigns.map((c) => (
              <tr key={c.id}>
                <td><strong>{c.name}</strong></td>
                <td>{c.emailTemplateName}</td>
                <td>{senderLabel(c)}</td>
                <td>{c.emailListName ?? 'All contacts'}</td>
                <td><span className={statusClass(c.status)}>{c.status}</span></td>
                <td>{c.scheduledAt ? new Date(c.scheduledAt).toLocaleString() : '—'}</td>
                <td>{c.totalRecipients}</td>
                <td>{c.sentCount} / {c.failedCount}</td>
                <td className="actions">
                  {(c.status === 'Draft' || c.status === 'Scheduled') && (
                    <button
                      className="btn primary small"
                      onClick={() => handleSend(c.id)}
                      disabled={sending === c.id}
                    >
                      {sending === c.id ? 'Sending...' : 'Send Now'}
                    </button>
                  )}
                  {c.status !== 'Sending' && (
                    <button className="btn danger small" onClick={() => handleDelete(c.id)}>Delete</button>
                  )}
                </td>
              </tr>
            ))}
            {campaigns.length === 0 && (
              <tr><td colSpan={9} className="empty">No campaigns yet.</td></tr>
            )}
          </tbody>
        </table>
      </section>
    </div>
  );
}
