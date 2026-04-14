const API = '/api';
let selectedShowtimeId = null;
let selectedSeats = new Map();
let snackCatalog = [];
let selectedSnacks = new Map();

function showBookingFeedback(message, type = 'info') {
  const el = document.getElementById('bookingFeedback');
  if (!el) return;
  const colors = {
    info: '#93c5fd',
    success: '#4ade80',
    error: '#fca5a5'
  };
  el.style.display = 'block';
  el.style.color = colors[type] || colors.info;
  el.textContent = message;
}

function clearBookingFeedback() {
  const el = document.getElementById('bookingFeedback');
  if (!el) return;
  el.style.display = 'none';
  el.textContent = '';
}

function renderNavAuth() {
  const el = document.getElementById('navAuth');
  if (!el) return;
  if (typeof AUTH !== 'undefined' && AUTH.isLoggedIn()) {
    el.innerHTML = '<button class="btn-logout" onclick="AUTH.clear();location.reload()">Đăng xuất</button>';
  } else {
    el.innerHTML = '<a href="/login.html" class="btn-login">Đăng nhập</a>';
  }
  const notice = document.getElementById('guestNotice');
  if (notice) notice.style.display = AUTH.isLoggedIn() ? 'none' : 'block';
}

async function loadMoviesSelect() {
  const data = await fetch(`${API}/movies?size=100`).then(r => r.json());
  const list = data.content || data;
  const arr = Array.isArray(list) ? list : [];
  const select = document.getElementById('movieSelect');
  const params = new URLSearchParams(location.search);
  const preMovie = params.get('movieId');
  select.innerHTML = '<option value="">-- Chọn phim --</option>' + arr.map(m =>
    `<option value="${m.id}" ${preMovie == m.id ? 'selected' : ''}>${m.title}</option>`
  ).join('');
}

async function loadCinemasSelect() {
  const cinemas = await fetch(`${API}/cinemas`).then(r => r.json());
  const select = document.getElementById('cinemaSelect');
  select.innerHTML = '<option value="">-- Chọn rạp --</option>' + (cinemas || []).map(c =>
    `<option value="${c.id}">${c.name}</option>`
  ).join('');
}

async function loadSnacks() {
  const select = document.getElementById('snackSelect');
  const list = document.getElementById('snacksList');
  if (!select || !list) return;
  select.innerHTML = '<option value="">Đang tải đồ ăn vặt...</option>';
  list.innerHTML = '<p style="color:#888">Danh sách món đã chọn sẽ hiển thị ở đây</p>';

  try {
    const snacks = await fetch(`${API}/snacks`).then(r => r.json());
    snackCatalog = Array.isArray(snacks) ? snacks : [];
    renderSnackPicker();
  } catch (error) {
    console.error('Lỗi tải đồ ăn vặt:', error);
    select.innerHTML = '<option value="">Không tải được danh sách đồ ăn vặt</option>';
    list.innerHTML = '<p style="color:#ef4444">Không tải được danh sách đồ ăn vặt</p>';
  }
}

function renderSnackPicker() {
  const select = document.getElementById('snackSelect');
  if (!select) return;

  if (!snackCatalog.length) {
    select.innerHTML = '<option value="">Chưa có đồ ăn vặt nào được cấu hình</option>';
    renderSelectedSnacks();
    updateTotal();
    return;
  }

  select.innerHTML = '<option value="">-- Chọn đồ ăn vặt --</option>' + snackCatalog.map(snack => {
    const disabled = !snack.isAvailable || (snack.stock || 0) <= 0;
    const label = `${snack.name} - ${(snack.price || 0).toLocaleString('vi-VN')} ₫${disabled ? ' (hết hàng)' : ''}`;
    return `<option value="${snack.id}" ${disabled ? 'disabled' : ''}>${label}</option>`;
  }).join('');

  renderSelectedSnacks();
}

function getSnackSelection(snackId) {
  return selectedSnacks.get(snackId) || { quantity: 0 };
}

function renderSelectedSnacks() {
  const container = document.getElementById('snacksList');
  if (!container) return;

  if (!selectedSnacks.size) {
    container.innerHTML = '<p class="snack-summary-note">Chưa chọn món nào.</p>';
    updateTotal();
    return;
  }

  container.innerHTML = Array.from(selectedSnacks.entries()).map(([snackId, item]) => {
    const snack = snackCatalog.find(x => x.id === snackId);
    if (!snack) return '';
    const isCombo = (snack.category || '').toUpperCase() === 'COMBO';
    const qty = item.quantity || 0;
    const detailLine = `${snack.category || 'FOOD'} • ${(item.price || 0).toLocaleString('vi-VN')} ₫ / món${qty > 1 ? ` • ×${qty}` : ''}`;
    return `
      <div class="snack-selected-item ${isCombo ? 'combo' : ''}">
        <div class="snack-selected-main">
          <strong>${snack.name}</strong>
          <div class="snack-type-badge">${detailLine}</div>
        </div>
        <button type="button" class="snack-remove" aria-label="Xóa món" onclick="removeSnack(${snackId})">Xóa</button>
      </div>
    `;
  }).filter(Boolean).join('');

  updateTotal();
}

function addSelectedSnack() {
  clearBookingFeedback();
  const select = document.getElementById('snackSelect');
  const qtyInput = document.getElementById('snackQty');
  if (!select || !qtyInput) return;

  const snackId = parseInt(select.value, 10);
  const quantity = Math.max(1, parseInt(qtyInput.value, 10) || 1);
  if (!snackId) {
    showBookingFeedback('Vui long chon mon an', 'error');
    return;
  }

  const snack = snackCatalog.find(item => item.id === snackId);
  if (!snack || !snack.isAvailable || (snack.stock || 0) <= 0) {
    showBookingFeedback('Mon an nay hien khong kha dung', 'error');
    return;
  }

  const current = selectedSnacks.get(snackId)?.quantity || 0;
  const next = current + quantity;
  if (next > (snack.stock || 0)) {
    showBookingFeedback('Vuot qua so luong ton kho cua san pham', 'error');
    return;
  }

  selectedSnacks.set(snackId, { quantity: next, price: snack.price || 0, name: snack.name });
  select.value = '';
  qtyInput.value = '1';
  renderSelectedSnacks();
}

function removeSnack(snackId) {
  selectedSnacks.delete(snackId);
  renderSelectedSnacks();
}

function changeSnackQuantity(snackId, delta) {
  clearBookingFeedback();
  const snack = snackCatalog.find(item => item.id === snackId);
  if (!snack || !snack.isAvailable || (snack.stock || 0) <= 0) {
    showBookingFeedback('Do an vat nay hien khong kha dung', 'error');
    return;
  }

  const current = getSnackSelection(snackId).quantity || 0;
  const next = current + delta;

  if (next <= 0) {
    selectedSnacks.delete(snackId);
  } else if (next > (snack.stock || 0)) {
    showBookingFeedback('Vuot qua so luong ton kho cua san pham', 'error');
    return;
  } else {
    selectedSnacks.set(snackId, {
      quantity: next,
      price: snack.price || 0,
      name: snack.name
    });
  }

  renderSelectedSnacks();
}

async function loadShowtimes() {
  const movieId = document.getElementById('movieSelect').value;
  const cinemaId = document.getElementById('cinemaSelect').value;
  const date = document.getElementById('dateSelect').value;
  if (!movieId || !date) {
    alert('Vui lòng chọn phim và ngày');
    return;
  }
  let url = `${API}/showtimes?movieId=${movieId}&date=${date}`;
  if (cinemaId) url += `&cinemaId=${cinemaId}`;
  const showtimes = await fetch(url).then(r => r.json());
  const list = document.getElementById('showtimesList');
  list.innerHTML = (showtimes || []).map(s => `
    <div class="showtime-item" onclick="selectShowtime(${s.id}, this)">
      <div><strong>${new Date(s.startTime).toLocaleTimeString('vi-VN', {hour:'2-digit',minute:'2-digit'})}</strong></div>
      <div style="color:#e50914;font-weight:600">${(s.price || 0).toLocaleString('vi-VN')} ₫</div>
    </div>
  `).join('');
  document.getElementById('seatsSection').style.display = 'none';
}

function selectShowtime(id, el) {
  clearBookingFeedback();
  if (!AUTH.isLoggedIn()) {
    if (confirm('Bạn cần đăng nhập để chọn ghế. Đi đến trang đăng nhập?')) {
      location.href = '/login.html?redirect=' + encodeURIComponent(location.href);
    }
    return;
  }
  selectedShowtimeId = id;
  document.querySelectorAll('.showtime-item').forEach(x => x.classList.remove('selected'));
  el.classList.add('selected');
  selectedSeats.clear();
  loadSeats(id);
}

async function loadSeats(showtimeId) {
  try {
    const response = await fetch(`${API}/showtimes/${showtimeId}/seats`);
    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new Error(errorData.message || `Lỗi tải ghế: ${response.statusText}`);
    }
    const data = await response.json();
    const grid = document.getElementById('seatsGrid');
    const showtimeInfo = document.getElementById('showtimeInfo');

    const rows = data?.rows || [];
    if (rows.length === 0) {
      grid.innerHTML = '<p style="grid-column:1/-1;color:#e50914;text-align:center;margin:2rem 0">Suất chiếu này chưa có ghế được thiết lập</p>';
      showtimeInfo.textContent = `${data?.roomName || 'Phòng'} - Chưa có ghế`;
    } else {
      showtimeInfo.textContent = `${data?.roomName || 'Phòng'} - Chọn ghế của bạn`;
      grid.innerHTML = rows.map(r => (r.seats || []).map(s => {
        const booked = s.status === 'BOOKED' || s.status === 'LOCKED';
        return `<div class="seat ${booked ? 'booked' : 'available'}"
          data-id="${s.id}" data-price="${s.price}"
          ${booked ? '' : 'onclick="toggleSeat(this)"'}>${r.row}${s.number}</div>`;
      }).join('')).join('');
    }
    document.getElementById('seatsSection').style.display = 'block';
    updateTotal();
  } catch (error) {
    console.error('Lỗi tải ghế:', error);
    const grid = document.getElementById('seatsGrid');
    const showtimeInfo = document.getElementById('showtimeInfo');
    grid.innerHTML = `<p style="grid-column:1/-1;color:#e50914;text-align:center;margin:2rem 0">${error.message || 'Lỗi tải sơ đồ ghế'}</p>`;
    showtimeInfo.textContent = 'Lỗi tải ghế';
    document.getElementById('seatsSection').style.display = 'block';
  }
}

function toggleSeat(el) {
  clearBookingFeedback();
  if (el.classList.contains('booked')) return;
  el.classList.toggle('selected');
  const id = parseInt(el.dataset.id, 10);
  const price = parseFloat(el.dataset.price);
  if (el.classList.contains('selected')) selectedSeats.set(id, price);
  else selectedSeats.delete(id);
  updateTotal();
}

function updateTotal() {
  let total = 0;
  selectedSeats.forEach(p => total += p);
  selectedSnacks.forEach(item => {
    total += (item.price || 0) * (item.quantity || 0);
  });
  const el = document.getElementById('totalPrice');
  if (el) {
    el.textContent = total.toLocaleString('vi-VN') + ' ₫';
  }
}

async function confirmBooking() {
  clearBookingFeedback();
  if (!selectedShowtimeId) {
    showBookingFeedback('Vui long chon suat chieu', 'error');
    alert('Vui lòng chọn suất chiếu');
    return;
  }
  const seatIds = Array.from(selectedSeats.keys());
  if (seatIds.length === 0) {
    showBookingFeedback('Vui long chon it nhat mot ghe', 'error');
    alert('Vui lòng chọn ít nhất một ghế');
    return;
  }
  const confirmBtn = document.getElementById('confirmBookingBtn');
  if (confirmBtn) {
    confirmBtn.setAttribute('disabled', 'true');
    confirmBtn.textContent = 'Dang xu ly...';
  }
  showBookingFeedback('Dang gui yeu cau dat ve...', 'info');

  const opts = {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...AUTH.getAuthHeader() },
    body: JSON.stringify({
      showtimeId: selectedShowtimeId,
      seatIds,
      snacks: Array.from(selectedSnacks.entries()).map(([snackId, item]) => ({
        snackId,
        quantity: item.quantity
      })),
      promotionCode: document.getElementById('promoCode')?.value?.trim() || undefined
    })
  };
  try {
    const res = await AUTH.fetchWithAuth(`${API}/bookings`, opts);
    const data = await res.json().catch(() => ({}));

    if (res.ok) {
      if (!data.bookingCode) {
        showBookingFeedback('Khong nhan duoc ma dat ve, vui long thu lai', 'error');
        alert('Không nhận được mã đặt vé, vui lòng thử lại');
        return;
      }
      showBookingFeedback('Dat ve thanh cong, dang chuyen sang trang thanh toan...', 'success');
      window.location.href = `/pages/payment.html?code=${encodeURIComponent(data.bookingCode)}`;
      return;
    }

    if (res.status === 401 || res.status === 403) {
      AUTH.clear();
      alert('Vui lòng đăng nhập lại để đặt vé');
      location.href = '/login.html?redirect=' + encodeURIComponent(location.href);
      return;
    }

    showBookingFeedback(data.error || data.message || 'Dat ve that bai', 'error');
    alert(data.error || data.message || 'Đặt vé thất bại');
  } catch (err) {
    console.error('Lỗi xác nhận đặt vé:', err);
    showBookingFeedback('Khong the ket noi may chu, vui long thu lai', 'error');
    alert('Không thể kết nối máy chủ, vui lòng thử lại');
  } finally {
    if (confirmBtn) {
      confirmBtn.removeAttribute('disabled');
      confirmBtn.textContent = 'Xac nhan dat ve';
    }
  }
}

document.addEventListener('DOMContentLoaded', () => {
  renderNavAuth();
  const dateInput = document.getElementById('dateSelect');
  const params = new URLSearchParams(location.search);
  if (dateInput) {
    dateInput.min = new Date().toISOString().split('T')[0];
    dateInput.value = params.get('date') || dateInput.min;
  }
  loadMoviesSelect();
  loadCinemasSelect();
  loadSnacks();
});

window.loadShowtimes = loadShowtimes;
window.selectShowtime = selectShowtime;
window.toggleSeat = toggleSeat;
window.addSelectedSnack = addSelectedSnack;
window.removeSnack = removeSnack;
window.changeSnackQuantity = changeSnackQuantity;
window.confirmBooking = confirmBooking;
