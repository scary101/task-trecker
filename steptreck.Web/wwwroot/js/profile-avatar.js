(function () {
    function setDragState(zone, active) {
        zone.classList.toggle("drag-over", active);
    }

    function init(dropzoneId, inputId) {
        var zone = document.getElementById(dropzoneId);
        var input = document.getElementById(inputId);

        if (!zone || !input || zone.dataset.avatarDropBound === "true") {
            return;
        }

        zone.dataset.avatarDropBound = "true";

        ["dragenter", "dragover"].forEach(function (eventName) {
            zone.addEventListener(eventName, function (event) {
                event.preventDefault();
                setDragState(zone, true);
            });
        });

        ["dragleave", "drop"].forEach(function (eventName) {
            zone.addEventListener(eventName, function (event) {
                event.preventDefault();
                setDragState(zone, false);
            });
        });

        zone.addEventListener("drop", function (event) {
            var files = event.dataTransfer && event.dataTransfer.files;
            if (!files || files.length === 0) {
                return;
            }

            var transfer = new DataTransfer();
            transfer.items.add(files[0]);
            input.files = transfer.files;
            input.dispatchEvent(new Event("change", { bubbles: true }));
        });
    }

    window.steptreckProfileAvatar = {
        init: init
    };
})();
