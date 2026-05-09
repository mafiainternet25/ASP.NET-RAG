const API = '/api';
let currentBooking = null;

function renderNavAuth() {
  const el = document.getElementById('navAuth');
  if (!el) return;
  if (typeof AUTH !== 'undefined' && AUTH.isLoggedIn()) {
    el.innerHTML = '<button class="btn-logout" onclick="AUTH.clear();location.reload()">Đăng xuất</button>';
  } else {
    el.innerHTML = '<a href="/login" class="btn-login">Đăng nhập</a>';
  }
}

async function confirmBookingAfterPayment() {
  if (!currentBooking || !currentBooking.id) return;
  
  try {
    const res = await AUTH.fetchWithAuth(`${API}/bookings/${currentBooking.id}/confirm`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' }
    });
    
    if (res.ok) {
      const updated = await res.json();
      currentBooking = updated.booking; // Cập nhật booking info
      return true; // Thành công
    }
  } catch (err) {
    console.log('Lỗi confirm booking:', err);
  }
  return false;
}

async function loadPayment() {
  const code = new URLSearchParams(location.search).get('code');
  const payment_Status = new URLSearchParams(location.search).get('vnp_ResponseCode'); // Callback từ VNPay
  
  if (!code) {
    document.getElementById('paymentContent').innerHTML = '<p style="color:#888">Không tìm thấy đơn đặt vé. <a href="/pages/my-bookings">Vé của tôi</a></p>';
    return;
  }

  // Tải thông tin booking
  const res = await AUTH.fetchWithAuth(`${API}/bookings/${encodeURIComponent(code)}`);
  if (!res.ok) {
    if (res.status === 401 || res.status === 403) {
      document.getElementById('paymentContent').innerHTML = '<p style="color:#ef4444">Vui lòng đăng nhập để thanh toán.</p><a class="btn btn-primary" href="/login?redirect=' + encodeURIComponent(location.href) + '">Đăng nhập</a>';
    } else if (res.status === 404) {
      document.getElementById('paymentContent').innerHTML = '<p style="color:#888">Đơn không tồn tại. <a href="/pages/my-bookings">Vé của tôi</a></p>';
    } else {
      document.getElementById('paymentContent').innerHTML = '<p style="color:#ef4444">Có lỗi xảy ra. <a href="/pages/my-bookings">Quay lại</a></p>';
    }
    return;
  }

  currentBooking = await res.json();
  
  const detail = [];
  if (currentBooking.movieTitle) detail.push(currentBooking.movieTitle);
  if (currentBooking.cinemaName) detail.push(currentBooking.cinemaName);
  if (currentBooking.showtimeStartTime) detail.push(new Date(currentBooking.showtimeStartTime).toLocaleString('vi-VN'));
  if (currentBooking.snacks && currentBooking.snacks.length) {
    detail.push(currentBooking.snacks.map(s => `${s.name} x${s.quantity}`).join(', '));
  }
  
  document.getElementById('bookingInfo').innerHTML = `Mã vé: <strong>${currentBooking.bookingCode}</strong>${detail.length ? '<br><span style="color:#888;font-size:0.9rem">' + detail.join(' • ') + '</span>' : ''}`;
  document.getElementById('totalAmount').textContent = (currentBooking.totalPrice || 0).toLocaleString('vi-VN') + ' ₫';
  
  // Nếu VNPay redirect back với response code
  if (payment_Status === '00') {
    // Thanh toán thành công, confirm booking
    const confirmed = await confirmBookingAfterPayment();
    if (confirmed) {
      document.getElementById('paymentContent').innerHTML = '<div style="padding:2rem;text-align:center;background:rgba(16,185,129,0.1);border-radius:8px;border:2px solid #10b981"><p style="color:#10b981;font-size:1.2rem;font-weight:600">✓ Thanh toán thành công!</p><p style="color:#888;margin-top:0.5rem">Đơn đặt vé của bạn đã được xác nhận.</p><p style="margin-top:1rem"><a class="btn btn-primary" href="/pages/my-bookings">Xem vé của tôi</a></p></div>';
      const btn = document.getElementById('btnPay');
      if (btn) btn.style.display = 'none';
      return;
    }
  } else if (payment_Status && payment_Status !== '00') {
    // Thanh toán thất bại
    document.getElementById('paymentContent').innerHTML = '<div style="padding:2rem;text-align:center;background:rgba(239,68,68,0.1);border-radius:8px;border:2px solid #ef4444"><p style="color:#ef4444;font-size:1.1rem;font-weight:600">⚠ Thanh toán thất bại</p><p style="color:#888;margin-top:0.5rem">Mã lỗi: ' + payment_Status + '. Vui lòng thử lại.</p></div>';
    // Vẫn cho phép thử thanh toán lại
  }

  // Kiểm tra trạng thái booking hiện tại
  if (currentBooking.status === 'CONFIRMED') {
    const btn = document.getElementById('btnPay');
    if (btn) btn.style.display = 'none';
    document.getElementById('paymentContent').innerHTML = '<div style="padding:2rem;text-align:center;background:rgba(16,185,129,0.1);border-radius:8px"><p style="color:#10b981;">✓ Đơn đã được thanh toán</p></div>';
    return;
  }
  
  if (currentBooking.status === 'CANCELLED') {
    const btn = document.getElementById('btnPay');
    if (btn) btn.style.display = 'none';
    document.getElementById('paymentContent').innerHTML = '<p style="color:#ef4444;margin-top:1rem">Đơn đã bị hủy.</p>';
    return;
  }

  // Hiển thị nút thanh toán nếu trạng thái là PENDING
  const btnPay = document.getElementById('btnPay');
  if (btnPay) {
    btnPay.onclick = async () => {
      btnPay.disabled = true;
      btnPay.textContent = 'Đang xử lý...';
      
      try {
        const res = await AUTH.fetchWithAuth(`${API}/payments/simple/${currentBooking.id}`, { method: 'POST' });
        const data = await res.json();
        
        if (res.ok && data.success === 'true') {
          // Thanh toán thành công, cập nhật UI
          document.getElementById('paymentContent').innerHTML = '<div style="padding:2rem;text-align:center;background:rgba(16,185,129,0.1);border-radius:8px;border:2px solid #10b981"><p style="color:#10b981;font-size:1.2rem;font-weight:600">✓ Thanh toán thành công!</p><p style="color:#888;margin-top:0.5rem">Đơn đặt vé của bạn đã được xác nhận.</p><p style="margin-top:1rem"><a class="btn btn-primary" href="/pages/my-bookings">Xem vé của tôi</a></p></div>';
          btnPay.style.display = 'none';
        } else {
          alert(data.error || data.message || 'Thanh toán thất bại');
          btnPay.disabled = false;
          btnPay.textContent = 'Thanh toán';
        }
      } catch (err) {
        console.error('Lỗi thanh toán:', err);
        alert('Lỗi: ' + err.message);
        btnPay.disabled = false;
        btnPay.textContent = 'Thanh toán';
      }
    };
  }
}

document.addEventListener('DOMContentLoaded', () => {
  renderNavAuth();
  if (!AUTH.isLoggedIn()) {
    const code = new URLSearchParams(location.search).get('code');
    if (code) {
      document.getElementById('paymentContent').innerHTML = '<p style="color:#ef4444">Vui lòng đăng nhập để thanh toán.</p><a class="btn btn-primary" href="/login?redirect=' + encodeURIComponent(location.href) + '">Đăng nhập</a>';
      return;
    }
  }
  loadPayment();
});
