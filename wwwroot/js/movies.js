const API = '/api';
const PAGE_SIZE = 12;
let currentPage = 0;
let totalPages = 0;
let totalElements = 0;
let suggestionsTimeout;
let cinemsList = [];

function renderNavAuth() {
  const el = document.getElementById('navAuth');
  if (!el) return;
  if (typeof AUTH !== 'undefined' && AUTH.isLoggedIn()) {
    el.innerHTML = '<button class="btn-logout" onclick="AUTH.clear();location.reload()">Đăng xuất</button>';
  } else {
    el.innerHTML = '<a href="/login" class="btn-login">Đăng nhập</a>';
  }
}

// Load cinemas for filter dropdown
async function loadCinemas() {
  console.log('📍 Loading cinemas...');
  try {
    const response = await fetch(`${API}/cinemas`);
    const data = await response.json();
    console.log('Cinemas API response:', data);
    
    cinemsList = Array.isArray(data) ? data : (data?.content || []);
    const cinemaSelect = document.getElementById('cinemaSelect');
    
    console.log('cinemaSelect element:', cinemaSelect);
    console.log('Cinemas count:', cinemsList.length);
    
    if (!cinemaSelect) {
      console.error('❌ cinemaSelect element not found!');
      return;
    }
    
    if (cinemsList.length === 0) {
      console.warn('⚠️ No cinemas returned');
      return;
    }
    
    // Clear existing options except first one
    while (cinemaSelect.options.length > 1) {
      cinemaSelect.remove(1);
    }
    
    // Add options
    for (const cinema of cinemsList) {
      const option = new Option(cinema.name || cinema.Name, cinema.id);
      cinemaSelect.appendChild(option);
      console.log(`✓ Added cinema: ${cinema.name}`);
    }
    
    console.log(`✅ Successfully loaded ${cinemsList.length} cinemas`);
  } catch (error) {
    console.error('❌ Error loading cinemas:', error);
  }
}

// Load genres from database
async function loadGenres() {
  console.log('🎬 Loading genres...');
  try {
    const response = await fetch(`${API}/movies/genres`);
    const genres = await response.json();
    console.log('Genres API response:', genres);
    
    const genreSelect = document.getElementById('genreSelect');
    console.log('genreSelect element:', genreSelect);
    console.log('Genres count:', Array.isArray(genres) ? genres.length : 'not an array');
    
    if (!genreSelect) {
      console.error('❌ genreSelect element not found!');
      return;
    }
    
    if (!Array.isArray(genres) || genres.length === 0) {
      console.warn('⚠️ No genres returned or invalid format');
      return;
    }
    
    // Clear existing options except first one
    while (genreSelect.options.length > 1) {
      genreSelect.remove(1);
    }
    
    // Add options
    for (const genre of genres) {
      const option = new Option(genre, genre);
      genreSelect.appendChild(option);
      console.log(`✓ Added genre: ${genre}`);
    }
    
    console.log(`✅ Successfully loaded ${genres.length} genres`);
  } catch (error) {
    console.error('❌ Error loading genres:', error);
  }
}

// Real-time search suggestions
async function fetchSuggestions(query) {
  if (!query || query.length < 1) {
    hideSuggestionsDropdown();
    return;
  }

  try {
    const genre = document.getElementById('genreSelect')?.value?.trim() || '';
    const cinemaId = document.getElementById('cinemaSelect')?.value?.trim() || '';

    let url = `${API}/movies/suggestions?q=${encodeURIComponent(query)}`;
    if (genre) url += `&genre=${encodeURIComponent(genre)}`;
    if (cinemaId) url += `&cinemaId=${cinemaId}`;

    const response = await fetch(url);
    const suggestions = await response.json();
    
    displaySuggestions(Array.isArray(suggestions) ? suggestions : []);
  } catch (error) {
    console.error('Error fetching suggestions:', error);
  }
}

// Display suggestions in dropdown
function displaySuggestions(suggestions) {
  const dropdown = document.getElementById('suggestionsDropdown');
  const list = document.getElementById('suggestionsList');
  
  if (!list) return;
  
  if (!suggestions || suggestions.length === 0) {
    hideSuggestionsDropdown();
    return;
  }

  list.innerHTML = suggestions.map(movie => `
    <li class="suggestion-item" onclick="selectSuggestion('${movie.title}', ${movie.id})">
      <div style="display: flex; gap: 0.5rem;">
        <img src="${movie.posterUrl || 'https://via.placeholder.com/40x60?text=No+Image'}" 
             alt="${movie.title}" 
             style="width: 30px; height: 45px; object-fit: cover; border-radius: 3px;">
        <div style="flex: 1; overflow: hidden;">
          <div style="font-weight: 600; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; color: #ffffff;">
            ${movie.title}
          </div>
          <div style="font-size: 0.75rem; color: #888;">
            ${movie.genre || 'N/A'} • ⭐ ${movie.rating != null ? movie.rating : 'N/A'} • ${movie.durationMin || 0}p
          </div>
        </div>
      </div>
    </li>
  `).join('');

  dropdown.style.display = 'block';
}

// Hide suggestions dropdown
function hideSuggestionsDropdown() {
  const dropdown = document.getElementById('suggestionsDropdown');
  if (dropdown) dropdown.style.display = 'none';
}

// Select a suggestion
function selectSuggestion(title, movieId) {
  const input = document.getElementById('searchInput');
  if (input) input.value = title;
  hideSuggestionsDropdown();
  currentPage = 0;
  searchMovies(0);
}

async function displayMovies(movies, meta) {
  const grid = document.getElementById('moviesGrid');
  if (!grid) return;
  const list = Array.isArray(movies) ? movies : (movies?.content || []);
  grid.innerHTML = list.map(m => `
    <div class="movie-card">
      <a href="/pages/movie-detail?id=${m.id}">
        <img src="${m.posterUrl || 'https://via.placeholder.com/250x350?text=No+Image'}" alt="${m.title}" class="movie-poster">
      </a>
      <div class="movie-info">
        <a href="/pages/movie-detail?id=${m.id}" style="text-decoration:none;color:inherit">
          <div class="movie-title">${m.title}</div>
        </a>
        <div class="movie-genre">${m.genre || 'N/A'}</div>
        <div class="movie-rating">⭐ ${m.rating != null ? m.rating : 'N/A'}</div>
        <p style="font-size:0.85rem;color:#888;margin-top:0.5rem">${m.durationMin || 0} phút</p>
        <div id="showtimes-${m.id}" style="font-size:0.85rem;color:#e50914;margin-top:0.5rem;min-height:1.5rem">
          <span style="color:#aaa">Đang tải suất chiếu...</span>
        </div>
        <a href="/pages/booking?movieId=${m.id}" class="btn btn-primary" style="width:100%;margin-top:0.5rem;text-align:center;display:block">Đặt vé</a>
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
  const cinemaId = document.getElementById('cinemaSelect')?.value?.trim();
  
  if (q || genre || cinemaId) {
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
  const cinemaId = document.getElementById('cinemaSelect')?.value?.trim() || '';
  
  currentPage = page;
  
  let url = `${API}/movies/search?q=${encodeURIComponent(q)}&genre=${encodeURIComponent(genre)}&page=${page}&size=${PAGE_SIZE}`;
  if (cinemaId) url += `&cinemaId=${cinemaId}`;
  
  fetch(url)
    .then(res => res.json())
    .then(data => {
      const list = Array.isArray(data) ? data : (data?.content || []);
      const meta = {
        totalPages: data?.totalPages ?? 0,
        totalElements: data?.totalElements ?? list.length
      };
      displayMovies(list, meta);
      hideSuggestionsDropdown();
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
  console.log('🎬 ========== Movies Page Initializing ==========');
  
  renderNavAuth();
  
  // Load cinemas and genres immediately
  loadCinemas();
  loadGenres();
  
  // Load movies
  loadMovies();
  
  // Real-time search with debouncing
  const searchInput = document.getElementById('searchInput');
  if (searchInput) {
    searchInput.addEventListener('input', (e) => {
      clearTimeout(suggestionsTimeout);
      const query = e.target.value.trim();
      
      if (query.length >= 1) {
        suggestionsTimeout = setTimeout(() => {
          fetchSuggestions(query);
        }, 300); // 300ms debounce
      } else {
        hideSuggestionsDropdown();
      }
    });
    
    searchInput.addEventListener('keypress', (e) => {
      if (e.key === 'Enter') {
        clearTimeout(suggestionsTimeout);
        hideSuggestionsDropdown();
        currentPage = 0;
        searchMovies(0);
      }
    });
  }

  // Genre filter change
  const genreSelect = document.getElementById('genreSelect');
  if (genreSelect) {
    genreSelect.addEventListener('change', () => {
      console.log('🎭 Genre changed:', genreSelect.value);
      currentPage = 0;
      searchMovies(0);
    });
  } else {
    console.warn('⚠️ genreSelect not found during event listener setup');
  }

  // Cinema filter change
  const cinemaSelect = document.getElementById('cinemaSelect');
  if (cinemaSelect) {
    cinemaSelect.addEventListener('change', () => {
      console.log('🏢 Cinema changed:', cinemaSelect.value);
      currentPage = 0;
      searchMovies(0);
    });
  } else {
    console.warn('⚠️ cinemaSelect not found during event listener setup');
  }

  // Close suggestions when clicking outside
  document.addEventListener('click', (e) => {
    const dropdown = document.getElementById('suggestionsDropdown');
    if (dropdown && !e.target.closest('.filter-section')) {
      hideSuggestionsDropdown();
    }
  });
  
  console.log('✅ ========== Movies Page Ready ==========');
});
