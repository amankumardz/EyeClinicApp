(() => {
    const animatedItems = document.querySelectorAll('.fade-in');
    if (!('IntersectionObserver' in window) || animatedItems.length === 0) {
        return;
    }

    const observer = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
            if (entry.isIntersecting) {
                entry.target.classList.add('is-visible');
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.15 });

    animatedItems.forEach((item) => {
        observer.observe(item);
    });
})();
