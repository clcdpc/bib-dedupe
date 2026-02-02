document.addEventListener('DOMContentLoaded', () => {
  const filterForm = document.getElementById('pairsFilters');
  if (!filterForm) {
    return;
  }

  const filterInputs = filterForm.querySelectorAll('select');
  filterInputs.forEach((select) => {
    select.addEventListener('change', () => {
      if (typeof filterForm.requestSubmit === 'function') {
        filterForm.requestSubmit();
      } else {
        filterForm.submit();
      }
    });
  });
});
