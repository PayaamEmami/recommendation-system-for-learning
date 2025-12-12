// Theme management with localStorage persistence
export function saveTheme(isDarkMode) {
    try {
        localStorage.setItem('rsl-theme', isDarkMode ? 'dark' : 'light');
    } catch (e) {
        console.warn('Failed to save theme preference:', e);
    }
}

export function loadTheme() {
    try {
        const saved = localStorage.getItem('rsl-theme');
        // Return true for dark mode, false for light mode
        // Default to false (light mode) if nothing saved
        return saved === 'dark';
    } catch (e) {
        console.warn('Failed to load theme preference:', e);
        return false; // Default to light mode
    }
}

export function applyTheme(isDarkMode) {
    try {
        document.body.className = isDarkMode ? 'dark-mode' : 'light-mode';
    } catch (e) {
        console.warn('Failed to apply theme:', e);
    }
}

export function detectSystemPreference() {
    try {
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    } catch (e) {
        return false;
    }
}

