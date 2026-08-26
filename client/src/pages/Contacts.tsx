import { useEffect, useState, type FormEvent } from 'react';
import { api } from '../api/client';
import type { Contact, EmailList, ImportResult } from '../types';

export function ContactsPage() {
  const [contacts, setContacts] = useState<Contact[]>([]);
  const [lists, setLists] = useState<EmailList[]>([]);
  const [search, setSearch] = useState('');
  const [importFile, setImportFile] = useState<File | null>(null);
  const [selectedListId, setSelectedListId] = useState('');
  const [importResult, setImportResult] = useState<ImportResult | null>(null);
  const [newListName, setNewListName] = useState('');
  const [newContact, setNewContact] = useState({ email: '', firstName: '', lastName: '' });
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const loadData = async () => {
    const [contactsData, listsData] = await Promise.all([
      api.contacts.getAll(search || undefined),
      api.contacts.getLists(),
    ]);
    setContacts(contactsData);
    setLists(listsData);
  };

  useEffect(() => {
    loadData().catch((e) => setError(e.message));
  }, [search]);

  const handleImport = async (e: FormEvent) => {
    e.preventDefault();
    if (!importFile) return;

    setLoading(true);
    setError('');
    setImportResult(null);

    try {
      const result = await api.contacts.import(importFile, selectedListId || undefined);
      setImportResult(result);
      setMessage(`Imported ${result.imported} contacts successfully.`);
      setImportFile(null);
      await loadData();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Import failed');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateList = async (e: FormEvent) => {
    e.preventDefault();
    if (!newListName.trim()) return;

    try {
      await api.contacts.createList({ name: newListName.trim() });
      setNewListName('');
      await loadData();
      setMessage('Email list created.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create list');
    }
  };

  const handleCreateContact = async (e: FormEvent) => {
    e.preventDefault();
    if (!newContact.email.trim()) return;

    try {
      await api.contacts.create({
        email: newContact.email.trim(),
        firstName: newContact.firstName.trim() || undefined,
        lastName: newContact.lastName.trim() || undefined,
      });
      setNewContact({ email: '', firstName: '', lastName: '' });
      await loadData();
      setMessage('Contact added.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add contact');
    }
  };

  const handleDeleteContact = async (id: string) => {
    if (!confirm('Delete this contact?')) return;
    await api.contacts.delete(id);
    await loadData();
  };

  const handleDeleteList = async (id: string) => {
    if (!confirm('Delete this email list?')) return;
    await api.contacts.deleteList(id);
    await loadData();
  };

  return (
    <div>
      <header className="page-header">
        <h2>Contacts</h2>
        <p>Import and manage your email subscribers</p>
      </header>

      {message && <div className="alert success" onClick={() => setMessage('')}>{message}</div>}
      {error && <div className="alert error" onClick={() => setError('')}>{error}</div>}

      <div className="grid-2">
        <section className="card">
          <h3>Import from CSV / Excel</h3>
          <p className="hint">Expected columns: Email, FirstName, LastName (flexible naming)</p>
          <form onSubmit={handleImport} className="form">
            <label>
              File
              <input
                type="file"
                accept=".csv,.xlsx,.xls"
                onChange={(e) => setImportFile(e.target.files?.[0] ?? null)}
                required
              />
            </label>
            <label>
              Add to list (optional)
              <select value={selectedListId} onChange={(e) => setSelectedListId(e.target.value)}>
                <option value="">All contacts only</option>
                {lists.map((list) => (
                  <option key={list.id} value={list.id}>{list.name} ({list.contactCount})</option>
                ))}
              </select>
            </label>
            <button type="submit" className="btn primary" disabled={loading || !importFile}>
              {loading ? 'Importing...' : 'Import Contacts'}
            </button>
          </form>
          {importResult && (
            <div className="import-result">
              <p>Imported: {importResult.imported} | Skipped: {importResult.skipped} | Duplicates: {importResult.duplicates}</p>
              {importResult.errors.length > 0 && (
                <ul>{importResult.errors.map((err, i) => <li key={i}>{err}</li>)}</ul>
              )}
            </div>
          )}
        </section>

        <section className="card">
          <h3>Email Lists</h3>
          <form onSubmit={handleCreateList} className="form inline">
            <input
              placeholder="New list name"
              value={newListName}
              onChange={(e) => setNewListName(e.target.value)}
            />
            <button type="submit" className="btn">Create List</button>
          </form>
          <ul className="list">
            {lists.map((list) => (
              <li key={list.id}>
                <span>{list.name} <small>({list.contactCount} contacts)</small></span>
                <button className="btn danger small" onClick={() => handleDeleteList(list.id)}>Delete</button>
              </li>
            ))}
            {lists.length === 0 && <li className="empty">No lists yet</li>}
          </ul>
        </section>
      </div>

      <section className="card">
        <div className="card-header">
          <h3>All Contacts ({contacts.length})</h3>
          <input
            className="search"
            placeholder="Search contacts..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        <form onSubmit={handleCreateContact} className="form inline">
          <input placeholder="Email *" value={newContact.email} onChange={(e) => setNewContact({ ...newContact, email: e.target.value })} required />
          <input placeholder="First name" value={newContact.firstName} onChange={(e) => setNewContact({ ...newContact, firstName: e.target.value })} />
          <input placeholder="Last name" value={newContact.lastName} onChange={(e) => setNewContact({ ...newContact, lastName: e.target.value })} />
          <button type="submit" className="btn">Add Contact</button>
        </form>

        <table>
          <thead>
            <tr>
              <th>Email</th>
              <th>Name</th>
              <th>Subscribed</th>
              <th>Added</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {contacts.map((c) => (
              <tr key={c.id}>
                <td>{c.email}</td>
                <td>{[c.firstName, c.lastName].filter(Boolean).join(' ') || '—'}</td>
                <td>{c.isSubscribed ? 'Yes' : 'No'}</td>
                <td>{new Date(c.createdAt).toLocaleDateString()}</td>
                <td>
                  <button className="btn danger small" onClick={() => handleDeleteContact(c.id)}>Delete</button>
                </td>
              </tr>
            ))}
            {contacts.length === 0 && (
              <tr><td colSpan={5} className="empty">No contacts yet. Import a CSV or Excel file to get started.</td></tr>
            )}
          </tbody>
        </table>
      </section>
    </div>
  );
}
