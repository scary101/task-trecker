window.steptreckChat = {
    scrollToBottom: (el) => {
        if (!el) return;
        el.scrollTop = el.scrollHeight;
    },
    scrollToMessage: (streamEl, messageId) => {
        if (!streamEl || !messageId) return;
        const target = streamEl.querySelector(`[data-message-id="${messageId}"]`);
        if (!target) return;
        target.scrollIntoView({ behavior: "smooth", block: "center" });
    },
    isNearBottom: (el, threshold = 120) => {
        if (!el) return true;
        const distance = el.scrollHeight - el.scrollTop - el.clientHeight;
        return distance < threshold;
    },
    autosize: (el, maxHeight = 180) => {
        if (!el) return;
        el.style.height = "auto";
        const height = Math.min(el.scrollHeight, maxHeight);
        el.style.height = `${height}px`;
    },
    clearComposer: (el) => {
        if (!el) return;
        el.value = "";
        el.focus();
        el.dispatchEvent(new Event("input", { bubbles: true }));
        window.steptreckChat.autosize(el);
    },
    focusComposer: (el) => {
        if (!el) return;
        el.focus();
    },
    setComposerValue: (el, value) => {
        if (!el) return;
        el.value = value ?? "";
        el.focus();
        const position = el.value.length;
        el.setSelectionRange(position, position);
        el.dispatchEvent(new Event("input", { bubbles: true }));
        window.steptreckChat.autosize(el);
    },
    copyText: async (text) => {
        if (!text) return false;

        try {
            if (navigator.clipboard?.writeText) {
                await navigator.clipboard.writeText(text);
                return true;
            }
        } catch {
        }

        try {
            const textarea = document.createElement("textarea");
            textarea.value = text;
            textarea.setAttribute("readonly", "");
            textarea.style.position = "fixed";
            textarea.style.opacity = "0";
            document.body.appendChild(textarea);
            textarea.select();
            const success = document.execCommand("copy");
            document.body.removeChild(textarea);
            return success;
        } catch {
            return false;
        }
    },
    insertAtCursor: (el, text) => {
        if (!el || !text) return;

        const value = el.value ?? "";
        const start = el.selectionStart ?? value.length;
        const end = el.selectionEnd ?? value.length;

        el.value = `${value.slice(0, start)}${text}${value.slice(end)}`;
        const nextPosition = start + text.length;
        el.setSelectionRange(nextPosition, nextPosition);
        el.focus();
        el.dispatchEvent(new Event("input", { bubbles: true }));
        window.steptreckChat.autosize(el);
    },
    bindComposer: (el, dotnetRef) => {
        if (!el) return;
        if (el.__steptreckComposerCleanup) {
            el.__steptreckComposerCleanup();
        }

        const onKeyDown = (e) => {
            if (e.key === "Enter" && !e.shiftKey && !e.isComposing) {
                e.preventDefault();
                if (dotnetRef) {
                    dotnetRef.invokeMethodAsync("SendFromComposer");
                }
            }
        };

        const onInput = () => {
            window.steptreckChat.autosize(el);
        };

        el.addEventListener("keydown", onKeyDown);
        el.addEventListener("input", onInput);

        el.__steptreckComposerCleanup = () => {
            el.removeEventListener("keydown", onKeyDown);
            el.removeEventListener("input", onInput);
        };

        window.steptreckChat.autosize(el);
    },
    bindEmojiPicker: (buttonEl, panelEl, composerEl) => {
        if (!buttonEl || !panelEl || !composerEl) return;
        if (buttonEl.__steptreckEmojiCleanup) {
            buttonEl.__steptreckEmojiCleanup();
        }

        let isOpen = !panelEl.hidden;

        const open = () => {
            panelEl.hidden = false;
            isOpen = true;
            buttonEl.setAttribute("aria-expanded", "true");
        };

        const close = () => {
            panelEl.hidden = true;
            isOpen = false;
            buttonEl.setAttribute("aria-expanded", "false");
        };

        const onButtonClick = (e) => {
            e.preventDefault();
            e.stopPropagation();
            if (isOpen) {
                close();
                return;
            }

            open();
        };

        const onPanelClick = (e) => {
            const target = e.target;
            if (!(target instanceof HTMLElement)) return;

            const emojiButton = target.closest("[data-emoji]");
            if (!(emojiButton instanceof HTMLElement)) return;

            const emoji = emojiButton.dataset.emoji;
            if (!emoji) return;

            window.steptreckChat.insertAtCursor(composerEl, emoji);
            composerEl.focus();
        };

        const onDocumentPointerDown = (e) => {
            if (!isOpen) return;
            if (panelEl.contains(e.target) || buttonEl.contains(e.target)) return;
            close();
        };

        const onDocumentKeyDown = (e) => {
            if (e.key === "Escape") {
                close();
                composerEl.focus();
            }
        };

        buttonEl.addEventListener("click", onButtonClick);
        panelEl.addEventListener("click", onPanelClick);
        document.addEventListener("pointerdown", onDocumentPointerDown);
        document.addEventListener("keydown", onDocumentKeyDown);

        buttonEl.__steptreckEmojiCleanup = () => {
            buttonEl.removeEventListener("click", onButtonClick);
            panelEl.removeEventListener("click", onPanelClick);
            document.removeEventListener("pointerdown", onDocumentPointerDown);
            document.removeEventListener("keydown", onDocumentKeyDown);
        };
    },
    bindMessageCopy: (streamEl) => {
        if (!streamEl) return;
        if (streamEl.__steptreckCopyCleanup) {
            streamEl.__steptreckCopyCleanup();
        }

        const onContextMenu = async (e) => {
            const target = e.target;
            if (!(target instanceof HTMLElement)) return;

            const bubble = target.closest(".message-bubble");
            if (!(bubble instanceof HTMLElement)) return;

            const text = bubble.dataset.copyText?.trim() || bubble.querySelector(".message-text")?.textContent?.trim();
            if (!text) return;

            e.preventDefault();
            await window.steptreckChat.copyText(text);
        };

        streamEl.addEventListener("contextmenu", onContextMenu);

        streamEl.__steptreckCopyCleanup = () => {
            streamEl.removeEventListener("contextmenu", onContextMenu);
        };
    }
};
