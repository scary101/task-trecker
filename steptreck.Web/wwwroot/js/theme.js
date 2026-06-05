window.steptreckTheme = {
    apply: (theme) => {
        const root = document.documentElement;
        if (theme === "dark") {
            root.setAttribute("data-theme", "dark");
        } else {
            root.removeAttribute("data-theme");
        }
    },
    init: () => {
        try {
            const saved = localStorage.getItem("steptreck-theme");
            if (saved === "dark") {
                window.steptreckTheme.apply("dark");
                return "dark";
            }
        } catch {}
        window.steptreckTheme.apply("light");
        return "light";
    },
    toggle: () => {
        const root = document.documentElement;
        const isDark = root.getAttribute("data-theme") === "dark";
        const next = isDark ? "light" : "dark";
        window.steptreckTheme.apply(next);
        try {
            localStorage.setItem("steptreck-theme", next);
        } catch {}
        return next;
    }
};
