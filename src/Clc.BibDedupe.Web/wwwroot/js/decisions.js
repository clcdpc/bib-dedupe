document.addEventListener('DOMContentLoaded', () => {
  const submitButton = document.getElementById('submit-decisions');
  const summaryElements = {
    total: document.getElementById('summary-total'),
    keepLeft: document.getElementById('summary-keep-left'),
    keepRight: document.getElementById('summary-keep-right'),
    notDuplicate: document.getElementById('summary-not-duplicate'),
    skip: document.getElementById('summary-skip'),
  };
  const modalElements = {
    total: document.getElementById('modal-total'),
    keepLeft: document.getElementById('modal-keep-left'),
    keepRight: document.getElementById('modal-keep-right'),
    notDuplicate: document.getElementById('modal-not-duplicate'),
    skip: document.getElementById('modal-skip'),
  };
  const errorAlert = document.querySelector('[data-submit-error]');
  const tableBody = document.querySelector('#decision-table tbody');
  const modalElement = document.getElementById('submitModal');
  const confirmButton = document.getElementById('confirm-submit');
  const submitForm = document.getElementById('submit-form');

  const updateSummaryDisplay = (summary) => {
    if (!summaryElements.total) {
      return;
    }

    summaryElements.total.textContent = `Total: ${summary.total}`;
    summaryElements.keepLeft.textContent = `Keep Left: ${summary.keepLeft}`;
    summaryElements.keepRight.textContent = `Keep Right: ${summary.keepRight}`;
    summaryElements.notDuplicate.textContent = `Not Duplicate: ${summary.notDuplicate}`;
    summaryElements.skip.textContent = `Skip: ${summary.skip}`;
  };

  const updateModalSummary = (summary) => {
    modalElements.total.textContent = summary.total;
    modalElements.keepLeft.textContent = summary.keepLeft;
    modalElements.keepRight.textContent = summary.keepRight;
    modalElements.notDuplicate.textContent = summary.notDuplicate;
    modalElements.skip.textContent = summary.skip;
  };

  const calculateSummary = () => {
    const summary = {
      total: 0,
      keepLeft: 0,
      keepRight: 0,
      notDuplicate: 0,
      skip: 0,
    };

    if (!tableBody) {
      return summary;
    }

    tableBody.querySelectorAll('tr').forEach((row) => {
      const action = Number(row.getAttribute('data-action'));
      if (!Number.isFinite(action)) {
        return;
      }

      summary.total += 1;

      switch (action) {
        case 1:
          summary.keepLeft += 1;
          break;
        case 2:
          summary.notDuplicate += 1;
          break;
        case 3:
          summary.skip += 1;
          break;
        case 4:
          summary.keepRight += 1;
          break;
        default:
          break;
      }
    });

    return summary;
  };

  const updateSubmitButtonState = (summary) => {
    if (!submitButton) {
      return;
    }

    submitButton.disabled = summary.total === 0;
  };

  const showError = (message) => {
    if (!errorAlert) {
      return;
    }

    errorAlert.textContent = message;
    errorAlert.classList.remove('d-none');
  };

  const clearError = () => {
    if (!errorAlert) {
      return;
    }

    errorAlert.textContent = '';
    errorAlert.classList.add('d-none');
  };

  if (submitButton && modalElement) {
    const modal = new bootstrap.Modal(modalElement);

    submitButton.addEventListener('click', () => {
      const summary = calculateSummary();
      updateModalSummary(summary);
      clearError();
      modal.show();
    });

    confirmButton?.addEventListener('click', async () => {
      if (!submitForm) {
        return;
      }

      clearError();
      confirmButton.disabled = true;

      const token = submitForm.querySelector('input[name="__RequestVerificationToken"]').value;

      try {
        const response = await fetch(submitForm.action, {
          method: 'POST',
          headers: {
            RequestVerificationToken: token,
            'X-Requested-With': 'XMLHttpRequest',
          },
        });

        if (!response.ok) {
          const data = await response.json().catch(() => ({}));
          const message = data.error || 'Unable to submit decisions at this time.';
          showError(message);
          confirmButton.disabled = false;
          return;
        }

        modal.hide();
        window.location.reload();
      } catch (err) {
        console.error(err);
        showError('An unexpected error occurred.');
        confirmButton.disabled = false;
      }
    });
  }

  document.querySelectorAll('.remove-form').forEach((form) => {
    form.addEventListener('submit', async (event) => {
      event.preventDefault();
      const token = form.querySelector('input[name="__RequestVerificationToken"]').value;
      const left = form.querySelector('input[name="leftBibId"]').value;
      const right = form.querySelector('input[name="rightBibId"]').value;

      try {
        const response = await fetch(form.action, {
          method: 'POST',
          headers: {
            RequestVerificationToken: token,
            'X-Requested-With': 'XMLHttpRequest',
            'Content-Type': 'application/x-www-form-urlencoded',
          },
          body: new URLSearchParams({ leftBibId: left, rightBibId: right }),
        });

        if (!response.ok) {
          const data = await response.json().catch(() => ({}));
          const message = data.error || 'Unable to remove this decision while processing is in progress.';
          showError(message);
          return;
        }

        const row = form.closest('tr');
        row?.remove();
        const badge = document.querySelector('.menu .badge');
        if (badge) {
          badge.textContent = Math.max(parseInt(badge.textContent || '0', 10) - 1, 0).toString();
        }

        const summary = calculateSummary();
        updateSummaryDisplay(summary);
        updateSubmitButtonState(summary);
      } catch (err) {
        console.error(err);
        showError('Unable to remove this decision.');
      }
    });
  });
});
