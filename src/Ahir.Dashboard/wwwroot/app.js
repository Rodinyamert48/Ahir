const API = 'http://localhost:8080';
let token = localStorage.getItem('ahir_token');

async function api(path, options = {}) {
    const headers = { 'Content-Type': 'application/json' };
    if (token) headers['Authorization'] = `Bearer ${token}`;
    const res = await fetch(`${API}${path}`, { ...options, headers });
    if (res.status === 401 && path !== '/api/v1/auth/login') { logout(); return null; }
    return res.json();
}

async function login() {
    const username = document.getElementById('login-username').value;
    const password = document.getElementById('login-password').value;
    const err = document.getElementById('login-error');
    const res = await fetch(`${API}/api/v1/auth/login`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password })
    });
    const data = await res.json();
    if (data.token) {
        token = data.token.accessToken;
        localStorage.setItem('ahir_token', token);
        showMain();
    } else { err.textContent = 'Login failed'; }
}

function logout() {
    token = null; localStorage.removeItem('ahir_token');
    document.getElementById('main-screen').classList.add('hidden');
    document.getElementById('login-screen').classList.remove('hidden');
}

async function showMain() {
    document.getElementById('login-screen').classList.add('hidden');
    document.getElementById('main-screen').classList.remove('hidden');
    showPage('overview');
}

function showPage(name) {
    document.querySelectorAll('.page').forEach(p => p.classList.add('hidden'));
    document.getElementById(`page-${name}`).classList.remove('hidden');
    if (name === 'overview') loadOverview();
    if (name === 'databases') loadDatabases();
    if (name === 'backups') loadBackups();
}

async function loadOverview() {
    const [info, metrics] = await Promise.all([
        api('/api/v1/server/info'), api('/api/v1/metrics')
    ]);
    const grid = document.getElementById('system-info');
    const items = [
        { label: 'Status', value: info?.state || 'N/A' },
        { label: 'Uptime', value: formatUptime(info?.uptime) },
        { label: 'Instance', value: info?.instanceId?.slice(0, 8) || 'N/A' },
    ];
    grid.innerHTML = items.map(i => `<div class="stat-card"><div class="label">${i.label}</div><div class="value">${i.value}</div></div>`).join('');

    const mgrid = document.getElementById('metrics-display');
    const m = metrics || {};
    const mitems = [
        { label: 'CPU', value: `${m.cpuUsagePercent ?? 0}%` },
        { label: 'Memory', value: formatBytes(m.memoryUsageBytes) },
        { label: 'Database', value: formatBytes(m.databaseSizeBytes) },
        { label: 'Requests', value: m.totalRequests ?? 0 },
        { label: 'Connections', value: m.activeConnections ?? 0 },
        { label: 'WebSockets', value: m.activeWebSockets ?? 0 },
    ];
    mgrid.innerHTML = mitems.map(i => `<div class="stat-card"><div class="label">${i.label}</div><div class="value">${i.value}</div></div>`).join('');
}

async function loadDatabases() {
    const dbs = await api('/api/v1/databases') || [];
    let html = `<table><tr><th>Name</th><th>Collections</th><th>Records</th><th>Size</th><th></th></tr>`;
    for (const db of dbs) {
        html += `<tr><td>${db.name}</td><td>${db.collectionCount}</td><td>${db.recordCount}</td><td>${formatBytes(db.sizeBytes)}</td>`;
        html += `<td><button class="btn-sm" onclick="exploreDb('${db.name}')">Explore</button></td></tr>`;
    }
    html += `</table>`;
    document.getElementById('database-list').innerHTML = html;
}

async function exploreDb(name) {
    const cols = await api(`/api/v1/databases/${name}/collections`) || [];
    let html = `<h3>${name}</h3><table><tr><th>Collection</th><th>Records</th><th></th></tr>`;
    for (const c of cols) {
        html += `<tr><td>${c.name}</td><td>${c.recordCount}</td>`;
        html += `<td><button class="btn-sm" onclick="viewRecords('${name}','${c.name}')">View Records</button></td></tr>`;
    }
    html += `</table>`;
    document.getElementById('collection-view').classList.remove('hidden');
    document.getElementById('collection-title').textContent = `Database: ${name}`;
    document.getElementById('records-display').innerHTML = html;
}

async function viewRecords(db, col) {
    const res = await api(`/api/v1/databases/${db}/collections/${col}/records/query`, {
        method: 'POST', body: JSON.stringify({ page: 1, pageSize: 20 })
    });
    const data = res?.items || [];
    if (data.length === 0) { document.getElementById('records-display').innerHTML = '<p>No records found.</p>'; return; }
    const keys = Object.keys(data[0].fields || {});
    let html = `<p>${res.totalCount} records</p><table><tr><th>ID</th>${keys.map(k => `<th>${k}</th>`).join('')}</tr>`;
    for (const r of data) {
        html += `<tr><td style="font-family:monospace;font-size:11px">${r.id.slice(0,8)}</td>`;
        html += keys.map(k => `<td>${r.fields?.[k] ?? ''}</td>`).join('');
        html += `</tr>`;
    }
    html += `</table>`;
    document.getElementById('records-display').innerHTML = html;
}

async function loadBackups() {
    const res = await api('/api/v1/backup') || [];
    let html = `<table><tr><th>ID</th><th>Type</th><th>Status</th><th>Size</th><th>Date</th><th></th></tr>`;
    for (const b of res) {
        html += `<tr><td style="font-family:monospace;font-size:11px">${b.id?.slice(0,12)}</td>`;
        html += `<td>${b.type}</td><td>${b.status}</td><td>${formatBytes(b.sizeBytes)}</td>`;
        html += `<td>${b.startedAt ? new Date(b.startedAt).toLocaleString() : ''}</td>`;
        html += `<td><button class="btn-sm" onclick="restoreBackup('${b.id}')">Restore</button></td></tr>`;
    }
    html += `</table>`;
    document.getElementById('backup-list').innerHTML = html;
}

async function createBackup() {
    await api('/api/v1/backup', { method: 'POST', body: '{}' });
    loadBackups();
}

async function restoreBackup(id) {
    if (!confirm('Restore this backup? This will overwrite current data.')) return;
    await api(`/api/v1/backup/${id}/restore`, { method: 'POST' });
    loadBackups();
}

function formatUptime(u) {
    if (!u) return 'N/A';
    const parts = u.replace(':', '.').split('.');
    return parts[0] + 's';
}

function formatBytes(b) {
    if (!b || b === 0) return '0 B';
    const sizes = ['B','KB','MB','GB']; const i = Math.floor(Math.log(b)/Math.log(1024));
    return (b/Math.pow(1024,i)).toFixed(1) + ' ' + sizes[i];
}

if (token) showMain();
