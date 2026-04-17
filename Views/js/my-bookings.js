const API = '/api';

function renderNavAuth() {
  const el = document.getElementById('navAuth');
  if (!el) return;
  if (typeof AUTH !== 'undefined' && AUTH.isLoggedIn()) {
    el.innerHTML = '<button class="btn-logout" onclick="AUTH.clear();location.reload()">Đăng xuất</button>';
  } else {
    el.innerHTML = '<a href="/login?redirect=' + encodeURIComponent(location.pathname + location.search) + '" class="btn-login">Đăng nhập</a>';
  }
}

async function loadBookings() {
  if (!AUTH.isLoggedIn()) {
    document.getElementById('loginPrompt').style.display = 'block';
    const loginLink = document.getElementById('loginLink');
    if (loginLink) loginLink.href = '/login?redirect=' + encodeURIComponent(location.href);
    document.getElementById('bookingsList').innerHTML = '';
    return;
  }
  document.getElementById('loginPrompt').style.display = 'none';
  const res = await AUTH.fetchWithAuth(`${API}/bookings/my`);
  if (res.status === 401) {
    document.getElementById('loginPrompt').style.display = 'block';
    document.getElementById('bookingsList').innerHTML = '';
    return;
  }
  const list = await res.json();
  const container = document.getElementById('bookingsList');
  container.innerHTML = (list || []).map(b => {
    const showtimeStr = b.showtimeStartTime ? new Date(b.showtimeStartTime).toLocaleString('vi-VN', { dateStyle: 'medium', timeStyle: 'short' }) : '';
    const seatsStr = (b.seatLabels && b.seatLabels.length) ? b.seatLabels.join(', ') : (b.seatIds?.length ? b.seatIds.length + ' ghế' : '');
    const snacksStr = (b.snacks && b.snacks.length)
      ? b.snacks.map(s => `${s.name} x${s.quantity}`).join(', ')
      : '';
    return `
    <div class="booking-card" style="display:flex;flex-wrap:wrap;justify-content:space-between;align-items:flex-start;gap:1rem;padding:1.25rem;background:#1f1f1f;border-radius:12px;margin-bottom:1rem">
      <div style="flex:1;min-width:200px">
        <strong style="color:white;font-size:1.1rem">Mã vé: ${b.bookingCode}</strong>
        <p style="color:#e5e5e5;margin:0.5rem 0">${b.movieTitle || 'Phim'}</p>
        <p style="color:#888;margin:0.25rem 0">${b.cinemaName || ''} - ${b.roomName || ''}</p>
        <p style="color:#888;margin:0.25rem 0">${showtimeStr}</p>
        <p style="color:#888;margin:0.25rem 0">Ghế: ${seatsStr}</p>
        ${snacksStr ? `<p style="color:#888;margin:0.25rem 0">Đồ ăn: ${snacksStr}</p>` : ''}
        <p style="color:#e50914;font-weight:600;margin-top:0.5rem">${(b.totalPrice || 0).toLocaleString('vi-VN')} ₫</p>
        <p style="font-size:0.9rem;margin-top:0.5rem">Trạng thái: <span style="color:${b.status === 'CONFIRMED' ? '#10b981' : b.status === 'CANCELLED' ? '#ef4444' : '#f59e0b'}">${b.status === 'CONFIRMED' ? 'Đã thanh toán' : b.status === 'CANCELLED' ? 'Đã hủy' : 'Chờ thanh toán'}</span></p>
      </div>
      <div style="display:flex;flex-direction:column;gap:0.5rem">
        ${b.status === 'PENDING' ? `
          <a href="/pages/payment?code=${encodeURIComponent(b.bookingCode)}" class="btn btn-primary">Thanh toán</a>
          <button class="btn btn-outline" onclick="cancelBooking(${b.id})">Hủy vé</button>
        ` : b.movieId ? `<a href="/pages/movie-detail?id=${b.movieId}" class="btn btn-outline">Xem phim</a>` : ''}
      </div>
    </div>
  `}).join('') || '<p style="color:#888">Chưa có đơn đặt vé. <a href="/pages/booking" style="color:var(--primary)">Đặt vé ngay</a></p>';
}

async function cancelBooking(id) {
  if (!confirm('Bạn có chắc muốn hủy đơn vé này?')) return;
  const res = await AUTH.fetchWithAuth(`${API}/bookings/${id}/cancel`, { method: 'DELETE' });
  if (res.ok) loadBookings();
  else {
    const d = await res.json();
    alert(d.error || 'Hủy vé thất bại');
  }
}

document.addEventListener('DOMContentLoaded', () => {
  renderNavAuth();
  loadBookings();
});
