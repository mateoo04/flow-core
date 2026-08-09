(function () {
  var columns = document.querySelectorAll("[data-home-dnd-column]");
  if (columns.length === 0) return;

  var mobileQuery = window.matchMedia("(max-width: 1023px)");
  var sortables = [];

  var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
  var antiforgery = tokenInput ? tokenInput.value : "";

  function enableDragAndDrop() {
    if (typeof Sortable === "undefined" || mobileQuery.matches || sortables.length) return;

    columns.forEach(function (col) {
      sortables.push(new Sortable(col, {
      group: "home",
      animation: 150,
      delay: 150,
      delayOnTouchOnly: true,
      touchStartThreshold: 3,
      ghostClass: "fc-dnd-ghost",
      onEnd: function (evt) {
        if (evt.from === evt.to && evt.oldIndex === evt.newIndex) return;

        var taskId = evt.item.dataset.taskId;
        var statusName = evt.to.dataset.statusName;
        var statusColor = evt.to.dataset.statusColor;
        var position = evt.newIndex;

        if (evt.from !== evt.to && statusColor) {
          evt.item.style.borderLeftColor = statusColor;
        }

        fetch("/home/tasks/" + encodeURIComponent(taskId) + "/move", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            "RequestVerificationToken": antiforgery
          },
          body: JSON.stringify({ statusName: statusName, position: position })
        })
          .then(function (r) { if (!r.ok) location.reload(); })
          .catch(function () { location.reload(); });
      }
      }));
    });
  }

  function disableDragAndDrop() {
    sortables.forEach(function (sortable) { sortable.destroy(); });
    sortables = [];
  }

  function syncHomeForViewport() {
    document.querySelectorAll("[data-home-mobile-toggle]").forEach(function (toggle, index) {
      var content = document.getElementById(toggle.getAttribute("aria-controls"));
      if (!content) return;

      if (mobileQuery.matches) {
        var expanded = toggle.getAttribute("aria-expanded") === "true";
        if (index === 0) expanded = true;
        toggle.setAttribute("aria-expanded", expanded ? "true" : "false");
        content.classList.toggle("hidden", !expanded);
        toggle.querySelector("svg")?.classList.toggle("rotate-180", expanded);
      } else {
        content.classList.remove("hidden");
        toggle.querySelector("svg")?.classList.remove("rotate-180");
      }
    });

    if (mobileQuery.matches) disableDragAndDrop();
    else enableDragAndDrop();
  }

  document.addEventListener("click", function (e) {
    var toggle = e.target.closest("[data-home-mobile-toggle]");
    if (!toggle || !mobileQuery.matches) return;

    var content = document.getElementById(toggle.getAttribute("aria-controls"));
    if (!content) return;
    var expanded = toggle.getAttribute("aria-expanded") !== "true";
    toggle.setAttribute("aria-expanded", expanded ? "true" : "false");
    content.classList.toggle("hidden", !expanded);
    toggle.querySelector("svg")?.classList.toggle("rotate-180", expanded);
  });

  syncHomeForViewport();
  mobileQuery.addEventListener("change", syncHomeForViewport);
})();
