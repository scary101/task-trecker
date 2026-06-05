(function () {
    const controlKeys = new Set([
        "Backspace", "Delete", "Tab", "Enter", "Escape",
        "ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown",
        "Home", "End"
    ]);

    function isDigitsOnlyTarget(target) {
        return target instanceof HTMLInputElement && target.hasAttribute("data-digits-only");
    }

    document.addEventListener("keydown", function (event) {
        if (!isDigitsOnlyTarget(event.target)) {
            return;
        }

        if (event.ctrlKey || event.metaKey || event.altKey || controlKeys.has(event.key)) {
            return;
        }

        if (!/^\d$/.test(event.key)) {
            event.preventDefault();
        }
    });

    document.addEventListener("beforeinput", function (event) {
        if (!isDigitsOnlyTarget(event.target)) {
            return;
        }

        if (typeof event.data === "string" && /\D/.test(event.data)) {
            event.preventDefault();
        }
    });
})();
