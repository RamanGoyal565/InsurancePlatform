import { useEffect, useRef, useState } from 'react';
import {
  Avatar, Box, CircularProgress, Collapse, Divider,
  IconButton, Paper, TextField, Tooltip, Typography,
} from '@mui/material';
import SendIcon from '@mui/icons-material/Send';
import CloseIcon from '@mui/icons-material/Close';
import SmartToyIcon from '@mui/icons-material/SmartToy';
import PersonIcon from '@mui/icons-material/Person';
import MinimizeIcon from '@mui/icons-material/Remove';
import { askChatbot, checkChatbotHealth } from '../../api/chatbot';

const BOT_COLOR = '#002045';
const USER_COLOR = '#e65100';

/** Single chat message bubble */
function MessageBubble({ msg }) {
  const isBot = msg.role === 'bot';
  return (
    <Box
      sx={{
        display: 'flex',
        gap: 1,
        alignItems: 'flex-end',
        flexDirection: isBot ? 'row' : 'row-reverse',
        mb: 1.5,
      }}
    >
      <Avatar
        sx={{
          width: 28,
          height: 28,
          bgcolor: isBot ? BOT_COLOR : USER_COLOR,
          flexShrink: 0,
        }}
      >
        {isBot ? <SmartToyIcon sx={{ fontSize: 16 }} /> : <PersonIcon sx={{ fontSize: 16 }} />}
      </Avatar>

      <Box
        sx={{
          maxWidth: '78%',
          px: 1.75,
          py: 1,
          borderRadius: isBot ? '4px 12px 12px 4px' : '12px 4px 4px 12px',
          bgcolor: isBot ? '#f4f3f7' : BOT_COLOR,
          color: isBot ? '#1a1c1e' : '#fff',
          fontSize: 13.5,
          lineHeight: 1.55,
          whiteSpace: 'pre-wrap',
          wordBreak: 'break-word',
        }}
      >
        {msg.text}
      </Box>
    </Box>
  );
}

/** Animated typing indicator */
function TypingIndicator() {
  return (
    <Box sx={{ display: 'flex', gap: 1, alignItems: 'flex-end', mb: 1.5 }}>
      <Avatar sx={{ width: 28, height: 28, bgcolor: BOT_COLOR, flexShrink: 0 }}>
        <SmartToyIcon sx={{ fontSize: 16 }} />
      </Avatar>
      <Box
        sx={{
          px: 1.75, py: 1,
          borderRadius: '4px 12px 12px 4px',
          bgcolor: '#f4f3f7',
          display: 'flex',
          gap: 0.5,
          alignItems: 'center',
        }}
      >
        {[0, 1, 2].map((i) => (
          <Box
            key={i}
            sx={{
              width: 7, height: 7,
              borderRadius: '50%',
              bgcolor: '#74777f',
              animation: 'bounce 1.2s infinite',
              animationDelay: `${i * 0.2}s`,
              '@keyframes bounce': {
                '0%, 80%, 100%': { transform: 'scale(0.6)', opacity: 0.4 },
                '40%': { transform: 'scale(1)', opacity: 1 },
              },
            }}
          />
        ))}
      </Box>
    </Box>
  );
}

const WELCOME = {
  role: 'bot',
  text: "Hi! I'm your Trust Guard AI assistant. I can answer questions about your policies, coverage, claims, and more. How can I help you today?",
};

export default function ChatbotWidget() {
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState([WELCOME]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [online, setOnline] = useState(true);
  const bottomRef = useRef(null);
  const inputRef = useRef(null);

  // Check health on first open
  useEffect(() => {
    if (open) {
      checkChatbotHealth().then(setOnline);
      setTimeout(() => inputRef.current?.focus(), 100);
    }
  }, [open]);

  // Auto-scroll to bottom on new messages
  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, loading]);

  const sendMessage = async () => {
    const q = input.trim();
    if (!q || loading) return;

    setInput('');
    setMessages((prev) => [...prev, { role: 'user', text: q, sources: [] }]);
    setLoading(true);

    try {
      const data = await askChatbot(q);
      // Strip any "Sources: ..." suffix the backend appends to the answer text
      const cleanAnswer = (data.answer || '')
        .replace(/\n*Sources?:[\s\S]*/i, '')
        .trim();
      setMessages((prev) => [
        ...prev,
        { role: 'bot', text: cleanAnswer },
      ]);
      setOnline(true);
    } catch (err) {
      const isOffline = err.message.includes('fetch') || err.message.includes('Failed');
      setOnline(!isOffline);
      setMessages((prev) => [
        ...prev,
        {
          role: 'bot',
          text: isOffline
            ? "⚠️ I can't reach the AI service right now. Please check that the chatbot server is running."
            : `Sorry, I encountered an error: ${err.message}`,
        },
      ]);
    } finally {
      setLoading(false);
    }
  };

  const handleKey = (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      sendMessage();
    }
  };

  const clearChat = () => setMessages([WELCOME]);

  return (
    <>
      {/* ── Floating action button ── */}
      <Tooltip title="Ask AI Assistant" placement="left">
        <Box
          onClick={() => setOpen((v) => !v)}
          sx={{
            position: 'fixed',
            bottom: 28,
            right: 28,
            zIndex: 1300,
            width: 56,
            height: 56,
            borderRadius: '50%',
            bgcolor: BOT_COLOR,
            color: '#fff',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            cursor: 'pointer',
            boxShadow: '0 4px 20px rgba(0,32,69,0.35)',
            transition: 'transform 0.2s, box-shadow 0.2s',
            '&:hover': { transform: 'scale(1.08)', boxShadow: '0 6px 28px rgba(0,32,69,0.45)' },
          }}
        >
          {open ? <CloseIcon /> : <SmartToyIcon />}
          {/* Online indicator dot */}
          <Box
            sx={{
              position: 'absolute',
              top: 4, right: 4,
              width: 10, height: 10,
              borderRadius: '50%',
              bgcolor: online ? '#4caf50' : '#f44336',
              border: '2px solid #fff',
            }}
          />
        </Box>
      </Tooltip>

      {/* ── Chat panel ── */}
      <Collapse
        in={open}
        timeout={200}
        sx={{
          position: 'fixed',
          bottom: 96,
          right: 28,
          zIndex: 1299,
          width: { xs: 'calc(100vw - 32px)', sm: 380 },
          maxWidth: 420,
        }}
      >
        <Paper
          elevation={8}
          sx={{
            borderRadius: '12px',
            overflow: 'hidden',
            display: 'flex',
            flexDirection: 'column',
            height: 520,
            border: '1px solid rgba(0,32,69,0.12)',
          }}
        >
          {/* Header */}
          <Box
            sx={{
              bgcolor: BOT_COLOR,
              px: 2,
              py: 1.5,
              display: 'flex',
              alignItems: 'center',
              gap: 1.5,
              flexShrink: 0,
            }}
          >
            <Avatar sx={{ width: 32, height: 32, bgcolor: 'rgba(255,255,255,0.15)' }}>
              <SmartToyIcon sx={{ fontSize: 18, color: '#fff' }} />
            </Avatar>
            <Box sx={{ flexGrow: 1 }}>
              <Typography sx={{ color: '#fff', fontWeight: 700, fontSize: 14, lineHeight: 1.2 }}>
                Trust Guard AI Assistant
              </Typography>
              <Typography sx={{ color: 'rgba(255,255,255,0.65)', fontSize: 11 }}>
                {online ? '● Online' : '● Offline'}
              </Typography>
            </Box>
            <Tooltip title="Clear chat">
              <IconButton
                size="small"
                onClick={clearChat}
                sx={{ color: 'rgba(255,255,255,0.7)', '&:hover': { color: '#fff' } }}
              >
                <MinimizeIcon fontSize="small" />
              </IconButton>
            </Tooltip>
            <IconButton
              size="small"
              onClick={() => setOpen(false)}
              sx={{ color: 'rgba(255,255,255,0.7)', '&:hover': { color: '#fff' } }}
            >
              <CloseIcon fontSize="small" />
            </IconButton>
          </Box>

          {/* Messages */}
          <Box
            sx={{
              flexGrow: 1,
              overflowY: 'auto',
              px: 2,
              py: 1.5,
              bgcolor: '#faf9fd',
              '&::-webkit-scrollbar': { width: 4 },
              '&::-webkit-scrollbar-thumb': { bgcolor: '#c4c6cf', borderRadius: 2 },
            }}
          >
            {messages.map((msg, i) => (
              <MessageBubble key={i} msg={msg} />
            ))}
            {loading && <TypingIndicator />}
            <div ref={bottomRef} />
          </Box>

          {/* Suggested questions — shown only when chat is fresh */}
          {messages.length === 1 && !loading && (
            <>
              <Divider />
              <Box sx={{ px: 2, py: 1, bgcolor: '#fff', flexShrink: 0 }}>
                <Typography sx={{ fontSize: 11, color: '#74777f', mb: 0.75, fontWeight: 600 }}>
                  Suggested questions
                </Typography>
                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.75 }}>
                  {[
                    'What does my policy cover?',
                    'How do I file a claim?',
                    'What is the renewal process?',
                    'How many leave days are allowed?',
                  ].map((q) => (
                    <Box
                      key={q}
                      onClick={() => { setInput(q); inputRef.current?.focus(); }}
                      sx={{
                        px: 1.25, py: 0.5,
                        borderRadius: '9999px',
                        border: '1px solid #c4c6cf',
                        fontSize: 12,
                        color: '#43474e',
                        cursor: 'pointer',
                        bgcolor: '#fff',
                        '&:hover': { bgcolor: '#efedf1', borderColor: '#002045', color: '#002045' },
                        transition: 'all 0.12s',
                      }}
                    >
                      {q}
                    </Box>
                  ))}
                </Box>
              </Box>
            </>
          )}

          <Divider />

          {/* Input */}
          <Box
            sx={{
              px: 1.5,
              py: 1.25,
              display: 'flex',
              gap: 1,
              alignItems: 'flex-end',
              bgcolor: '#fff',
              flexShrink: 0,
            }}
          >
            <TextField
              inputRef={inputRef}
              fullWidth
              multiline
              maxRows={3}
              size="small"
              placeholder="Ask about policies, claims, coverage…"
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={handleKey}
              disabled={loading}
              sx={{
                '& .MuiOutlinedInput-root': {
                  borderRadius: '8px',
                  fontSize: 13.5,
                  '& fieldset': { borderColor: '#c4c6cf' },
                  '&:hover fieldset': { borderColor: '#002045' },
                  '&.Mui-focused fieldset': { borderColor: '#002045' },
                },
              }}
            />
            <IconButton
              onClick={sendMessage}
              disabled={!input.trim() || loading}
              sx={{
                bgcolor: BOT_COLOR,
                color: '#fff',
                width: 38,
                height: 38,
                borderRadius: '8px',
                flexShrink: 0,
                '&:hover': { bgcolor: '#1a365d' },
                '&.Mui-disabled': { bgcolor: '#c4c6cf', color: '#fff' },
              }}
            >
              {loading ? <CircularProgress size={16} color="inherit" /> : <SendIcon sx={{ fontSize: 18 }} />}
            </IconButton>
          </Box>
        </Paper>
      </Collapse>
    </>
  );
}
