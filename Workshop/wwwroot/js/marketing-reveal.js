window.marketingReveals = window.marketingReveals || (function () {
    let observer;

    function ensureObserver() {
        if (observer) {
            return observer;
        }

        observer = new IntersectionObserver((entries) => {
            for (const entry of entries) {
                if (entry.isIntersecting) {
                    entry.target.classList.add("mk-inview");
                    observer.unobserve(entry.target);
                }
            }
        }, {
            root: null,
            rootMargin: "0px 0px -12% 0px",
            threshold: 0.15
        });

        return observer;
    }

    function refresh() {
        const io = ensureObserver();
        document.querySelectorAll(".mk-reveal").forEach((el) => {
            if (el.classList.contains("mk-inview")) {
                return;
            }
            io.observe(el);
        });
    }

    return { refresh };
})();
