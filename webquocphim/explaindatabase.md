---
## Tổng quan các bảng

| # | Bảng | Mục đích |
  |---|------|----------|
  | 1 | `users` | Tài khoản đăng nhập của người dùng và admin |
  | 3 | `movies` | Thông tin các bộ phim |
  | 4 | `cinemas` | Thông tin các rạp chiếu phim |
  | 5 | `rooms` | Phòng chiếu thuộc từng rạp |
  | 6 | `seats` | Ghế ngồi thuộc từng phòng |
  | 7 | `showtimes` | Suất chiếu (phim + phòng + giờ + giá) |
  | 8 | `promotions` | Mã khuyến mãi giảm giá |
  | 9 | `bookings` | Đơn đặt vé của người dùng |
  | 10 | `booking_seats` | Chi tiết ghế trong mỗi đơn đặt vé |
  | 11 | `reviews` | Đánh giá và bình luận phim |
  | 12 | `snacks` | Danh mục đồ ăn vặt bán tại rạp |
  | 13 | `booking_snacks` | Snack được chọn trong mỗi đơn đặt vé |
---

## 1. Bảng `users` — Tài khoản người dùng

> Lưu thông tin tài khoản của tất cả người dùng hệ thống (cả khách hàng lẫn admin).

| Cột             | Kiểu               | Bắt buộc | Mặc định            | Giải thích                                                   |
| --------------- | ------------------ | -------- | ------------------- | ------------------------------------------------------------ |
| `id`            | INT AUTO_INCREMENT | ✅       | —                   | Khóa chính, tự tăng, định danh duy nhất mỗi user             |
| `username`      | VARCHAR(50)        | ✅       | —                   | Tên đăng nhập, phải duy nhất trong hệ thống                  |
| `email`         | VARCHAR(120)       | ✅       | —                   | Email đăng nhập, phải duy nhất, dùng để xác thực             |
| `password_hash` | VARCHAR(120)       | ✅       | —                   | Mật khẩu đã được mã hóa bằng BCrypt, không lưu mật khẩu thật |
| `full_name`     | VARCHAR(120)       | ❌       | NULL                | Họ tên đầy đủ, hiển thị trên giao diện                       |
| `phone`         | VARCHAR(30)        | ❌       | NULL                | Số điện thoại liên hệ                                        |
| `role`          | VARCHAR(20)        | ✅       | `'USER'`            | Phân quyền: `USER` (khách hàng) hoặc `ADMIN` (quản trị viên) |
| `created_at`    | DATETIME           | ✅       | `CURRENT_TIMESTAMP` | Thời điểm tạo tài khoản, tự động gán khi INSERT              |

**Ràng buộc:**

- `UNIQUE(username)` — không cho phép 2 tài khoản cùng username
- `UNIQUE(email)` — không cho phép 2 tài khoản cùng email

---

## 3. Bảng `movies` — Phim

> Lưu thông tin tất cả các bộ phim trong hệ thống, kể cả phim đang chiếu, sắp chiếu, và đã kết thúc.

| Cột            | Kiểu               | Bắt buộc | Mặc định        | Giải thích                                                                               |
| -------------- | ------------------ | -------- | --------------- | ---------------------------------------------------------------------------------------- |
| `id`           | INT AUTO_INCREMENT | ✅       | —               | Khóa chính                                                                               |
| `title`        | VARCHAR(180)       | ✅       | —               | Tên phim hiển thị                                                                        |
| `genre`        | VARCHAR(80)        | ❌       | NULL            | Thể loại phim, ví dụ: Action, Sci-Fi, Animation, Horror                                  |
| `duration_min` | INT                | ❌       | NULL            | Thời lượng phim tính bằng phút, ví dụ: 169                                               |
| `poster_url`   | VARCHAR(500)       | ❌       | NULL            | Đường dẫn ảnh poster phim (URL hoặc đường dẫn local)                                     |
| `trailer_url`  | VARCHAR(500)       | ❌       | NULL            | Đường dẫn video trailer (thường là link YouTube)                                         |
| `status`       | VARCHAR(20)        | ✅       | `'NOW_SHOWING'` | Trạng thái: `NOW_SHOWING` (đang chiếu), `COMING_SOON` (sắp chiếu), `ENDED` (đã kết thúc) |
| `rating`       | DECIMAL(3,1)       | ❌       | NULL            | Điểm đánh giá trung bình, ví dụ: 9.0, 8.8 (thang 0-10)                                   |
| `description`  | TEXT               | ❌       | NULL            | Mô tả nội dung phim, không giới hạn độ dài                                               |

---

## 4. Bảng `cinemas` — Rạp chiếu phim

> Lưu thông tin các rạp chiếu phim. Mỗi rạp có thể có nhiều phòng chiếu.

| Cột       | Kiểu               | Bắt buộc | Mặc định | Giải thích                              |
| --------- | ------------------ | -------- | -------- | --------------------------------------- |
| `id`      | INT AUTO_INCREMENT | ✅       | —        | Khóa chính                              |
| `name`    | VARCHAR(120)       | ✅       | —        | Tên rạp, ví dụ: Quoc Phim Center        |
| `address` | VARCHAR(250)       | ❌       | NULL     | Địa chỉ đường phố của rạp               |
| `city`    | VARCHAR(80)        | ❌       | NULL     | Thành phố, dùng để lọc rạp theo khu vực |

---

## 5. Bảng `rooms` — Phòng chiếu

> Mỗi rạp có nhiều phòng chiếu. Phòng chiếu là nơi diễn ra suất chiếu cụ thể.

| Cột           | Kiểu               | Bắt buộc | Mặc định   | Giải thích                                          |
| ------------- | ------------------ | -------- | ---------- | --------------------------------------------------- |
| `id`          | INT AUTO_INCREMENT | ✅       | —          | Khóa chính                                          |
| `cinema_id`   | INT                | ✅       | —          | FK → `cinemas.id`, phòng này thuộc rạp nào          |
| `name`        | VARCHAR(60)        | ✅       | —          | Tên phòng chiếu, ví dụ: Room A, Phòng 1             |
| `total_seats` | INT                | ✅       | —          | Tổng số ghế trong phòng, dùng để kiểm tra sức chứa  |
| `room_type`   | VARCHAR(20)        | ✅       | `'NORMAL'` | Loại phòng: `NORMAL` (thường), `IMAX`, `4DX`, `VIP` |

**Ràng buộc:**

- `FK(cinema_id) ON DELETE CASCADE` — xóa rạp thì xóa hết phòng của rạp đó

---

## 6. Bảng `seats` — Ghế ngồi

> Lưu từng chiếc ghế cụ thể trong phòng. Mỗi ghế có vị trí (hàng + số) và loại (thường/VIP).

| Cột           | Kiểu               | Bắt buộc | Mặc định   | Giải thích                                                         |
| ------------- | ------------------ | -------- | ---------- | ------------------------------------------------------------------ |
| `id`          | INT AUTO_INCREMENT | ✅       | —          | Khóa chính                                                         |
| `room_id`     | INT                | ✅       | —          | FK → `rooms.id`, ghế này thuộc phòng nào                           |
| `seat_row`    | VARCHAR(4)         | ✅       | —          | Hàng ghế, ví dụ: A, B, C, D                                        |
| `seat_number` | INT                | ✅       | —          | Số ghế trong hàng, ví dụ: 1, 2, 3, ..., 10                         |
| `seat_type`   | VARCHAR(20)        | ✅       | `'NORMAL'` | Loại ghế: `NORMAL` (thường), `VIP`, `COUPLE` (ghế đôi)             |
| `extra_price` | DECIMAL(12,2)      | ✅       | `0`        | Phụ phí thêm so với giá vé cơ bản. Ghế NORMAL = 0, ghế VIP = 20000 |

**Ràng buộc:**

- `FK(room_id) ON DELETE CASCADE` — xóa phòng thì xóa hết ghế trong phòng

**Ví dụ đọc dữ liệu:** Ghế `seat_row='C', seat_number=5, seat_type='VIP', extra_price=20000` nghĩa là ghế VIP hàng C số 5, giá vé = giá showtime + 20.000đ.

---

## 7. Bảng `showtimes` — Suất chiếu

> Mỗi suất chiếu là 1 lần chiếu phim cụ thể tại 1 phòng cụ thể vào 1 thời điểm cụ thể.

| Cột          | Kiểu               | Bắt buộc | Mặc định | Giải thích                                                       |
| ------------ | ------------------ | -------- | -------- | ---------------------------------------------------------------- |
| `id`         | INT AUTO_INCREMENT | ✅       | —        | Khóa chính                                                       |
| `movie_id`   | INT                | ✅       | —        | FK → `movies.id`, chiếu phim nào                                 |
| `room_id`    | INT                | ✅       | —        | FK → `rooms.id`, chiếu tại phòng nào                             |
| `start_time` | DATETIME           | ✅       | —        | Ngày giờ bắt đầu chiếu, ví dụ: 2025-06-15 19:00:00               |
| `price`      | DECIMAL(12,2)      | ✅       | —        | Giá vé cơ bản cho suất chiếu này (chưa tính extra_price của ghế) |

**Ràng buộc:**

- `FK(movie_id) ON DELETE RESTRICT` — không cho xóa phim nếu còn suất chiếu
- `FK(room_id) ON DELETE CASCADE` — xóa phòng thì xóa hết suất chiếu trong phòng

**Tính giá vé thực tế:** `giá vé = showtimes.price + seats.extra_price`

---

## 8. Bảng `promotions` — Khuyến mãi

> Lưu các mã giảm giá. User nhập mã khi đặt vé để được giảm % tổng tiền.

| Cột                | Kiểu               | Bắt buộc | Mặc định | Giải thích                                                    |
| ------------------ | ------------------ | -------- | -------- | ------------------------------------------------------------- |
| `id`               | INT AUTO_INCREMENT | ✅       | —        | Khóa chính                                                    |
| `code`             | VARCHAR(40)        | ✅       | —        | Mã khuyến mãi user nhập vào, ví dụ: WELCOME10, SUMMER20       |
| `description`      | VARCHAR(250)       | ❌       | NULL     | Mô tả ngắn về khuyến mãi để hiển thị cho user                 |
| `discount_percent` | INT                | ✅       | —        | Phần trăm giảm giá, ví dụ: 10 (nghĩa là giảm 10%)             |
| `valid_from`       | DATETIME           | ✅       | —        | Ngày bắt đầu có hiệu lực                                      |
| `valid_to`         | DATETIME           | ✅       | —        | Ngày hết hạn                                                  |
| `max_uses`         | INT                | ✅       | —        | Tổng số lần tối đa mã này được sử dụng                        |
| `used_count`       | INT                | ✅       | `0`      | Số lần đã dùng thực tế, tăng 1 mỗi khi booking áp dụng mã này |

**Ràng buộc:**

- `UNIQUE(code)` — mỗi mã khuyến mãi là duy nhất

**Kiểm tra hợp lệ khi dùng:**

```
valid_from <= NOW() <= valid_to  AND  used_count < max_uses
```

---

## 9. Bảng `bookings` — Đơn đặt vé

> Mỗi lần user đặt vé thành công tạo ra 1 booking. Booking liên kết user với suất chiếu và lưu tổng tiền.

| Cột            | Kiểu               | Bắt buộc | Mặc định            | Giải thích                                                                                    |
| -------------- | ------------------ | -------- | ------------------- | --------------------------------------------------------------------------------------------- |
| `id`           | INT AUTO_INCREMENT | ✅       | —                   | Khóa chính                                                                                    |
| `user_id`      | INT                | ✅       | —                   | FK → `users.id`, booking này của user nào                                                     |
| `showtime_id`  | INT                | ✅       | —                   | FK → `showtimes.id`, đặt vé cho suất chiếu nào                                                |
| `booking_code` | VARCHAR(20)        | ✅       | —                   | Mã đặt vé duy nhất, hiển thị cho user, dạng: `ABC12345` (8 ký tự viết hoa)                    |
| `status`       | VARCHAR(20)        | ✅       | `'PENDING'`         | Trạng thái đơn: `PENDING` (chờ thanh toán), `CONFIRMED` (đã thanh toán), `CANCELLED` (đã hủy) |
| `total_price`  | DECIMAL(12,2)      | ✅       | —                   | Tổng tiền phải trả = tiền ghế + tiền snack - giảm giá                                         |
| `promotion_id` | INT                | ❌       | NULL                | FK → `promotions.id`, mã khuyến mãi đã áp dụng (NULL nếu không dùng)                          |
| `created_at`   | DATETIME           | ✅       | `CURRENT_TIMESTAMP` | Thời điểm tạo đơn                                                                             |

**Ràng buộc:**

- `UNIQUE(booking_code)` — mỗi mã đặt vé là duy nhất trên toàn hệ thống
- `FK(user_id) ON DELETE CASCADE` — xóa user thì xóa booking
- `FK(showtime_id) ON DELETE RESTRICT` — không cho xóa suất chiếu đã có booking
- `FK(promotion_id) ON DELETE SET NULL` — xóa khuyến mãi thì booking vẫn giữ, chỉ SET promotion_id = NULL

---

## 10. Bảng `booking_seats` — Chi tiết ghế trong đơn

> Mỗi booking có thể gồm nhiều ghế. Bảng này lưu chi tiết từng ghế được đặt trong 1 booking.

| Cột          | Kiểu               | Bắt buộc | Mặc định | Giải thích                                                                                                                           |
| ------------ | ------------------ | -------- | -------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| `id`         | INT AUTO_INCREMENT | ✅       | —        | Khóa chính                                                                                                                           |
| `booking_id` | INT                | ✅       | —        | FK → `bookings.id`, thuộc đơn đặt vé nào                                                                                             |
| `seat_id`    | INT                | ✅       | —        | FK → `seats.id`, ghế nào được đặt                                                                                                    |
| `price`      | DECIMAL(12,2)      | ✅       | —        | Giá của riêng ghế này tại thời điểm đặt (= showtime.price + seat.extra_price). Lưu lại để tránh thay đổi khi giá vé thay đổi sau này |

**Ràng buộc:**

- `FK(booking_id) ON DELETE CASCADE` — xóa booking thì xóa chi tiết ghế
- `FK(seat_id) ON DELETE RESTRICT` — không cho xóa ghế nếu đã có trong booking

**Tại sao lưu price riêng?** Vì giá vé có thể thay đổi theo thời gian. Lưu giá tại thời điểm đặt để lịch sử giao dịch luôn chính xác.

---

## 11. Bảng `reviews` — Đánh giá phim

> User có thể đánh giá phim sau khi xem. Mỗi review gồm điểm số và bình luận.

| Cột          | Kiểu               | Bắt buộc | Mặc định            | Giải thích                                            |
| ------------ | ------------------ | -------- | ------------------- | ----------------------------------------------------- |
| `id`         | INT AUTO_INCREMENT | ✅       | —                   | Khóa chính                                            |
| `user_id`    | INT                | ✅       | —                   | FK → `users.id`, ai viết review này                   |
| `movie_id`   | INT                | ✅       | —                   | FK → `movies.id`, review cho phim nào                 |
| `rating`     | INT                | ✅       | —                   | Điểm đánh giá từ 1 đến 10                             |
| `comment`    | TEXT               | ❌       | NULL                | Nội dung bình luận, có thể để trống nếu chỉ chấm điểm |
| `created_at` | DATETIME           | ✅       | `CURRENT_TIMESTAMP` | Thời điểm viết review                                 |

**Ràng buộc:**

- `FK(user_id) ON DELETE CASCADE` — xóa user thì xóa hết review của user đó
- `FK(movie_id) ON DELETE CASCADE` — xóa phim thì xóa hết review của phim đó

---

## 12. Bảng `snacks` — Đồ ăn vặt

> Danh mục các loại đồ ăn vặt, nước uống bán tại rạp. User có thể chọn snack khi đặt vé.

| Cột            | Kiểu               | Bắt buộc | Mặc định            | Giải thích                                                        |
| -------------- | ------------------ | -------- | ------------------- | ----------------------------------------------------------------- |
| `id`           | INT AUTO_INCREMENT | ✅       | —                   | Khóa chính                                                        |
| `name`         | VARCHAR(120)       | ✅       | —                   | Tên snack hiển thị, ví dụ: Bắp rang bơ lớn, Pepsi lon             |
| `description`  | VARCHAR(250)       | ❌       | NULL                | Mô tả ngắn, ví dụ: Bắp rang bơ size L, Pepsi 330ml lon            |
| `price`        | DECIMAL(12,2)      | ✅       | —                   | Giá bán của snack, ví dụ: 35000 (35.000 đồng)                     |
| `image_url`    | VARCHAR(500)       | ❌       | NULL                | Đường dẫn ảnh của snack, lưu sau khi admin upload                 |
| `category`     | VARCHAR(40)        | ✅       | `'FOOD'`            | Nhóm snack: `FOOD` (đồ ăn), `DRINK` (đồ uống), `COMBO` (bộ combo) |
| `stock`        | INT                | ✅       | `0`                 | Số lượng tồn kho hiện tại. Trừ đi 1 mỗi khi có booking            |
| `is_available` | TINYINT(1)         | ✅       | `1`                 | Có hiển thị cho user chọn không: `1` = đang bán, `0` = tạm ẩn     |
| `created_at`   | DATETIME           | ✅       | `CURRENT_TIMESTAMP` | Thời điểm thêm snack vào hệ thống                                 |

**Giải thích `is_available`:** Admin có thể tắt snack tạm thời mà không cần xóa (ví dụ hết hàng tạm thời). Khi `is_available = 0`, snack không hiển thị trong trang chọn snack của user.

**Giải thích `stock`:** Mỗi khi user đặt snack thành công, `stock` giảm đúng số lượng đã đặt. Nếu `stock = 0`, snack đó không cho đặt thêm dù `is_available = 1`.

---

## 13. Bảng `booking_snacks` — Snack trong đơn đặt vé

> Mỗi booking có thể kèm theo nhiều snack. Bảng này lưu chi tiết từng snack và số lượng trong 1 booking.

| Cột          | Kiểu               | Bắt buộc | Mặc định | Giải thích                                                                                    |
| ------------ | ------------------ | -------- | -------- | --------------------------------------------------------------------------------------------- |
| `id`         | INT AUTO_INCREMENT | ✅       | —        | Khóa chính                                                                                    |
| `booking_id` | INT                | ✅       | —        | FK → `bookings.id`, thuộc đơn đặt vé nào                                                      |
| `snack_id`   | INT                | ✅       | —        | FK → `snacks.id`, snack nào được chọn                                                         |
| `quantity`   | INT                | ✅       | `1`      | Số lượng snack này trong đơn, ví dụ: 2 lon Pepsi                                              |
| `price`      | DECIMAL(12,2)      | ✅       | —        | Giá của 1 snack tại thời điểm đặt. Lưu lại để tránh thay đổi khi giá snack điều chỉnh sau này |

**Ràng buộc:**

- `FK(booking_id) ON DELETE CASCADE` — xóa booking thì xóa hết snack trong booking
- `FK(snack_id) ON DELETE RESTRICT` — không cho xóa snack nếu đã có trong booking (phải soft delete)

**Tại sao lưu price riêng?** Lý do tương tự `booking_seats.price` — giá snack có thể thay đổi, cần lưu lại giá tại thời điểm giao dịch để đảm bảo lịch sử chính xác.

---

## Sơ đồ quan hệ tóm tắt

```
users ──────────────────────────────────────────────┐
  │                                                  │
  ├──1:N──► bookings ──1:N──► booking_seats ──N:1──► seats
  │              │                                     │
  │              ├──1:N──► booking_snacks ──N:1──► snacks
  │              │
  │              ├──N:1──► showtimes ──N:1──► movies
  │              │              └──N:1──► rooms ──N:1──► cinemas
  │              │
  │              └──N:1──► promotions
  │
  ├──1:N──► reviews ──N:1──► movies
  │
  └──1:N──► auth_tokens
```

---

## Quy tắc tính tiền

```
Tổng tiền 1 booking = Tiền ghế + Tiền snack - Giảm giá

Tiền ghế   = Σ (showtimes.price + seats.extra_price) cho mỗi ghế được chọn
Tiền snack = Σ (snacks.price × booking_snacks.quantity) cho mỗi snack được chọn
Giảm giá   = (Tiền ghế + Tiền snack) × promotions.discount_percent / 100
             (nếu có nhập mã khuyến mãi hợp lệ)
```

---

## Trạng thái quan trọng

### `bookings.status`

| Giá trị     | Ý nghĩa                                                      |
| ----------- | ------------------------------------------------------------ |
| `PENDING`   | Đơn vừa tạo, chờ thanh toán (ghế đang bị lock Redis 10 phút) |
| `CONFIRMED` | Đã thanh toán thành công                                     |
| `CANCELLED` | Đã hủy (hết giờ thanh toán hoặc user tự hủy)                 |

### `movies.status`

| Giá trị       | Ý nghĩa                   |
| ------------- | ------------------------- |
| `NOW_SHOWING` | Đang chiếu tại rạp        |
| `COMING_SOON` | Sắp chiếu, chưa mở bán vé |
| `ENDED`       | Đã kết thúc chiếu         |

### `snacks.is_available`

| Giá trị | Ý nghĩa                                                   |
| ------- | --------------------------------------------------------- |
| `1`     | Đang bán, hiển thị cho user chọn                          |
| `0`     | Tạm ẩn (hết hàng tạm thời hoặc admin tắt), không hiển thị |
