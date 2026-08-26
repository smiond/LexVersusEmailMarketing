import { useEffect, useState, type FormEvent } from 'react';
import { api } from '../api/client';
import type { SenderIdentity } from '../types';

export function SendersPage() {
  const [senders, setSenders] = useState<SenderIdentity[]>([]);
  const [form, setForm] = useState({ email: '', displayName: '', replyToEmail: '', isDefault: false });
  const [editing, setEditing] = useState<SenderIdentity | null>(null);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  const load = () => api.senders.getAll().then(setSenders);

  useEffect(() => {
    load().catch((e) => setError(e.message));
  }, []);

  const resetForm = () => {
    setForm({ email: '', displayName: '', replyToEmail: '', isDefault: false });
    setEditing(null);
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');

    try {
      if (editing) {
        await api.senders.update(editing.id, {
          email: form.email.trim(),
          displayName: form.displayName.trim(),
          replyToEmail: form.replyToEmail.trim() || undefined,
          isDefault: form.isDefault,
          isActive: editing.isActive,
        });
        setMessage('Sender updated.');
      } else {
        await api.senders.create({
          email: form.email.trim(),
          displayName: form.displayName.trim(),
          replyToEmail: form.replyToEmail.trim() || undefined,
          isDefault: form.isDefault,
        });
        setMessage('Sender added. Make sure this address is verified in SendGrid.');
      }
      resetForm();
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Save failed');
    }
  };

  const handleEdit = (sender: SenderIdentity) => {
    setEditing(sender);
    setForm({
      email: sender.email,
      displayName: sender.displayName,
      replyToEmail: sender.replyToEmail ?? '',
      isDefault: sender.isDefault,
    });
  };

  const toggleActive = async (sender: SenderIdentity) => {
    try {
      await api.senders.update(sender.id, {
        email: sender.email,
        displayName: sender.displayName,
        replyToEmail: sender.replyToEmail,
        isDefault: sender.isDefault,
        isActive: !sender.isActive,
      });
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Update failed');
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this sender?')) return;
    try {
      await api.senders.delete(id);
      await load();
      setMessage('Sender deleted.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Delete failed');
    }
  };

  return (
    <div>
      <header className="page-header">
        <h2>Senders</h2>
        <p>Verified SendGrid From addresses used when sending campaigns</p>
      </header>

      {message && <div className="alert success" onClick={() => setMessage('')}>{message}</div>}
      {error && <div className="alert error" onClick={() => setError('')}>{error}</div>}

      <section className="card">
        <h3>{editing ? 'Edit Sender' : 'Add Sender'}</h3>
        <p className="hint">
          Only use emails already verified in SendGrid (Single Sender or Domain Authentication).
          Reply-To is optional and used when recipients reply to the campaign.
        </p>
        <form onSubmit={handleSubmit} className="form">
          <label>
            From Email
            <input
              type="email"
              value={form.email}
              onChange={(e) => setForm({ ...form, email: e.target.value })}
              placeholder="marketing@yourdomain.com"
              required
            />
          </label>
          <label>
            Display Name
            <input
              value={form.displayName}
              onChange={(e) => setForm({ ...form, displayName: e.target.value })}
              placeholder="LexVersus Marketing"
              required
            />
          </label>
          <label>
            Reply-To Email (optional)
            <input
              type="email"
              value={form.replyToEmail}
              onChange={(e) => setForm({ ...form, replyToEmail: e.target.value })}
              placeholder="replies@yourdomain.com"
            />
          </label>
          <label className="checkbox-row">
            <input
              type="checkbox"
              checked={form.isDefault}
              onChange={(e) => setForm({ ...form, isDefault: e.target.checked })}
            />
            Default sender for campaigns
          </label>
          <div className="form-actions">
            <button type="submit" className="btn primary">{editing ? 'Update' : 'Add'} Sender</button>
            {editing && <button type="button" className="btn" onClick={resetForm}>Cancel</button>}
          </div>
        </form>
      </section>

      <section className="card">
        <h3>Configured Senders ({senders.length})</h3>
        <table>
          <thead>
            <tr>
              <th>From Email</th>
              <th>Display Name</th>
              <th>Reply-To</th>
              <th>Default</th>
              <th>Active</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {senders.map((s) => (
              <tr key={s.id}>
                <td>{s.email}</td>
                <td>{s.displayName}</td>
                <td>{s.replyToEmail ?? '—'}</td>
                <td>{s.isDefault ? 'Yes' : '—'}</td>
                <td>{s.isActive ? 'Yes' : 'No'}</td>
                <td className="actions">
                  <button className="btn small" onClick={() => handleEdit(s)}>Edit</button>
                  <button className="btn small" onClick={() => toggleActive(s)}>
                    {s.isActive ? 'Deactivate' : 'Activate'}
                  </button>
                  <button className="btn danger small" onClick={() => handleDelete(s.id)}>Delete</button>
                </td>
              </tr>
            ))}
            {senders.length === 0 && (
              <tr><td colSpan={6} className="empty">No senders yet. Add your verified SendGrid addresses.</td></tr>
            )}
          </tbody>
        </table>
      </section>
    </div>
  );
}
