const API = '/api';

function renderNavAuth() {
  const el = document.getElementById('navAuth');
  if (!el) return;
  if (typeof AUTH !== 'undefined' && AUTH.isLoggedIn()) {
    el.innerHTML = '<button class="btn-logout" onclick="AUTH.clear();location.href=\'/login\'">Đăng xuất</button>';
  } else {
    el.innerHTML = '<a href="/login?redirect=' + encodeURIComponent(location.pathname) + '" class="btn-login">Đăng nhập</a>';
  }
}

async function loadProfile() {
  if (!AUTH.isLoggedIn()) {
    location.href = '/login?redirect=' + encodeURIComponent(location.href);
    return;
  }
  const res = await AUTH.fetchWithAuth(`${API}/users/me`);
  if (!res.ok) {
    if (res.status === 401) {
      location.href = '/login?redirect=' + encodeURIComponent(location.href);
      return;
    }
    showError('Không tải được thông tin');
    return;
  }
  const p = await res.json();
  document.getElementById('username').value = p.username || '';
  document.getElementById('fullName').value = p.fullName || '';
  document.getElementById('email').value = p.email || '';
  document.getElementById('phone').value = p.phone || '';
}

function showError(msg) {
  const el = document.getElementById('profileError');
  if (el) {
    el.textContent = msg;
    el.style.display = 'block';
  }
}

function hideError() {
  const el = document.getElementById('profileError');
  if (el) el.style.display = 'none';
}

document.addEventListener('DOMContentLoaded', () => {
  renderNavAuth();
  if (!AUTH.isLoggedIn()) {
    location.href = '/login?redirect=' + encodeURIComponent(location.pathname);
    return;
  }
  loadProfile();

  document.getElementById('profileForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    hideError();
    const newPw = document.getElementById('newPassword').value;
    const newPw2 = document.getElementById('newPassword2').value;
    if (newPw && newPw !== newPw2) {
      showError('Mật khẩu mới không khớp');
      return;
    }
    const body = {
      fullName: document.getElementById('fullName').value,
      email: document.getElementById('email').value,
      phone: document.getElementById('phone').value
    };
    if (newPw) {
      body.currentPassword = document.getElementById('currentPassword').value;
      body.newPassword = newPw;
    }
    const res = await AUTH.fetchWithAuth(`${API}/users/me`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    });
    const data = await res.json().catch(() => ({}));
    if (res.ok) {
      alert('Cập nhật thành công!');
      loadProfile();
      document.getElementById('currentPassword').value = '';
      document.getElementById('newPassword').value = '';
      document.getElementById('newPassword2').value = '';
    } else {
      showError(data.error || 'Cập nhật thất bại');
    }
  });
});
