// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', () => {
    if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
        const tooltipTriggerList = Array.prototype.slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.forEach(triggerEl => {
            if (!triggerEl.getAttribute('title')) {
                return;
            }

            new bootstrap.Tooltip(triggerEl);
        });
    }

    initialiseMatchPointClipboard();
});

function initialiseMatchPointClipboard() {
    const badges = document.querySelectorAll('.match-point-badge[data-clipboard-text]');
    if (!badges.length) {
        return;
    }

    const toastContainer = document.getElementById('global-toast-container');

    const showToast = (message, isError) => {
        if (!toastContainer || typeof bootstrap === 'undefined' || !bootstrap.Toast) {
            return;
        }

        const toastElement = document.createElement('div');
        toastElement.className = `toast align-items-center text-bg-${isError ? 'danger' : 'primary'} border-0`;
        toastElement.setAttribute('role', 'status');
        toastElement.setAttribute('aria-live', 'polite');
        toastElement.setAttribute('aria-atomic', 'true');

        const toastContent = document.createElement('div');
        toastContent.className = 'd-flex';

        const body = document.createElement('div');
        body.className = 'toast-body';
        body.textContent = message;

        const closeButton = document.createElement('button');
        closeButton.type = 'button';
        closeButton.className = 'btn-close btn-close-white me-2 m-auto';
        closeButton.setAttribute('data-bs-dismiss', 'toast');
        closeButton.setAttribute('aria-label', 'Close');

        toastContent.appendChild(body);
        toastContent.appendChild(closeButton);
        toastElement.appendChild(toastContent);
        toastContainer.appendChild(toastElement);

        const toast = new bootstrap.Toast(toastElement, { delay: 2000 });
        toastElement.addEventListener('hidden.bs.toast', () => {
            toastElement.remove();
        });
        toast.show();
    };

    const copyWithFallback = text => new Promise((resolve, reject) => {
        const textarea = document.createElement('textarea');
        textarea.value = text;
        textarea.setAttribute('readonly', '');
        textarea.style.position = 'fixed';
        textarea.style.top = '-9999px';
        textarea.style.opacity = '0';
        document.body.appendChild(textarea);
        textarea.select();

        try {
            if (document.execCommand('copy')) {
                resolve();
            } else {
                reject(new Error('Copy command was unsuccessful.'));
            }
        } catch (error) {
            reject(error);
        } finally {
            document.body.removeChild(textarea);
        }
    });

    const handleBadgeActivation = async badge => {
        const text = badge.getAttribute('data-clipboard-text');
        if (!text) {
            return;
        }

        try {
            if (navigator.clipboard && navigator.clipboard.writeText) {
                await navigator.clipboard.writeText(text);
            } else {
                await copyWithFallback(text);
            }

            showToast('Match value copied to clipboard.', false);
        } catch (error) {
            showToast('Unable to copy match value.', true);
        }
    };

    badges.forEach(badge => {
        badge.addEventListener('click', event => {
            event.preventDefault();
            handleBadgeActivation(badge);
        });

        badge.addEventListener('keydown', event => {
            if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                handleBadgeActivation(badge);
            }
        });
    });
}
