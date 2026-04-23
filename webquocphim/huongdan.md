---

## 📁 Cấu trúc project (MVC Pattern)

```
CinemaBooking/
├── Controllers/
│   ├── AdminController.cs
│   ├── ApiControllerBase.cs
│   ├── AuthController.cs
│   ├── BookingController.cs
│   ├── ChatbotController.cs
│   ├── CinemasController.cs
│   ├── MoviesController.cs
│   ├── PagesController.cs
│   ├── PaymentsController.cs
│   ├── ReviewsController.cs
│   ├── ShowtimesController.cs
│   ├── SnacksController.cs
│   └── UsersController.cs
│     
├── Models/
│   ├── Entities/
│   │   ├── Booking.cs
│   │   ├── BookingSeat.cs
│   │   ├── BookingSnack.cs
│   │   ├── Cinema.cs
│   │   ├── Movie.cs
│   │   ├── Promotion.cs
│   │   ├── Review.cs
│   │   ├── Room.cs
│   │   ├── Seat.cs
│   │   ├── Showtime.cs
│   │   ├── Snack.cs                       
│   │   └── User.cs                
│   ├── ApiOptions.cs     
│   └── ApiRequests.cs
│
├── Views/
│   ├── css/
│   │   └── style.css
│   ├── js/
│   │   ├── admin.js
│   │   ├── auth.js
│   │   ├── booking.js
│   │   ├── main.js
│   │   ├── movie-detail.js
│   │   ├── movie.js
│   │   ├── my-bookings.js
│   │   ├── payment.js
│   │   └── Profile.js
│   │ 
│   ├── pages/
│   │   ├── admin.cshtml             
│   │   ├── booking.cshtml             
│   │   ├── movie-detail.cshtml             
│   │   ├── movies.cshtml             
│   │   ├── my-bookings.cshtml             
│   │   ├── payment.cshtml             
│   │   └── profile.cshtml
│   │ 
│   ├── Shared/
│   │   ├── _Footer_.cshtml             
│   │   ├── _Layout_.cshtml
│   │   └── _Navbar_.cshtml
│   │ 
│   ├── _ViewStart.cshtml
│   ├── index.cshtml
│   └── login.cshtml
│
├── RagService/
│   ├── ingestor.py               
│   ├── rag_service.py     
│   ├── requirements.txgt                    
│   └── retriever.py
│
├── Security/
│   ├── JwtUtil.cs                    
│   ├── TokenAuthenticationDefaults.cs                    
│   └── TokenAuthenticationHandler.cs
│
├── Services/
│   ├── AdminService.cs
│   ├── ShowtimeService.cs
│   ├── AuthService.cs
│   ├── BookingService.cs
│   ├── CurrentUserResolver.cs
│   ├── MovieService.cs
│   ├── PaymentService.cs       
│   ├── ReviewService.cs                    
│   ├── ServiceResult.cs                    
│   └── ShowtimeService.cs
│
├── Data/
│   └── ApplicationDbContext.cs
├── Migrations/
├── wwwroot/
│   ├── css/
│   ├── js/
│   ├── uploads/
│   │   └── snacks/                         
│   └── images/
└── appsettings.json
```

---

## 🗄️ Database Schema (MySQL)

### Danh sách bảng

| Bảng             | Mô tả                                     |
| ---------------- | ----------------------------------------- |
| `users`          | Tài khoản (User/Admin), lưu role          |
| `movies`         | Thông tin phim, poster, trailer, rating   |
| `cinemas`        | Rạp chiếu phim (tên, địa chỉ)             |
| `rooms`          | Phòng chiếu thuộc rạp                     |
| `seats`          | Ghế ngồi thuộc phòng (row, number, type)  |
| `showtimes`      | Suất chiếu (movie + room + time + giá vé) |
| `bookings`       | Đơn đặt vé của user                       |
| `booking_seats`  | Chi tiết ghế trong mỗi booking            |
| `payments`       | Thanh toán online                         |
| `promotions`     | Mã khuyến mãi, % giảm giá                 |
| `reviews`        | Đánh giá + bình luận phim                 |
| `snacks`         | Danh mục đồ ăn vặt (FOOD/DRINK/COMBO)     |
| `booking_snacks` | Snack được chọn trong mỗi booking         |

### Quan hệ chính

- `users` 1—N `bookings`, `reviews`
- `movies` 1—N `showtimes`, `reviews`
- `cinemas` 1—N `rooms` 1—N `seats`
- `rooms` 1—N `showtimes`
- `showtimes` 1—N `bookings`
- `bookings` 1—N `booking_seats`, 1—1 `payments`, 1—N `booking_snacks`
- `seats` 1—N `booking_seats`
- `snacks` 1—N `booking_snacks`
- `promotions` 1—N `bookings`

### SQL tạo bảng snacks ()

```sql
CREATE TABLE snacks (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(120) NOT NULL,
    description VARCHAR(250) NULL,
    price DECIMAL(12, 2) NOT NULL,
    image_url VARCHAR(500) NULL,
    category VARCHAR(20) NOT NULL DEFAULT 'FOOD',  -- FOOD / DRINK / COMBO
    stock INT NOT NULL DEFAULT 0,
    is_available TINYINT(1) NOT NULL DEFAULT 1,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE booking_snacks (
    id INT AUTO_INCREMENT PRIMARY KEY,
    booking_id INT NOT NULL,
    snack_id INT NOT NULL,
    quantity INT NOT NULL DEFAULT 1,
    price DECIMAL(12, 2) NOT NULL,   -- giá tại thời điểm đặt
    INDEX idx_booking_snack_booking (booking_id),
    INDEX idx_booking_snack_snack (snack_id),
    CONSTRAINT fk_booking_snack_booking FOREIGN KEY (booking_id)
        REFERENCES bookings (id) ON DELETE CASCADE,
    CONSTRAINT fk_booking_snack_snack FOREIGN KEY (snack_id)
        REFERENCES snacks (id) ON DELETE RESTRICT
);

-- Seed data mẫu
INSERT INTO snacks (name, description, price, category, stock) VALUES
('Bắp rang bơ nhỏ',  'Bắp rang bơ size S',        35000, 'FOOD',  100),
('Bắp rang bơ lớn',  'Bắp rang bơ size L',        55000, 'FOOD',  100),
('Pepsi lon',         'Pepsi 330ml',               25000, 'DRINK', 200),
('Coca-Cola lon',     'Coca-Cola 330ml',           25000, 'DRINK', 200),
('Nước suối',         'Aquafina 500ml',            15000, 'DRINK', 200),
('Combo Bắp + Nước', 'Bắp rang lớn + Pepsi lon',  70000, 'COMBO',  80),
('Khoai tây chiên',  'Khoai tây chiên giòn',       40000, 'FOOD',   80),
('Kẹo dẻo',          'Kẹo dẻo thập cẩm 100g',     20000, 'FOOD',  150);
```

---

## 🔑 Phân quyền (ASP.NET Identity + Cookie Auth)

| Role                     | Quyền truy cập                                    |
| ------------------------ | ------------------------------------------------- |
| `Guest` (chưa đăng nhập) | Xem phim, lịch chiếu, tìm kiếm                    |
| `User`                   | Đặt vé, chọn snack, thanh toán, lịch sử, đánh giá |
| `Admin`                  | Toàn bộ + quản lý hệ thống + báo cáo              |

---

## 📦 NuGet Packages cần cài

```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Pomelo.EntityFrameworkCore.MySql
dotnet add package Microsoft.AspNetCore.Authentication.Cookies
dotnet add package BCrypt.Net-Next
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add package Newtonsoft.Json
dotnet add package StackExchange.Redis
```

---

## ⚙️ appsettings.json mẫu

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=cinemadbaspnet;User=root;Password=yourpassword;CharSet=utf8mb4;"
  },
  "Jwt": {
    "Secret": "your-256-bit-secret-key-here",
    "ExpirationMinutes": 15
  },
  "Groq": {
    "ApiKey": "",
    "Model": "llama-3.1-8b-instant",
    "ApiUrl": "https://api.groq.com/openai/v1/chat/completions",
    "UseLlm": false
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

---

### 1. Tạo Entity + DbContext

```
Tạo các Entity class C# cho dự án ASP.NET MVC cinema booking dùng MySQL với Entity Framework Core.
Dùng Pomelo.EntityFrameworkCore.MySql.
Danh sách entity: User, Movie, Cinema, Room, Seat, Showtime, Booking, BookingSeat,
Payment, Promotion, Review, Snack, BookingSnack.

Quan hệ:
- User 1-N Booking, Review
- Movie 1-N Showtime, Review
- Cinema 1-N Room 1-N Seat
- Room 1-N Showtime
- Showtime 1-N Booking
- Booking 1-N BookingSeat, 1-1 Payment, 1-N BookingSnack
- Seat 1-N BookingSeat
- Promotion 1-N Booking
- Snack 1-N BookingSnack

Snack: Id, Name, Description, Price, ImageUrl, Category(FOOD/DRINK/COMBO),
       Stock, IsAvailable(bool), CreatedAt.
BookingSnack: Id, BookingId(FK), SnackId(FK), Quantity, Price(giá lúc đặt).

Enum: UserRole(User/Admin), BookingStatus(Pending/Confirmed/Cancelled),
PaymentStatus(Pending/Success/Failed), SeatType(Normal/Vip/Couple),
SnackCategory(Food/Drink/Combo).

Tạo AppDbContext kế thừa DbContext, cấu hình Fluent API trong OnModelCreating.
Thêm CreatedAt, UpdatedAt tự động bằng override SaveChangesAsync.
```

---

### 2. Tạo Authentication (Cookie-based)

```
Tạo hệ thống đăng nhập/đăng ký cho ASP.NET MVC cinema booking.
Yêu cầu:
- Dùng Cookie Authentication (không dùng ASP.NET Identity)
- AuthController có action: Login (GET/POST), Register (GET/POST), Logout
- Mã hóa password bằng BCrypt.Net-Next
- Lưu thông tin user vào Claims: UserId, Email, Role
- Redirect sau login: Admin -> /Admin/Dashboard, User -> /Movie/Index
- Dùng [Authorize] và [Authorize(Roles = "Admin")] để bảo vệ route
- LoginViewModel: Email, Password, RememberMe
- RegisterViewModel: FullName, Email, Password, ConfirmPassword (có DataAnnotations validation)

Cấu hình trong Program.cs:
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => { options.LoginPath = "/Auth/Login"; options.ExpireTimeSpan = TimeSpan.FromDays(7); });
```

---

### 3. Tạo Movie Controller + Views

```
Tạo MovieController và Views cho cinema ASP.NET MVC.
Actions:
- Index: GET /Movie — danh sách phim đang chiếu, phân trang 12 phim/trang, lọc theo thể loại
- Detail: GET /Movie/Detail/{id} — chi tiết phim + danh sách suất chiếu + reviews
- Search: GET /Movie/Search?q=&genre= — tìm kiếm phim
- NowPlaying: GET /Movie/NowPlaying — phim đang chiếu
- ComingSoon: GET /Movie/ComingSoon — phim sắp chiếu

ViewModels:
- MovieListViewModel: List<MovieCardViewModel>, int TotalPages, int CurrentPage, string Genre
- MovieCardViewModel: Id, Title, PosterUrl, Genre, DurationMin, Rating
- MovieDetailViewModel: tất cả thông tin phim + List<ShowtimeViewModel> + List<ReviewViewModel>

Views dùng Bootstrap 5, responsive.
Movie/Index.cshtml: grid 3 cột, mỗi card có poster, tên phim, thể loại, rating, nút "Đặt vé".
Movie/Detail.cshtml: poster lớn bên trái, thông tin bên phải, bên dưới có lịch chiếu và reviews.
```

---

### 4. Tạo Seat Map + Booking Flow

```
Tạo luồng đặt vé cho ASP.NET MVC cinema booking.

BookingController actions:
- SeatMap: GET /Booking/SeatMap/{showtimeId} — hiển thị sơ đồ ghế
- Create: POST /Booking/Create — tạo booking từ danh sách seatIds đã chọn,
  redirect → /Booking/SelectSnack/{bookingCode}
- Confirm: GET /Booking/Confirm/{bookingCode} — trang xác nhận đơn (ghế + snack + tổng tiền)
- History: GET /Booking/History — lịch sử đặt vé của user hiện tại
- Cancel: POST /Booking/Cancel/{id} — hủy vé

BookingService logic:
1. Kiểm tra ghế đã bị đặt chưa (query BookingSeats where BookingStatus != Cancelled)
2. Lock ghế tạm bằng Redis key "seat:lock:{seatId}:{showtimeId}" TTL 10 phút
3. Tính tổng tiền = tiền ghế + tiền snack, áp dụng khuyến mãi nếu có
4. Tạo Booking status = Pending, BookingCode = 8 ký tự UUID viết hoa
5. Lưu BookingSeats + BookingSnacks

SeatMapViewModel:
- ShowtimeId, MovieTitle, RoomName, StartTime
- List<SeatRowViewModel> Rows (mỗi row có SeatRow A/B/C và List<SeatViewModel>)
- SeatViewModel: Id, Number, Type, Price, Status (Available/Booked/Locked)

View Showtime/SeatMap.cshtml:
- Hiển thị màn hình (screen) ở trên
- Grid ghế bên dưới, màu sắc theo trạng thái: xanh=trống, đỏ=đã đặt, vàng=đang giữ
- Click ghế để chọn/bỏ chọn (JS)
- Sidebar tóm tắt: ghế đã chọn + tổng tiền + ô nhập mã khuyến mãi + nút "Tiếp theo"
```

---

### 5. Tạo Snack Selection ()

```
Tạo bước chọn snack trong luồng đặt vé ASP.NET MVC cinema booking.

BookingController actions (bổ sung):
- SelectSnack GET /Booking/SelectSnack/{bookingCode}:
  Hiển thị danh sách snack IsAvailable=true, nhóm theo Category (FOOD/DRINK/COMBO)
  Kèm thông tin booking hiện tại (phim, ghế, tổng tiền ghế)

- AddSnacks POST /Booking/AddSnacks:
  Nhận List<{SnackId, Quantity}> từ form
  Validate: Quantity >= 0, snack IsAvailable=true, stock đủ
  Xóa BookingSnacks cũ (nếu có), lưu BookingSnacks mới
  Trừ stock snack
  Cập nhật TotalPrice của Booking = tiền ghế + tiền snack
  Redirect → /Booking/Confirm/{bookingCode}

SnackSelectViewModel:
- BookingCode, MovieTitle, SeatsSummary, SeatTotal
- List<SnackGroupViewModel>:
  - Category (FOOD/DRINK/COMBO), List<SnackItemViewModel>
  - SnackItemViewModel: Id, Name, Description, Price, ImageUrl, Stock

View Booking/SelectSnack.cshtml:
- Header: thông tin phim + ghế đã chọn
- Nhóm snack theo tab: Đồ ăn | Đồ uống | Combo
- Mỗi snack: ảnh, tên, giá, nút tăng/giảm số lượng (JS)
- Sidebar: danh sách snack đã chọn + subtotal snack + tổng tiền
- Nút "Bỏ qua" (redirect thẳng đến Confirm không thêm snack)
- Nút "Xác nhận" (submit form AddSnacks)
```

---

### 6. Tạo Admin CRUD Snack ()

```
Tạo AdminSnackController và Views cho quản lý snack.
[Authorize(Roles = "Admin")]
Route: /Admin/Snack

Actions:
- Index GET: danh sách snack, lọc theo Category, tìm kiếm theo tên, phân trang 10/trang
- Create GET/POST: thêm snack mới, có upload ảnh (lưu vào wwwroot/uploads/snacks/)
- Edit GET/POST: sửa thông tin snack, có thể đổi ảnh
- Delete POST: nếu snack đã có trong booking_snacks → soft delete (IsAvailable=false)
              nếu chưa có → xóa cứng
- ToggleAvailable POST /Admin/Snack/ToggleAvailable/{id}: bật/tắt hiển thị (trả JSON)

SnackViewModel:
- Id, Name (Required MaxLength 120), Description (MaxLength 250)
- Price (Required Range 1000-10000000)
- ImageUrl (hiển thị preview)
- Category (Required): FOOD / DRINK / COMBO
- Stock (Range 0-10000)
- IsAvailable (bool)

Views:
- Index: bảng Bootstrap, cột Ảnh/Tên/Danh mục/Giá/Tồn kho/Trạng thái/Hành động
  Toggle switch cho IsAvailable (gọi AJAX ToggleAvailable)
- Create/Edit: form có preview ảnh khi chọn file, dropdown Category, validation

Logic upload ảnh:
- Lưu vào wwwroot/uploads/snacks/{guid}.{ext}
- Cho phép: .jpg, .jpeg, .png, .webp, tối đa 2MB
- Nếu Edit mà không upload ảnh mới → giữ nguyên ảnh cũ
```

---

### 7. Tạo Payment (VNPay QR)

```
Tạo PaymentController và VNPayService cho ASP.NET MVC cinema booking.
Yêu cầu:
- POST /Payment/CreateVNPay/{bookingId} — tạo URL thanh toán VNPay
- GET /Payment/VNPayReturn — xử lý callback từ VNPay
- GET /Payment/QRCode/{bookingId} — hiển thị QR code thanh toán

VNPayService:
- CreatePaymentUrl(bookingId, amount, orderInfo): tạo URL redirect VNPay theo chuẩn 2.1
- ValidateCallback(queryParams): xác thực chữ ký HMAC-SHA512
- Tham số bắt buộc: vnp_Version, vnp_Command, vnp_TmnCode, vnp_Amount (x100),
  vnp_CreateDate, vnp_TxnRef, vnp_OrderInfo, vnp_ReturnUrl, vnp_SecureHash
- Sort params theo alphabet trước khi hash

Khi callback hợp lệ và vnp_ResponseCode = "00":
- Cập nhật Payment.Status = Success
- Cập nhật Booking.Status = Confirmed
- Redirect đến /Booking/Confirm/{bookingCode}

Lưu ý: amount = TotalPrice của Booking (đã bao gồm cả tiền ghế + snack)
```

---

### 8. Tạo Admin Dashboard

```
Tạo khu vực Admin cho ASP.NET MVC cinema booking.
Tất cả controller trong vùng Admin đặt trong thư mục Controllers/Admin/.
Dùng [Authorize(Roles = "Admin")] cho toàn bộ.

AdminMovieController:
- Index: GET — danh sách phim, phân trang, tìm kiếm
- Create: GET/POST — thêm phim mới (upload poster)
- Edit: GET/POST — sửa phim
- Delete: POST — xóa phim (soft delete đổi status)

AdminShowtimeController:
- Index: GET — danh sách suất chiếu, lọc theo ngày/phim/rạp
- Create: GET/POST — tạo suất chiếu mới (chọn phim, phòng, ngày giờ, giá)
- Delete: POST — hủy suất chiếu (kiểm tra có booking chưa)

AdminReportController:
- Revenue: GET — báo cáo doanh thu theo khoảng ngày (chart + bảng)
  Doanh thu = tiền vé + tiền snack
- TopMovies: GET — top 10 phim doanh thu cao nhất
- TopSnacks: GET — top snack bán chạy (query BookingSnacks GROUP BY snack_id)
- BookingList: GET — danh sách tất cả đơn đặt vé, lọc theo ngày/trạng thái

Views dùng Bootstrap 5, sidebar navigation, DataTables cho bảng dữ liệu.
```

---

### 9. Tạo Chatbot Service (Groq API)

```
Tạo ChatbotController và ChatbotService cho ASP.NET MVC cinema booking.
Tích hợp Groq API (llama-3.1-8b-instant), đọc key từ appsettings.json.

ChatbotController:
- POST /api/chat — nhận {message, history[]}, trả về {reply}
- Không cần [Authorize] (public)

ChatbotService:
1. Phân tích keyword trong message (fold về lowercase, bỏ dấu)
2. Query đúng bảng DB theo intent:
   - "phim", "đang chiếu" → query Movies status=NOW_SHOWING
   - "lịch chiếu", "giờ chiếu" → query Showtimes hôm nay
   - "giá", "vé" → query giá từ Showtimes
   - "khuyến mãi", "mã giảm" → query Promotions còn hạn
   - "rap", "rạp" → query Cinemas
   - "snack", "đồ ăn", "bắp rang", "nước", "combo" → query Snacks IsAvailable=true
3. Build system prompt chứa data DB
4. Gọi Groq API nếu UseLlm = true, ngược lại trả thẳng DB context
5. Giữ tối đa 10 tin nhắn history

Intent snack ():
Keyword (bỏ dấu): "snack", "an vat", "bap rang", "nuoc uong", "do an", "combo"
Query: _context.Snacks.Where(s => s.IsAvailable).OrderBy(s => s.Category)
Format:
"Đồ ăn vặt hiện có tại rạp:
- {Name} ({Category}): {Price:N0} VND
...
Bạn có thể chọn snack khi đặt vé nhé!"

System prompt chuẩn:
"Bạn là trợ lý rạp phim. Chỉ dùng dữ liệu trong [DỮ LIỆU DB].
Trả lời tiếng Việt, ngắn gọn 2-6 dòng.
Không bịa hotline/website/giá nếu không có trong dữ liệu.
Kết thúc bằng 1 câu gợi ý hành động tiếp theo."

Thêm _ChatbotWidget.cshtml partial view (nút chat nổi góc phải)
và nhúng vào _Layout.cshtml.
```

---

### 10. Tạo Migration + Seed Data

```
Tạo EF Core Migration và Seed Data cho cinema ASP.NET MVC MySQL.

Migration tạo đủ 13 bảng với:
- Index cho: email, booking_code, showtime_id, movie_id, snack_id
- UNIQUE constraint: users.email, promotions.code
- ON DELETE CASCADE: rooms → showtimes, bookings → booking_snacks
- ON DELETE RESTRICT: showtimes → bookings, snacks → booking_snacks

Seed data trong OnModelCreating hoặc DbInitializer.cs:
- 1 tài khoản Admin: admin@cinema.com / Admin@123
- 3-5 phim mẫu (2 NOW_PLAYING, 1 COMING_SOON)
- 2 rạp mẫu, mỗi rạp 2 phòng
- Mỗi phòng có 40 ghế (4 hàng x 10 ghế)
- Vài suất chiếu hôm nay
- 1 mã khuyến mãi: WELCOME10 giảm 10%
- 8 snack mẫu (xem SQL bảng snacks ở phần Database Schema)
```

---

### 11. Tạo Program.cs + DI Configuration

```
Tạo Program.cs cho ASP.NET MVC cinema booking.
Cấu hình đầy đủ:

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Authentication Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

// Redis
builder.Services.AddStackExchangeRedisCache(options => {
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Register Services
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IShowtimeService, ShowtimeService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISnackService, SnackService>();     //
builder.Services.AddScoped<IChatbotService, ChatbotService>();
builder.Services.AddHttpClient();

// MVC
builder.Services.AddControllersWithViews();
```

---

### 12. Tạo AutoMapper Profile

```
Tạo AutoMapper MappingProfile.cs cho cinema ASP.NET MVC.
Map các Entity sang ViewModel:
- Movie → MovieCardViewModel (Id, Title, PosterUrl, Genre, DurationMin, Rating)
- Movie → MovieDetailViewModel (tất cả + list Showtime, Review)
- Showtime → ShowtimeViewModel (Id, StartTime, EndTime, Price, RoomName, CinemaName, AvailableSeats)
- Booking → BookingHistoryViewModel (BookingCode, MovieTitle, ShowtimeDate, TotalPrice, Status, Seats, Snacks)
- Seat → SeatViewModel (Id, SeatRow, SeatNumber, SeatType, Price, Status)
- Snack → SnackViewModel (Id, Name, Description, Price, ImageUrl, Category, Stock, IsAvailable)
- User → UserProfileViewModel (Id, FullName, Email, Phone, CreatedAt)

Dùng Ignore() cho các navigation property không cần map.
```

---

## 🗂️ Thứ tự implement

```
Bước 1  → Tạo project ASP.NET MVC, cài NuGet packages
Bước 2  → Tạo Entity classes (13 bảng, thêm Snack + BookingSnack)
Bước 3  → Tạo AppDbContext, cấu hình Pomelo MySQL
Bước 4  → Tạo Migration + chạy Update-Database
Bước 5  → Seed data (admin account + snack mẫu + dữ liệu khác)
Bước 6  → Cấu hình Program.cs (Auth, Redis, DI)
Bước 7  → AuthController (Login, Register, Logout)
Bước 8  → MovieController + Views (Index, Detail, Search)
Bước 9  → ShowtimeController + SeatMap View
Bước 10 → BookingService + BookingController (SeatMap + Create)
Bước 11 → SnackService + SelectSnack flow
Bước 12 → PaymentController + VNPayService
Bước 13 → ReviewController
Bước 14 → Admin: MovieController, ShowtimeController
Bước 15 → Admin: SnackController (CRUD + upload ảnh)
Bước 16 → Admin: ReportController (thêm TopSnacks)
Bước 17 → ChatbotController + ChatbotService + Widget
Bước 18 → Polish UI (Bootstrap 5, responsive)
```

---

## 🐛 Lỗi thường gặp & cách fix

| Lỗi                          | Nguyên nhân                         | Fix                                           |
| ---------------------------- | ----------------------------------- | --------------------------------------------- |
| `Table doesn't exist`        | Chưa chạy migration                 | `dotnet ef database update`                   |
| `Access denied for user`     | Sai connection string               | Kiểm tra user/password MySQL                  |
| `Object cycle detected`      | Entity có circular reference        | Thêm `JsonIgnore` hoặc dùng ViewModel         |
| `No route matched`           | Sai tên Controller/Action           | Kiểm tra route convention, tên phải đúng      |
| `Redis connection refused`   | Redis chưa chạy                     | `sudo systemctl start redis`                  |
| `Invalid model state`        | Validation fail                     | Kiểm tra DataAnnotations trong ViewModel      |
| `Unauthorized 401`           | Chưa đăng nhập                      | Thêm `[AllowAnonymous]` hoặc đăng nhập        |
| `403 Forbidden`              | Sai role                            | Kiểm tra Claims role khi đăng nhập            |
| `Pomelo version mismatch`    | EF Core và Pomelo không tương thích | Dùng cùng version: EF 8.x + Pomelo 8.x        |
| Stock snack âm               | Không check tồn kho                 | Validate `stock >= quantity` trước khi lưu    |
| Snack bị xóa khi đã có order | Xóa cứng                            | Check BookingSnacks trước, soft delete nếu có |

---

## 🐳 Docker Compose để chạy local

```yaml
version: "3.8"
services:
  mysql:
    image: mysql:8.0
    environment:
      MYSQL_DATABASE: cinemadbaspnet
      MYSQL_ROOT_PASSWORD: yourpassword
    ports:
      - "3306:3306"
    volumes:
      - mysql_data:/var/lib/mysql

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

volumes:
  mysql_data:
```

Chạy: `docker-compose up -d`

---

> 💬 **Tip vibe code:** Khi prompt mỗi task, luôn paste phần **Database Schema** + **Quan hệ chính** vào đầu prompt. Với tính năng snack, nhớ paste thêm **SQL tạo bảng snacks** và **SnackSelectViewModel** để AI sinh đúng flow chọn snack sau khi chọn ghế.
