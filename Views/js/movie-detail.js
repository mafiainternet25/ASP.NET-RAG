const API = '/api';

function renderNavAuth() {
  const el = document.getElementById('navAuth');
  if (!el) return;
  if (typeof AUTH !== 'undefined' && AUTH.isLoggedIn()) {
    el.innerHTML = '<button class="btn-logout" onclick="AUTH.clear();location.reload()">Đăng xuất</button>';
  } else {
    el.innerHTML = '<a href="/login.html" class="btn-login">Đăng nhập</a>';
  }
}

async function loadMovie() {
  const id = new URLSearchParams(location.search).get('id');
  if (!id) {
    document.getElementById('movieDetail').innerHTML = '<p style="color:#888">Không tìm thấy phim</p>';
    return;
  }
  let m;
  try {
    m = await fetch(`${API}/movies/${id}`).then(r => {
      if (!r.ok) throw new Error('Không tải được');
      return r.json();
    });
  } catch (e) {
    document.getElementById('movieDetail').innerHTML = '<p style="color:#ef4444">Không tìm thấy phim hoặc lỗi tải dữ liệu.</p>';
    return;
  }
  document.getElementById('movieDetail').innerHTML = `
    <img src="${m.posterUrl || 'https://via.placeholder.com/300x450'}" alt="${m.title}" class="movie-detail-poster">
    <div class="movie-detail-info">
      <h1>${m.title}</h1>
      <div class="movie-detail-meta">
        ${m.genre || ''} | ${m.durationMin || 0} phút | ⭐ ${m.rating != null ? m.rating : 'N/A'} | ${m.status || ''}
      </div>
      <p style="color:#ccc;margin:1rem 0">${m.description || ''}</p>
      <div style="display:flex;gap:0.5rem;flex-wrap:wrap;margin-top:1rem">
        <a href="/pages/booking.html?movieId=${m.id}" class="btn btn-primary">Đặt vé ngay</a>
        ${m.trailerUrl ? `<a href="${m.trailerUrl}" target="_blank" rel="noopener" class="btn btn-outline">Xem trailer</a>` : ''}
      </div>
    </div>
  `;
  window._currentMovieId = id;
  loadShowtimesSection(id);
  loadReviews(id);
  const addForm = document.getElementById('addReviewForm');
  const loginPrompt = document.getElementById('reviewLoginPrompt');
  const loginLink = document.getElementById('reviewLoginLink');
  if (AUTH.isLoggedIn()) {
    if (addForm) addForm.style.display = 'block';
    if (loginPrompt) loginPrompt.style.display = 'none';
    initReviewStars();
  } else {
    if (addForm) addForm.style.display = 'none';
    if (loginPrompt) {
      loginPrompt.style.display = 'block';
      if (loginLink) loginLink.href = '/login.html?redirect=' + encodeURIComponent(location.href);
    }
  }
}

function initReviewStars() {
  const stars = Array.from(document.querySelectorAll('#reviewRatingStars .star'));
  const ratingInput = document.getElementById('reviewRating');
  if (!stars.length || !ratingInput) return;

  const updateStars = (value) => {
    stars.forEach(s => {
      const v = parseInt(s.dataset.value);
      s.classList.toggle('selected', v <= value);
    });
    ratingInput.value = value;
  };

  stars.forEach(star => {
    star.addEventListener('click', () => updateStars(parseInt(star.dataset.value)));
    star.addEventListener('mouseenter', () => {
      const hoverValue = parseInt(star.dataset.value);
      stars.forEach(s => s.classList.toggle('hover', parseInt(s.dataset.value) <= hoverValue));
    });
    star.addEventListener('mouseleave', () => {
      stars.forEach(s => s.classList.remove('hover'));
    });
  });

  updateStars(parseInt(ratingInput.value) || 5);
}


async function loadShowtimesSection(movieId) {
  const dateInput = document.getElementById('showtimeDate');
  const cinemaSelect = document.getElementById('showtimeCinema');
  if (!dateInput || !cinemaSelect) return;

  dateInput.min = new Date().toISOString().split('T')[0];
  if (!dateInput.value) dateInput.value = dateInput.min;

  // Load cinemas
  const cinemas = await fetch(`${API}/cinemas`).then(r => r.json()).catch(() => []);
  cinemaSelect.innerHTML = '<option value="">Tất cả rạp</option>' + (cinemas || []).map(c =>
    `<option value="${c.id}">${c.name}</option>`).join('');

  const load = async () => {
    const date = dateInput.value;
    const cinemaId = cinemaSelect.value;
    if (!movieId || !date) return;
    let url = `${API}/showtimes?movieId=${movieId}&date=${date}`;
    if (cinemaId) url += `&cinemaId=${cinemaId}`;
    const list = await fetch(url).then(r => r.json()).catch(() => []);
    const container = document.getElementById('showtimesList');
    container.innerHTML = (list || []).map(s => {
      const d = new Date(s.startTime);
      const roomName = s.roomName || 'Phòng';
      const cinemaName = s.cinemaName || '';
      const bookingUrl = `/pages/booking.html?movieId=${movieId}&date=${date}`;
      return `
        <div class="showtime-item" style="padding:1rem;background:#1f1f1f;border-radius:8px">
          <div style="font-weight:600;color:white">${d.toLocaleTimeString('vi-VN', {hour:'2-digit',minute:'2-digit'})}</div>
          <div style="color:#e50914;font-size:0.95rem">${(s.price || 0).toLocaleString('vi-VN')} ₫</div>
          <div style="color:#888;font-size:0.85rem;margin-top:0.25rem">${roomName}${cinemaName ? ' - ' + cinemaName : ''}</div>
          <a href="${bookingUrl}" class="btn btn-primary" style="margin-top:0.75rem;display:block;text-align:center">Đặt vé</a>
        </div>
      `;
    }).join('') || '<p style="color:#888;grid-column:1/-1">Chưa có suất chiếu</p>';
  };

  dateInput.addEventListener('change', load);
  cinemaSelect.addEventListener('change', load);
  load();
}

let _currentUserId = null;
async function getCurrentUserId() {
  if (_currentUserId !== null) return _currentUserId;
  if (!AUTH.isLoggedIn()) return null;
  try {
    const res = await AUTH.fetchWithAuth(`${API}/users/me`);
    if (res.ok) {
      const p = await res.json();
      _currentUserId = p.id;
    }
  } catch (e) {}
  return _currentUserId;
}

async function loadReviews(movieId) {
  let list = [];
  try {
    list = await fetch(`${API}/movies/${movieId}/reviews`).then(r => r.json());
  } catch (e) {}
  const currentUserId = await getCurrentUserId();
  const container = document.getElementById('reviewsList');
  container.innerHTML = (list || []).map(r => `
    <div class="review-item" style="padding:1rem;background:#1f1f1f;border-radius:8px;margin-bottom:0.75rem">
      <div style="display:flex;justify-content:space-between;align-items:flex-start">
        <div>
          <strong style="color:white">${r.username || 'User'}</strong> 
          <span class="rating">${'⭐'.repeat(r.rating || 0)}</span>
          <p style="margin:0.5rem 0;color:#ccc">${r.comment || ''}</p>
          <small style="color:#666">${r.createdAt ? new Date(r.createdAt).toLocaleString('vi-VN') : ''}</small>
        </div>
        ${currentUserId && r.userId === currentUserId ? `<button class="btn btn-outline" style="padding:0.25rem 0.5rem;font-size:0.8rem" onclick="deleteReview(${r.id}, ${movieId})">Xóa</button>` : ''}
      </div>
    </div>
  `).join('') || '<p style="color:#888">Chưa có đánh giá</p>';
}

async function deleteReview(reviewId, movieId) {
  if (!confirm('Xóa đánh giá này?')) return;
  const res = await AUTH.fetchWithAuth(`${API}/reviews/${reviewId}`, { method: 'DELETE' });
  if (res.ok) loadReviews(movieId);
  else {
    const d = await res.json().catch(() => ({}));
    alert(typeof d === 'string' ? d : (d.error || 'Không thể xóa'));
  }
}

async function submitReview() {
  const movieId = window._currentMovieId;
  if (!movieId) return;
  const rating = parseInt(document.getElementById('reviewRating').value);
  const comment = document.getElementById('reviewComment').value;
  const res = await AUTH.fetchWithAuth(`${API}/reviews`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...AUTH.getAuthHeader() },
    body: JSON.stringify({ movieId, rating, comment })
  });
  if (res.ok) {
    loadReviews(movieId);
    document.getElementById('reviewComment').value = '';
  } else {
    const d = await res.json();
    alert(d.error || 'Gửi đánh giá thất bại');
  }
}

document.addEventListener('DOMContentLoaded', () => {
  renderNavAuth();
  loadMovie();
});
