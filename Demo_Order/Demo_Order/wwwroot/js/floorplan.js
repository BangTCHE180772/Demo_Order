// ===== FLOOR PLAN JS =====

let currentTableId = null;
let connection = null;

// ===== SIGNALR CONNECTION =====
async function initSignalR() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/orderHub")
        .withAutomaticReconnect()
        .build();

    // Nhận order mới
    connection.on("ReceiveNewOrder", (tableId, tableName, orderSummary, totalAmount) => {
        showToast('order', `🍽️ ${tableName}`, `${orderSummary}`, `Tổng: ${formatPrice(totalAmount)}`);
        refreshTableCard(tableId);
        playNotificationSound();

        // Nếu modal đang mở cho bàn này, refresh detail
        if (currentTableId === tableId) {
            loadTableDetail(tableId);
        }
    });

    // Nhận gọi nhân viên
    connection.on("ReceiveCallStaff", (tableId, tableName) => {
        showToast('call', `🔔 Gọi nhân viên!`, `${tableName} đang yêu cầu hỗ trợ`, 'Bấm để xem chi tiết');
        highlightTableCard(tableId);
        playNotificationSound();
    });

    // Nhận thay đổi trạng thái bàn
    connection.on("ReceiveTableStatusChanged", (tableId, status, statusText) => {
        updateTableCardStatus(tableId, status, statusText);
    });

    try {
        await connection.start();
        console.log("SignalR connected");
        // Join staff group
        await connection.invoke("JoinStaffGroup");
    } catch (err) {
        console.error("SignalR connection error:", err);
        setTimeout(initSignalR, 5000);
    }
}

// ===== TABLE CARD ACTIONS =====
function openTableDetail(tableId) {
    currentTableId = tableId;
    loadTableDetail(tableId);
    loadQrCode(tableId);
    new bootstrap.Modal(document.getElementById('tableDetailModal')).show();
}

async function loadTableDetail(tableId) {
    try {
        const response = await fetch(`/Table/GetTableDetail/${tableId}`);
        const data = await response.json();

        // Update modal header
        document.getElementById('modal-table-name').textContent = data.table.tableName;
        const statusBadge = document.getElementById('modal-table-status');
        statusBadge.textContent = data.table.statusText;
        statusBadge.className = `fp-modal-status-badge ${data.table.statusCss}`;

        if (data.table.statusCss === 'serving') {
            statusBadge.style.background = 'rgba(245, 158, 11, 0.15)';
            statusBadge.style.color = '#fbbf24';
        } else {
            statusBadge.style.background = 'rgba(34, 197, 94, 0.15)';
            statusBadge.style.color = '#4ade80';
        }

        // Update order section
        const orderContent = document.getElementById('order-detail-content');
        if (data.order && data.order.items && data.order.items.length > 0) {
            let html = `<div class="fp-order-id" style="font-size:0.75rem; color:rgba(255,255,255,0.4); margin-bottom:0.75rem;">Mã đơn: #${data.order.orderId} · ${data.order.createdAt}</div>`;
            data.order.items.forEach(item => {
                html += `
                    <div class="fp-order-item">
                        <div>
                            <div class="fp-order-item-name">${item.name}</div>
                            <div class="fp-order-item-time">Đặt lúc ${item.orderedAt}</div>
                        </div>
                        <div style="display:flex; align-items:center; gap:0.75rem;">
                            <span class="fp-order-item-qty">x${item.quantity}</span>
                            <span class="fp-order-item-price">${formatPrice(item.price * item.quantity)}</span>
                        </div>
                    </div>`;
            });
            html += `
                <div class="fp-order-total">
                    <span>Tổng cộng (${data.order.totalItems} món)</span>
                    <span class="fp-order-total-amount">${formatPrice(data.order.totalAmount)}</span>
                </div>`;
            orderContent.innerHTML = html;
        } else {
            orderContent.innerHTML = `
                <div class="fp-no-order">
                    <span class="fp-no-order-icon">🍽️</span>
                    <p>Chưa có đơn hàng</p>
                </div>`;
        }

        // Update modal actions
        renderModalActions(data.table, data.order);
    } catch (err) {
        console.error("Load table detail error:", err);
    }
}

async function loadQrCode(tableId) {
    const wrapper = document.getElementById('qr-code-wrapper');
    wrapper.innerHTML = '<div class="spinner-border text-light" role="status"></div>';

    try {
        const response = await fetch(`/Table/GetQrCode/${tableId}`);
        const data = await response.json();

        wrapper.innerHTML = `<img src="data:image/png;base64,${data.qrBase64}" alt="QR Code" />`;
        document.getElementById('qr-url').textContent = data.orderUrl;
        document.getElementById('qr-link').href = data.orderUrl;
    } catch (err) {
        wrapper.innerHTML = '<p style="color:#f87171;">Lỗi tạo QR</p>';
    }
}

function renderModalActions(table, order) {
    const actionsDiv = document.getElementById('modal-actions');
    let html = '';

    if (table.status === 'Available') {
        html += `<button class="fp-btn fp-btn-serving" onclick="setTableStatus(${table.tableId}, 'Serving')">
                    🟡 Bật phục vụ
                 </button>`;
    } else if (table.status === 'Serving') {
        html += `<button class="fp-btn fp-btn-available" onclick="setTableStatus(${table.tableId}, 'Available')">
                    🟢 Tắt phục vụ
                 </button>`;
        if (order && order.items && order.items.length > 0) {
            html += `<button class="fp-btn fp-btn-close-table" onclick="closeTable(${table.tableId})">
                        🔴 Đóng bàn & Thanh toán
                     </button>`;
        }
    }

    actionsDiv.innerHTML = html;
}

async function setTableStatus(tableId, status) {
    try {
        const response = await fetch(`/Table/SetStatus/${tableId}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ status })
        });

        const data = await response.json();
        if (data.success) {
            loadTableDetail(tableId);
            updateTableCardStatus(tableId, data.status, data.statusText);
            updateStats();
        }
    } catch (err) {
        console.error("Set status error:", err);
    }
}

async function closeTable(tableId) {
    if (!confirm('Bạn có chắc muốn đóng bàn và thanh toán?')) return;

    try {
        const response = await fetch(`/Table/CloseTable/${tableId}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' }
        });

        const data = await response.json();
        if (data.success) {
            bootstrap.Modal.getInstance(document.getElementById('tableDetailModal')).hide();
            refreshAllTables();
            showToast('order', '✅ Đóng bàn', `Bàn ${tableId} đã được đóng và thanh toán`, '');
        }
    } catch (err) {
        console.error("Close table error:", err);
    }
}

// ===== UI HELPERS =====
function updateTableCardStatus(tableId, status, statusText) {
    const card = document.getElementById(`table-card-${tableId}`);
    if (!card) return;

    card.className = `fp-table-card ${status.toLowerCase()}`;
    card.querySelector('.fp-table-badge').textContent = statusText;
    updateStats();
}

function highlightTableCard(tableId) {
    const card = document.getElementById(`table-card-${tableId}`);
    if (!card) return;
    card.classList.add('has-notification');
    setTimeout(() => card.classList.remove('has-notification'), 1000);
}

async function refreshTableCard(tableId) {
    try {
        const response = await fetch(`/Table/GetTableDetail/${tableId}`);
        const data = await response.json();

        const orderInfo = document.getElementById(`order-info-${tableId}`);
        if (data.order && data.order.totalItems > 0) {
            orderInfo.style.display = 'block';
            orderInfo.querySelector('.fp-order-count').textContent = `${data.order.totalItems} món · ${formatPrice(data.order.totalAmount)}`;
        } else {
            orderInfo.style.display = 'none';
        }

        highlightTableCard(tableId);
    } catch (err) {
        console.error("Refresh table card error:", err);
    }
}

async function refreshAllTables() {
    try {
        const response = await fetch('/Table/GetAllTables');
        const tables = await response.json();

        tables.forEach(t => {
            updateTableCardStatus(t.tableId, t.status, t.statusText);

            const orderInfo = document.getElementById(`order-info-${t.tableId}`);
            if (t.totalItems > 0) {
                orderInfo.style.display = 'block';
                orderInfo.querySelector('.fp-order-count').textContent = `${t.totalItems} món · ${formatPrice(t.totalAmount)}`;
            } else {
                orderInfo.style.display = 'none';
            }
        });

        updateStats();
    } catch (err) {
        console.error("Refresh all tables error:", err);
    }
}

function updateStats() {
    const cards = document.querySelectorAll('.fp-table-card');
    let available = 0, serving = 0;
    cards.forEach(card => {
        if (card.classList.contains('available')) available++;
        if (card.classList.contains('serving')) serving++;
    });
    document.getElementById('count-available').textContent = available;
    document.getElementById('count-serving').textContent = serving;
}

// ===== TOAST NOTIFICATIONS =====
function showToast(type, title, message, extra) {
    const container = document.getElementById('toast-container');
    const toast = document.createElement('div');
    toast.className = `fp-toast toast-${type}`;

    const icon = type === 'order' ? '🍽️' : '🔔';
    const now = new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });

    toast.innerHTML = `
        <span class="fp-toast-icon">${icon}</span>
        <div class="fp-toast-body">
            <div class="fp-toast-title">${title}</div>
            <div class="fp-toast-message">${message}</div>
            ${extra ? `<div class="fp-toast-time">${extra} · ${now}</div>` : `<div class="fp-toast-time">${now}</div>`}
        </div>
        <button class="fp-toast-close" onclick="removeToast(this.parentElement)">✕</button>
    `;

    container.prepend(toast);

    // Auto remove after 8 seconds
    setTimeout(() => removeToast(toast), 8000);
}

function removeToast(toast) {
    if (!toast || !toast.parentElement) return;
    toast.classList.add('removing');
    setTimeout(() => toast.remove(), 300);
}

// ===== NOTIFICATION SOUND =====
function playNotificationSound() {
    try {
        const audioCtx = new (window.AudioContext || window.webkitAudioContext)();
        const oscillator = audioCtx.createOscillator();
        const gainNode = audioCtx.createGain();

        oscillator.connect(gainNode);
        gainNode.connect(audioCtx.destination);

        oscillator.frequency.setValueAtTime(800, audioCtx.currentTime);
        oscillator.frequency.setValueAtTime(1000, audioCtx.currentTime + 0.1);
        oscillator.frequency.setValueAtTime(800, audioCtx.currentTime + 0.2);

        gainNode.gain.setValueAtTime(0.3, audioCtx.currentTime);
        gainNode.gain.exponentialRampToValueAtTime(0.01, audioCtx.currentTime + 0.4);

        oscillator.start(audioCtx.currentTime);
        oscillator.stop(audioCtx.currentTime + 0.4);
    } catch (e) { }
}

// ===== FORMAT HELPERS =====
function formatPrice(price) {
    return new Intl.NumberFormat('vi-VN').format(price) + 'đ';
}

// ===== INIT =====
document.addEventListener('DOMContentLoaded', () => {
    initSignalR();
    refreshAllTables();

    // Clean up modal state on close
    document.getElementById('tableDetailModal').addEventListener('hidden.bs.modal', () => {
        currentTableId = null;
    });
});
