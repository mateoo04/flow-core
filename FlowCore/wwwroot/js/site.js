(function () {
  var storageKey = "flowcore-theme";

  function themeFromPreference() {
    return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
  }

  function getStoredTheme() {
    try {
      var stored = localStorage.getItem(storageKey);
      if (stored === "light" || stored === "dark") {
        return stored;
      }
    } catch (_) {}
    return null;
  }

  function resolveTheme() {
    return getStoredTheme() ?? themeFromPreference();
  }

  function isDarkTheme(theme) {
    return theme === "dark";
  }

  function applyTheme(theme, persist) {
    var dark = isDarkTheme(theme);
    document.documentElement.classList.toggle("dark", dark);
    if (persist) {
      try {
        localStorage.setItem(storageKey, theme);
      } catch (_) {}
    }

    var btn = document.getElementById("theme-toggle");
    if (btn) {
      btn.setAttribute("aria-label", dark ? "Switch to light mode" : "Switch to dark mode");
      btn.setAttribute("aria-pressed", dark ? "true" : "false");
    }
  }

  function toggleTheme() {
    var next = document.documentElement.classList.contains("dark") ? "light" : "dark";
    applyTheme(next, true);
  }

  window.addEventListener("DOMContentLoaded", function () {
    applyTheme(resolveTheme(), false);
    var btn = document.getElementById("theme-toggle");
    if (btn) {
      btn.addEventListener("click", toggleTheme);
    }
  });

  window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", function () {
    if (getStoredTheme() != null) {
      return;
    }
    applyTheme(themeFromPreference(), false);
  });
})();
