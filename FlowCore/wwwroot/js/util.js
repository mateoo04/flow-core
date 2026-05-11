window.FlowCore = window.FlowCore || {};

window.FlowCore.debounce = function (fn, ms) {
  var t;
  return function () {
    var ctx = this, args = arguments;
    clearTimeout(t);
    t = setTimeout(function () { fn.apply(ctx, args); }, ms);
  };
};
