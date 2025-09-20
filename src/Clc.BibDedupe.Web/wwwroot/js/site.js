// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', () => {
    if (typeof bootstrap === 'undefined' || !bootstrap.Tooltip) {
        return;
    }

    const tooltipTriggerList = Array.prototype.slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.forEach(triggerEl => {
        if (!triggerEl.getAttribute('title')) {
            return;
        }

        new bootstrap.Tooltip(triggerEl);
    });
});
