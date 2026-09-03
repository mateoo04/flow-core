(function () {
  function initAssistant() {
    var root = document.querySelector("[data-ai-assistant]");
    if (!root) return;
    var modal = root.querySelector("[data-ai-modal]");
    var form = root.querySelector("[data-ai-form]");
    if (!form) return;
    var mode = root.querySelector("[data-ai-mode]");
    var prompt = root.querySelector("[data-ai-prompt]");
    var project = root.querySelector("[data-ai-project]");
    var workspace = root.querySelector("[data-ai-workspace]");
    var taskContext = root.querySelector("[data-ai-task-context]");
    var projectContext = root.querySelector("[data-ai-project-context]");
    var status = root.querySelector("[data-ai-status]");
    var preview = root.querySelector("[data-ai-preview]");
    var opener = root.querySelector("[data-ai-open]");
    var submit = root.querySelector("[data-ai-generate]");
    var create = root.querySelector("[data-ai-create]");
    var token = root.querySelector('input[name="__RequestVerificationToken"]').value;
    var draft = null;
    var previousFocus = null;
    function isProjectMode() { return mode.value === "project"; }
    function setStatus(message, error) { status.textContent = message; status.classList.remove("hidden", "text-fg-secondary", "text-[var(--color-error)]"); status.classList.add(error ? "text-[var(--color-error)]" : "text-fg-secondary"); }
    function clearStatus() { status.classList.add("hidden"); }
    function setBusy(button, busy, label) { button.disabled = busy; button.classList.toggle("opacity-60", busy); button.textContent = label; }
    function updateMode() {
      var projectMode = isProjectMode();
      taskContext.classList.toggle("hidden", projectMode); projectContext.classList.toggle("hidden", !projectMode);
      project.disabled = projectMode; workspace.disabled = !projectMode;
      root.querySelector("[data-ai-prompt-label]").textContent = projectMode ? "What project should we create?" : "What task should we create?";
      prompt.placeholder = projectMode ? "Create a high-priority mobile app project starting next week" : "Create a high-priority presentation task for Friday";
      root.querySelector("[data-ai-example]").textContent = projectMode ? "For example: “Create a high-priority mobile app project starting next week.”" : "For example: “Prepare the sprint review slides by Friday, high priority.”";
      submit.textContent = projectMode ? "Generate project" : "Generate task"; clearStatus();
    }
    function close() { modal.classList.add("hidden"); modal.classList.remove("flex"); opener.setAttribute("aria-expanded", "false"); preview.classList.add("hidden"); form.classList.remove("hidden"); clearStatus(); draft = null; if (previousFocus) previousFocus.focus(); }
    function open() { previousFocus = document.activeElement; modal.classList.remove("hidden"); modal.classList.add("flex"); opener.setAttribute("aria-expanded", "true"); window.setTimeout(function () { prompt.focus(); }, 0); }
    function showPreview(nextDraft) {
      draft = nextDraft; var projectMode = isProjectMode();
      root.querySelector("[data-ai-preview-kind]").textContent = projectMode ? "project" : "task";
      root.querySelector("[data-ai-preview-name-label]").textContent = projectMode ? "Name: " : "Title: ";
      root.querySelector("[data-ai-preview-name]").textContent = projectMode ? draft.name : draft.title;
      root.querySelector("[data-ai-preview-description]").textContent = draft.description || "";
      root.querySelector("[data-ai-preview-description-row]").classList.toggle("hidden", !draft.description);
      root.querySelector("[data-ai-preview-priority]").textContent = draft.priority;
      root.querySelector("[data-ai-preview-status]").textContent = draft.status || "";
      root.querySelector("[data-ai-preview-status-row]").classList.toggle("hidden", !projectMode);
      root.querySelector("[data-ai-preview-start-date]").textContent = draft.startDate || "";
      root.querySelector("[data-ai-preview-start-date-row]").classList.toggle("hidden", !projectMode || !draft.startDate);
      root.querySelector("[data-ai-preview-due-date]").textContent = draft.dueDate || "";
      root.querySelector("[data-ai-preview-due-date-row]").classList.toggle("hidden", !draft.dueDate);
      create.textContent = projectMode ? "Create project" : "Create task"; form.classList.add("hidden"); preview.classList.remove("hidden");
    }
    async function post(url, values) { var response = await fetch(url, { method: "POST", headers: { "RequestVerificationToken": token, "Accept": "application/json" }, body: new URLSearchParams(values) }); var payload = await response.json().catch(function () { return {}; }); if (!response.ok) throw new Error(payload.message || "Something went wrong. Please try again."); return payload; }
    mode.addEventListener("change", updateMode); opener.addEventListener("click", open);
    root.querySelectorAll("[data-ai-close], [data-ai-backdrop]").forEach(function (element) { element.addEventListener("click", close); });
    root.querySelector("[data-ai-back-to-prompt]").addEventListener("click", function () { preview.classList.add("hidden"); form.classList.remove("hidden"); prompt.focus(); });
    form.addEventListener("submit", async function (event) {
      event.preventDefault(); if (!prompt.value.trim()) return; var projectMode = isProjectMode(); clearStatus(); setBusy(submit, true, projectMode ? "Generating project..." : "Generating task...");
      try { var response = projectMode ? await post(root.dataset.projectExtractUrl, { workspaceId: workspace.value, prompt: prompt.value.trim() }) : await post(root.dataset.taskExtractUrl, { projectId: project.value, prompt: prompt.value.trim() }); showPreview(response); } catch (error) { setStatus(error.message, true); } finally { setBusy(submit, false, projectMode ? "Generate project" : "Generate task"); }
    });
    create.addEventListener("click", async function () {
      if (!draft) return; var projectMode = isProjectMode(); setBusy(create, true, projectMode ? "Creating project..." : "Creating task...");
      try { var response = projectMode ? await post(root.dataset.projectCreateUrl, { workspaceId: workspace.value, name: draft.name, description: draft.description || "", status: draft.status, priority: draft.priority, startDate: draft.startDate || "", dueDate: draft.dueDate || "" }) : await post(root.dataset.taskCreateUrl, { projectId: project.value, title: draft.title, description: draft.description || "", priority: draft.priority, dueDate: draft.dueDate || "" }); window.location.assign(response.redirectUrl); } catch (error) { preview.classList.add("hidden"); form.classList.remove("hidden"); setStatus(error.message, true); setBusy(create, false, projectMode ? "Create project" : "Create task"); }
    });
    document.addEventListener("keydown", function (event) { if (event.key === "Escape" && !modal.classList.contains("hidden")) close(); }); updateMode();
  }
  window.addEventListener("DOMContentLoaded", initAssistant);
})();
