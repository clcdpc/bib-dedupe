(function () {
  const root = document.documentElement;
  const toggleButton = document.querySelector('[data-theme-toggle]');
  const storageKey = 'theme-preference';

  if (!toggleButton) {
    return;
  }

  const darkModeQuery = window.matchMedia('(prefers-color-scheme: dark)');

  const updateLabels = (theme) => {
    const isDark = theme === 'dark';
    const nextLabel = isDark ? 'Switch to light theme' : 'Switch to dark theme';
    toggleButton.setAttribute('aria-pressed', isDark ? 'true' : 'false');
    toggleButton.setAttribute('aria-label', nextLabel);
    toggleButton.setAttribute('title', nextLabel);
  };

  const applyTheme = (theme) => {
    root.classList.remove('theme-light', 'theme-dark');

    if (theme === 'dark') {
      root.classList.add('theme-dark');
    } else {
      root.classList.add('theme-light');
      theme = 'light';
    }

    updateLabels(theme);
  };

  const getStoredTheme = () => {
    try {
      const stored = localStorage.getItem(storageKey);
      return stored === 'dark' || stored === 'light' ? stored : null;
    } catch (error) {
      return null;
    }
  };

  const getPreferredTheme = () => {
    const storedTheme = getStoredTheme();
    if (storedTheme) {
      return storedTheme;
    }

    return darkModeQuery.matches ? 'dark' : 'light';
  };

  let currentTheme = getPreferredTheme();
  applyTheme(currentTheme);

  const handlePreferenceChange = (event) => {
    if (getStoredTheme() !== null) {
      return;
    }

    currentTheme = event.matches ? 'dark' : 'light';
    applyTheme(currentTheme);
  };

  if (typeof darkModeQuery.addEventListener === 'function') {
    darkModeQuery.addEventListener('change', handlePreferenceChange);
  } else if (typeof darkModeQuery.addListener === 'function') {
    darkModeQuery.addListener(handlePreferenceChange);
  }

  toggleButton.addEventListener('click', () => {
    currentTheme = currentTheme === 'dark' ? 'light' : 'dark';

    try {
      localStorage.setItem(storageKey, currentTheme);
    } catch (error) {
      /* localStorage might be unavailable; ignore and continue. */
    }

    applyTheme(currentTheme);
  });
})();
