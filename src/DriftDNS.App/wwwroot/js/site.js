window.themeManager = {
    get: function () {
        return localStorage.getItem('driftdns-theme') || 'light';
    },
    set: function (theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);
        localStorage.setItem('driftdns-theme', theme);
    }
};

document.addEventListener('DOMContentLoaded', function () {
    function initTooltips() {
        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
            bootstrap.Tooltip.getOrCreateInstance(el);
        });
    }

    initTooltips();

    const observer = new MutationObserver(initTooltips);
    observer.observe(document.body, { childList: true, subtree: true });
});
