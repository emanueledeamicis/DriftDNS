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
