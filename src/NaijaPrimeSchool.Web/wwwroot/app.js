// Naija Prime School — small JS interop helpers.
// Keep this file tiny: each helper exists so Blazor's
// IJSRuntime.InvokeVoidAsync (which expects a global function name
// rather than an arbitrary JS expression) can drive native browser
// behaviour with one identifier.

window.npsClickElement = function (id) {
    var el = document.getElementById(id);
    if (el) el.click();
};
