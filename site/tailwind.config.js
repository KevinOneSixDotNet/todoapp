/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{vue,js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        background: '#F5F5F0',
        card: '#FFFFFF',
        charcoal: '#2C2A29',
        taupe: '#A39F90',
        'taupe-light': '#D6D3CA',
        primary: '#000000',
        'primary-hover': '#1a1a1a',
        error: '#B94A48',
        'error-light': '#F8E8E8',
      },
      fontFamily: {
        sans: ['Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        serif: ['Georgia', 'ui-serif', 'serif'],
      },
      boxShadow: {
        card: '0 1px 3px 0 rgba(44,42,41,0.08), 0 1px 2px -1px rgba(44,42,41,0.06)',
        'card-hover': '0 4px 12px 0 rgba(44,42,41,0.12)',
      },
    },
  },
  plugins: [],
}

