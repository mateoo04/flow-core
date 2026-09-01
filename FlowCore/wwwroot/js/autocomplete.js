(function () {
  var debounce = window.FlowCore.debounce;

  function selectedIds(root) {
    var inputs = root.querySelectorAll('[data-ac-chips] input[type="hidden"]');
    var ids = [];
    for (var i = 0; i < inputs.length; i++) ids.push(inputs[i].value);
    return ids;
  }

  function init(root) {
    var searchUrl = root.getAttribute("data-search-url");
    var fieldName = root.getAttribute("data-field-name");
    var input = root.querySelector("[data-ac-input]");
    var chipsContainer = root.querySelector("[data-ac-chips]");
    var results = root.querySelector("[data-ac-results]");
    var control = root.querySelector("[data-ac-control]");

    var highlight = -1;
    var searchController = null;
    var searchVersion = 0;

    function showResults() { results.classList.remove("hidden"); }
    function hideResults() { results.classList.add("hidden"); highlight = -1; }

    function rows() { return results.querySelectorAll("[data-ac-result]"); }

    function applyHighlight() {
      var all = rows();
      for (var i = 0; i < all.length; i++) {
        all[i].classList.toggle("bg-[var(--color-surface-container)]", i === highlight);
      }
    }

    function pick(row) {
      var tpl = row.querySelector("[data-ac-chip-template]");
      if (!tpl) return;
      var chip = tpl.content.firstElementChild.cloneNode(true);
      chipsContainer.appendChild(chip);
      input.value = "";
      results.innerHTML = "";
      hideResults();
      input.focus();
    }

    var doSearch = debounce(function () {
      var q = input.value.trim();
      searchVersion++;
      var version = searchVersion;

      if (searchController) searchController.abort();

      if (q.length === 0) { results.innerHTML = ""; hideResults(); return; }

      searchController = new AbortController();
      var url = searchUrl
        + "?q=" + encodeURIComponent(q)
        + "&fieldName=" + encodeURIComponent(fieldName);
      var taken = selectedIds(root);
      for (var i = 0; i < taken.length; i++) url += "&exclude=" + encodeURIComponent(taken[i]);

      fetch(url, {
        headers: { "Accept": "text/html" },
        signal: searchController.signal
      })
        .then(function (r) { return r.ok ? r.text() : ""; })
        .then(function (html) {
          if (version !== searchVersion) return;
          results.innerHTML = html;
          highlight = rows().length > 0 ? 0 : -1;
          applyHighlight();
          showResults();
        })
        .catch(function (error) {
          if (error.name === "AbortError" || version !== searchVersion) return;
          hideResults();
        });
    }, 250);

    input.addEventListener("input", doSearch);

    input.addEventListener("focus", function () {
      if (rows().length > 0) showResults();
    });

    input.addEventListener("keydown", function (e) {
      var all = rows();
      if (e.key === "ArrowDown" && all.length > 0) {
        e.preventDefault();
        highlight = (highlight + 1) % all.length;
        applyHighlight();
        showResults();
      } else if (e.key === "ArrowUp" && all.length > 0) {
        e.preventDefault();
        highlight = (highlight - 1 + all.length) % all.length;
        applyHighlight();
      } else if (e.key === "Enter" && highlight >= 0 && all[highlight]) {
        e.preventDefault();
        pick(all[highlight]);
      } else if (e.key === "Escape") {
        hideResults();
      } else if (e.key === "Backspace" && input.value === "") {
        var chips = chipsContainer.querySelectorAll("[data-ac-chip]");
        if (chips.length > 0) chips[chips.length - 1].remove();
      }
    });

    results.addEventListener("mousedown", function (e) {
      var row = e.target.closest("[data-ac-result]");
      if (!row) return;
      e.preventDefault();
      pick(row);
    });

    chipsContainer.addEventListener("click", function (e) {
      var btn = e.target.closest("[data-ac-remove]");
      if (!btn) return;
      var chip = btn.closest("[data-ac-chip]");
      if (chip) chip.remove();
    });

    document.addEventListener("click", function (e) {
      if (!root.contains(e.target)) hideResults();
    });

    control.addEventListener("click", function (e) {
      if (e.target === control) input.focus();
    });
  }

  function boot() {
    var roots = document.querySelectorAll("[data-autocomplete-multi]");
    for (var i = 0; i < roots.length; i++) init(roots[i]);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }
})();
