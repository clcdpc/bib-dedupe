// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

const ACTION_TOAST_STORAGE_KEY = 'bibDedupe:lastAction';
const ACTION_TOAST_MAX_AGE = 5 * 60 * 1000;

function normalizeBibId(value) {
    if (value === null || value === undefined) {
        return '';
    }

    if (typeof value === 'number' && !Number.isNaN(value)) {
        return String(value);
    }

    if (typeof value === 'string') {
        const trimmed = value.trim();
        return trimmed;
    }

    return '';
}

function formatBibLabel(value) {
    const normalized = normalizeBibId(value);
    if (!normalized) {
        return '';
    }

    return normalized.toLowerCase().startsWith('bib ')
        ? normalized
        : `Bib ${normalized}`;
}

async function copyToClipboard(text) {
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
}

function trimmedTitle(title) {
    return (title || '').trim();
}

function formatRecordTitle(title) {
    const trimmed = trimmedTitle(title);
    if (!trimmed) {
        return '';
    }

    return trimmed.length > 90 ? `${trimmed.slice(0, 87)}…` : trimmed;
}

function getKeptPosition(action) {
    if (!action) {
        return null;
    }

    const normalized = String(action).trim().toLowerCase();
    if (!normalized) {
        return null;
    }

    if (normalized === 'keepleft') {
        return 'left';
    }

    if (normalized === 'keepright') {
        return 'right';
    }

    return null;
}

function createToastRecordRow(position, title, bibId, isKept) {
    const bibLabel = formatBibLabel(bibId);
    const formattedTitle = formatRecordTitle(title);
    const hasTitle = Boolean(formattedTitle);
    const displayTitle = hasTitle ? formattedTitle : 'Title unavailable';

    const row = document.createElement('div');
    row.className = 'action-toast__record';

    const label = document.createElement('span');
    label.className = 'action-toast__record-label';
    if (isKept) {
        label.classList.add('action-toast__record-label--kept');
    }
    label.textContent = `${position} record`;
    row.appendChild(label);

    const titleEl = document.createElement('span');
    titleEl.className = 'action-toast__record-title';
    if (!hasTitle) {
        titleEl.classList.add('action-toast__record-title--muted');
    }
    titleEl.textContent = displayTitle;
    const rawTitle = trimmedTitle(title);
    if (hasTitle && formattedTitle !== rawTitle) {
        titleEl.title = rawTitle;
    } else if (!hasTitle && bibLabel) {
        titleEl.title = bibLabel;
    } else if (!hasTitle) {
        titleEl.title = 'Title unavailable';
    }
    row.appendChild(titleEl);

    if (bibLabel) {
        const copyValue = normalizeBibId(bibId) || bibLabel;
        const copyLabel = bibLabel;

        const bibButton = document.createElement('button');
        bibButton.type = 'button';
        bibButton.className = 'action-toast__record-bib';
        bibButton.setAttribute('aria-label', `Copy ${copyLabel} to clipboard`);
        bibButton.title = `Copy ${copyLabel} to clipboard`;

        const icon = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        icon.setAttribute('class', 'action-toast__record-bib-icon');
        icon.setAttribute('viewBox', '0 0 16 16');
        icon.setAttribute('aria-hidden', 'true');
        icon.setAttribute('focusable', 'false');

        const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
        path.setAttribute('d', 'M10 1.5H9.5a1.5 1.5 0 0 0-3 0H6A1.5 1.5 0 0 0 4.5 3v1A1.5 1.5 0 0 0 3 5.5v7A1.5 1.5 0 0 0 4.5 14h7a1.5 1.5 0 0 0 1.5-1.5v-7A1.5 1.5 0 0 0 11.5 4V3A1.5 1.5 0 0 0 10 1.5Zm-2-.5a.5.5 0 0 1 .5.5V3h-2V1.5a.5.5 0 0 1 .5-.5ZM11.5 5a.5.5 0 0 1 .5.5v7a.5.5 0 0 1-.5.5h-7a.5.5 0 0 1-.5-.5v-7a.5.5 0 0 1 .5-.5h7Z');
        icon.appendChild(path);

        const textSpan = document.createElement('span');
        textSpan.className = 'action-toast__record-bib-text';
        textSpan.textContent = bibLabel;

        bibButton.appendChild(icon);
        bibButton.appendChild(textSpan);

        bibButton.addEventListener('click', async (event) => {
            event.preventDefault();
            event.stopPropagation();

            const successful = await copyToClipboard(copyValue);
            if (successful) {
                bibButton.classList.add('action-toast__record-bib--copied');
                setTimeout(() => bibButton.classList.remove('action-toast__record-bib--copied'), 1200);

                if (typeof window.showCopyFeedback === 'function') {
                    window.showCopyFeedback(copyLabel);
                }
            }
        });

        row.appendChild(bibButton);
    }

    return row;
}

function buildActionToastMessage(data, summaryText) {
    if (!data) {
        return null;
    }

    const message = document.createElement('div');
    message.className = 'action-toast__message';

    const keptPosition = getKeptPosition(data.action);

    const fallbackActionLabel = (typeof summaryText === 'string' && summaryText.includes(':'))
        ? summaryText.split(':')[0].trim()
        : '';
    const actionLabel = (data.actionLabel && data.actionLabel.trim()) || fallbackActionLabel;

    if (actionLabel) {
        const actionEl = document.createElement('div');
        actionEl.className = 'action-toast__action';
        actionEl.textContent = actionLabel;
        message.appendChild(actionEl);
    }

    const recordsWrapper = document.createElement('div');
    recordsWrapper.className = 'action-toast__records';

    const leftRow = createToastRecordRow('Left', data.leftTitle, data.leftBibId, keptPosition === 'left');
    if (leftRow) {
        recordsWrapper.appendChild(leftRow);
    }

    const rightRow = createToastRecordRow('Right', data.rightTitle, data.rightBibId, keptPosition === 'right');
    if (rightRow) {
        recordsWrapper.appendChild(rightRow);
    }

    if (recordsWrapper.childElementCount > 0) {
        message.appendChild(recordsWrapper);
    }

    if (!message.childElementCount) {
        return null;
    }

    if (summaryText) {
        message.setAttribute('data-summary', summaryText);
    }

    return message;
}

window.buildActionToastMessage = buildActionToastMessage;

function loadActionToast(expectedTarget) {
    try {
        const raw = sessionStorage.getItem(ACTION_TOAST_STORAGE_KEY);
        if (!raw) {
            return null;
        }

        const data = JSON.parse(raw);
        if (!data || !data.action) {
            sessionStorage.removeItem(ACTION_TOAST_STORAGE_KEY);
            return null;
        }

        if (typeof data.timestamp === 'number' && Date.now() - data.timestamp > ACTION_TOAST_MAX_AGE) {
            sessionStorage.removeItem(ACTION_TOAST_STORAGE_KEY);
            return null;
        }

        const target = data.target || 'review';
        if (expectedTarget && target !== expectedTarget) {
            return null;
        }

        sessionStorage.removeItem(ACTION_TOAST_STORAGE_KEY);
        return data;
    } catch (error) {
        console.warn('Unable to load last action toast payload.', error);
        sessionStorage.removeItem(ACTION_TOAST_STORAGE_KEY);
        return null;
    }
}

function showActionToast(data) {
    const container = document.querySelector('.action-toast-container');
    if (!container) {
        return;
    }

    const summary = data.summary;
    if (!summary) {
        return;
    }

    const toast = document.createElement('div');
    toast.className = 'action-toast';
    toast.setAttribute('role', 'status');
    toast.setAttribute('aria-label', summary);

    const message = buildActionToastMessage(data, summary);
    if (message) {
        toast.appendChild(message);
    } else {
        const fallback = document.createElement('div');
        fallback.className = 'action-toast__message';
        fallback.textContent = summary;
        toast.appendChild(fallback);
    }

    const actions = document.createElement('div');
    actions.className = 'action-toast__actions';

    const reviewButton = document.createElement('button');
    reviewButton.type = 'button';
    reviewButton.className = 'action-toast__button';
    reviewButton.textContent = 'Review again';
    if (data.reviewUrl) {
        reviewButton.addEventListener('click', () => {
            clearTimeout(hideTimer);
            window.location.href = data.reviewUrl;
        });
    } else {
        reviewButton.disabled = true;
    }
    actions.appendChild(reviewButton);

    const dismissButton = document.createElement('button');
    dismissButton.type = 'button';
    dismissButton.className = 'action-toast__dismiss';
    dismissButton.setAttribute('aria-label', 'Dismiss notification');
    dismissButton.textContent = '×';
    actions.appendChild(dismissButton);

    toast.appendChild(actions);
    container.appendChild(toast);

    const hideToast = () => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 200);
    };

    const hideDelay = 6000;
    let hideTimer = null;

    const startHideTimer = () => {
        clearTimeout(hideTimer);
        hideTimer = setTimeout(hideToast, hideDelay);
    };

    dismissButton.addEventListener('click', () => {
        clearTimeout(hideTimer);
        hideToast();
    });

    toast.addEventListener('mouseenter', () => {
        clearTimeout(hideTimer);
        hideTimer = null;
    });

    toast.addEventListener('mouseleave', () => {
        startHideTimer();
    });

    startHideTimer();

    requestAnimationFrame(() => toast.classList.add('show'));
}

document.addEventListener('DOMContentLoaded', () => {
    const storedAction = loadActionToast('list');
    if (storedAction) {
        showActionToast(storedAction);
    }

    if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
        const tooltipTriggerList = Array.prototype.slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.forEach(triggerEl => {
            if (!triggerEl.getAttribute('title')) {
                return;
            }

            new bootstrap.Tooltip(triggerEl);
        });
    }

    const toastElement = document.getElementById('match-copy-toast');
    let toastInstance = null;

    if (toastElement && typeof bootstrap !== 'undefined' && bootstrap.Toast) {
        toastInstance = bootstrap.Toast.getOrCreateInstance(toastElement, { delay: 2000 });
    }

    const showCopyFeedback = (label) => {
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

    window.showCopyFeedback = showCopyFeedback;

    const matchBadges = document.querySelectorAll('.match-point-badge[data-match-values]');
    if (!matchBadges.length) {
        return;
    }

    const handleBadgeAction = async (event) => {
        const badge = event.currentTarget;
        const values = badge?.dataset?.matchValues;

        if (!values) {
            return;
        }

        const label = badge.dataset.matchLabel || badge.textContent?.trim();

        const copied = await copyToClipboard(values);
        if (copied) {
            showCopyFeedback(label);
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
