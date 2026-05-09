(function () {
  if (typeof Sortable === "undefined") return;

  var columns = document.querySelectorAll("[data-home-dnd-column]");
  if (columns.length === 0) return;

  var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
  var antiforgery = tokenInput ? tokenInput.value : "";

  columns.forEach(function (col) {
    new Sortable(col, {
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
    });
  });
})();
