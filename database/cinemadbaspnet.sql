CREATE DATABASE IF NOT EXISTS cinemadbaspnet CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE cinemadbaspnet;

SET FOREIGN_KEY_CHECKS = 1;

CREATE TABLE users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) NOT NULL,
    email VARCHAR(120) NOT NULL,
    password_hash VARCHAR(120) NOT NULL,
    full_name VARCHAR(120) NULL,
    phone VARCHAR(30) NULL,
    role VARCHAR(20) NOT NULL DEFAULT 'USER',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uq_users_username (username),
    UNIQUE KEY uq_users_email (email)
);

CREATE TABLE auth_tokens (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    access_token VARCHAR(200) NOT NULL,
    refresh_token VARCHAR(200) NOT NULL,
    access_expires_at DATETIME NOT NULL,
    refresh_expires_at DATETIME NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uq_auth_access (access_token),
    UNIQUE KEY uq_auth_refresh (refresh_token),
    CONSTRAINT fk_auth_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);

CREATE TABLE movies (
    id INT AUTO_INCREMENT PRIMARY KEY,
    title VARCHAR(180) NOT NULL,
    genre VARCHAR(80) NULL,
    duration_min INT NULL,
    poster_url VARCHAR(500) NULL,
    trailer_url VARCHAR(500) NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'NOW_SHOWING',
    rating DECIMAL(3, 1) NULL,
    description TEXT NULL
);

CREATE TABLE cinemas (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(120) NOT NULL,
    address VARCHAR(250) NULL,
    city VARCHAR(80) NULL
);

CREATE TABLE rooms (
    id INT AUTO_INCREMENT PRIMARY KEY,
    cinema_id INT NOT NULL,
    name VARCHAR(60) NOT NULL,
    total_seats INT NOT NULL,
    room_type VARCHAR(20) NOT NULL DEFAULT 'NORMAL',
    CONSTRAINT fk_room_cinema FOREIGN KEY (cinema_id) REFERENCES cinemas (id) ON DELETE CASCADE
);

CREATE TABLE seats (
    id INT AUTO_INCREMENT PRIMARY KEY,
    room_id INT NOT NULL,
    seat_row VARCHAR(4) NOT NULL,
    seat_number INT NOT NULL,
    seat_type VARCHAR(20) NOT NULL DEFAULT 'NORMAL',
    extra_price DECIMAL(12, 2) NOT NULL DEFAULT 0,
    CONSTRAINT fk_seat_room FOREIGN KEY (room_id) REFERENCES rooms (id) ON DELETE CASCADE
);

CREATE TABLE showtimes (
    id INT AUTO_INCREMENT PRIMARY KEY,
    movie_id INT NOT NULL,
    room_id INT NOT NULL,
    start_time DATETIME NOT NULL,
    price DECIMAL(12, 2) NOT NULL,
    INDEX idx_showtimes_movie (movie_id),
    INDEX idx_showtimes_room (room_id),
    CONSTRAINT fk_showtime_movie FOREIGN KEY (movie_id) REFERENCES movies (id) ON DELETE RESTRICT,
    CONSTRAINT fk_showtime_room FOREIGN KEY (room_id) REFERENCES rooms (id) ON DELETE CASCADE
);

CREATE TABLE promotions (
    id INT AUTO_INCREMENT PRIMARY KEY,
    code VARCHAR(40) NOT NULL,
    description VARCHAR(250) NULL,
    discount_percent INT NOT NULL,
    valid_from DATETIME NOT NULL,
    valid_to DATETIME NOT NULL,
    max_uses INT NOT NULL,
    used_count INT NOT NULL DEFAULT 0,
    UNIQUE KEY uq_promotion_code (code)
);

CREATE TABLE bookings (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    showtime_id INT NOT NULL,
    booking_code VARCHAR(20) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    total_price DECIMAL(12, 2) NOT NULL,
    promotion_id INT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uq_booking_code (booking_code),
    INDEX idx_booking_showtime (showtime_id),
    CONSTRAINT fk_booking_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT fk_booking_showtime FOREIGN KEY (showtime_id) REFERENCES showtimes (id) ON DELETE RESTRICT,
    CONSTRAINT fk_booking_promotion FOREIGN KEY (promotion_id) REFERENCES promotions (id) ON DELETE SET NULL
);

CREATE TABLE booking_seats (
    id INT AUTO_INCREMENT PRIMARY KEY,
    booking_id INT NOT NULL,
    seat_id INT NOT NULL,
    price DECIMAL(12, 2) NOT NULL,
    INDEX idx_booking_seat_booking (booking_id),
    INDEX idx_booking_seat_seat (seat_id),
    CONSTRAINT fk_booking_seat_booking FOREIGN KEY (booking_id) REFERENCES bookings (id) ON DELETE CASCADE,
    CONSTRAINT fk_booking_seat_seat FOREIGN KEY (seat_id) REFERENCES seats (id) ON DELETE RESTRICT
);

CREATE TABLE reviews (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    movie_id INT NOT NULL,
    rating INT NOT NULL,
    comment TEXT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_review_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT fk_review_movie FOREIGN KEY (movie_id) REFERENCES movies (id) ON DELETE CASCADE
);

INSERT INTO
    users (
        username,
        email,
        password_hash,
        full_name,
        phone,
        role
    )
VALUES (
        'admin',
        'admin@cinema.com',
        'Admin@123',
        'Quoc Phim Admin',
        '0900000000',
        'ADMIN'
    ),
    (
        'user',
        'user@cinema.com',
        'User@123',
        'Nguyen Van User',
        '0911222333',
        'USER'
    );

INSERT INTO
    movies (
        title,
        genre,
        duration_min,
        poster_url,
        status,
        rating,
        description
    )
VALUES (
        'Interstellar',
        'Sci-Fi',
        169,
        'https://images.unsplash.com/photo-1478720568477-152d9b164e26?w=400',
        'NOW_SHOWING',
        9.0,
        'Hanh trinh xuyen khong gian tim hanh tinh moi cho loai nguoi.'
    ),
    (
        'Avengers: Endgame',
        'Action',
        181,
        'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=400',
        'NOW_SHOWING',
        8.8,
        'Tran chien cuoi cung cua cac sieu anh hung.'
    ),
    (
        'Inside Out 2',
        'Animation',
        100,
        'https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=400',
        'COMING_SOON',
        8.2,
        'Nhung cam xuc tuoi teen day bat ngo.'
    );

INSERT INTO
    cinemas (name, address, city)
VALUES (
        'Quoc Phim Center',
        '1 Nguyen Hue',
        'TP.HCM'
    ),
    (
        'Quoc Phim Landmark',
        '720 Dien Bien Phu',
        'TP.HCM'
    );

INSERT INTO
    rooms (
        cinema_id,
        name,
        total_seats,
        room_type
    )
VALUES (1, 'Room A', 40, 'NORMAL'),
    (2, 'Room B', 40, 'IMAX');

INSERT INTO
    seats (
        room_id,
        seat_row,
        seat_number,
        seat_type,
        extra_price
    )
VALUES (1, 'A', 1, 'NORMAL', 0),
    (1, 'A', 2, 'NORMAL', 0),
    (1, 'A', 3, 'NORMAL', 0),
    (1, 'A', 4, 'NORMAL', 0),
    (1, 'A', 5, 'NORMAL', 0),
    (1, 'A', 6, 'NORMAL', 0),
    (1, 'A', 7, 'NORMAL', 0),
    (1, 'A', 8, 'NORMAL', 0),
    (1, 'A', 9, 'NORMAL', 0),
    (1, 'A', 10, 'NORMAL', 0),
    (1, 'B', 1, 'NORMAL', 0),
    (1, 'B', 2, 'NORMAL', 0),
    (1, 'B', 3, 'NORMAL', 0),
    (1, 'B', 4, 'NORMAL', 0),
    (1, 'B', 5, 'NORMAL', 0),
    (1, 'B', 6, 'NORMAL', 0),
    (1, 'B', 7, 'NORMAL', 0),
    (1, 'B', 8, 'NORMAL', 0),
    (1, 'B', 9, 'NORMAL', 0),
    (1, 'B', 10, 'NORMAL', 0),
    (1, 'C', 1, 'VIP', 20000),
    (1, 'C', 2, 'VIP', 20000),
    (1, 'C', 3, 'VIP', 20000),
    (1, 'C', 4, 'VIP', 20000),
    (1, 'C', 5, 'VIP', 20000),
    (1, 'C', 6, 'VIP', 20000),
    (1, 'C', 7, 'VIP', 20000),
    (1, 'C', 8, 'VIP', 20000),
    (1, 'C', 9, 'VIP', 20000),
    (1, 'C', 10, 'VIP', 20000),
    (1, 'D', 1, 'VIP', 20000),
    (1, 'D', 2, 'VIP', 20000),
    (1, 'D', 3, 'VIP', 20000),
    (1, 'D', 4, 'VIP', 20000),
    (1, 'D', 5, 'VIP', 20000),
    (1, 'D', 6, 'VIP', 20000),
    (1, 'D', 7, 'VIP', 20000),
    (1, 'D', 8, 'VIP', 20000),
    (1, 'D', 9, 'VIP', 20000),
    (1, 'D', 10, 'VIP', 20000),
    (2, 'A', 1, 'NORMAL', 0),
    (2, 'A', 2, 'NORMAL', 0),
    (2, 'A', 3, 'NORMAL', 0),
    (2, 'A', 4, 'NORMAL', 0),
    (2, 'A', 5, 'NORMAL', 0),
    (2, 'A', 6, 'NORMAL', 0),
    (2, 'A', 7, 'NORMAL', 0),
    (2, 'A', 8, 'NORMAL', 0),
    (2, 'A', 9, 'NORMAL', 0),
    (2, 'A', 10, 'NORMAL', 0),
    (2, 'B', 1, 'NORMAL', 0),
    (2, 'B', 2, 'NORMAL', 0),
    (2, 'B', 3, 'NORMAL', 0),
    (2, 'B', 4, 'NORMAL', 0),
    (2, 'B', 5, 'NORMAL', 0),
    (2, 'B', 6, 'NORMAL', 0),
    (2, 'B', 7, 'NORMAL', 0),
    (2, 'B', 8, 'NORMAL', 0),
    (2, 'B', 9, 'NORMAL', 0),
    (2, 'B', 10, 'NORMAL', 0),
    (2, 'C', 1, 'VIP', 20000),
    (2, 'C', 2, 'VIP', 20000),
    (2, 'C', 3, 'VIP', 20000),
    (2, 'C', 4, 'VIP', 20000),
    (2, 'C', 5, 'VIP', 20000),
    (2, 'C', 6, 'VIP', 20000),
    (2, 'C', 7, 'VIP', 20000),
    (2, 'C', 8, 'VIP', 20000),
    (2, 'C', 9, 'VIP', 20000),
    (2, 'C', 10, 'VIP', 20000),
    (2, 'D', 1, 'VIP', 20000),
    (2, 'D', 2, 'VIP', 20000),
    (2, 'D', 3, 'VIP', 20000),
    (2, 'D', 4, 'VIP', 20000),
    (2, 'D', 5, 'VIP', 20000),
    (2, 'D', 6, 'VIP', 20000),
    (2, 'D', 7, 'VIP', 20000),
    (2, 'D', 8, 'VIP', 20000),
    (2, 'D', 9, 'VIP', 20000),
    (2, 'D', 10, 'VIP', 20000);

INSERT INTO
    showtimes (
        movie_id,
        room_id,
        start_time,
        price
    )
VALUES (
        1,
        1,
        DATE_ADD(CURDATE(), INTERVAL 12 HOUR),
        85000
    ),
    (
        1,
        1,
        DATE_ADD(CURDATE(), INTERVAL 18 HOUR),
        85000
    ),
    (
        2,
        2,
        DATE_ADD(CURDATE(), INTERVAL 15 HOUR),
        95000
    ),
    (
        3,
        2,
        DATE_ADD(
            DATE_ADD(CURDATE(), INTERVAL 3 DAY),
            INTERVAL 16 HOUR
        ),
        90000
    );

INSERT INTO
    promotions (
        code,
        description,
        discount_percent,
        valid_from,
        valid_to,
        max_uses,
        used_count
    )
VALUES (
        'WELCOME10',
        'Giam 10% cho user moi',
        10,
        DATE_SUB(NOW(), INTERVAL 7 DAY),
        DATE_ADD(NOW(), INTERVAL 3 MONTH),
        500,
        0
    );

CREATE TABLE snacks (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(120) NOT NULL,
    description VARCHAR(250) NULL,
    price DECIMAL(12, 2) NOT NULL,
    image_url VARCHAR(500) NULL,
    category VARCHAR(40) NOT NULL DEFAULT 'FOOD', -- FOOD / DRINK / COMBO
    stock INT NOT NULL DEFAULT 0,
    is_available TINYINT(1) NOT NULL DEFAULT 1,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE booking_snacks (
    id INT AUTO_INCREMENT PRIMARY KEY,
    booking_id INT NOT NULL,
    snack_id INT NOT NULL,
    quantity INT NOT NULL DEFAULT 1,
    price DECIMAL(12, 2) NOT NULL, -- giá tại thời điểm đặt
    INDEX idx_booking_snack_booking (booking_id),
    INDEX idx_booking_snack_snack (snack_id),
    CONSTRAINT fk_booking_snack_booking FOREIGN KEY (booking_id) REFERENCES bookings (id) ON DELETE CASCADE,
    CONSTRAINT fk_booking_snack_snack FOREIGN KEY (snack_id) REFERENCES snacks (id) ON DELETE RESTRICT
);

INSERT INTO
    snacks (
        name,
        description,
        price,
        category,
        stock,
        is_available
    )
VALUES (
        'Bắp rang bơ nhỏ',
        'Bắp rang bơ size S',
        35000,
        'FOOD',
        100,
        1
    ),
    (
        'Bắp rang bơ lớn',
        'Bắp rang bơ size L',
        55000,
        'FOOD',
        100,
        1
    ),
    (
        'Pepsi lon',
        'Pepsi 330ml lon',
        25000,
        'DRINK',
        200,
        1
    ),
    (
        'Coca-Cola lon',
        'Coca-Cola 330ml lon',
        25000,
        'DRINK',
        200,
        1
    ),
    (
        'Nước suối',
        'Aquafina 500ml',
        15000,
        'DRINK',
        200,
        1
    ),
    (
        'Combo Bắp + Nước',
        'Bắp rang lớn + Pepsi lon',
        70000,
        'COMBO',
        80,
        1
    ),
    (
        'Khoai tây chiên',
        'Khoai tây chiên giòn',
        40000,
        'FOOD',
        80,
        1
    ),
    (
        'Kẹo dẻo',
        'Kẹo dẻo thập cẩm 100g',
        20000,
        'FOOD',
        150,
        1
    );