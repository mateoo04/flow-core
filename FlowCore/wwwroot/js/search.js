(function () {
  function debounce(fn, ms) {
    var t;
    return function () {
      var ctx = this, args = arguments;
      clearTimeout(t);
      t = setTimeout(function () { fn.apply(ctx, args); }, ms);
    };
  }

  function init(root) {
    var searchUrl = root.getAttribute("data-search-url");
    var defaultTab = root.getAttribute("data-default-tab") || "projects";

    var trigger = root.querySelector("[data-search-trigger]");
    var modal = root.querySelector("[data-search-modal]");
    var backdrop = root.querySelector("[data-search-backdrop]");
    var closeBtn = root.querySelector("[data-search-close]");
    var input = root.querySelector("[data-search-input]");
    var results = root.querySelector("[data-search-results]");
    var tabs = root.querySelectorAll("[data-search-tab]");

    var activeTab = defaultTab;
    var highlight = -1;
    var isOpen = false;

    function setActiveTabUi() {
      for (var i = 0; i < tabs.length; i++) {
        var isActive = tabs[i].getAttribute("data-search-tab") === activeTab;
        tabs[i].setAttribute("data-active", isActive ? "true" : "false");
        tabs[i].setAttribute("aria-selected", isActive ? "true" : "false");
      }
    }

    function rows() { return results.querySelectorAll("[data-search-result]"); }

    function applyHighlight() {
      var all = rows();
      for (var i = 0; i < all.length; i++) {
        all[i].setAttribute("data-highlight", i === highlight ? "true" : "false");
      }
    }

    function openModal() {
      if (isOpen) return;
      isOpen = true;
      activeTab = defaultTab;
      setActiveTabUi();
      modal.classList.remove("hidden");
      modal.classList.add("flex");
      document.body.style.overflow = "hidden";
      input.value = "";
      results.innerHTML = "";
      highlight = -1;
      setTimeout(function () { input.focus(); }, 0);
    }

    function closeModal() {
      if (!isOpen) return;
      isOpen = false;
      modal.classList.add("hidden");
      modal.classList.remove("flex");
      document.body.style.overflow = "";
      highlight = -1;
    }

    function fetchResults() {
      var q = input.value.trim();
      if (q.length === 0) {
        results.innerHTML = "";
        highlight = -1;
        return;
      }
      var url = searchUrl + "?tab=" + encodeURIComponent(activeTab) + "&q=" + encodeURIComponent(q);

      results.setAttribute("data-loading", "true");
      fetch(url, { headers: { "Accept": "text/html" } })
        .then(function (r) { return r.ok ? r.text() : ""; })
        .then(function (html) {
          results.innerHTML = html;
          results.removeAttribute("data-loading");
          highlight = rows().length > 0 ? 0 : -1;
          applyHighlight();
        })
        .catch(function () {
          results.removeAttribute("data-loading");
          results.innerHTML = "";
        });
    }

    var debouncedFetch = debounce(fetchResults, 250);

    trigger.addEventListener("click", openModal);
    if (closeBtn) closeBtn.addEventListener("click", closeModal);
    if (backdrop) backdrop.addEventListener("click", closeModal);

    input.addEventListener("input", function () {
      if (input.value.trim().length === 0) {
        results.innerHTML = "";
        highlight = -1;
        return;
      }
      debouncedFetch();
    });

    input.addEventListener("keydown", function (e) {
      var all = rows();
      if (e.key === "ArrowDown" && all.length > 0) {
        e.preventDefault();
        highlight = (highlight + 1) % all.length;
        applyHighlight();
        scrollHighlightIntoView();
      } else if (e.key === "ArrowUp" && all.length > 0) {
        e.preventDefault();
        highlight = (highlight - 1 + all.length) % all.length;
        applyHighlight();
        scrollHighlightIntoView();
      } else if (e.key === "Enter" && highlight >= 0 && all[highlight]) {
        e.preventDefault();
        var href = all[highlight].getAttribute("href");
        if (href) window.location.href = href;
      } else if (e.key === "Escape") {
        e.preventDefault();
        closeModal();
      }
    });

    document.addEventListener("keydown", function (e) {
      if (isOpen && e.key === "Escape") closeModal();
    });

    function scrollHighlightIntoView() {
      var all = rows();
      if (highlight < 0 || !all[highlight]) return;
      all[highlight].scrollIntoView({ block: "nearest" });
    }

    for (var i = 0; i < tabs.length; i++) {
      tabs[i].addEventListener("click", function (e) {
        var tab = e.currentTarget.getAttribute("data-search-tab");
        if (!tab || tab === activeTab) return;
        activeTab = tab;
        setActiveTabUi();
        fetchResults();
        input.focus();
      });
    }

    results.addEventListener("mousemove", function (e) {
      var row = e.target.closest("[data-search-result]");
      if (!row) return;
      var all = rows();
      for (var i = 0; i < all.length; i++) {
        if (all[i] === row) { highlight = i; applyHighlight(); return; }
      }
    });
  }

  function boot() {
    var roots = document.querySelectorAll("[data-search-root]");
    for (var i = 0; i < roots.length; i++) init(roots[i]);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }
})();
