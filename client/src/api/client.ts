const API_BASE = '/api';

let authToken: string | null = null;

export function setAuthToken(token: string | null) {
  authToken = token;
}

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const headers = new Headers(options?.headers);

  if (authToken) {
    headers.set('Authorization', `Bearer ${authToken}`);
  }

  if (options?.body && !(options.body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  const response = await fetch(`${API_BASE}${url}`, { ...options, headers });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(parseApiError(text, response.statusText));
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json();
}

function parseApiError(text: string, fallback: string): string {
  if (!text) return fallback;

  try {
    const json = JSON.parse(text) as { message?: string; title?: string; detail?: string };
    if (json.message) return json.message;
    if (json.detail) return json.detail;
    if (json.title) return json.title;
  } catch {
    // not JSON
  }

  const firstLine = text
    .replace(/<[^>]+>/g, ' ')
    .split('\n')
    .map((l) => l.trim())
    .find((l) => l.length > 0 && !l.startsWith('HEADERS') && !l.startsWith('at '));

  if (firstLine && firstLine.length < 400) {
    return firstLine.replace(/^System\.\w+(\.\w+)*Exception:\s*/i, '');
  }

  return fallback || 'Request failed';
}

export const api = {
  auth: {
    login: (data: { email: string; password: string }) =>
      request<import('../types').AuthResponse>('/auth/login', {
        method: 'POST',
        body: JSON.stringify(data),
      }),
    register: (data: { email: string; password: string; fullName: string }) =>
      request<import('../types').AuthResponse>('/auth/register', {
        method: 'POST',
        body: JSON.stringify(data),
      }),
    me: () => request<import('../types').User>('/auth/me'),
  },
  unsubscribe: {
    getInfo: (token: string) =>
      request<import('../types').UnsubscribeInfo>(`/unsubscribe/${token}`),
    unsubscribe: (token: string) =>
      request<import('../types').UnsubscribeResult>(`/unsubscribe/${token}`, { method: 'POST' }),
  },
  dashboard: {
    getStats: () => request<import('../types').DashboardStats>('/dashboard/stats'),
  },
  contacts: {
    getAll: (search?: string) =>
      request<import('../types').Contact[]>(`/contacts${search ? `?search=${encodeURIComponent(search)}` : ''}`),
    create: (data: { email: string; firstName?: string; lastName?: string }) =>
      request<import('../types').Contact>('/contacts', {
        method: 'POST',
        body: JSON.stringify(data),
      }),
    delete: (id: string) =>
      request<void>(`/contacts/${id}`, { method: 'DELETE' }),
    import: (file: File, emailListId?: string) => {
      const form = new FormData();
      form.append('file', file);
      if (emailListId) form.append('emailListId', emailListId);
      return request<import('../types').ImportResult>('/contacts/import', {
        method: 'POST',
        body: form,
      });
    },
    getLists: () => request<import('../types').EmailList[]>('/contacts/lists'),
    createList: (data: { name: string; description?: string }) =>
      request<import('../types').EmailList>('/contacts/lists', {
        method: 'POST',
        body: JSON.stringify(data),
      }),
    deleteList: (id: string) =>
      request<void>(`/contacts/lists/${id}`, { method: 'DELETE' }),
  },
  templates: {
    getAll: () => request<import('../types').EmailTemplate[]>('/templates'),
    getById: (id: string) => request<import('../types').EmailTemplate>(`/templates/${id}`),
    create: (data: { name: string; subject: string; htmlBody: string }) =>
      request<import('../types').EmailTemplate>('/templates', {
        method: 'POST',
        body: JSON.stringify(data),
      }),
    update: (id: string, data: { name: string; subject: string; htmlBody: string }) =>
      request<import('../types').EmailTemplate>(`/templates/${id}`, {
        method: 'PUT',
        body: JSON.stringify(data),
      }),
    delete: (id: string) =>
      request<void>(`/templates/${id}`, { method: 'DELETE' }),
  },
  senders: {
    getAll: (activeOnly = false) =>
      request<import('../types').SenderIdentity[]>(`/senders${activeOnly ? '?activeOnly=true' : ''}`),
    create: (data: { email: string; displayName: string; replyToEmail?: string; isDefault: boolean }) =>
      request<import('../types').SenderIdentity>('/senders', {
        method: 'POST',
        body: JSON.stringify(data),
      }),
    update: (id: string, data: { email: string; displayName: string; replyToEmail?: string; isDefault: boolean; isActive: boolean }) =>
      request<import('../types').SenderIdentity>(`/senders/${id}`, {
        method: 'PUT',
        body: JSON.stringify(data),
      }),
    delete: (id: string) =>
      request<void>(`/senders/${id}`, { method: 'DELETE' }),
  },
  campaigns: {
    getAll: () => request<import('../types').Campaign[]>('/campaigns'),
    create: (data: {
      name: string;
      emailTemplateId: string;
      emailListId?: string;
      senderIdentityId?: string;
      subjectOverride?: string;
      scheduledAt?: string;
    }) =>
      request<import('../types').Campaign>('/campaigns', {
        method: 'POST',
        body: JSON.stringify(data),
      }),
    send: (id: string) =>
      request<import('../types').CampaignSendResult>(`/campaigns/${id}/send`, { method: 'POST' }),
    delete: (id: string) =>
      request<void>(`/campaigns/${id}`, { method: 'DELETE' }),
  },
};
