import { useEffect, useState, type FormEvent } from 'react';
import { api } from '../api/client';
import type { EmailTemplate } from '../types';

const defaultHtml = `<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
  <h1>Hello {{firstName}}!</h1>
  <p>We're excited to share our latest news with you.</p>
  <p>Best regards,<br/>LexVersus Team</p>
  <p style="font-size:12px;color:#888;margin-top:32px;">
    <a href="{{unsubscribeUrl}}">Unsubscribe</a> from future emails.
  </p>
</body>
</html>`;

export function TemplatesPage() {
  const [templates, setTemplates] = useState<EmailTemplate[]>([]);
  const [editing, setEditing] = useState<EmailTemplate | null>(null);
  const [form, setForm] = useState({ name: '', subject: '', htmlBody: defaultHtml });
  const [showForm, setShowForm] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');

  const loadTemplates = () => api.templates.getAll().then(setTemplates);

  useEffect(() => {
    loadTemplates().catch((e) => setError(e.message));
  }, []);

  const resetForm = () => {
    setForm({ name: '', subject: '', htmlBody: defaultHtml });
    setEditing(null);
    setShowForm(false);
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');

    try {
      if (editing) {
        await api.templates.update(editing.id, form);
        setMessage('Template updated.');
      } else {
        await api.templates.create(form);
        setMessage('Template created.');
      }
      resetForm();
      await loadTemplates();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Save failed');
    }
  };

  const handleEdit = (template: EmailTemplate) => {
    setEditing(template);
    setForm({ name: template.name, subject: template.subject, htmlBody: template.htmlBody });
    setShowForm(true);
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this template?')) return;
    try {
      await api.templates.delete(id);
      await loadTemplates();
      setMessage('Template deleted.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Delete failed');
    }
  };

  return (
    <div>
      <header className="page-header">
        <h2>Email Templates</h2>
        <p>Create reusable HTML email templates with personalization</p>
      </header>

      {message && <div className="alert success" onClick={() => setMessage('')}>{message}</div>}
      {error && <div className="alert error" onClick={() => setError('')}>{error}</div>}

      <div className="actions-bar">
        <button className="btn primary" onClick={() => { resetForm(); setShowForm(true); }}>
          + New Template
        </button>
      </div>

      {showForm && (
        <section className="card">
          <h3>{editing ? 'Edit Template' : 'New Template'}</h3>
          <form onSubmit={handleSubmit} className="form">
            <label>
              Template Name
              <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
            </label>
            <label>
              Subject Line
              <input value={form.subject} onChange={(e) => setForm({ ...form, subject: e.target.value })} required />
            </label>
            <label>
              HTML Body
              <textarea
                rows={12}
                value={form.htmlBody}
                onChange={(e) => setForm({ ...form, htmlBody: e.target.value })}
                required
              />
            </label>
            <p className="hint">
              Use <code>{'{{firstName}}'}</code>, <code>{'{{lastName}}'}</code>, <code>{'{{email}}'}</code>, <code>{'{{fullName}}'}</code>, <code>{'{{unsubscribeUrl}}'}</code> for personalization.
              An unsubscribe footer is added automatically if not included.
            </p>
            <div className="form-actions">
              <button type="submit" className="btn primary">{editing ? 'Update' : 'Create'} Template</button>
              <button type="button" className="btn" onClick={resetForm}>Cancel</button>
            </div>
          </form>

          <div className="preview">
            <h4>Preview</h4>
            <iframe
              title="Template preview"
              srcDoc={form.htmlBody
                .replace(/\{\{firstName\}\}/gi, 'John')
                .replace(/\{\{lastName\}\}/gi, 'Doe')
                .replace(/\{\{fullName\}\}/gi, 'John Doe')
                .replace(/\{\{email\}\}/gi, 'john@example.com')}
              sandbox=""
            />
          </div>
        </section>
      )}

      <section className="card">
        <h3>Your Templates ({templates.length})</h3>
        <div className="template-grid">
          {templates.map((t) => (
            <article key={t.id} className="template-card">
              <h4>{t.name}</h4>
              <p className="subject">{t.subject}</p>
              <p className="meta">Updated {new Date(t.updatedAt).toLocaleDateString()}</p>
              <div className="card-actions">
                <button className="btn small" onClick={() => handleEdit(t)}>Edit</button>
                <button className="btn danger small" onClick={() => handleDelete(t.id)}>Delete</button>
              </div>
            </article>
          ))}
          {templates.length === 0 && <p className="empty">No templates yet. Create your first email template.</p>}
        </div>
      </section>
    </div>
  );
}
