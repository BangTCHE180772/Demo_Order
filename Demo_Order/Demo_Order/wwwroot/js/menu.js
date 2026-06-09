// ===== MENU PAGE JS =====

const cart = {}; // { menuItemId: { name, price, quantity } }

// ===== QUANTITY CONTROL =====
function changeQty(itemId, delta) {
    const qtyEl = document.getElementById(`qty-${itemId}`);
    const minusBtn = document.querySelector(`#qty-group-${itemId} .minus`);
    const card = document.getElementById(`menu-item-${itemId}`);

    let current = parseInt(qtyEl.textContent) || 0;
    current += delta;
    if (current < 0) current = 0;

    qtyEl.textContent = current;

    if (current > 0) {
        qtyEl.style.display = 'inline';
        minusBtn.style.display = 'flex';
        card.classList.add('in-cart');

        // Update cart
        const name = card.querySelector('.menu-item-name').textContent;
        const priceText = card.querySelector('.menu-item-price').textContent;
        const price = parseInt(priceText.replace(/[^\d]/g, ''));
        cart[itemId] = { name, price, quantity: current };
    } else {
        qtyEl.style.display = 'none';
        minusBtn.style.display = 'none';
        card.classList.remove('in-cart');
        delete cart[itemId];
    }

    updateFloatingCart();
}

// ===== FLOATING CART =====
function updateFloatingCart() {
    const cartEl = document.getElementById('floating-cart');
    const items = Object.values(cart);
    const totalItems = items.reduce((sum, item) => sum + item.quantity, 0);
    const totalPrice = items.reduce((sum, item) => sum + (item.price * item.quantity), 0);

    if (totalItems > 0) {
        cartEl.style.display = 'block';
        document.getElementById('cart-count').textContent = totalItems;
        document.getElementById('cart-total').textContent = formatPrice(totalPrice);
    } else {
        cartEl.style.display = 'none';
    }
}

// ===== CART DETAIL =====
function showCartDetail() {
    const overlay = document.getElementById('cart-overlay');
    const body = document.getElementById('cart-detail-items');

    let html = '';
    Object.entries(cart).forEach(([itemId, item]) => {
        html += `
            <div class="cart-detail-item">
                <span class="cart-detail-name">${item.name}</span>
                <div class="cart-detail-qty">
                    <button onclick="changeCartQty(${itemId}, -1)">−</button>
                    <span>${item.quantity}</span>
                    <button onclick="changeCartQty(${itemId}, 1)">+</button>
                </div>
                <span class="cart-detail-price">${formatPrice(item.price * item.quantity)}</span>
            </div>`;
    });

    body.innerHTML = html;

    const totalPrice = Object.values(cart).reduce((sum, item) => sum + (item.price * item.quantity), 0);
    document.getElementById('cart-detail-total').textContent = formatPrice(totalPrice);

    overlay.style.display = 'flex';
}

function hideCartDetail() {
    document.getElementById('cart-overlay').style.display = 'none';
}

function changeCartQty(itemId, delta) {
    changeQty(itemId, delta);
    // Re-render cart detail if still open
    if (Object.keys(cart).length > 0) {
        showCartDetail();
    } else {
        hideCartDetail();
    }
}

// ===== PLACE ORDER =====
async function placeOrder() {
    const btn = document.getElementById('btn-place-order');
    const btnText = btn.querySelector('.btn-text');
    const btnLoading = btn.querySelector('.btn-loading');

    btn.disabled = true;
    btnText.style.display = 'none';
    btnLoading.style.display = 'inline';

    const tableId = parseInt(document.getElementById('tableId').value);
    const items = Object.entries(cart).map(([menuItemId, item]) => ({
        menuItemId: parseInt(menuItemId),
        quantity: item.quantity
    }));

    try {
        const response = await fetch('/Order/PlaceOrder', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ tableId, items })
        });

        const data = await response.json();

        if (data.success) {
            // Clear cart
            Object.keys(cart).forEach(key => delete cart[key]);
            updateFloatingCart();
            hideCartDetail();

            // Reset all quantity displays
            document.querySelectorAll('.menu-qty-value').forEach(el => {
                el.textContent = '0';
                el.style.display = 'none';
            });
            document.querySelectorAll('.menu-qty-btn.minus').forEach(el => {
                el.style.display = 'none';
            });
            document.querySelectorAll('.menu-item-card').forEach(el => {
                el.classList.remove('in-cart');
            });

            // Show success
            document.getElementById('success-overlay').style.display = 'flex';
        } else {
            alert(data.message || 'Có lỗi xảy ra!');
        }
    } catch (err) {
        alert('Có lỗi kết nối, vui lòng thử lại!');
        console.error("Place order error:", err);
    } finally {
        btn.disabled = false;
        btnText.style.display = 'inline';
        btnLoading.style.display = 'none';
    }
}

// ===== SUCCESS OVERLAY =====
function closeSuccess() {
    document.getElementById('success-overlay').style.display = 'none';
    // Reload page to show updated existing order
    window.location.reload();
}

// ===== CATEGORY SCROLL =====
function scrollToCategory(category, btn) {
    const sectionId = `cat-${category.replace(/ /g, '-')}`;
    const section = document.getElementById(sectionId);
    if (section) {
        section.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    // Update active tab
    document.querySelectorAll('.menu-cat-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
}

// ===== EXISTING ORDER TOGGLE =====
function toggleExistingOrder() {
    const items = document.getElementById('existing-order-items');
    items.style.display = items.style.display === 'none' ? 'block' : 'none';
}

// ===== FORMAT HELPERS =====
function formatPrice(price) {
    return new Intl.NumberFormat('vi-VN').format(price) + 'đ';
}
