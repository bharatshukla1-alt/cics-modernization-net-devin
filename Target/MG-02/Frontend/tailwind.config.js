/** Tailwind theme mapped to the BFSI design tokens in src/styles/bfsi-theme.css (config/Style-Sheet.md). */
export default {
  content: ['./index.html', './src/**/*.{js,jsx}', '../../MG-01/Frontend/src/**/*.{js,jsx}', '../../MG-03/Frontend/src/**/*.{js,jsx}'],
  corePlugins: { preflight: false },
  theme: {
    extend: {
      colors: {
        primary: {
          50: 'var(--color-primary-50)', 100: 'var(--color-primary-100)', 300: 'var(--color-primary-300)',
          600: 'var(--color-primary-600)', 800: 'var(--color-primary-800)', 900: 'var(--color-primary-900)',
        },
        danger: { 600: 'var(--color-danger-600)', 700: 'var(--color-danger-700)' },
        muted: 'var(--color-text-muted)',
        surface: 'var(--color-surface)',
        'surface-alt': 'var(--color-surface-alt)',
        border: 'var(--color-border)',
      },
      spacing: {
        'sp-1': 'var(--sp-1)', 'sp-2': 'var(--sp-2)', 'sp-3': 'var(--sp-3)', 'sp-4': 'var(--sp-4)',
        'sp-5': 'var(--sp-5)', 'sp-6': 'var(--sp-6)', 'sp-7': 'var(--sp-7)',
      },
      fontSize: { sm: 'var(--fs-sm)', base: 'var(--fs-base)', md: 'var(--fs-md)', lg: 'var(--fs-lg)', xl: 'var(--fs-xl)' },
      fontFamily: { mono: 'var(--font-mono)' },
      borderRadius: { md: 'var(--radius-md)', lg: 'var(--radius-lg)' },
      boxShadow: { sm: 'var(--shadow-sm)', lg: 'var(--shadow-lg)' },
      maxWidth: { container: 'var(--container-max)', form: '640px' },
    },
  },
};
