/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      fontFamily: {
        sans: ['ui-sans-serif', '-apple-system', 'BlinkMacSystemFont', '"Segoe UI"', 'sans-serif'],
        mono: ['ui-monospace', 'SFMono-Regular', 'Consolas', 'monospace'],
      },
      colors: {
        ink: '#090c10',
        panel: '#12171e',
        line: '#29313c',
        lime: '#9fe870',
      },
      boxShadow: {
        glow: '0 0 32px rgba(159, 232, 112, 0.12)',
      },
    },
  },
  plugins: [],
};
