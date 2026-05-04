/**
 * Chatbot API — calls the FastAPI RAG service at http://127.0.0.1:8000
 * The service runs separately from the main backend (port 5000).
 */

const CHATBOT_BASE = import.meta.env.VITE_CHATBOT_URL || 'http://127.0.0.1:8000';

/**
 * Ask a question to the RAG chatbot.
 * @param {string} question
 * @param {number} topK  number of chunks to retrieve (default 4)
 * @returns {Promise<{ answer: string, sources: Array }>}
 */
export async function askChatbot(question, topK = 4) {
  const res = await fetch(`${CHATBOT_BASE}/api/ask`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ question, top_k: topK }),
  });

  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.detail || `Chatbot error: ${res.status}`);
  }

  return res.json();
}

/**
 * Check if the chatbot service is reachable.
 * @returns {Promise<boolean>}
 */
export async function checkChatbotHealth() {
  try {
    const res = await fetch(`${CHATBOT_BASE}/api/health`, { signal: AbortSignal.timeout(3000) });
    return res.ok;
  } catch {
    return false;
  }
}
