window.marketingIcons = window.marketingIcons || {
    refresh: function () {
        if (window.lucide && typeof window.lucide.createIcons === "function") {
            window.lucide.createIcons({
                attrs: {
                    "stroke-width": "2",
                    "aria-hidden": "true"
                }
            });
        }
    }
};
