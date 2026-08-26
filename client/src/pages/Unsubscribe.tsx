import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { api } from '../api/client';
import type { UnsubscribeInfo } from '../types';

export function UnsubscribePage() {
  const { token } = useParams<{ token: string }>();
  const [info, setInfo] = useState<UnsubscribeInfo | null>(null);
  const [result, setResult] = useState<string | null>(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (!token) {
      setError('Invalid unsubscribe link.');
      setLoading(false);
      return;
    }

    api.unsubscribe.getInfo(token)
      .then(setInfo)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, [token]);

  const handleUnsubscribe = async () => {
    if (!token) return;

    setSubmitting(true);
    setError('');

    try {
      const response = await api.unsubscribe.unsubscribe(token);
      setResult(response.message);
      setInfo((prev) => prev ? { ...prev, isSubscribed: false } : prev);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unsubscribe failed');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return <div className="auth-page"><div className="loading">Loading...</div></div>;
  }

  return (
    <div className="auth-page">
      <div className="auth-card unsubscribe-card">
        <div className="brand auth-brand">
          <span className="brand-icon">LV</span>
          <div>
            <h1>LexVersus</h1>
            <p>Email Preferences</p>
          </div>
        </div>

        {error && <div className="alert error">{error}</div>}
        {result && <div className="alert success">{result}</div>}

        {info && !result && (
          <>
            <h2>Manage subscription</h2>
            <p className="hint">
              Email: <strong>{info.email}</strong>
            </p>

            {info.isSubscribed ? (
              <>
                <p>Click below to unsubscribe from future marketing emails.</p>
                <button
                  className="btn danger full"
                  onClick={handleUnsubscribe}
                  disabled={submitting}
                >
                  {submitting ? 'Processing...' : 'Unsubscribe'}
                </button>
              </>
            ) : (
              <p>You are already unsubscribed from our mailing list.</p>
            )}
          </>
        )}
      </div>
    </div>
  );
}
