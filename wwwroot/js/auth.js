// Auth utilities
const AUTH = {
    getToken() {
        return localStorage.getItem('accessToken');
    },
    getRefreshToken() {
        return localStorage.getItem('refreshToken');
    },
    setTokens(access, refresh) {
        localStorage.setItem('accessToken', access);
        if (refresh) localStorage.setItem('refreshToken', refresh);
    },
    clear() {
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
    },
    getAuthHeader() {
        const t = this.getToken();
        return t ? { 'Authorization': 'Bearer ' + t } : {};
    },
    isLoggedIn() {
        return !!this.getToken();
    },
    async refreshToken() {
        const rt = this.getRefreshToken();
        if (!rt) return false;
        try {
            const res = await fetch('/api/auth/refresh', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ refreshToken: rt })
            });
            if (res.ok) {
                const d = await res.json();
                this.setTokens(d.accessToken, d.refreshToken);
                return true;
            }
        } catch (e) {}
        return false;
    },
    async fetchWithAuth(url, opts = {}) {
        let headers = { ...opts.headers, ...this.getAuthHeader() };
        let res = await fetch(url, { ...opts, headers });
        if (res.status === 401 && await this.refreshToken()) {
            headers = { ...opts.headers, ...this.getAuthHeader() };
            res = await fetch(url, { ...opts, headers });
        }
        return res;
    }
};
