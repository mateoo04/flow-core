(function () {
  function initAssistant() {
    var root = document.querySelector("[data-ai-assistant]");
    if (!root) return;

    var modal = root.querySelector("[data-ai-modal]");
    var prompt = root.querySelector("[data-ai-prompt]");
    var project = root.querySelector("[data-ai-project]");
    var form = root.querySelector("[data-ai-form]");
    if (!form || !project) return;

    var status = root.querySelector("[data-ai-status]");
    var preview = root.querySelector("[data-ai-preview]");
    var opener = root.querySelector("[data-ai-open]");
    var submit = form.querySelector('button[type="submit"]');
    var create = root.querySelector("[data-ai-create]");
    var draft = null;
    var previousFocus = null;
    var token = root.querySelector('input[name="__RequestVerificationToken"]').value;

    function setStatus(message, error) {
      status.textContent = message;
      status.classList.remove("hidden", "text-fg-secondary", "text-[var(--color-error)]");
      status.classList.add(error ? "text-[var(--color-error)]" : "text-fg-secondary");
    }

    function clearStatus() { status.classList.add("hidden"); }

    function close() {
      modal.classList.add("hidden");
      modal.classList.remove("flex");
      preview.classList.add("hidden");
      form.classList.remove("hidden");
      clearStatus();
      draft = null;
      if (previousFocus) previousFocus.focus();
    }

    function open() {
      previousFocus = document.activeElement;
      modal.classList.remove("hidden");
      modal.classList.add("flex");
      window.setTimeout(function () { prompt.focus(); }, 0);
    }

    function setBusy(button, busy, label) {
      button.disabled = busy;
      button.classList.toggle("opacity-60", busy);
      if (label) button.textContent = label;
    }

    function showPreview(nextDraft) {
      draft = nextDraft;
      root.querySelector("[data-ai-preview-title]").textContent = draft.title;
      root.querySelector("[data-ai-preview-description]").textContent = draft.description || "—";
      root.querySelector("[data-ai-preview-description-row]").classList.toggle("hidden", !draft.description);
      root.querySelector("[data-ai-preview-priority]").textContent = draft.priority;
      root.querySelector("[data-ai-preview-date]").textContent = draft.dueDate || "No due date";
      root.querySelector("[data-ai-preview-date-row]").classList.toggle("hidden", !draft.dueDate);
      form.classList.add("hidden");
      preview.classList.remove("hidden");
    }

    async function post(url, values) {
      var response = await fetch(url, {
        method: "POST",
        headers: { "RequestVerificationToken": token, "Accept": "application/json" },
        body: new URLSearchParams(values)
      });
      var payload = await response.json().catch(function () { return {}; });
      if (!response.ok) throw new Error(payload.message || "Something went wrong. Please try again.");
      return payload;
    }

    opener.addEventListener("click", open);
    root.querySelectorAll("[data-ai-close], [data-ai-backdrop]").forEach(function (element) {
      element.addEventListener("click", close);
    });
    root.querySelector("[data-ai-back-to-prompt]").addEventListener("click", function () {
      preview.classList.add("hidden");
      form.classList.remove("hidden");
      prompt.focus();
    });

    form.addEventListener("submit", async function (event) {
      event.preventDefault();
      if (!prompt.value.trim()) return;
      clearStatus();
      setBusy(submit, true, "Generating…");
      try {
        var response = await post(root.dataset.extractUrl, { projectId: project.value, prompt: prompt.value.trim() });
        showPreview(response);
      } catch (error) {
        setStatus(error.message, true);
      } finally {
        setBusy(submit, false, "Generate task");
      }
    });

    create.addEventListener("click", async function () {
      if (!draft) return;
      setBusy(create, true, "Creating…");
      try {
        var response = await post(root.dataset.createUrl, {
          projectId: project.value, title: draft.title, description: draft.description || "",
          priority: draft.priority, dueDate: draft.dueDate || ""
        });
        window.location.assign(response.redirectUrl);
      } catch (error) {
        preview.classList.add("hidden");
        form.classList.remove("hidden");
        setStatus(error.message, true);
        setBusy(create, false, "Create task");
      }
    });

    document.addEventListener("keydown", function (event) {
      if (event.key === "Escape" && !modal.classList.contains("hidden")) close();
    });
  }

  window.addEventListener("DOMContentLoaded", initAssistant);
})();
