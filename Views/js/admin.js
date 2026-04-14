const API = '/api/admin';

/** Danh sách snack từ API — dùng cho nút Sửa (tránh onclick lồng chuỗi dễ lỗi). */
let adminSnackCatalog = [];

function escapeHtml(text) {
  return String(text ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

function renderNavAuth() {
  const el = document.getElementById('navAuth');
  if (!el) return;
  if (typeof AUTH !== 'undefined' && AUTH.isLoggedIn()) {
    el.innerHTML = '<button class="btn-logout" onclick="AUTH.clear();location.href=\'/\'">Đăng xuất</button>';
  } else {
    el.innerHTML = '<a href="/login.html" class="btn-login">Đăng nhập</a>';
  }
}

async function fetchAdmin(url, opts = {}) {
  const res = await AUTH.fetchWithAuth(url, { ...opts, headers: { 'Content-Type': 'application/json', ...opts.headers } });
  if (res.status === 403) throw new Error('FORBIDDEN');
  return res;
}

async function initAdmin() {
  if (!AUTH.isLoggedIn()) {
    location.href = '/login.html?redirect=' + encodeURIComponent(location.href);
    return;
  }
  try {
    await fetchAdmin(API + '/cinemas');
    document.getElementById('adminDenied').style.display = 'none';
    document.getElementById('adminContent').style.display = 'block';
    setupTabs();
    loadMovies();
    loadCinemas();
    loadRooms();
    loadShowtimes();
    loadPromotions();
    loadSnacks();
    loadUsers();
    loadSelects();
    setupReportDates();
    openAdminTabFromHash();
  } catch (e) {
    document.getElementById('adminDenied').style.display = 'block';
    document.getElementById('adminContent').style.display = 'none';
  }
}

function openAdminTab(tabName) {
  const tab = document.querySelector(`.admin-tab[data-tab="${tabName}"]`);
  const panel = document.getElementById('panel-' + tabName);
  if (!tab || !panel) return;
  document.querySelectorAll('.admin-tab').forEach(x => x.classList.remove('active'));
  document.querySelectorAll('.admin-panel').forEach(x => x.classList.remove('active'));
  tab.classList.add('active');
  panel.classList.add('active');
  if (tabName === 'snacks') loadSnacks();
}

function openAdminTabFromHash() {
  const tabName = (location.hash || '').replace('#', '').trim();
  if (tabName) openAdminTab(tabName);
}

function setupTabs() {
  document.querySelectorAll('.admin-tab').forEach(t => {
    t.onclick = () => {
      location.hash = t.dataset.tab;
      openAdminTab(t.dataset.tab);
    };
  });

  window.addEventListener('hashchange', openAdminTabFromHash);
}

function setupReportDates() {
  const d = new Date();
  document.getElementById('repFrom').value = new Date(d.getFullYear(), d.getMonth(), 1).toISOString().split('T')[0];
  document.getElementById('repTo').value = d.toISOString().split('T')[0];
}

async function loadSelects() {
  try {
    const [cinemas, movies] = await Promise.all([
      fetchAdmin(API + '/cinemas').then(r => r.json()),
      fetchAdmin(API + '/movies').then(r => r.json())
    ]);
    const rSel = document.getElementById('rCinemaId');
    rSel.innerHTML = '<option value="">-- Chọn rạp --</option>' + (cinemas || []).map(c =>
      `<option value="${c.id}">${c.name}</option>`).join('');
    const sMovie = document.getElementById('sMovieId');
    sMovie.innerHTML = '<option value="">-- Chọn phim --</option>' + (movies || []).map(m =>
      `<option value="${m.id}">${m.title}</option>`).join('');
    const rooms = await fetchAdmin(API + '/rooms').then(r => r.json());
    const sRoom = document.getElementById('sRoomId');
    sRoom.innerHTML = '<option value="">-- Chọn phòng --</option>' + (rooms || []).map(r =>
      `<option value="${r.id}">${r.cinemaName} - ${r.name}</option>`).join('');
  } catch (e) {}
}

// Movies
async function loadMovies() {
  try {
    const list = await fetchAdmin(API + '/movies').then(r => r.json());
    document.getElementById('moviesTable').innerHTML = `
      <table class="admin-table">
        <tr><th>ID</th><th>Tên</th><th>Thể loại</th><th>Trạng thái</th><th></th></tr>
        ${(list || []).map(m => `
          <tr><td>${m.id}</td><td>${m.title}</td><td>${m.genre || ''}</td><td>${m.status || ''}</td>
          <td>
            <button class="btn btn-outline" onclick="editMovie(${m.id}, '${m.title}', '${m.genre || ''}', ${m.durationMin || 0}, '${m.posterUrl || ''}', '${m.status || 'NOW_SHOWING'}')">Sửa</button>
            <button class="btn btn-outline" onclick="deleteMovie(${m.id})">Xóa</button>
          </td></tr>
        `).join('')}
      </table>`;
  } catch (e) {
    document.getElementById('moviesTable').innerHTML = '<p style="color:#888">Không tải được</p>';
  }
}

async function addMovie() {
  const body = {
    title: document.getElementById('mTitle').value,
    genre: document.getElementById('mGenre').value,
    durationMin: parseInt(document.getElementById('mDuration').value) || null,
    posterUrl: document.getElementById('mPoster').value || null,
    status: document.getElementById('mStatus').value,
    description: '',
    rating: 0
  };
  if (!body.title) { alert('Nhập tên phim'); return; }
  const res = await fetchAdmin(API + '/movies', { method: 'POST', body: JSON.stringify(body) });
  if (res.ok) { loadMovies(); loadSelects(); clearMovieForm(); }
  else alert((await res.json()).error || 'Lỗi');
}

function editMovie(id, title, genre, duration, poster, status) {
  document.getElementById('mTitle').value = title;
  document.getElementById('mGenre').value = genre;
  document.getElementById('mDuration').value = duration || '';
  document.getElementById('mPoster').value = poster;
  document.getElementById('mStatus').value = status;
  
  const btn = document.querySelector('#panel-movies button[onclick*="addMovie"]');
  btn.textContent = 'Cập nhật phim';
  btn.onclick = () => updateMovie(id);
}

function clearMovieForm() {
  document.getElementById('mTitle').value = '';
  document.getElementById('mGenre').value = '';
  document.getElementById('mDuration').value = '';
  document.getElementById('mPoster').value = '';
  document.getElementById('mStatus').value = 'NOW_SHOWING';
  
  const btn = document.querySelector('#panel-movies button[onclick*="updateMovie"]');
  if (btn) {
    btn.textContent = 'Thêm phim';
    btn.onclick = addMovie;
  }
}

async function updateMovie(id) {
  const body = {
    title: document.getElementById('mTitle').value,
    genre: document.getElementById('mGenre').value,
    durationMin: parseInt(document.getElementById('mDuration').value) || null,
    posterUrl: document.getElementById('mPoster').value || null,
    status: document.getElementById('mStatus').value,
    description: '',
    rating: 0
  };
  if (!body.title) { alert('Nhập tên phim'); return; }
  const res = await fetchAdmin(API + '/movies/' + id, { method: 'PUT', body: JSON.stringify(body) });
  if (res.ok) { loadMovies(); loadSelects(); clearMovieForm(); }
  else alert((await res.json()).error || 'Lỗi');
}

async function deleteMovie(id) {
  if (!confirm('Xóa phim này?')) return;
  const res = await fetchAdmin(API + '/movies/' + id, { method: 'DELETE' });
  if (res.ok) { loadMovies(); loadSelects(); }
  else alert((await res.json()).message || 'Lỗi');
}

// Cinemas
async function loadCinemas() {
  try {
    const list = await fetchAdmin(API + '/cinemas').then(r => r.json());
    document.getElementById('cinemasTable').innerHTML = `
      <table class="admin-table">
        <tr><th>ID</th><th>Tên</th><th>Địa chỉ</th><th>TP</th><th></th></tr>
        ${(list || []).map(c => `
          <tr><td>${c.id}</td><td>${c.name}</td><td>${c.address || ''}</td><td>${c.city || ''}</td>
          <td>
            <button class="btn btn-outline" onclick="editCinema(${c.id}, '${c.name}', '${c.address || ''}', '${c.city || ''}')">Sửa</button>
            <button class="btn btn-outline" onclick="deleteCinema(${c.id})">Xóa</button>
          </td></tr>
        `).join('')}
      </table>`;
  } catch (e) {
    document.getElementById('cinemasTable').innerHTML = '<p style="color:#888">Không tải được</p>';
  }
}

async function addCinema() {
  const body = {
    name: document.getElementById('cName').value,
    address: document.getElementById('cAddress').value || null,
    city: document.getElementById('cCity').value || null
  };
  if (!body.name) { alert('Nhập tên rạp'); return; }
  const res = await fetchAdmin(API + '/cinemas', { method: 'POST', body: JSON.stringify(body) });
  if (res.ok) { loadCinemas(); loadSelects(); clearCinemaForm(); }
  else alert((await res.json()).error || 'Lỗi');
}

function editCinema(id, name, address, city) {
  document.getElementById('cName').value = name;
  document.getElementById('cAddress').value = address;
  document.getElementById('cCity').value = city;
  
  const btn = document.querySelector('#panel-cinemas button[onclick*="addCinema"]');
  btn.textContent = 'Cập nhật rạp';
  btn.onclick = () => updateCinema(id);
}

function clearCinemaForm() {
  document.getElementById('cName').value = '';
  document.getElementById('cAddress').value = '';
  document.getElementById('cCity').value = '';
  
  const btn = document.querySelector('#panel-cinemas button[onclick*="updateCinema"]');
  if (btn) {
    btn.textContent = 'Thêm rạp';
    btn.onclick = addCinema;
  }
}

async function updateCinema(id) {
  const body = {
    name: document.getElementById('cName').value,
    address: document.getElementById('cAddress').value || null,
    city: document.getElementById('cCity').value || null
  };
  if (!body.name) { alert('Nhập tên rạp'); return; }
  const res = await fetchAdmin(API + '/cinemas/' + id, { method: 'PUT', body: JSON.stringify(body) });
  if (res.ok) { loadCinemas(); loadSelects(); clearCinemaForm(); }
  else alert((await res.json()).error || 'Lỗi');
}

async function deleteCinema(id) {
  if (!confirm('Xóa rạp này?')) return;
  const res = await fetchAdmin(API + '/cinemas/' + id, { method: 'DELETE' });
  if (res.ok) { loadCinemas(); loadSelects(); }
  else alert((await res.json()).message || 'Lỗi');
}

// Rooms
async function loadRooms() {
  try {
    const list = await fetchAdmin(API + '/rooms').then(r => r.json());
    document.getElementById('roomsTable').innerHTML = `
      <table class="admin-table">
        <tr><th>ID</th><th>Rạp</th><th>Phòng</th><th>Số ghế</th><th>Loại</th><th></th></tr>
        ${(list || []).map(r => `
          <tr><td>${r.id}</td><td>${r.cinemaName}</td><td>${r.name}</td><td>${r.totalSeats}</td><td>${r.roomType || ''}</td>
          <td>
            <button class="btn btn-outline" onclick="editRoom(${r.id}, ${r.cinemaId}, '${r.name}', ${r.totalSeats}, '${r.roomType || 'NORMAL'}')">Sửa</button>
            <button class="btn btn-outline" onclick="deleteRoom(${r.id})">Xóa</button>
          </td></tr>
        `).join('')}
      </table>`;
  } catch (e) {
    document.getElementById('roomsTable').innerHTML = '<p style="color:#888">Không tải được</p>';
  }
}

async function addRoom() {
  const cinemaId = document.getElementById('rCinemaId').value;
  const body = {
    cinemaId: cinemaId ? parseInt(cinemaId) : null,
    name: document.getElementById('rName').value,
    totalSeats: parseInt(document.getElementById('rTotalSeats').value) || 100,
    roomType: document.getElementById('rRoomType').value || 'NORMAL'
  };
  if (!body.cinemaId || !body.name) { alert('Chọn rạp và nhập tên phòng'); return; }
  const res = await fetchAdmin(API + '/rooms', { method: 'POST', body: JSON.stringify(body) });
  if (res.ok) { loadRooms(); loadSelects(); clearRoomForm(); }
  else alert((await res.json()).message || 'Lỗi');
}

function editRoom(id, cinemaId, name, totalSeats, roomType) {
  document.getElementById('rCinemaId').value = cinemaId;
  document.getElementById('rName').value = name;
  document.getElementById('rTotalSeats').value = totalSeats;
  document.getElementById('rRoomType').value = roomType;
  
  const btn = document.querySelector('#panel-rooms button[onclick*="addRoom"]');
  btn.textContent = 'Cập nhật phòng';
  btn.onclick = () => updateRoom(id);
}

function clearRoomForm() {
  document.getElementById('rCinemaId').value = '';
  document.getElementById('rName').value = '';
  document.getElementById('rTotalSeats').value = '';
  document.getElementById('rRoomType').value = 'NORMAL';
  
  const btn = document.querySelector('#panel-rooms button[onclick*="updateRoom"]');
  if (btn) {
    btn.textContent = 'Thêm phòng';
    btn.onclick = addRoom;
  }
}

async function updateRoom(id) {
  const cinemaId = document.getElementById('rCinemaId').value;
  const body = {
    cinemaId: cinemaId ? parseInt(cinemaId) : null,
    name: document.getElementById('rName').value,
    totalSeats: parseInt(document.getElementById('rTotalSeats').value) || 100,
    roomType: document.getElementById('rRoomType').value || 'NORMAL'
  };
  if (!body.cinemaId || !body.name) { alert('Chọn rạp và nhập tên phòng'); return; }
  const res = await fetchAdmin(API + '/rooms/' + id, { method: 'PUT', body: JSON.stringify(body) });
  if (res.ok) { loadRooms(); loadSelects(); clearRoomForm(); }
  else alert((await res.json()).message || 'Lỗi');
}

async function deleteRoom(id) {
  if (!confirm('Xóa phòng này?')) return;
  const res = await fetchAdmin(API + '/rooms/' + id, { method: 'DELETE' });
  if (res.ok) { loadRooms(); loadSelects(); }
  else alert((await res.json()).message || 'Lỗi');
}

// Showtimes
async function loadShowtimes() {
  try {
    const list = await fetchAdmin(API + '/showtimes').then(r => r.json());
    document.getElementById('showtimesTable').innerHTML = `
      <table class="admin-table">
        <tr><th>ID</th><th>Phim</th><th>Phòng</th><th>Giờ</th><th>Giá</th><th></th></tr>
        ${(list || []).map(s => `
          <tr><td>${s.id}</td><td>${s.movieId}</td><td>${s.roomId}</td>
          <td>${s.startTime ? new Date(s.startTime).toLocaleString('vi-VN') : ''}</td>
          <td>${(s.price || 0).toLocaleString('vi-VN')} ₫</td>
          <td>
            <button class="btn btn-outline" onclick="editShowtime(${s.id}, ${s.movieId}, ${s.roomId}, '${s.startTime}', ${s.price || 75000})">Sửa</button>
            <button class="btn btn-outline" onclick="deleteShowtime(${s.id})">Xóa</button>
          </td></tr>
        `).join('')}
      </table>`;
  } catch (e) {
    document.getElementById('showtimesTable').innerHTML = '<p style="color:#888">Không tải được</p>';
  }
}

async function addShowtime() {
  const movieId = document.getElementById('sMovieId').value;
  const roomId = document.getElementById('sRoomId').value;
  const startVal = document.getElementById('sStartTime').value;
  const body = {
    movieId: movieId ? parseInt(movieId) : null,
    roomId: roomId ? parseInt(roomId) : null,
    startTime: startVal ? startVal + ':00' : null,
    price: parseFloat(document.getElementById('sPrice').value) || 75000
  };
  if (!body.movieId || !body.roomId || !body.startTime) {
    alert('Chọn phim, phòng và nhập giờ chiếu');
    return;
  }
  // datetime-local returns "yyyy-MM-ddTHH:mm" - append :00 for seconds
  if (body.startTime && body.startTime.length === 16) body.startTime += ':00';
  const res = await fetchAdmin(API + '/showtimes', { method: 'POST', body: JSON.stringify(body) });
  if (res.ok) { loadShowtimes(); clearShowtimeForm(); }
  else {
    const data = await res.json().catch(() => ({}));
    alert(data.message || data.error || 'Lỗi');
  }
}

function editShowtime(id, movieId, roomId, startTime, price) {
  document.getElementById('sMovieId').value = movieId;
  document.getElementById('sRoomId').value = roomId;
  // Convert ISO datetime to datetime-local format
  const dateObj = new Date(startTime);
  const localStr = dateObj.toISOString().slice(0, 16);
  document.getElementById('sStartTime').value = localStr;
  document.getElementById('sPrice').value = price;
  
  const btn = document.querySelector('#panel-showtimes button[onclick*="addShowtime"]');
  btn.textContent = 'Cập nhật suất chiếu';
  btn.onclick = () => updateShowtime(id);
}

function clearShowtimeForm() {
  document.getElementById('sMovieId').value = '';
  document.getElementById('sRoomId').value = '';
  document.getElementById('sStartTime').value = '';
  document.getElementById('sPrice').value = '';
  
  const btn = document.querySelector('#panel-showtimes button[onclick*="updateShowtime"]');
  if (btn) {
    btn.textContent = 'Thêm suất chiếu';
    btn.onclick = addShowtime;
  }
}

async function updateShowtime(id) {
  const movieId = document.getElementById('sMovieId').value;
  const roomId = document.getElementById('sRoomId').value;
  const startVal = document.getElementById('sStartTime').value;
  const body = {
    movieId: movieId ? parseInt(movieId) : null,
    roomId: roomId ? parseInt(roomId) : null,
    startTime: startVal ? startVal + ':00' : null,
    price: parseFloat(document.getElementById('sPrice').value) || 75000
  };
  if (!body.movieId || !body.roomId || !body.startTime) {
    alert('Chọn phim, phòng và nhập giờ chiếu');
    return;
  }
  if (body.startTime && body.startTime.length === 16) body.startTime += ':00';
  const res = await fetchAdmin(API + '/showtimes/' + id, { method: 'PUT', body: JSON.stringify(body) });
  if (res.ok) { loadShowtimes(); clearShowtimeForm(); }
  else {
    const data = await res.json().catch(() => ({}));
    alert(data.message || data.error || 'Lỗi');
  }
}

async function deleteShowtime(id) {
  if (!confirm('Xóa suất chiếu này?')) return;
  const res = await fetchAdmin(API + '/showtimes/' + id, { method: 'DELETE' });
  if (res.ok) loadShowtimes();
  else {
    const data = await res.json().catch(() => ({}));
    alert(data.message || data.error || 'Lỗi');
  }
}

// Promotions
async function loadPromotions() {
  try {
    const list = await fetchAdmin(API + '/promotions').then(r => r.json());
    document.getElementById('promotionsTable').innerHTML = `
      <table class="admin-table">
        <tr><th>ID</th><th>Mã</th><th>%</th><th>Từ</th><th>Đến</th><th></th></tr>
        ${(list || []).map(p => `
          <tr><td>${p.id}</td><td>${p.code}</td><td>${p.discountPercent}%</td><td>${p.validFrom}</td><td>${p.validTo}</td>
          <td>
            <button class="btn btn-outline" onclick="editPromotion(${p.id}, '${p.code}', '${p.description || ''}', ${p.discountPercent}, '${p.validFrom}', '${p.validTo}', ${p.maxUses || 1000})">Sửa</button>
            <button class="btn btn-outline" onclick="deletePromotion(${p.id})">Xóa</button>
          </td></tr>
        `).join('')}
      </table>`;
  } catch (e) {
    document.getElementById('promotionsTable').innerHTML = '<p style="color:#888">Không tải được</p>';
  }
}

async function addPromotion() {
  const body = {
    code: document.getElementById('pCode').value,
    description: document.getElementById('pDesc').value,
    discountPercent: parseInt(document.getElementById('pPercent').value) || 10,
    validFrom: document.getElementById('pFrom').value || new Date().toISOString().split('T')[0],
    validTo: document.getElementById('pTo').value || new Date(Date.now() + 30*24*60*60*1000).toISOString().split('T')[0],
    maxUses: 1000
  };
  if (!body.code) { alert('Nhập mã khuyến mãi'); return; }
  const res = await fetchAdmin(API + '/promotions', { method: 'POST', body: JSON.stringify(body) });
  if (res.ok) { loadPromotions(); clearPromotionForm(); }
  else alert((await res.json()).error || 'Lỗi');
}

function editPromotion(id, code, desc, percent, from, to, maxUses) {
  document.getElementById('pCode').value = code;
  document.getElementById('pDesc').value = desc;
  document.getElementById('pPercent').value = percent;
  document.getElementById('pFrom').value = from;
  document.getElementById('pTo').value = to;
  document.getElementById('pMaxUses').value = maxUses;
  
  // Change button to update
  const btn = document.querySelector('#panel-promotions button[onclick*="addPromotion"]');
  btn.textContent = 'Cập nhật khuyến mãi';
  btn.onclick = () => updatePromotion(id);
}

function clearPromotionForm() {
  document.getElementById('pCode').value = '';
  document.getElementById('pDesc').value = '';
  document.getElementById('pPercent').value = '10';
  document.getElementById('pFrom').value = new Date().toISOString().split('T')[0];
  document.getElementById('pTo').value = new Date(Date.now() + 30*24*60*60*1000).toISOString().split('T')[0];
  document.getElementById('pMaxUses').value = '1000';
  
  // Reset button to add
  const btn = document.querySelector('#panel-promotions button[onclick*="updatePromotion"]');
  if (btn) {
    btn.textContent = 'Thêm khuyến mãi';
    btn.onclick = addPromotion;
  }
}

async function updatePromotion(id) {
  const body = {
    code: document.getElementById('pCode').value,
    description: document.getElementById('pDesc').value,
    discountPercent: parseInt(document.getElementById('pPercent').value) || 10,
    validFrom: document.getElementById('pFrom').value,
    validTo: document.getElementById('pTo').value,
    maxUses: parseInt(document.getElementById('pMaxUses').value) || 1000
  };
  if (!body.code) { alert('Nhập mã khuyến mãi'); return; }
  const res = await fetchAdmin(API + '/promotions/' + id, { method: 'PUT', body: JSON.stringify(body) });
  if (res.ok) { loadPromotions(); clearPromotionForm(); }
  else alert((await res.json()).error || 'Lỗi');
}

async function deletePromotion(id) {
  if (!confirm('Xóa khuyến mãi này?')) return;
  const res = await fetchAdmin(API + '/promotions/' + id, { method: 'DELETE' });
  if (res.ok) loadPromotions();
  else alert((await res.json()).message || 'Lỗi');
}

function adminErrorText(data) {
  if (!data || typeof data !== 'object') return 'Lỗi';
  return data.error || data.message || data.title || 'Lỗi';
}

// Snacks
function editSnackById(id) {
  const s = adminSnackCatalog.find(x => Number(x.id) === Number(id));
  if (!s) {
    alert('Không tìm thấy món trong danh sách đang hiển thị. Đang tải lại…');
    loadSnacks();
    return;
  }
  editSnack(s.id, s.name, s.description, s.price, s.imageUrl, s.category, s.stock, s.isAvailable);
}

async function loadSnacks() {
  const box = document.getElementById('snacksTable');
  if (!box) return;
  try {
    const res = await fetchAdmin(API + '/snacks');
    if (!res.ok) {
      box.innerHTML =
        '<p style="color:#ef4444">Không tải được danh sách (' + res.status + ')</p>';
      return;
    }
    const list = await res.json();
    const rows = Array.isArray(list) ? list : [];
    adminSnackCatalog = rows;
    box.innerHTML = `
      <table class="admin-table">
        <tr><th>ID</th><th>Tên</th><th>Loại</th><th>Giá</th><th>Tồn kho</th><th>Trạng thái</th><th></th></tr>
        ${rows.map(s => `
          <tr><td>${s.id}</td><td>${escapeHtml(s.name)}</td><td>${escapeHtml(s.category || '')}</td><td>${(Number(s.price) || 0).toLocaleString('vi-VN')} ₫</td>
          <td>${escapeHtml(String(s.stock ?? 0))}</td><td>${s.isAvailable ? 'Đang bán' : 'Tạm ngưng'}</td>
          <td>
            <button type="button" class="btn btn-outline" onclick="editSnackById(${s.id})">Sửa</button>
            <button type="button" class="btn btn-outline" onclick="deleteSnack(${s.id})">Xóa</button>
          </td></tr>
        `).join('')}
      </table>`;
  } catch (e) {
    box.innerHTML =
      '<p style="color:#ef4444">Không tải được: ' + escapeHtml(e.message || e) + '</p>';
  }
}

async function addSnack() {
  const body = {
    name: document.getElementById('snName').value,
    description: document.getElementById('snDesc').value || null,
    price: parseFloat(document.getElementById('snPrice').value) || 0,
    imageUrl: document.getElementById('snImage').value || null,
    category: document.getElementById('snCategory').value || 'FOOD',
    stock: parseInt(document.getElementById('snStock').value) || 0,
    isAvailable: document.getElementById('snAvailable').value === 'true'
  };
  if (!body.name) { alert('Nhập tên đồ ăn'); return; }
  const res = await fetchAdmin(API + '/snacks', { method: 'POST', body: JSON.stringify(body) });
  if (res.ok) { loadSnacks(); clearSnackForm(); }
  else alert(adminErrorText(await res.json().catch(() => ({}))));
}

function editSnack(id, name, description, price, imageUrl, category, stock, isAvailable) {
  document.getElementById('snName').value = name ?? '';
  document.getElementById('snDesc').value = description ?? '';
  document.getElementById('snPrice').value = price;
  document.getElementById('snImage').value = imageUrl ?? '';
  document.getElementById('snCategory').value = category || 'FOOD';
  document.getElementById('snStock').value = stock;
  const avail = isAvailable === true || isAvailable === 'true';
  document.getElementById('snAvailable').value = avail ? 'true' : 'false';

  const btn = document.getElementById('snackSubmitBtn');
  btn.textContent = 'Cập nhật món';
  btn.onclick = () => updateSnack(id);
}

function clearSnackForm() {
  document.getElementById('snName').value = '';
  document.getElementById('snDesc').value = '';
  document.getElementById('snPrice').value = '';
  document.getElementById('snImage').value = '';
  document.getElementById('snCategory').value = 'FOOD';
  document.getElementById('snStock').value = '0';
  document.getElementById('snAvailable').value = 'true';

  const btn = document.getElementById('snackSubmitBtn');
  if (btn) {
    btn.textContent = 'Thêm món';
    btn.onclick = addSnack;
  }
}

async function updateSnack(id) {
  const body = {
    name: document.getElementById('snName').value,
    description: document.getElementById('snDesc').value || null,
    price: parseFloat(document.getElementById('snPrice').value) || 0,
    imageUrl: document.getElementById('snImage').value || null,
    category: document.getElementById('snCategory').value || 'FOOD',
    stock: parseInt(document.getElementById('snStock').value) || 0,
    isAvailable: document.getElementById('snAvailable').value === 'true'
  };
  if (!body.name) { alert('Nhập tên đồ ăn'); return; }
  const res = await fetchAdmin(API + '/snacks/' + id, { method: 'PUT', body: JSON.stringify(body) });
  if (res.ok) { loadSnacks(); clearSnackForm(); }
  else alert(adminErrorText(await res.json().catch(() => ({}))));
}

async function deleteSnack(id) {
  if (!confirm('Xóa đồ ăn này?')) return;
  const res = await fetchAdmin(API + '/snacks/' + id, { method: 'DELETE' });
  if (res.ok) { loadSnacks(); }
  else alert(adminErrorText(await res.json().catch(() => ({}))));
}

// Users
async function loadUsers() {
  try {
    const list = await fetchAdmin(API + '/users').then(r => r.json());
    document.getElementById('usersTable').innerHTML = `
      <table class="admin-table">
        <tr><th>ID</th><th>Username</th><th>Email</th><th>Họ tên</th><th>Vai trò</th></tr>
        ${(list || []).map(u => `
          <tr><td>${u.id}</td><td>${u.username}</td><td>${u.email}</td><td>${u.fullName || ''}</td><td>${u.role}</td></tr>
        `).join('')}
      </table>`;
  } catch (e) {
    document.getElementById('usersTable').innerHTML = '<p style="color:#888">Không tải được</p>';
  }
}

// Reports
async function loadRevenue() {
  const from = document.getElementById('repFrom').value;
  const to = document.getElementById('repTo').value;
  const res = await fetchAdmin(API + '/reports/revenue?from=' + from + '&to=' + to);
  const data = await res.json();
  let html = `<p><strong>Doanh thu:</strong> ${(data.totalRevenue || 0).toLocaleString('vi-VN')} ₫</p>`;
  html += `<p><strong>Số vé:</strong> ${data.totalTickets || 0}</p>`;
  if (data.byDate && Object.keys(data.byDate).length) {
    html += '<h4>Chi tiết theo ngày</h4><ul>';
    for (const [d, v] of Object.entries(data.byDate)) {
      html += `<li>${d}: ${Number(v).toLocaleString('vi-VN')} ₫</li>`;
    }
    html += '</ul>';
  }
  const topRes = await fetchAdmin(API + '/reports/top-movies?limit=5');
  const top = await topRes.json();
  if (top && top.length) {
    html += '<h4>Top phim doanh thu</h4><ol>';
    top.forEach(t => { html += `<li>${t.title}: ${(t.revenue || 0).toLocaleString('vi-VN')} ₫</li>`; });
    html += '</ol>';
  }
  document.getElementById('reportsContent').innerHTML = html;
}

document.addEventListener('DOMContentLoaded', () => {
  renderNavAuth();
  initAdmin();
});
