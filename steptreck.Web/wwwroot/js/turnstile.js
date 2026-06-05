window.steptreckTurnstile = (() => {
    const scriptId = "cloudflare-turnstile-api";
    let loadPromise;

    function loadApi() {
        if (window.turnstile) {
            return Promise.resolve();
        }

        if (loadPromise) {
            return loadPromise;
        }

        loadPromise = new Promise((resolve, reject) => {
            const existing = document.getElementById(scriptId);
            if (existing) {
                existing.addEventListener("load", () => resolve(), { once: true });
                existing.addEventListener("error", () => reject(new Error("Turnstile failed to load")), { once: true });
                return;
            }

            const script = document.createElement("script");
            script.id = scriptId;
            script.src = "https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit";
            script.async = true;
            script.defer = true;
            script.onload = () => resolve();
            script.onerror = () => reject(new Error("Turnstile failed to load"));
            document.head.appendChild(script);
        });

        return loadPromise;
    }

    return {
        render: async (element, siteKey, theme, dotNetRef) => {
            await loadApi();

            if (element.__steptreckTurnstileWidgetId) {
                try {
                    window.turnstile.remove(element.__steptreckTurnstileWidgetId);
                } catch {
                    // The previous widget may already be gone.
                }

                element.__steptreckTurnstileWidgetId = null;
            }

            element.replaceChildren();

            const widgetId = window.turnstile.render(element, {
                sitekey: siteKey,
                theme: theme || "auto",
                size: "normal",
                appearance: "always",
                callback: token => dotNetRef.invokeMethodAsync("OnTurnstileSuccess", token),
                "expired-callback": () => dotNetRef.invokeMethodAsync("OnTurnstileExpired"),
                "error-callback": () => dotNetRef.invokeMethodAsync("OnTurnstileError")
            });

            element.__steptreckTurnstileWidgetId = widgetId;
            return widgetId;
        },
        reset: widgetId => {
            if (!window.turnstile || !widgetId) {
                return false;
            }

            try {
                window.turnstile.reset(widgetId);
                return true;
            } catch {
                return false;
            }
        },
        rerender: async (element, siteKey, theme, dotNetRef) => {
            return await window.steptreckTurnstile.render(element, siteKey, theme, dotNetRef);
        },
        remove: widgetId => {
            if (!window.turnstile || !widgetId) {
                return;
            }

            try {
                window.turnstile.remove(widgetId);
            } catch {
                // The widget may already be gone after Blazor rerendered the container.
            }
        }
    };
})();
