const API = '/api';
const PAGE_SIZE = 12;
let currentPage = 0;
let totalPages = 0;
let totalElements = 0;

function renderNavAuth() {
  const el = document.getElementById('navAuth');
  if (!el) return;
  if (typeof AUTH !== 'undefined' && AUTH.isLoggedIn()) {
    el.innerHTML = '<button class="btn-logout" onclick="AUTH.clear();location.reload()">Đăng xuất</button>';
  } else {
    el.innerHTML = '<a href="/login.html" class="btn-login">Đăng nhập</a>';
  }
}

async function displayMovies(movies, meta) {
  const grid = document.getElementById('moviesGrid');
  if (!grid) return;
  const list = Array.isArray(movies) ? movies : (movies?.content || []);
  grid.innerHTML = list.map(m => `
    <div class="movie-card">
      <a href="/pages/movie-detail.html?id=${m.id}">
        <img src="${m.posterUrl || 'https://via.placeholder.com/250x350?text=No+Image'}" alt="${m.title}" class="movie-poster">
      </a>
      <div class="movie-info">
        <a href="/pages/movie-detail.html?id=${m.id}" style="text-decoration:none;color:inherit">
          <div class="movie-title">${m.title}</div>
        </a>
        <div class="movie-genre">${m.genre || 'N/A'}</div>
        <div class="movie-rating">⭐ ${m.rating != null ? m.rating : 'N/A'}</div>
        <p style="font-size:0.85rem;color:#888;margin-top:0.5rem">${m.durationMin || 0} phút</p>
        <div id="showtimes-${m.id}" style="font-size:0.85rem;color:#e50914;margin-top:0.5rem;min-height:1.5rem">
          <span style="color:#aaa">Đang tải suất chiếu...</span>
        </div>
        <a href="/pages/booking.html?movieId=${m.id}" class="btn btn-primary" style="width:100%;margin-top:0.5rem;text-align:center;display:block">Đặt vé</a>
      </div>
    </div>
  `).join('');

  // Load showtimes for each movie (non-blocking)
  list.forEach(m => loadShowtimesForCard(m.id));

  if (meta) {
    totalPages = meta.totalPages || 0;
    totalElements = meta.totalElements || list.length;
    renderPagination(meta);
    const countEl = document.getElementById('moviesCount');
    if (countEl) countEl.textContent = `Hiển thị ${list.length} / ${totalElements} phim`;
  }
}

async function loadShowtimesForCard(movieId) {
  try {
    const el = document.getElementById(`showtimes-${movieId}`);
    if (!el) return;
    
    const response = await fetch(`${API}/showtimes?movieId=${movieId}`);
    const showtimes = await response.json();
    
    if (!showtimes || showtimes.length === 0) {
      el.innerHTML = '<span style="color:#aaa">Chưa có suất chiếu</span>';
      return;
    }
    
    // Group showtimes by date
    const grouped = {};
    showtimes.forEach(s => {
      const date = new Date(s.startTime).toLocaleDateString('vi-VN', { month: '2-digit', day: '2-digit' });
      if (!grouped[date]) grouped[date] = [];
      grouped[date].push(s);
    });
    
    let html = '';
    for (const date in grouped) {
      const times = grouped[date].map(s => 
        new Date(s.startTime).toLocaleTimeString('vi-VN', {hour:'2-digit',minute:'2-digit'})
      ).join(', ');
      html += `<div style="margin-bottom:0.3rem"><strong>${date}:</strong> ${times}</div>`;
    }
    el.innerHTML = html;
  } catch (error) {
    console.error('Error loading showtimes:', error);
    const el = document.getElementById(`showtimes-${movieId}`);
    if (el) el.innerHTML = '<span style="color:#aaa">Lỗi tải suất chiếu</span>';
  }
}

function renderPagination(meta) {
  const pagination = document.getElementById('pagination');
  if (!pagination || totalPages <= 1) {
    if (pagination) pagination.innerHTML = '';
    return;
  }
  const pages = [];
  const p = currentPage;
  pages.push(`<button class="btn btn-outline" ${p === 0 ? 'disabled' : ''} onclick="goToPage(${p - 1})">Trước</button>`);
  for (let i = Math.max(0, p - 2); i < Math.min(totalPages, p + 3); i++) {
    pages.push(`<button class="btn ${i === p ? 'btn-primary' : 'btn-outline'}" onclick="goToPage(${i})">${i + 1}</button>`);
  }
  pages.push(`<button class="btn btn-outline" ${p >= totalPages - 1 ? 'disabled' : ''} onclick="goToPage(${p + 1})">Sau</button>`);
  pagination.innerHTML = pages.join('');
}

function goToPage(page) {
  if (page < 0 || page >= totalPages) return;
  currentPage = page;
  loadMovies();
}

function loadMovies() {
  const q = document.getElementById('searchInput')?.value?.trim();
  const genre = document.getElementById('genreSelect')?.value?.trim();
  if (q || genre) {
    searchMovies(currentPage);
    return;
  }
  const url = `${API}/movies?page=${currentPage}&size=${PAGE_SIZE}`;
  fetch(url)
    .then(res => res.json())
    .then(data => {
      const list = Array.isArray(data) ? data : (data?.content || []);
      const meta = {
        totalPages: data?.totalPages ?? 0,
        totalElements: data?.totalElements ?? list.length
      };
      displayMovies(list, meta);
    })
    .catch(err => {
      console.error(err);
      const el = document.getElementById('moviesGrid');
      if (el) el.innerHTML = '<p style="color:#888">Không tải được danh sách phim</p>';
      const pagEl = document.getElementById('pagination');
      if (pagEl) pagEl.innerHTML = '';
    });
}

function searchMovies(page = 0) {
  const q = document.getElementById('searchInput')?.value?.trim() || '';
  const genre = document.getElementById('genreSelect')?.value?.trim() || '';
  currentPage = page;
  const url = `${API}/movies/search?q=${encodeURIComponent(q)}&genre=${encodeURIComponent(genre)}&page=${page}&size=${PAGE_SIZE}`;
  fetch(url)
    .then(res => res.json())
    .then(data => {
      const list = Array.isArray(data) ? data : (data?.content || []);
      const meta = {
        totalPages: data?.totalPages ?? 0,
        totalElements: data?.totalElements ?? list.length
      };
      displayMovies(list, meta);
    })
    .catch(err => {
      console.error(err);
      const el = document.getElementById('moviesGrid');
      if (el) el.innerHTML = '<p style="color:#888">Không tìm thấy phim</p>';
      const pagEl = document.getElementById('pagination');
      if (pagEl) pagEl.innerHTML = '';
    });
}

document.addEventListener('DOMContentLoaded', () => {
  renderNavAuth();
  loadMovies();
  const si = document.getElementById('searchInput');
  const gs = document.getElementById('genreSelect');
  if (si) si.addEventListener('keypress', e => e.key === 'Enter' && (currentPage = 0, searchMovies(0)));
  if (gs) gs.addEventListener('change', () => { currentPage = 0; searchMovies(0); });
});
