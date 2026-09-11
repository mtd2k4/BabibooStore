/**
 * BabyShop Studio Kids - Enterprise Admin Script
 * Handles Responsive Sidebar Drawer, Mini-sidebar, Quick Search & Realtime Badges
 */

document.addEventListener('DOMContentLoaded', function () {
    const wrapper = document.querySelector('.admin-wrapper');
    const sidebar = document.getElementById('adminSidebar');
    const backdrop = document.getElementById('adminBackdrop');
    const mobileToggleBtn = document.getElementById('mobileMenuBtn');
    const desktopToggleBtn = document.getElementById('desktopCollapseBtn');
    const commandModalEl = document.getElementById('quickCommandModal');

    // 1. Khôi phục trạng thái thu gọn Sidebar trên Desktop từ localStorage
    if (window.innerWidth >= 992) {
        const isCollapsed = localStorage.getItem('admin_sidebar_collapsed') === 'true';
        if (isCollapsed && wrapper) {
            wrapper.classList.add('sidebar-collapsed');
            updateDesktopToggleIcon(true);
        }
    }

    // 2. Xử lý Toggle Sidebar trên Mobile & Tablet (Drawer)
    if (mobileToggleBtn && sidebar && backdrop) {
        mobileToggleBtn.addEventListener('click', function () {
            sidebar.classList.toggle('mobile-open');
            backdrop.classList.toggle('active');
            document.body.classList.toggle('overflow-hidden');
        });

        backdrop.addEventListener('click', closeMobileSidebar);

        // Đóng khi bấm vào bất kỳ nav-link nào trên mobile
        const navLinks = sidebar.querySelectorAll('.nav-link');
        navLinks.forEach(link => {
            link.addEventListener('click', function () {
                if (window.innerWidth < 992) {
                    closeMobileSidebar();
                }
            });
        });
    }

    function closeMobileSidebar() {
        if (sidebar && backdrop) {
            sidebar.classList.remove('mobile-open');
            backdrop.classList.remove('active');
            document.body.classList.remove('overflow-hidden');
        }
    }

    // 3. Xử lý Toggle Mini-Sidebar trên Desktop (>= 992px)
    if (desktopToggleBtn && wrapper) {
        desktopToggleBtn.addEventListener('click', function () {
            const willCollapse = !wrapper.classList.contains('sidebar-collapsed');
            wrapper.classList.toggle('sidebar-collapsed', willCollapse);
            localStorage.setItem('admin_sidebar_collapsed', willCollapse);
            updateDesktopToggleIcon(willCollapse);
        });
    }

    function updateDesktopToggleIcon(collapsed) {
        if (desktopToggleBtn) {
            const icon = desktopToggleBtn.querySelector('i');
            if (icon) {
                if (collapsed) {
                    icon.className = 'bi bi-layout-sidebar-inset';
                    desktopToggleBtn.title = 'Mở rộng menu';
                } else {
                    icon.className = 'bi bi-layout-sidebar-inset-reverse';
                    desktopToggleBtn.title = 'Thu gọn menu';
                }
            }
        }
    }

    // 4. Phím tắt ESC để đóng Drawer / Modal
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            closeMobileSidebar();
        }
        // Phím tắt Ctrl + K hoặc Cmd + K để mở Quick Command Modal
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
            e.preventDefault();
            openQuickCommand();
        }
    });

    // 5. Quick Command Modal Trigger
    const searchBtns = document.querySelectorAll('.trigger-quick-search');
    searchBtns.forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            openQuickCommand();
        });
    });

    function openQuickCommand() {
        if (commandModalEl && typeof bootstrap !== 'undefined') {
            const modal = bootstrap.Modal.getOrCreateInstance(commandModalEl);
            modal.show();
            setTimeout(() => {
                const searchInput = document.getElementById('quickCommandInput');
                if (searchInput) {
                    searchInput.value = '';
                    searchInput.focus();
                    filterCommands('');
                }
            }, 200);
        }
    }

    // 6. Tìm kiếm thông minh trong Quick Command Modal
    const commandInput = document.getElementById('quickCommandInput');
    if (commandInput) {
        commandInput.addEventListener('input', function () {
            filterCommands(this.value.trim().toLowerCase());
        });
    }

    function filterCommands(term) {
        const items = document.querySelectorAll('.command-item');
        let visibleCount = 0;
        items.forEach(item => {
            const text = (item.getAttribute('data-search') || item.innerText).toLowerCase();
            if (!term || text.includes(term)) {
                item.style.display = 'flex';
                visibleCount++;
            } else {
                item.style.display = 'none';
            }
        });

        const noResultEl = document.getElementById('commandNoResults');
        if (noResultEl) {
            noResultEl.style.display = visibleCount === 0 ? 'block' : 'none';
        }
    }

    // 7. Polling thời gian thực số lượng Tin nhắn Chat chưa đọc & Đổi trả chờ duyệt
    async function updateAdminRealtimeBadges() {
        try {
            // Live Chat Unread
            const chatResp = await fetch('/Admin/Chat/GetUnreadCount');
            if (chatResp.ok) {
                const chatData = await chatResp.json();
                const count = chatData.unreadCount || 0;
                
                // Sidebar badge
                const sidebarBadge = document.getElementById('adminSidebarChatBadge');
                if (sidebarBadge) {
                    sidebarBadge.innerText = count;
                    sidebarBadge.style.display = count > 0 ? 'inline-block' : 'none';
                }
                // Navbar action icon badge
                const navBadge = document.getElementById('adminNavChatBadge');
                if (navBadge) {
                    navBadge.style.display = count > 0 ? 'block' : 'none';
                }
            }
        } catch (e) {}
    }

    // Chạy kiểm tra ngay khi tải và định kỳ mỗi 5s
    updateAdminRealtimeBadges();
    setInterval(updateAdminRealtimeBadges, 5000);
});
