(function () {
  if (typeof flatpickr === "undefined") return;

  function pickLocale() {
    var lang = (document.documentElement.lang || "").toLowerCase();
    if (lang.indexOf("hr") === 0 && flatpickr.l10ns && flatpickr.l10ns.hr) return "hr";
    return "default";
  }

  function altFormat(locale, includeTime) {
    if (locale === "hr") return includeTime ? "d.m.Y H:i" : "d.m.Y";
    return includeTime ? "M j, Y H:i" : "M j, Y";
  }

  function init(el) {
    if (el._flatpickrBound) return;
    var locale = pickLocale();
    var includeTime = el.getAttribute("data-include-time") !== "false";
    flatpickr(el, {
      enableTime: includeTime,
      time_24hr: true,
      dateFormat: includeTime ? "Y-m-d H:i" : "Y-m-d",
      altInput: true,
      altFormat: altFormat(locale, includeTime),
      altInputClass: el.className,
      defaultHour: 9,
      defaultMinute: 0,
      minuteIncrement: 5,
      locale: locale,
      allowInput: true
    });
    el._flatpickrBound = true;
  }

  function boot() {
    var els = document.querySelectorAll("[data-datetime-picker]");
    for (var i = 0; i < els.length; i++) init(els[i]);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }
})();
