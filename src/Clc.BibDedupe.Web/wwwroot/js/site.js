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

    const matchBadges = document.querySelectorAll('.match-point-badge[data-match-values]');
    if (!matchBadges.length) {
        return;
    }

    const toastElement = document.getElementById('match-copy-toast');
    let toastInstance = null;

    if (toastElement && typeof bootstrap !== 'undefined' && bootstrap.Toast) {
        toastInstance = bootstrap.Toast.getOrCreateInstance(toastElement, { delay: 2000 });
    }

    const showToast = (label) => {
        if (!toastElement) {
            return;
        }

        const toastBody = toastElement.querySelector('.toast-body');
        if (toastBody) {
            const text = label ? `${label} copied to clipboard.` : 'Match value copied to clipboard.';
            toastBody.textContent = text;
        }

        toastElement.classList.remove('d-none');

        if (toastInstance) {
            toastInstance.show();
            return;
        }

        toastElement.classList.add('show');
        setTimeout(() => toastElement.classList.remove('show'), 2000);
    };

    const copyText = async (text) => {
        if (!text) {
            return false;
        }

        if (navigator.clipboard && typeof navigator.clipboard.writeText === 'function') {
            try {
                await navigator.clipboard.writeText(text);
                return true;
            } catch (error) {
                // Fallback to manual copy below
            }
        }

        const textArea = document.createElement('textarea');
        textArea.value = text;
        textArea.setAttribute('readonly', '');
        textArea.style.position = 'absolute';
        textArea.style.left = '-9999px';
        document.body.appendChild(textArea);

        let selection = null;
        if (window.getSelection) {
            selection = window.getSelection();
        }

        const selectedRange = selection && selection.rangeCount > 0 ? selection.getRangeAt(0) : null;

        textArea.select();

        let succeeded = false;
        try {
            succeeded = document.execCommand('copy');
        } catch (error) {
            succeeded = false;
        }

        document.body.removeChild(textArea);

        if (selectedRange && selection) {
            selection.removeAllRanges();
            selection.addRange(selectedRange);
        }

        return succeeded;
    };

    const handleBadgeAction = async (event) => {
        const badge = event.currentTarget;
        const values = badge?.dataset?.matchValues;

        if (!values) {
            return;
        }

        const label = badge.dataset.matchLabel || badge.textContent?.trim();

        const copied = await copyText(values);
        if (copied) {
            showToast(label);
        }
    };

    const handleKeydown = (event) => {
        if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault();
            handleBadgeAction(event);
        }
    };

    matchBadges.forEach((badge) => {
        badge.addEventListener('click', handleBadgeAction);
        badge.addEventListener('keydown', handleKeydown);
    });
});
