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

(function () {
  function setSidebarPanel(aside, panel) {
    aside.querySelectorAll("[data-sidebar-view]").forEach(function (view) {
      var match = view.getAttribute("data-sidebar-view") === panel;
      view.classList.toggle("hidden", !match);
      view.classList.toggle("flex", match);
    });
  }

  document.addEventListener("click", function (e) {
    var openBtn = e.target.closest("[data-sidebar-open]");
    if (openBtn) {
      e.preventDefault();
      var aside = openBtn.closest("[data-sidebar]");
      if (aside) setSidebarPanel(aside, openBtn.getAttribute("data-sidebar-open"));
      return;
    }
    var closeBtn = e.target.closest("[data-sidebar-close]");
    if (closeBtn) {
      e.preventDefault();
      var aside2 = closeBtn.closest("[data-sidebar]");
      if (aside2) setSidebarPanel(aside2, "main");
    }
  });
})();

(function () {
  function scrollToFirstError() {
    var target = document.querySelector(".input-validation-error, .field-validation-error:not(:empty)");
    if (!target) return;
    target.scrollIntoView({ behavior: "smooth", block: "center" });
    if (target.matches("input, textarea, select")) {
      target.focus({ preventScroll: true });
    }
  }

  window.addEventListener("DOMContentLoaded", function () {
    scrollToFirstError();
    document.querySelectorAll("form").forEach(function (form) {
      form.addEventListener("submit", function () {
        setTimeout(scrollToFirstError, 0);
      });
    });
  });
})();
