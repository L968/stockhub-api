/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        mono: ['"IBM Plex Mono"', 'ui-monospace', 'monospace'],
      },
      colors: {
        ink: '#080b10',
        panel: '#10151d',
        line: '#252d39',
        lime: '#b6f36b',
      },
      boxShadow: {
        glow: '0 0 32px rgba(182, 243, 107, 0.12)',
      },
    },
  },
  plugins: [],
};
