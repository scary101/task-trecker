(function () {
    var stopwatchCookieName = "steptreck_stopwatch";

    function bindRipple(button) {
        if (button.dataset.sidebarRippleBound === "true") {
            return;
        }

        button.dataset.sidebarRippleBound = "true";
        button.addEventListener("pointerdown", function (event) {
            var rect = button.getBoundingClientRect();
            var ripple = document.createElement("span");
            var size = Math.max(rect.width, rect.height);

            ripple.className = "sidebar-ripple";
            ripple.style.width = size + "px";
            ripple.style.height = size + "px";
            ripple.style.left = (event.clientX - rect.left - size / 2) + "px";
            ripple.style.top = (event.clientY - rect.top - size / 2) + "px";

            button.appendChild(ripple);
            window.setTimeout(function () {
                ripple.remove();
            }, 520);
        });
    }

    function bindGlow(element) {
        if (element.dataset.sidebarGlowBound === "true") {
            return;
        }

        element.dataset.sidebarGlowBound = "true";
        element.addEventListener("pointermove", function (event) {
            var rect = element.getBoundingClientRect();
            element.style.setProperty("--hover-x", (event.clientX - rect.left) + "px");
            element.style.setProperty("--hover-y", (event.clientY - rect.top) + "px");
        });

        element.addEventListener("pointerleave", function () {
            element.style.removeProperty("--hover-x");
            element.style.removeProperty("--hover-y");
        });
    }

    function init() {
        document.querySelectorAll(".work-sidebar .nav-link, .work-sidebar .sidebar-action, .work-sidebar .theme-switch, .work-sidebar .notif-btn, .work-sidebar .profile-link, .work-sidebar .sidebar-toggle, .floating-stopwatch, .floating-stopwatch .stopwatch-btn, .floating-stopwatch .stopwatch-collapse")
            .forEach(function (element) {
                bindRipple(element);
                bindGlow(element);
            });
    }

    function saveStopwatchState(state) {
        var json = JSON.stringify(state);
        var expires = new Date();
        expires.setDate(expires.getDate() + 30);

        document.cookie = stopwatchCookieName + "=" + encodeURIComponent(json)
            + "; expires=" + expires.toUTCString()
            + "; path=/; SameSite=Lax";
    }

    function loadStopwatchState() {
        var prefix = stopwatchCookieName + "=";
        var cookies = document.cookie ? document.cookie.split("; ") : [];

        for (var index = 0; index < cookies.length; index++) {
            if (cookies[index].indexOf(prefix) !== 0) {
                continue;
            }

            try {
                return JSON.parse(decodeURIComponent(cookies[index].substring(prefix.length)));
            }
            catch {
                return null;
            }
        }

        return null;
    }

    window.steptreckWorkplaceSidebar = {
        init: init,
        saveStopwatchState: saveStopwatchState,
        loadStopwatchState: loadStopwatchState
    };
})();
