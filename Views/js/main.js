const API = '/api';
const chatHistory = [];

function renderNavAuth() {
  const el = document.getElementById('navAuth');
  if (!el) return;
  if (AUTH.isLoggedIn()) {
    el.innerHTML = '<button class="btn-logout" onclick="logout()">Đăng xuất</button>';
  } else {
    el.innerHTML = '<a href="/login" class="btn-login">Đăng nhập</a>';
  }
}

function logout() {
  AUTH.clear();
  window.location.reload();
}

async function loadNowPlaying() {
  try {
    const [playing, coming] = await Promise.all([
      fetch(`${API}/movies/now-playing`).then(r => r.json()),
      fetch(`${API}/movies/coming-soon`).then(r => r.json())
    ]);
    const gridPlaying = document.getElementById('featuredMovies');
    if (gridPlaying) {
      gridPlaying.innerHTML = (Array.isArray(playing) ? playing : []).map(m => movieCardHtml(m)).join('');
      if (!gridPlaying.innerHTML) gridPlaying.innerHTML = '<p style="color:#888">Chưa có phim đang chiếu</p>';
    }
    const gridComing = document.getElementById('comingSoonMovies');
    if (gridComing) {
      gridComing.innerHTML = (Array.isArray(coming) ? coming : []).map(m => movieCardHtml(m)).join('');
      if (!gridComing.innerHTML) gridComing.innerHTML = '<p style="color:#888">Chưa có phim sắp chiếu</p>';
    }
  } catch (e) {
    console.error(e);
    const el = document.getElementById('featuredMovies');
    if (el) el.innerHTML = '<p style="color:#888">Chưa có phim đang chiếu hoặc lỗi tải dữ liệu</p>';
    const el2 = document.getElementById('comingSoonMovies');
    if (el2) el2.innerHTML = '<p style="color:#888">Lỗi tải dữ liệu</p>';
  }
}

function movieCardHtml(m) {
  return `
    <div class="movie-card">
      <a href="/pages/movie-detail?id=${m.id}">
        <img src="${m.posterUrl || 'https://via.placeholder.com/250x350?text=No+Image'}" alt="${m.title}" class="movie-poster">
      </a>
      <div class="movie-info">
        <a href="/pages/movie-detail?id=${m.id}" style="text-decoration:none;color:inherit">
          <div class="movie-title">${m.title}</div>
        </a>
        <div class="movie-genre">${m.genre || 'N/A'}</div>
        <div class="movie-rating">⭐ ${m.rating != null ? m.rating : 'N/A'}</div>
        <p style="font-size:0.85rem;color:#888;margin-top:0.5rem">${m.durationMin || 0} phút</p>
        <a href="/pages/booking?movieId=${m.id}" class="btn btn-primary" style="width:100%;margin-top:0.5rem;text-align:center;display:block">Đặt vé</a>
      </div>
    </div>
  `;
}

function appendChatMessage(role, text) {
  const box = document.getElementById('chatMessages');
  if (!box) return;
  const div = document.createElement('div');
  div.className = `chat-message ${role === 'user' ? 'user' : 'bot'}`;
  div.textContent = text || '';
  box.appendChild(div);
  box.scrollTop = box.scrollHeight;
}

async function sendChatMessage(rawText) {
  const message = (rawText || '').trim();
  if (!message) return;

  appendChatMessage('user', message);

  const payload = {
    message,
    history: chatHistory.slice(-8)
  };

  const sendBtn = document.getElementById('chatSendBtn');
  if (sendBtn) sendBtn.disabled = true;

  try {
    const res = await fetch('/api/chat', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });

    if (!res.ok) {
      throw new Error(`HTTP ${res.status}`);
    }

    const data = await res.json();
    const reply = data && data.reply ? data.reply : 'Xin lỗi, chưa có dữ liệu phản hồi.';

    chatHistory.push({ role: 'user', content: message });
    chatHistory.push({ role: 'assistant', content: reply });

    appendChatMessage('bot', reply);
  } catch (e) {
    console.error(e);
    appendChatMessage('bot', 'Đã có lỗi khi gọi AI. Vui lòng thử lại sau ít phút.');
  } finally {
    if (sendBtn) sendBtn.disabled = false;
  }
}

function initHomeChatWidget() {
  const widget = document.getElementById('chatWidget');
  const toggleBtn = document.getElementById('chatToggleBtn');
  const closeBtn = document.getElementById('chatCloseBtn');
  const form = document.getElementById('chatForm');
  const input = document.getElementById('chatInput');

  if (!widget || !toggleBtn || !closeBtn || !form || !input) {
    return;
  }

  toggleBtn.addEventListener('click', () => {
    widget.classList.add('open');
    input.focus();
  });

  closeBtn.addEventListener('click', () => {
    widget.classList.remove('open');
  });

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    const text = input.value;
    input.value = '';
    await sendChatMessage(text);
    input.focus();
  });
}

document.addEventListener('DOMContentLoaded', () => {
  renderNavAuth();
  loadNowPlaying();
  initHomeChatWidget();
});
