// Carousel scroll functionality
window.scrollCarousel = (element, direction) => {
    if (!element) return;

    // Scroll by approximately one card width (350px + gap)
    const scrollAmount = 360 * direction;
    element.scrollBy({
        left: scrollAmount,
        behavior: 'smooth'
    });
};

