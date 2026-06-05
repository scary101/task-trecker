window.steptreckLanding = {
    init: () => {
        // Scroll reveal
        const els = Array.from(document.querySelectorAll('.reveal'));
        const io = new IntersectionObserver((entries) => {
            for (const e of entries) {
                if (e.isIntersecting) {
                    e.target.classList.add('is-visible');
                    io.unobserve(e.target);
                }
            }
        }, { threshold: 0.12 });

        window.steptreckLanding._revealObserver = io;
        els.forEach(el => io.observe(el));

        // Simple tilt (parallax) for elements with [data-tilt]
        const tiltEls = Array.from(document.querySelectorAll('[data-tilt]'));
        tiltEls.forEach(el => {
            let rect;

            const onMove = (ev) => {
                rect = rect || el.getBoundingClientRect();
                const x = (ev.clientX - rect.left) / rect.width - 0.5;
                const y = (ev.clientY - rect.top) / rect.height - 0.5;

                const rx = (-y * 7).toFixed(2);
                const ry = (x * 10).toFixed(2);

                el.style.transform = `perspective(900px) rotateX(${rx}deg) rotateY(${ry}deg) translateY(-2px)`;
            };

            const onLeave = () => {
                rect = null;
                el.style.transform = 'perspective(900px) rotateX(0deg) rotateY(0deg)';
            };

            el.addEventListener('mousemove', onMove);
            el.addEventListener('mouseleave', onLeave);
        });
    },
    refresh: () => {
        const io = window.steptreckLanding._revealObserver;
        if (!io) {
            window.steptreckLanding.init();
            return;
        }

        const els = Array.from(document.querySelectorAll('.reveal:not(.is-visible)'));
        els.forEach(el => io.observe(el));
    }
};
