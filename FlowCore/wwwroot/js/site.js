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
  var mobileQuery = window.matchMedia("(max-width: 1023px)");

  function setSidebarPanel(aside, panel) {
    aside.querySelectorAll("[data-sidebar-view]").forEach(function (view) {
      var match = view.getAttribute("data-sidebar-view") === panel;
      view.classList.toggle("hidden", !match);
      view.classList.toggle("flex", match);
    });
  }

  function setMobileSidebar(open) {
    var aside = document.querySelector("[data-sidebar]");
    var toggle = document.querySelector("[data-mobile-sidebar-toggle]");
    var backdrop = document.querySelector("[data-mobile-sidebar-backdrop]");
    if (!aside || !toggle || !backdrop || !mobileQuery.matches) return;

    aside.classList.toggle("hidden", !open);
    aside.classList.toggle("flex", open);
    backdrop.classList.toggle("hidden", !open);
    aside.setAttribute("aria-hidden", open ? "false" : "true");
    toggle.setAttribute("aria-expanded", open ? "true" : "false");

    if (!open) {
      setSidebarPanel(aside, "main");
      toggle.focus();
    }
  }

  function syncSidebarForViewport() {
    var aside = document.querySelector("[data-sidebar]");
    var toggle = document.querySelector("[data-mobile-sidebar-toggle]");
    var backdrop = document.querySelector("[data-mobile-sidebar-backdrop]");
    if (!aside) return;

    if (mobileQuery.matches) {
      aside.classList.add("hidden");
      aside.classList.remove("flex");
      if (backdrop) backdrop.classList.add("hidden");
      aside.setAttribute("aria-hidden", "true");
      if (toggle) {
        toggle.setAttribute("aria-expanded", "false");
      }
    } else {
      aside.classList.remove("hidden", "flex");
      if (backdrop) backdrop.classList.add("hidden");
      aside.setAttribute("aria-hidden", "false");
    }
  }

  window.addEventListener("DOMContentLoaded", syncSidebarForViewport);
  mobileQuery.addEventListener("change", syncSidebarForViewport);

  document.addEventListener("click", function (e) {
    var mobileToggle = e.target.closest("[data-mobile-sidebar-toggle]");
    if (mobileToggle) {
      setMobileSidebar(mobileToggle.getAttribute("aria-expanded") !== "true");
      return;
    }

    if (e.target.closest("[data-mobile-sidebar-backdrop], [data-mobile-sidebar-close]")) {
      setMobileSidebar(false);
      return;
    }

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
      return;
    }

    var navigationLink = e.target.closest("[data-sidebar] a[href]");
    if (navigationLink && mobileQuery.matches) {
      setMobileSidebar(false);
    }
  });

  document.addEventListener("keydown", function (e) {
    if (e.key === "Escape") {
      var toggle = document.querySelector("[data-mobile-sidebar-toggle]");
      if (toggle && toggle.getAttribute("aria-expanded") === "true") setMobileSidebar(false);
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
