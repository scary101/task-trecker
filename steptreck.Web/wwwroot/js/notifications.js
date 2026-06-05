window.steptreckNotifySound = (() => {
    let audio;
    const ensure = () => {
        if (!audio) {
            audio = new Audio('/sound/not.mp3');
            audio.preload = 'auto';
            audio.volume = 0.6;
        }
        return audio;
    };

    const play = () => {
        try {
            const a = ensure();
            a.currentTime = 0;
            const p = a.play();
            if (p && typeof p.catch === 'function') {
                p.catch(() => {});
            }
        } catch {
        }
    };

    return { play };
})();
